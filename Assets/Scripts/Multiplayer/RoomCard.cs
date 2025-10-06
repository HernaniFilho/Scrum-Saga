using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class RoomCard : MonoBehaviour
{
    [Header("UI References - Manual Assignment")]
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public Button joinButton;

    private string currentRoomName;
    private LobbyManager lobbyManager;
    private const string GAME_STATE_KEY = "GameState";

    void Start()
    {
        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinButtonClicked);
        }
    }

    public void SetRoomInfo(RoomInfo roomInfo, LobbyManager lobby)
    {
        lobbyManager = lobby;
        currentRoomName = roomInfo.Name;

        if (roomNameText != null)
        {
            roomNameText.text = roomInfo.Name;
        }

        if (playerCountText != null)
        {
            playerCountText.text = $"Jogadores: {roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";
        }

        if (joinButton != null)
        {
            bool isRoomFull = roomInfo.PlayerCount >= roomInfo.MaxPlayers;
            bool isGameStarted = IsGameStarted(roomInfo);
            
            joinButton.interactable = roomInfo.IsOpen && !isRoomFull && !isGameStarted;
        }
    }

    bool IsGameStarted(RoomInfo roomInfo)
    {
        if (!roomInfo.IsOpen)
        {
            return true;
        }
        
        if (roomInfo.CustomProperties.ContainsKey("GameStarted"))
        {
            return (bool)roomInfo.CustomProperties["GameStarted"];
        }
        
        return false;
    }

    void OnJoinButtonClicked()
    {
        if (lobbyManager != null && !string.IsNullOrEmpty(currentRoomName))
        {
            lobbyManager.JoinRoom(currentRoomName);
        }
    }
}
