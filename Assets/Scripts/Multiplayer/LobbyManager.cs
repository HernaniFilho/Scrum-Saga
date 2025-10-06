using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI References - Manual Assignment")]
    public GameObject lobbyPanel;
    public Button createRoomButton;
    public Transform roomListContainer;
    public GameObject roomCardPrefab;
    public GameObject noRoomsMessage;

    private Dictionary<string, GameObject> roomCards = new Dictionary<string, GameObject>();
    private LoadingScreenManager loadingScreen;
    private RoomNameInputManager roomNameInputManager;

    void Start()
    {
        loadingScreen = FindObjectOfType<LoadingScreenManager>();
        if (loadingScreen == null)
        {
            GameObject loadingScreenObj = new GameObject("LoadingScreenManager");
            loadingScreen = loadingScreenObj.AddComponent<LoadingScreenManager>();
        }

        roomNameInputManager = FindObjectOfType<RoomNameInputManager>();
        if (roomNameInputManager != null)
        {
            roomNameInputManager.OnRoomNameConfirmed += CreateRoom;
        }

        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }
    }

    public void ShowLobby()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }

        if (!PhotonNetwork.InLobby)
        {
            loadingScreen?.ShowLoadingScreen("Entrando no lobby...");
            PhotonNetwork.JoinLobby();
        }
        else
        {
            UpdateRoomList();
        }
    }

    public void HideLobby()
    {
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }
    }

    void OnCreateRoomButtonClicked()
    {
        if (roomNameInputManager != null)
        {
            roomNameInputManager.ShowRoomNameInputScreen();
        }
        else
        {
            string roomName = "ScrumSaga_" + Random.Range(1000, 9999);
            CreateRoom(roomName);
        }
    }

    void CreateRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Não está conectado para criar sala");
            return;
        }

        loadingScreen?.ShowLoadingScreen("Criando sala...");

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 5;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Não está conectado para entrar na sala");
            return;
        }

        loadingScreen?.ShowLoadingScreen("Entrando na sala...");
        PhotonNetwork.JoinRoom(roomName);
    }

    void UpdateRoomList()
    {
        ClearRoomList();
    }

    void ClearRoomList()
    {
        foreach (var card in roomCards.Values)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }
        roomCards.Clear();
        UpdateNoRoomsMessage();
    }

    void AddRoomCard(RoomInfo roomInfo)
    {
        if (roomCardPrefab == null || roomListContainer == null)
        {
            Debug.LogWarning("RoomCardPrefab ou RoomListContainer não atribuído");
            return;
        }

        if (roomCards.ContainsKey(roomInfo.Name))
        {
            return;
        }

        GameObject cardObj = Instantiate(roomCardPrefab, roomListContainer);
        RoomCard roomCard = cardObj.GetComponent<RoomCard>();
        
        if (roomCard != null)
        {
            roomCard.SetRoomInfo(roomInfo, this);
        }

        roomCards.Add(roomInfo.Name, cardObj);
        UpdateNoRoomsMessage();
    }

    void RemoveRoomCard(string roomName)
    {
        if (roomCards.ContainsKey(roomName))
        {
            GameObject card = roomCards[roomName];
            roomCards.Remove(roomName);
            
            if (card != null)
            {
                Destroy(card);
            }
            
            UpdateNoRoomsMessage();
        }
    }

    void UpdateRoomCard(RoomInfo roomInfo)
    {
        if (roomCards.ContainsKey(roomInfo.Name))
        {
            GameObject cardObj = roomCards[roomInfo.Name];
            RoomCard roomCard = cardObj.GetComponent<RoomCard>();
            
            if (roomCard != null)
            {
                roomCard.SetRoomInfo(roomInfo, this);
            }
        }
    }

    #region Photon Callbacks

    public override void OnJoinedLobby()
    {
        Debug.Log("Entrou no lobby");
        loadingScreen?.HideLoadingScreen();
        UpdateRoomList();
    }

    public override void OnLeftLobby()
    {
        Debug.Log("Saiu do lobby");
        ClearRoomList();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"Lista de salas atualizada: {roomList.Count} salas");

        foreach (RoomInfo roomInfo in roomList)
        {
            if (roomInfo.RemovedFromList)
            {
                RemoveRoomCard(roomInfo.Name);
            }
            else
            {
                if (roomCards.ContainsKey(roomInfo.Name))
                {
                    UpdateRoomCard(roomInfo);
                }
                else
                {
                    AddRoomCard(roomInfo);
                }
            }
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Entrou na sala: {PhotonNetwork.CurrentRoom.Name}");
        loadingScreen?.HideLoadingScreen();
        HideLobby();
        
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null)
        {
            networkManager.UpdateUI();
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Sala criada: {PhotonNetwork.CurrentRoom.Name}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Falha ao criar sala: {message}");
        loadingScreen?.HideLoadingScreen();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Falha ao entrar na sala: {message}");
        loadingScreen?.HideLoadingScreen();
    }

    #endregion

    void UpdateNoRoomsMessage()
    {
        if (noRoomsMessage != null)
        {
            noRoomsMessage.SetActive(roomCards.Count == 0);
        }
    }
}
