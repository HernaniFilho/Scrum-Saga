using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using System;

public class RoomNameInputManager : MonoBehaviour
{
    [Header("UI Components - Manual Assignment")]
    public GameObject roomNameInputPanel;
    public TMP_InputField roomNameInputField;
    public Button confirmRoomNameButton;

    public event Action<string> OnRoomNameConfirmed;

    void Start()
    {
        if (confirmRoomNameButton != null)
        {
            confirmRoomNameButton.onClick.AddListener(OnConfirmRoomNameClicked);
        }

        if (roomNameInputPanel != null)
        {
            roomNameInputPanel.SetActive(false);
        }
    }

    public void ShowRoomNameInputScreen()
    {
        if (roomNameInputPanel != null)
        {
            roomNameInputPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("RoomNameInputPanel não foi atribuído no Inspector!");
        }
    }

    public void HideRoomNameInputScreen()
    {
        if (roomNameInputPanel != null)
        {
            roomNameInputPanel.SetActive(false);
        }
    }

    void OnConfirmRoomNameClicked()
    {
        if (roomNameInputField == null)
        {
            Debug.LogWarning("RoomNameInputField não foi atribuído!");
            return;
        }

        string roomName = roomNameInputField.text.Trim();
        
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.Log("Nome da sala não pode estar vazio!");
            return;
        }

        Debug.Log($"Nome da sala definido: {roomName}");

        HideRoomNameInputScreen();
        
        OnRoomNameConfirmed?.Invoke(roomName);
        
        if (roomNameInputField != null)
        {
            roomNameInputField.text = "";
        }
    }
}
