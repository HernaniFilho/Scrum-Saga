using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// Gerencia o botão de resetar partida disponível apenas para o PO
/// Permite resetar todos os aspectos da partida com confirmação
/// </summary>
public class ResetGameManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements - Assignable in Unity Editor")]
    [SerializeField] private Button resetGameButton;
    [SerializeField] private GameObject popupConfirmacaoContainer;
    [SerializeField] private Button confirmarResetButton;
    [SerializeField] private Button cancelarResetButton;
    [SerializeField] private GameObject avisoResetContainer;
    [SerializeField] private Button fecharAvisoResetButton;
    
    [Header("Text Elements")]
    [SerializeField] private TMP_Text popupConfirmacaoTexto;
    [SerializeField] private TMP_Text avisoResetTexto;
    
    [Header("Game References")]
    private ProductOwnerManager productOwnerManager;
    private GameStateManager gameStateManager;
    private ScoreManager scoreManager;
    private StartButtonManager startButtonManager;
    private ScrumMasterCardManager scrumMasterCardManager;
    private FimManager fimManager;
    private CanvasManager canvasManager;
    private CommandSaveSystem commandSaveSystem;
    private TimerManager timerManager;
    private SprintPlanningManager sprintPlanningManager;
    private ImprevistoManager imprevistoManager;
    private EscolhaManager escolhaManager;
    private SprintRetrospectiveManager sprintRetrospectiveManager;
    private SprintReviewManager sprintReviewManager;
    
    [Header("Configuration")]
    [SerializeField] private string textoConfirmacao = "ATENÇÃO: Esta ação irá reiniciar TODA a partida!\n\nSprints, pontuações e cartas obtidas serão reiniciadas.\n\nIMPORTANTE: Se houver cartas ou pop-ups abertos, feche-os ANTES do reset para evitar problemas visuais.\n\nEsta ação afetará TODOS os jogadores da partida. Tem certeza que deseja continuar?";
    [SerializeField] private string textoAvisoReset = "JOGO REINICIADO!\n\nO Product Owner reiniciou a partida.\nTodos os dados foram limpos e o jogo está pronto para começar novamente!";
    
    public static ResetGameManager Instance { get; private set; }
    public static bool IsResetPopupOpen { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        InitializeReferences();
        SetupUI();
    }

    void Update()
    {
        if (productOwnerManager == null)
            productOwnerManager = FindObjectOfType<ProductOwnerManager>();

        UpdateButtonVisibility();
    }

    void InitializeReferences()
    {
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        gameStateManager = GameStateManager.Instance;
        scoreManager = ScoreManager.Instance;
        startButtonManager = FindObjectOfType<StartButtonManager>();
        scrumMasterCardManager = ScrumMasterCardManager.Instance;
        fimManager = FimManager.Instance;
        canvasManager = CanvasManager.Instance;
        commandSaveSystem = FindObjectOfType<CommandSaveSystem>();
        timerManager = TimerManager.Instance;
        sprintPlanningManager = FindObjectOfType<SprintPlanningManager>();
        imprevistoManager = ImprevistoManager.Instance;
        escolhaManager = EscolhaManager.Instance;
        sprintRetrospectiveManager = SprintRetrospectiveManager.Instance;
        sprintReviewManager = FindObjectOfType<SprintReviewManager>();
        
        if (gameStateManager == null)
            Debug.LogError("GameStateManager não encontrado!");
    }

    void SetupUI()
    {
        // Setup do botão principal
        if (resetGameButton != null)
        {
            resetGameButton.transform.parent.gameObject.SetActive(false);
            resetGameButton.onClick.AddListener(OnResetGameButtonClicked);
        }

        // Setup do popup de confirmação
        if (popupConfirmacaoContainer != null)
        {
            popupConfirmacaoContainer.SetActive(false);
            
            if (popupConfirmacaoTexto != null)
                popupConfirmacaoTexto.text = textoConfirmacao;
        }

        if (confirmarResetButton != null)
        {
            confirmarResetButton.onClick.AddListener(OnConfirmarResetButtonClicked);
        }

        if (cancelarResetButton != null)
        {
            cancelarResetButton.onClick.AddListener(OnCancelarResetButtonClicked);
        }

        // Setup do aviso
        if (avisoResetContainer != null)
        {
            avisoResetContainer.SetActive(false);
            
            if (avisoResetTexto != null)
                avisoResetTexto.text = textoAvisoReset;
        }

        if (fecharAvisoResetButton != null)
        {
            fecharAvisoResetButton.onClick.AddListener(OnFecharAvisoResetButtonClicked);
        }
    }

    private void UpdateButtonVisibility()
    {
        if (resetGameButton == null || productOwnerManager == null || gameStateManager == null || startButtonManager == null) return;

        bool isLocalPlayerPO = productOwnerManager.IsLocalPlayerProductOwner();
        bool gameStarted = startButtonManager.GetCurrentSprint() != 0;
        bool shouldShowButton = isLocalPlayerPO && PhotonNetwork.InRoom && gameStarted;

        resetGameButton.transform.parent.gameObject.SetActive(shouldShowButton);
    }

    private void OnResetGameButtonClicked()
    {
        Debug.Log("Botão 'Reset Game' clicado!");

        IsResetPopupOpen = true;
        if (popupConfirmacaoContainer != null)
        {
            popupConfirmacaoContainer.SetActive(true);
        }
    }

    private void OnConfirmarResetButtonClicked()
    {
        Debug.Log("Confirmando reset do jogo");

        IsResetPopupOpen = false;
        if (popupConfirmacaoContainer != null)
        {
            popupConfirmacaoContainer.SetActive(false);
        }

        string playerName = PhotonNetwork.LocalPlayer.NickName;
        
        // Executar reset apenas localmente (as funções já sincronizam)
        ResetAllGameSystems();
        
        // Limpar canvas e desenhos
        CleanupCanvas();
        
        // Notificar todos os jogadores sobre o reset
        photonView.RPC("ShowResetNotification", RpcTarget.All, playerName);
    }

    private void OnCancelarResetButtonClicked()
    {
        Debug.Log("Cancelando reset do jogo");

        IsResetPopupOpen = false;
        if (popupConfirmacaoContainer != null)
        {
            popupConfirmacaoContainer.SetActive(false);
        }
    }

    private void OnFecharAvisoResetButtonClicked()
    {
        Debug.Log("Fechando aviso de reset");

        if (avisoResetContainer != null)
        {
            avisoResetContainer.SetActive(false);
        }
    }

    [PunRPC]
    void ShowResetNotification(string playerName)
    {
        Debug.Log($"Recebido aviso: Jogo resetado por {playerName}");
        
        // Limpar canvas e desenhos para todos
        CleanupCanvas();
        
        // Mostrar aviso para todos os jogadores
        if (avisoResetContainer != null)
        {
            avisoResetContainer.SetActive(true);
        }
    }

    private void ResetAllGameSystems()
    {
        Debug.Log("Iniciando reset completo dos sistemas do jogo...");

        // Reset do GameStateManager - voltar para o início
        if (gameStateManager != null)
        {
            gameStateManager.ResetToInitialState();
        }

        // Reset das pontuações
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        // Reset da carta Scrum Master
        if (scrumMasterCardManager != null)
        {
            scrumMasterCardManager.ResetCartaStatus();
        }

        // Reset do StartButtonManager (sprints)
        if (startButtonManager != null)
        {
            startButtonManager.ResetSprints();
        }

        // Reset do FimManager
        if (fimManager != null)
        {
            fimManager.ResetFimState();
        }

        // Reset do TimerManager
        if (timerManager != null)
        {
            timerManager.StopTimer(); // Para todos os timers ativos
        }

        // Reset dos managers via RPC para todos os jogadores
        photonView.RPC("ResetAllCardManagers", RpcTarget.All);

        // Reset do Product Owner não é feito pois foi ele quem clicou para resetar

        ClearRoomGameProperties();

        Debug.Log("Reset completo dos sistemas do jogo finalizado!");
    }

    private void CleanupCanvas()
    {
        Debug.Log("Limpando canvas e desenhos usando sistemas específicos...");
        
        // Desativar canvas usando o CanvasManager
        if (canvasManager != null)
        {
            canvasManager.ClearCanvasForAll();
            canvasManager.DeactivateCanvasForAll();
        }
        
        // Limpar desenhos salvos usando CommandSaveSystem
        if (commandSaveSystem != null)
        {
            commandSaveSystem.ClearAllSavedSessions();
        }
        
        // Usar o método específico do CanvasManager para limpeza completa
        if (canvasManager != null)
        {
            canvasManager.ClearAllSavedDrawings();
        }
    }

    private void ClearRoomGameProperties()
    {
        Hashtable props = new Hashtable();
        
        props["CartaScrumMasterUsada"] = null;
        props["CartaScrumMasterUsadaPor"] = null;
        props["CurrentSprint"] = 1;
        props["GameStarted"] = false;
        
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    [PunRPC]
    void ResetAllCardManagers()
    {
        Debug.Log("Resetando todos os card managers para todos os jogadores...");
        
        // Reset do SprintPlanningManager (remove cartas do PO)
        if (sprintPlanningManager != null)
        {
            sprintPlanningManager.ResetSprintPlanning();
            Debug.Log("SprintPlanningManager resetado");
        }

        // Reset do ImprevistoManager (remove cartas de Imprevisto)
        if (imprevistoManager != null)
        {
            imprevistoManager.ResetImprevistoManager();
            Debug.Log("ImprevistoManager resetado");
        }

        // Reset do EscolhaManager (remove cartas de Escolha)
        if (escolhaManager != null)
        {
            escolhaManager.ResetEscolhaManager();
            Debug.Log("EscolhaManager resetado");
        }

        // Reset do SprintRetrospectiveManager (remove cartas de Aprendizagem)
        if (sprintRetrospectiveManager != null)
        {
            sprintRetrospectiveManager.ResetSprintRetrospectiveManager();
            Debug.Log("SprintRetrospectiveManager resetado");
        }

        // Reset do SprintReviewManager (remove botões de avaliação)
        if (sprintReviewManager != null)
        {
            sprintReviewManager.ResetSprintReviewManager();
            Debug.Log("SprintReviewManager resetado");
        }
        
        Debug.Log("Reset de todos os card managers finalizado!");
    }

    #region Photon Callbacks

    public override void OnJoinedRoom()
    {
        UpdateButtonVisibility();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("IsProductOwner"))
        {
            UpdateButtonVisibility();
        }
    }

    #endregion
}
