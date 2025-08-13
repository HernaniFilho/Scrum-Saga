using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class MultiplayerSetup : MonoBehaviour
{
    [Header("Multiplayer Settings")]
    [SerializeField] private bool enableMultiplayer = true;
    
    [Header("Configuração Automática")]
    [SerializeField] private bool autoSetupOnStart = true;
    
    [Header("UI Elements to Create")]
    [SerializeField] private bool createConnectionUI = true;
    [SerializeField] private bool enableNameInput = true;
    
    [Header("UI References (Optional - se não atribuído, será criado automaticamente)")]
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private TextMeshProUGUI roomInfoText;

    private Canvas uiCanvas;
    private NetworkGameStateManager gameStateManager;
    private NetworkManager networkManager;
    private NameInputManager nameInputManager;

    void Start()
    {
        if (autoSetupOnStart && enableMultiplayer)
        {
            SetupMultiplayerScene();
            
            if (enableNameInput)
            {
                SetupNameInputManager();
            }
        }

        if (!enableMultiplayer)
        {
            if (connectionStatusText != null)
            {
                connectionStatusText.gameObject.SetActive(false);
            }

            if (roomInfoText != null)
            {
                roomInfoText.gameObject.SetActive(false);
            }
        }
    }

    [ContextMenu("Setup Multiplayer Scene")]
    public void SetupMultiplayerScene()
    {        
        Debug.Log("Configurando SampleScene para Multiplayer...");
        
        SetupCanvas();
        
        SetupNetworkGameStateManager();
        
        SetupNetworkManager();
        
        if (createConnectionUI) CreateConnectionUI();
        
        Debug.Log("Configuração de Multiplayer concluída!");
    }

    void SetupCanvas()
    {
        uiCanvas = FindObjectOfType<Canvas>();
        if (uiCanvas == null)
        {
            GameObject canvasObj = new GameObject("UI Canvas");
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Garantir que existe EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            Debug.Log("Canvas criado");
        }
    }

    void SetupNetworkGameStateManager()
    {
        if (NetworkGameStateManager.Instance == null)
        {
            GameObject gameStateObj = new GameObject("NetworkGameStateManager");
            gameStateManager = gameStateObj.AddComponent<NetworkGameStateManager>();
            
            Debug.Log("NetworkGameStateManager criado (ponte para GameStateManager)");
        }
        else
        {
            gameStateManager = NetworkGameStateManager.Instance;
            Debug.Log("NetworkGameStateManager já existe");
        }
    }

    void SetupNetworkManager()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager == null)
        {
            GameObject networkObj = new GameObject("NetworkManager");
            networkManager = networkObj.AddComponent<NetworkManager>();
            Debug.Log("NetworkManager criado");
        }
        else
        {
            Debug.Log("NetworkManager já existe");
        }
        
        // Se enableNameInput for true, impedir conexão automática
        if (enableNameInput && networkManager != null)
        {
            networkManager.autoConnect = false;
        }
    }



    void CreateConnectionUI()
    {
        // Cria o Connection Status Text se não tiver sido atribuído
        if (connectionStatusText == null)
        {
            GameObject connectionTextObj = new GameObject("ConnectionStatusText");
            connectionTextObj.transform.SetParent(uiCanvas.transform, false);

            connectionStatusText = connectionTextObj.AddComponent<TextMeshProUGUI>();
            connectionStatusText.text = "Conectando...";
            connectionStatusText.fontSize = 16;
            connectionStatusText.color = Color.yellow;
            connectionStatusText.alignment = TextAlignmentOptions.TopLeft;

            RectTransform connectionRect = connectionStatusText.GetComponent<RectTransform>();
            connectionRect.anchorMin = new Vector2(0, 1);
            connectionRect.anchorMax = new Vector2(0, 1);
            connectionRect.anchoredPosition = new Vector2(10, -10);
            connectionRect.sizeDelta = new Vector2(300, 30);

            Debug.Log("Connection Status Text criado automaticamente");
        }
        else
        {
            Debug.Log("Connection Status Text já atribuído no editor");
        }

        // Cria o Room Info Text se não tiver sido atribuído
        if (roomInfoText == null)
        {
            GameObject roomTextObj = new GameObject("RoomInfoText");
            roomTextObj.transform.SetParent(uiCanvas.transform, false);

            roomInfoText = roomTextObj.AddComponent<TextMeshProUGUI>();
            roomInfoText.text = "Sala: Não conectado";
            roomInfoText.fontSize = 14;
            roomInfoText.color = Color.cyan;
            roomInfoText.alignment = TextAlignmentOptions.TopLeft;

            RectTransform roomRect = roomInfoText.GetComponent<RectTransform>();
            roomRect.anchorMin = new Vector2(0, 1);
            roomRect.anchorMax = new Vector2(0, 1);
            roomRect.anchoredPosition = new Vector2(100, -40);
            roomRect.sizeDelta = new Vector2(300, 30);

            Debug.Log("Room Info Text criado automaticamente");
        }
        else
        {
            Debug.Log("Room Info Text já atribuído no editor");
        }
        
        // Conectar ao NetworkManager
        if (networkManager != null)
        {
            networkManager.connectionStatusText = connectionStatusText;
            networkManager.roomInfoText = roomInfoText;
        }
        
        Debug.Log("Connection UI configurada");
    }

    void SetupNameInputManager()
    {
        // Criar o componente NameInputManager se não existir
        if (nameInputManager == null)
        {
            GameObject nameInputObj = new GameObject("NameInputManager");
            nameInputManager = nameInputObj.AddComponent<NameInputManager>();
        }
        
        // Configurar evento para quando o nome for confirmado
        nameInputManager.OnNameConfirmed += OnPlayerNameConfirmed;
        
        // Mostrar tela de nome
        ShowNameInputScreen();
        
        // Esconder outros elementos da UI
        if (connectionStatusText != null) connectionStatusText.gameObject.SetActive(false);
        if (roomInfoText != null) roomInfoText.gameObject.SetActive(false);
    }

    void ShowNameInputScreen()
    {
        if (nameInputManager != null)
        {
            nameInputManager.ShowNameInputScreen();
        }
    }

    void OnPlayerNameConfirmed(string playerName)
    {
        // Definir o nickname no NetworkManager se disponível
        if (networkManager != null)
        {
            networkManager.SetNickname(playerName);
        }

        // Mostrar UI normal
        if (connectionStatusText != null) connectionStatusText.gameObject.SetActive(true);
        if (roomInfoText != null) roomInfoText.gameObject.SetActive(true);
        
        // Agora que o nome foi escolhido, conectar à sala
        if (networkManager != null)
        {
            networkManager.ConnectToPhoton();
        }
    }

    [ContextMenu("Remove All UI")]
    public void RemoveAllUI()
    {
        if (uiCanvas != null)
        {
            DestroyImmediate(uiCanvas.gameObject);
        }
    }
}
