using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using System;

public class NameInputManager : MonoBehaviour
{
    [Header("Name Input Settings")]
    [SerializeField] private bool autoShowOnStart = false;
    
    [Header("UI Components")]
    private GameObject nameInputPanel;
    private TMP_InputField nameInputField;
    private Button confirmNameButton;
    private Canvas nameCanvas;

    public event Action<string> OnNameConfirmed;
    
    private void Start()
    {
        if (autoShowOnStart)
        {
            ShowNameInputScreen();
        }
    }

    public void ShowNameInputScreen()
    {
        CreateNameInputUI();
        nameInputPanel.SetActive(true);
    }

    public void HideNameInputScreen()
    {
        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(false);
        }
    }

    void CreateNameInputUI()
    {
        if (nameInputPanel != null) return;

        // Criar painel principal com fundo branco
        GameObject panelObj = new GameObject("NameInputPanel");
        
        // Criar Canvas próprio para o painel
        nameCanvas = panelObj.AddComponent<Canvas>();
        nameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        nameCanvas.sortingOrder = 100; // Fica acima de outros elementos
        
        panelObj.AddComponent<CanvasScaler>();
        panelObj.AddComponent<GraphicRaycaster>();

        nameInputPanel = panelObj;
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = Color.white;

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Título
        CreateTitleText(panelObj);
        
        // Campo de input
        CreateInputField(panelObj);
        
        // Botão Confirmar
        CreateConfirmButton(panelObj);

        Debug.Log("Name Input UI criada");
    }

    void CreateTitleText(GameObject parent)
    {
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(parent.transform, false);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Digite seu nome:";
        titleText.fontSize = 24;
        titleText.color = Color.black;
        titleText.alignment = TextAlignmentOptions.Center;

        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0, 60);
        titleRect.sizeDelta = new Vector2(400, 50);
    }

    void CreateInputField(GameObject parent)
    {
        GameObject inputObj = new GameObject("NameInput");
        inputObj.transform.SetParent(parent.transform, false);

        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        nameInputField = inputObj.AddComponent<TMP_InputField>();
        nameInputField.textComponent = CreateInputTextComponent(inputObj);
        nameInputField.placeholder = CreatePlaceholderComponent(inputObj);
        nameInputField.characterLimit = 25;
        
        // Configurar cores de transição
        ColorBlock colors = nameInputField.colors;
        colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        nameInputField.colors = colors;
        
        // Adicionar borda para o highlight
        Outline outline = inputObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.6f, 1f, 0f);
        outline.effectDistance = new Vector2(2, 2);
        
        // Configurar eventos para mostrar/esconder borda e placeholder
        nameInputField.onSelect.AddListener((string value) => {
            outline.effectColor = new Color(0.2f, 0.6f, 1f, 1f);
            if (nameInputField.placeholder != null)
            {
                nameInputField.placeholder.gameObject.SetActive(false);
            }
        });
        
        nameInputField.onDeselect.AddListener((string value) => {
            outline.effectColor = new Color(0.2f, 0.6f, 1f, 0f);
            if (nameInputField.placeholder != null && string.IsNullOrEmpty(nameInputField.text))
            {
                nameInputField.placeholder.gameObject.SetActive(true);
            }
        });
        
        // Adicionar listener para confirmar com Enter
        nameInputField.onEndEdit.AddListener((string value) => {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnConfirmNameClicked();
            }
        });

        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.anchoredPosition = Vector2.zero;
        inputRect.sizeDelta = new Vector2(300, 40);
    }

    void CreateConfirmButton(GameObject parent)
    {
        GameObject buttonObj = new GameObject("ConfirmButton");
        buttonObj.transform.SetParent(parent.transform, false);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);

        confirmNameButton = buttonObj.AddComponent<Button>();
        confirmNameButton.targetGraphic = buttonImage;
        confirmNameButton.onClick.AddListener(OnConfirmNameClicked);

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0, -60);
        buttonRect.sizeDelta = new Vector2(200, 40);

        // Texto do botão
        GameObject buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Confirmar";
        buttonText.fontSize = 20;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        RectTransform buttonTextRect = buttonText.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
    }

    TextMeshProUGUI CreateInputTextComponent(GameObject parent)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent.transform, false);

        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = "";
        textComponent.fontSize = 18;
        textComponent.color = Color.black;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;

        RectTransform textRect = textComponent.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);

        return textComponent;
    }

    Graphic CreatePlaceholderComponent(GameObject parent)
    {
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(parent.transform, false);

        TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Insira seu nome...";
        placeholder.fontSize = 18;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.verticalAlignment = VerticalAlignmentOptions.Middle;

        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 0);
        placeholderRect.offsetMax = new Vector2(-10, 0);

        return placeholder;
    }

    void OnConfirmNameClicked()
    {
        string playerName = nameInputField.text.Trim();
        
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.Log("Nome não pode estar vazio!");
            return;
        }

        // Salvar o nome do jogador
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();

        // Definir o nickname no Photon Network
        PhotonNetwork.NickName = playerName;

        Debug.Log($"Nome do jogador definido: {playerName}");

        // Esconder tela de nome
        HideNameInputScreen();
        
        // Notificar outros componentes através do evento
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
