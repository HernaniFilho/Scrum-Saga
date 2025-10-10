using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ProductOwnerManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public Button becomeProductOwnerButton;
    public Button stopBeingProductOwnerButton;
    public TMP_Text buttonText;
    public TMP_Text waitingText;

    [Header("Popup Confirmation")]
    public GameObject popupBecomePOContainer;
    public Button confirmarBecomePOButton;
    public Button cancelarBecomePOButton;

    public GameObject popupStopPOContainer;
    public Button confirmarStopPOButton;
    public Button cancelarStopPOButton;

    [Header("Configuration")]
    public string productOwnerPropertyKey = "IsProductOwner";
    public bool requireMinimumPlayers = true;
    public int minimumPlayersToStart = 2;
    
    private NetworkManager networkManager;
    private GameStateManager gameStateManager;
    
    void Start()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        gameStateManager = GameStateManager.Instance;
        
        if (becomeProductOwnerButton != null)
        {
            becomeProductOwnerButton.onClick.AddListener(OnBecomeProductOwnerClicked);
        }
        
        if (stopBeingProductOwnerButton != null)
        {
            stopBeingProductOwnerButton.onClick.AddListener(OnStopBeingProductOwnerClicked);
        }

        SetupPopups();
        
        UpdateUI();
    }

    void SetupPopups()
    {
        if (popupBecomePOContainer != null)
        {
            popupBecomePOContainer.SetActive(false);
        }

        if (confirmarBecomePOButton != null)
        {
            confirmarBecomePOButton.onClick.AddListener(OnConfirmarBecomePOClicked);
        }

        if (cancelarBecomePOButton != null)
        {
            cancelarBecomePOButton.onClick.AddListener(OnCancelarBecomePOClicked);
        }

        if (popupStopPOContainer != null)
        {
            popupStopPOContainer.SetActive(false);
        }

        if (confirmarStopPOButton != null)
        {
            confirmarStopPOButton.onClick.AddListener(OnConfirmarStopPOClicked);
        }

        if (cancelarStopPOButton != null)
        {
            cancelarStopPOButton.onClick.AddListener(OnCancelarStopPOClicked);
        }
    }

    void Update()
    {
        UpdateUI();
    }

    public void OnBecomeProductOwnerClicked()
    {
        if (!PhotonNetwork.InRoom) return;

        if (GetCurrentProductOwner() != null)
        {
            Debug.Log("Já existe um Product Owner na sala");
            return;
        }

        if (popupBecomePOContainer != null)
        {
            popupBecomePOContainer.SetActive(true);
        }
    }

    private void OnConfirmarBecomePOClicked()
    {
        if (popupBecomePOContainer != null)
        {
            popupBecomePOContainer.SetActive(false);
        }

        BecomeProductOwner();
    }

    private void OnCancelarBecomePOClicked()
    {
        if (popupBecomePOContainer != null)
        {
            popupBecomePOContainer.SetActive(false);
        }
    }

    public void OnStopBeingProductOwnerClicked()
    {
        if (!PhotonNetwork.InRoom) return;

        if (popupStopPOContainer != null)
        {
            popupStopPOContainer.SetActive(true);
        }
    }

    private void OnConfirmarStopPOClicked()
    {
        if (popupStopPOContainer != null)
        {
            popupStopPOContainer.SetActive(false);
        }

        ClearProductOwner();
        Debug.Log("Você deixou de ser Product Owner");
    }

    private void OnCancelarStopPOClicked()
    {
        if (popupStopPOContainer != null)
        {
            popupStopPOContainer.SetActive(false);
        }
    }

    public void BecomeProductOwner()
    {
        if (!PhotonNetwork.InRoom) return;

        // Definir propriedade customizada para o jogador atual
        Hashtable props = new Hashtable();
        props[productOwnerPropertyKey] = true;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"Jogador {PhotonNetwork.LocalPlayer.NickName} se tornou Product Owner");
    }

    public Player GetCurrentProductOwner()
    {
        if (!PhotonNetwork.InRoom) return null;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue(productOwnerPropertyKey, out object isProductOwner))
            {
                if ((bool)isProductOwner)
                {
                    return player;
                }
            }
        }
        return null;
    }

    public bool IsPlayerProductOwner(Player player)
    {
        if (player == null) return false;
        
        if (player.CustomProperties.TryGetValue(productOwnerPropertyKey, out object isProductOwner))
        {
            return (bool)isProductOwner;
        }
        return false;
    }

    public bool IsLocalPlayerProductOwner()
    {
        return IsPlayerProductOwner(PhotonNetwork.LocalPlayer);
    }

    public void UpdateUI()
    {
        if (becomeProductOwnerButton == null) return;

        Player currentPO = GetCurrentProductOwner();
        bool hasProductOwner = currentPO != null;
        bool isLocalPlayerPO = IsLocalPlayerProductOwner();

        becomeProductOwnerButton.transform.parent.gameObject.SetActive(!hasProductOwner && PhotonNetwork.InRoom);

        if (gameStateManager == null) return;

        var currentState = gameStateManager.GetCurrentState();
        
        // Gerenciar botão de parar de ser PO (apenas durante fase Inicio e se for PO local)
        if (stopBeingProductOwnerButton != null)
        {
            bool shouldShowStopButton = isLocalPlayerPO && 
                                       currentState == GameStateManager.GameState.Inicio && 
                                       PhotonNetwork.InRoom;
            stopBeingProductOwnerButton.transform.parent.gameObject.SetActive(shouldShowStopButton);
        }

        if (currentState == GameStateManager.GameState.Inicio)
        {
            if (waitingText != null)
            {
                waitingText.gameObject.SetActive(true);
                int playerCount = PhotonNetwork.PlayerList.Length;
                bool hasMinimumPlayers = !requireMinimumPlayers || playerCount >= minimumPlayersToStart;

                if (hasProductOwner)
                {
                    if (!isLocalPlayerPO)
                    {
                        waitingText.text = "Aguardando PO começar a Sprint...";
                    }
                    else
                    {
                        if (!hasMinimumPlayers)
                        {
                            waitingText.text = $"Aguardando mais jogadores... ({playerCount}/{minimumPlayersToStart})";
                        }
                        else
                        {
                            waitingText.gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    waitingText.text = "Para começar, alguém deve ser o PO!";
                }
            }
        }

        becomeProductOwnerButton.interactable = !hasProductOwner && PhotonNetwork.InRoom;
        
        if (networkManager != null)
        {
            networkManager.UpdateRoomInfo();
        }
    }

    #region Photon Callbacks

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Verificar se a propriedade Product Owner foi alterada
        if (changedProps.ContainsKey(productOwnerPropertyKey))
        {
            Debug.Log($"Product Owner atualizado: {targetPlayer.NickName}");
            
            // Se um novo Product Owner foi definido, remover o antigo
            if ((bool)changedProps[productOwnerPropertyKey] == true)
            {
                RemoveOtherProductOwners(targetPlayer);
                
                // Fechar popup de confirmação para outros jogadores
                if (popupBecomePOContainer != null && popupBecomePOContainer.activeSelf)
                {
                    popupBecomePOContainer.SetActive(false);
                }
            }
            
            UpdateUI();
            
            // Notificar o NetworkManager para atualizar a lista de jogadores
            if (networkManager != null)
            {
                networkManager.UpdateRoomInfo();
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // Se o Product Owner saiu da sala, limpar o estado
        if (IsPlayerProductOwner(otherPlayer))
        {
            Debug.Log("Product Owner saiu da sala");
        }
        UpdateUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateUI();
    }

    #endregion

    private void RemoveOtherProductOwners(Player newPO)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player != newPO && IsPlayerProductOwner(player))
            {
                // Remover a propriedade Product Owner de outros jogadores
                Hashtable props = new Hashtable();
                props[productOwnerPropertyKey] = false;
                player.SetCustomProperties(props);
            }
        }
    }

    #region Public Utility Methods

    public string GetFormattedPlayerName(Player player)
    {
        if (player == null) return "";
        
        string playerName = player.NickName;
        if (IsPlayerProductOwner(player))
        {
            playerName += " (PO)";
        }
        
        return playerName;
    }

    public void ClearProductOwner()
    {
        if (IsLocalPlayerProductOwner())
        {
            Hashtable props = new Hashtable();
            props[productOwnerPropertyKey] = false;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            Debug.Log("Status de Product Owner removido");
        }
    }

    #endregion
}
