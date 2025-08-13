using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Configurações de Conexão")]
    public string gameVersion = "1.0";
    public int maxPlayersPerRoom = 5;

    [Header("UI References")]
    public TMP_Text connectionStatusText;
    public TMP_Text roomInfoText;
    public GameObject[] uiToHideWhenConnected;
    public GameObject[] uiToShowWhenConnected;
    
    [Header("Loading Screen")]
    private LoadingScreenManager loadingScreen;

    [Header("Auto Connect")]
    public bool autoConnect = true;

    void Start()
    {
        // Inicializar LoadingScreen
        loadingScreen = FindObjectOfType<LoadingScreenManager>();
        if (loadingScreen == null)
        {
            GameObject loadingScreenObj = new GameObject("LoadingScreenManager");
            loadingScreen = loadingScreenObj.AddComponent<LoadingScreenManager>();
        }

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

    private void UpdateRoomInfo()
    {
        if (roomInfoText != null)
        {
            if (PhotonNetwork.InRoom)
            {
                string players = "";
                foreach (var player in PhotonNetwork.PlayerList)
                {
                    players += "\n- " + player.NickName;
                }
                roomInfoText.text = $"Sala: {PhotonNetwork.CurrentRoom.Name}\nJogadores ({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}): {players}";
            }
            else
            {
                roomInfoText.text = "Não está em uma sala";
            }
        }
    }

    private void UpdateUI()
    {
        bool isConnectedToRoom = PhotonNetwork.InRoom;

        // Ocultar/mostrar elementos da UI conforme o estado da conexão
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
        Debug.Log("Conectado ao Master Server");
        loadingScreen?.UpdateLoadingMessage("Conectado ao Master Server");
        UpdateConnectionStatus("Conectado ao Master Server");
        JoinRandomOrCreateRoom();
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
        
        string roomName = "ScrumSaga_" + Random.Range(1000, 9999);
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Entrou na sala: {PhotonNetwork.CurrentRoom.Name}");
        UpdateConnectionStatus("Conectado à sala!");
        
        // Esconder tela de loading quando entrar na sala
        loadingScreen?.HideLoadingScreen();
        
        UpdateUI();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Saiu da sala");
        UpdateConnectionStatus("Não está em uma sala");
        
        // Mostrar tela de loading quando sair da sala
        loadingScreen?.ShowLoadingScreen("Conectando...");
        
        UpdateUI();
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Sala criada: {PhotonNetwork.CurrentRoom.Name}");
        UpdateConnectionStatus("Sala criada!");
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
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Jogador {otherPlayer.NickName} saiu da sala");
        UpdateRoomInfo();
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

    #endregion
}
