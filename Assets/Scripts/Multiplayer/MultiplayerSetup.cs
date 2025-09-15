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
    [SerializeField] private bool enableNameInput = true;
    
    [Header("UI References (Optional - se não atribuído, será criado automaticamente)")]
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI[] playerNameTexts = new TextMeshProUGUI[5];
    [SerializeField] private TextMeshProUGUI[] playerPOTexts = new TextMeshProUGUI[5];

    private Canvas uiCanvas;
    private NetworkGameStateManager gameStateManager;
    private NetworkManager networkManager;
    private NetworkScoreManager networkScoreManager;
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

            if (roomNameText != null)
            {
                roomNameText.gameObject.SetActive(false);
            }
            
            if (playerCountText != null)
            {
                playerCountText.gameObject.SetActive(false);
            }
        }
    }

    [ContextMenu("Setup Multiplayer Scene")]
    public void SetupMultiplayerScene()
    {        
        Debug.Log("Configurando SampleScene para Multiplayer...");
        
        SetupCanvas();
        
        SetupNetworkGameStateManager();
        
        SetupNetworkScoreManager();
        
        SetupNetworkManager();
        
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

    void SetupNetworkScoreManager()
    {
        if (NetworkScoreManager.Instance == null)
        {
            GameObject scoreManagerObj = new GameObject("NetworkScoreManager");
            networkScoreManager = scoreManagerObj.AddComponent<NetworkScoreManager>();
            
            Debug.Log("NetworkScoreManager criado (ponte para ScoreManager)");
        }
        else
        {
            networkScoreManager = NetworkScoreManager.Instance;
            Debug.Log("NetworkScoreManager já existe");
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

        if (networkManager != null)
        {
            networkManager.connectionStatusText = connectionStatusText;
            networkManager.roomNameText = roomNameText;
            networkManager.playerCountText = playerCountText;
            networkManager.playerNameTexts = playerNameTexts;
            networkManager.playerPOTexts = playerPOTexts;
        }
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
        if (roomNameText != null) roomNameText.gameObject.SetActive(false);
        if (playerCountText != null) playerCountText.gameObject.SetActive(false);
        // Limpar textos de jogadores
        ClearAllPlayerTexts();
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
        if (roomNameText != null) roomNameText.gameObject.SetActive(true);
        if (playerCountText != null) playerCountText.gameObject.SetActive(true);
        // Textos de jogadores ficam vazios inicialmente (serão gerenciados pelo NetworkManager)
        
        // Agora que o nome foi escolhido, conectar à sala
        // A loading screen será mostrada automaticamente pelo NetworkManager.ConnectToPhoton()
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
    
    void ClearAllPlayerTexts()
    {
        // Limpar todos os textos de nome
        if (playerNameTexts != null)
        {
            for (int i = 0; i < playerNameTexts.Length; i++)
            {
                if (playerNameTexts[i] != null)
                {
                    playerNameTexts[i].text = "";
                    
                    // Desativar componente Image do pai se existir
                    UnityEngine.UI.Image parentImage = playerNameTexts[i].transform.parent?.GetComponent<UnityEngine.UI.Image>();
                    if (parentImage != null)
                    {
                        parentImage.enabled = false;
                    }
                }
            }
        }
        
        // Limpar todos os textos de PO
        if (playerPOTexts != null)
        {
            for (int i = 0; i < playerPOTexts.Length; i++)
            {
                if (playerPOTexts[i] != null)
                    playerPOTexts[i].text = "";
            }
        }
    }
}
