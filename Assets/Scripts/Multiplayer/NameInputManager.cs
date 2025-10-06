using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using System;

public class NameInputManager : MonoBehaviour
{
    [Header("Name Input Settings")]
    [SerializeField] private bool autoShowOnStart = false;
    
    [Header("UI Components - Manual Assignment")]
    public GameObject nameInputPanel;
    public TMP_InputField nameInputField;
    public Button confirmNameButton;

    public event Action<string> OnNameConfirmed;

    public void ShowNameInputScreen()
    {
        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("NameInputPanel não foi atribuído no Inspector!");
        }
    }

    public void HideNameInputScreen()
    {
        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(false);
        }
    }

    void Start()
    {
        if (autoShowOnStart)
        {
            ShowNameInputScreen();
        }

        if (confirmNameButton != null)
        {
            confirmNameButton.onClick.AddListener(OnConfirmNameClicked);
        }
    }

    void OnConfirmNameClicked()
    {
        if (nameInputField == null)
        {
            Debug.LogWarning("NameInputField não foi atribuído!");
            return;
        }

        string playerName = nameInputField.text.Trim();
        
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.Log("Nome não pode estar vazio!");
            return;
        }

        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();

        PhotonNetwork.NickName = playerName;

        Debug.Log($"Nome do jogador definido: {playerName}");

        HideNameInputScreen();
        
        OnNameConfirmed?.Invoke(playerName);
    }

    public string GetPlayerName()
    {
        return PlayerPrefs.GetString("PlayerName", "");
    }

    public bool HasPlayerName()
    {
        return !string.IsNullOrEmpty(GetPlayerName());
    }
}
