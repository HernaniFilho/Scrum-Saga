using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Configurações de Conexão")]
    public string gameVersion = "1.0";
    public int maxPlayersPerRoom = 5;

    [Header("UI References")]
    public TMP_Text connectionStatusText;
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public TMP_Text[] playerNameTexts = new TMP_Text[5];
    public TMP_Text[] playerPOTexts = new TMP_Text[5];
    public GameObject[] uiToHideWhenConnected;
    public GameObject[] uiToShowWhenConnected;
    
    [Header("Loading Screen")]
    private LoadingScreenManager loadingScreen;

    [Header("Auto Connect")]
    public bool autoConnect = true;
    
    [Header("Product Owner")]
    private ProductOwnerManager productOwnerManager;
    
    [Header("Reset Manager")]
    private ResetGameManager resetGameManager;

    void Start()
    {
        // Inicializar LoadingScreen
        loadingScreen = FindObjectOfType<LoadingScreenManager>();
        if (loadingScreen == null)
        {
            GameObject loadingScreenObj = new GameObject("LoadingScreenManager");
            loadingScreen = loadingScreenObj.AddComponent<LoadingScreenManager>();
        }
        
        // Inicializar ProductOwnerManager
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();

        PhotonNetwork.GameVersion = gameVersion;
        
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Jogador_" + Random.Range(1000, 9999);
        }

        // Não mostrar loading screen no Start - só quando o nome for confirmado

        UpdateUI();

        if (autoConnect && !PhotonNetwork.IsConnected)
        {
            ConnectToPhoton();
        }
    }

    public void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected)
        {
            JoinRandomOrCreateRoom();
        }
        else
        {
            Debug.Log("Conectando ao Photon...");
            loadingScreen?.ShowLoadingScreen("Conectando...");
            UpdateConnectionStatus("Conectando...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void DisconnectFromPhoton()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            UpdateConnectionStatus("Desconectando...");
        }
    }

    private void JoinRandomOrCreateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Não está conectado e pronto para entrar em uma sala");
            return;
        }

        loadingScreen?.UpdateLoadingMessage("Procurando sala...");
        UpdateConnectionStatus("Procurando sala...");
        PhotonNetwork.JoinRandomRoom();
    }

    private void UpdateConnectionStatus(string status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = status;
        }
        Debug.Log($"Status de conexão: {status}");
    }

    public void UpdateRoomInfo()
    {
        UpdateRoomName();
        UpdatePlayerCount();
        UpdatePlayerContainers();
    }
    
    private void UpdateRoomName()
    {
        if (roomNameText != null)
        {
            if (PhotonNetwork.InRoom)
            {
                roomNameText.text = PhotonNetwork.CurrentRoom.Name;
            }
            else
            {
                roomNameText.text = "Não conectado";
            }
        }
    }
    
    private void UpdatePlayerCount()
    {
        if (playerCountText != null)
        {
            if (PhotonNetwork.InRoom)
            {
                int currentCount = PhotonNetwork.CurrentRoom.PlayerCount;
                int maxCount = PhotonNetwork.CurrentRoom.MaxPlayers;
                playerCountText.text = $"Jogadores ({currentCount}/{maxCount}):";
            }
            else
            {
                playerCountText.text = "Jogadores (0/0):";
            }
        }
    }
    
    private void UpdatePlayerContainers()
    {
        if (playerNameTexts == null || playerPOTexts == null) return;
        
        if (PhotonNetwork.InRoom)
        {
            var playerList = PhotonNetwork.PlayerList;
            
            // Atualizar textos baseado na quantidade de jogadores
            for (int i = 0; i < playerNameTexts.Length; i++)
            {
                if (i < playerList.Length)
                {
                    // Atualizar informações do jogador
                    UpdatePlayerTexts(playerList[i], i);
                }
                else
                {
                    // Limpar textos se não há jogador suficiente
                    ClearPlayerTexts(i);
                }
            }
        }
        else
        {
            // Se não está na sala, limpar todos os textos
            for (int i = 0; i < playerNameTexts.Length; i++)
            {
                ClearPlayerTexts(i);
            }
        }
    }
    
    private void UpdatePlayerTexts(Player player, int index)
    {
        if (index >= playerNameTexts.Length || index >= playerPOTexts.Length) return;
        
        // Atualizar nome do jogador
        if (playerNameTexts[index] != null)
        {
            playerNameTexts[index].text = player.NickName;
            
            // Ativar componente Image do pai se existir
            Image parentImage = playerNameTexts[index].transform.parent?.GetComponent<Image>();
            if (parentImage != null)
            {
                parentImage.enabled = true;
            }
        }
        
        // Atualizar status PO (NÃO sobrescrever se já tiver texto "Terminou!")
        if (playerPOTexts[index] != null)
        {
            string currentText = playerPOTexts[index].text;
            
            // Não sobrescrever se já tem "Terminou!"
            if (currentText == "Terminou!")
            {
                return;
            }
            
            if (productOwnerManager != null && productOwnerManager.IsPlayerProductOwner(player))
            {
                playerPOTexts[index].gameObject.SetActive(true);
                playerPOTexts[index].text = "(PO)";
            }
            else
            {
                playerPOTexts[index].text = "";
            }
        }
    }

    private void ClearPlayerTexts(int index)
    {
        if (index >= playerNameTexts.Length || index >= playerPOTexts.Length) return;
        
        // Limpar nome do jogador
        if (playerNameTexts[index] != null)
        {
            playerNameTexts[index].text = "";
            
            // Desativar componente Image do pai se existir
            Image parentImage = playerNameTexts[index].transform.parent?.GetComponent<Image>();
            if (parentImage != null)
            {
                parentImage.enabled = false;
            }
        }
        
        // Limpar status PO
        if (playerPOTexts[index] != null)
        {
            playerPOTexts[index].text = "";
        }
    }

    public void UpdateUI()
    {
        bool isConnectedToRoom = PhotonNetwork.InRoom;

        if (uiToHideWhenConnected != null)
        {
            foreach (GameObject obj in uiToHideWhenConnected)
            {
                if (obj != null)
                    obj.SetActive(!isConnectedToRoom);
            }
        }

        if (uiToShowWhenConnected != null)
        {
            foreach (GameObject obj in uiToShowWhenConnected)
            {
                if (obj != null)
                    obj.SetActive(isConnectedToRoom);
            }
        }

        UpdateRoomInfo();
    }

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        loadingScreen?.UpdateLoadingMessage("Conectado!");
        UpdateConnectionStatus("Conectado!");
        
        if (autoConnect)
        {
            JoinRandomOrCreateRoom();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Desconectado do Photon: {cause}");
        UpdateConnectionStatus($"Desconectado: {cause}");
        UpdateUI();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Não foi possível encontrar uma sala. Criando uma nova...");
        loadingScreen?.UpdateLoadingMessage("Criando sala...");
        UpdateConnectionStatus("Criando sala...");
        
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayersPerRoom;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "GameState" };
        
        string roomName = "ScrumSaga_" + Random.Range(1000, 9999);
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Entrou na sala: {PhotonNetwork.CurrentRoom.Name}");
        UpdateConnectionStatus("Conectado à sala!");
        
        loadingScreen?.HideLoadingScreen();
        
        if (productOwnerManager == null)
        {
            productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        }
        
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GameState"))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Hashtable roomProps = new Hashtable();
                roomProps["GameState"] = (int)GameStateManager.GameState.Inicio;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
        }
        
        UpdateUI();
        UpdateRoomInfo();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Saiu da sala");
        UpdateConnectionStatus("Não está em uma sala");
        
        // Resetar estado local do jogo (limpar cartas, popups, etc.)
        if (resetGameManager == null)
        {
            resetGameManager = ResetGameManager.Instance;
        }
        
        if (resetGameManager != null)
        {
            resetGameManager.ResetLocalGameState();
        }
        
        // Mostrar tela de loading quando sair da sala
        loadingScreen?.ShowLoadingScreen("Conectando...");
        
        UpdateUI();
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Sala criada: {PhotonNetwork.CurrentRoom.Name}");
        UpdateConnectionStatus("Sala criada!");
        
        Hashtable roomProps = new Hashtable();
        roomProps["GameState"] = (int)GameStateManager.GameState.Inicio;
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        
        UpdateUI();
        UpdateRoomInfo();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Falha ao criar sala: {message}");
        UpdateConnectionStatus($"Erro ao criar sala: {message}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Jogador {newPlayer.NickName} entrou na sala");
        UpdateRoomInfo();
        
        if (productOwnerManager != null)
        {
            productOwnerManager.UpdateUI();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Jogador {otherPlayer.NickName} saiu da sala");
        UpdateRoomInfo();
        
        if (productOwnerManager != null)
        {
            productOwnerManager.UpdateUI();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"Novo Master Client: {newMasterClient.NickName}");
        UpdateRoomInfo();
    }

    #endregion

    #region Public Methods for UI

    public void SetNickname(string nickname)
    {
        if (!string.IsNullOrEmpty(nickname))
        {
            PhotonNetwork.NickName = nickname;
            Debug.Log($"Nickname definido como: {nickname}");
        }
    }

    public void SetNickname(TMP_InputField inputField)
    {
        SetNickname(inputField.text);
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public void UpdatePlayerPOText(int playerIndex, string text)
    {
        if (playerPOTexts == null)
        {
            Debug.LogError("playerPOTexts array é null!");
            return;
        }
        
        if (playerIndex < 0 || playerIndex >= playerPOTexts.Length)
        {
            Debug.LogError($"PlayerIndex inválido: {playerIndex}");
            return;
        }
        
        if (playerPOTexts[playerIndex] == null)
        {
            Debug.LogError($"playerPOTexts[{playerIndex}] é null!");
            return;
        }
        
        if (!playerPOTexts[playerIndex].gameObject.activeSelf)
        {
            playerPOTexts[playerIndex].gameObject.SetActive(true);
        }
        
        playerPOTexts[playerIndex].text = text;
    }

    public void ClearFinishedPlayerTexts()
    {
        if (playerPOTexts != null)
        {
            int clearedCount = 0;
            for (int i = 0; i < playerPOTexts.Length; i++)
            {
                if (playerPOTexts[i] != null && playerPOTexts[i].text == "Terminou!")
                {
                    playerPOTexts[i].text = "";
                    clearedCount++;
                }
            }
            Debug.Log($"NetworkManager: {clearedCount} textos 'Terminou!' foram limpos");
        }
    }

    public void ClearAllPlayerPOTexts()
    {
        if (playerPOTexts != null)
        {
            for (int i = 0; i < playerPOTexts.Length; i++)
            {
                if (playerPOTexts[i] != null)
                {
                    playerPOTexts[i].text = "";
                }
            }
            Debug.Log("NetworkManager: Todos os playerPOTexts foram limpos");
        }
    }

    public string GetPlayerPOText(int playerIndex)
    {
        if (playerPOTexts != null && playerIndex >= 0 && playerIndex < playerPOTexts.Length)
        {
            if (playerPOTexts[playerIndex] != null)
            {
                return playerPOTexts[playerIndex].text;
            }
        }
        return "";
    }

    #endregion
}
