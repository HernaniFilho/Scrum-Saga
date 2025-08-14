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
    public TMP_Text buttonText;

    [Header("Configuration")]
    public string productOwnerPropertyKey = "IsProductOwner";
    
    private NetworkManager networkManager;
    
    void Start()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        
        if (becomeProductOwnerButton != null)
        {
            becomeProductOwnerButton.onClick.AddListener(OnBecomeProductOwnerClicked);
        }
        
        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    public void OnBecomeProductOwnerClicked()
    {
        if (!PhotonNetwork.InRoom) return;

        // Verificar se já existe um Product Owner
        if (GetCurrentProductOwner() != null)
        {
            Debug.Log("Já existe um Product Owner na sala");
            return;
        }

        // Tornar-se Product Owner
        BecomeProductOwner();
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

    private void UpdateUI()
    {
        if (becomeProductOwnerButton == null) return;

        Player currentPO = GetCurrentProductOwner();
        bool hasProductOwner = currentPO != null;
        bool isLocalPlayerPO = IsLocalPlayerProductOwner();

        // Atualizar visibilidade do botão
        becomeProductOwnerButton.gameObject.SetActive(!hasProductOwner && PhotonNetwork.InRoom);

        // Atualizar texto do botão
        if (buttonText != null)
        {
            if (hasProductOwner)
            {
                if (isLocalPlayerPO)
                {
                    buttonText.text = "Você é o Product Owner";
                }
                else
                {
                    buttonText.text = $"{currentPO.NickName} é o Product Owner";
                }
            }
            else
            {
                buttonText.text = "Tornar-se Product Owner";
            }
        }

        // Atualizar interatividade do botão
        becomeProductOwnerButton.interactable = !hasProductOwner && PhotonNetwork.InRoom;
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
            UpdateUI();
        }
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

    /// <summary>
    /// Obtém uma string formatada com o nome do jogador e indicador PO se aplicável
    /// </summary>
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

    /// <summary>
    /// Força a remoção do status de Product Owner (para uso administrativo)
    /// </summary>
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
