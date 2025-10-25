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
    private RoomInfo currentRoomInfo;
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
        currentRoomInfo = roomInfo;

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
            joinButton.interactable = !isRoomFull;
        }
    }

    bool IsGameStarted(RoomInfo roomInfo)
    {
        if (roomInfo.CustomProperties.ContainsKey(GAME_STATE_KEY))
        {
            int gameState = (int)roomInfo.CustomProperties[GAME_STATE_KEY];
            return gameState != 0;
        }
        
        return false;
    }

    void OnJoinButtonClicked()
    {
        if (lobbyManager != null && !string.IsNullOrEmpty(currentRoomName))
        {
            bool isGameInProgress = IsGameStarted(currentRoomInfo);
            lobbyManager.JoinRoom(currentRoomName, isGameInProgress);
        }
    }
}
