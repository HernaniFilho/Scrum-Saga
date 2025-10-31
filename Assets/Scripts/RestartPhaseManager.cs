using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RestartPhaseManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements - Assignable in Unity Editor")]
    [SerializeField] private Button restartPhaseButton;
    
    [Header("Popup Confirmation")]
    [SerializeField] private GameObject popupConfirmacaoContainer;
    [SerializeField] private Button confirmarRestartButton;
    [SerializeField] private Button cancelarRestartButton;
    [SerializeField] private TMP_Text popupTexto;
    
    [Header("Game References")]
    private GameStateManager gameStateManager;
    private ProductOwnerManager productOwnerManager;
    private SprintPlanningManager sprintPlanningManager;
    private DailyScrumManager dailyScrumManager;
    private ImprevistoManager imprevistoManager;
    private EscolhaManager escolhaManager;
    private RealizacaoTarefaManager realizacaoTarefaManager;
    private SprintReviewManager sprintReviewManager;
    private SprintRetrospectiveManager sprintRetrospectiveManager;
    private CanvasManager canvasManager;
    private TimerManager timerManager;
    private ScoreManager scoreManager;
    
    private GameStateManager.GameState currentPhaseToRestart;
    private Dictionary<string, int> scoreBackup = new Dictionary<string, int>();
    
    public static RestartPhaseManager Instance { get; private set; }
    public static bool IsRestartPopupOpen { get; private set; } = false;
    
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
        UpdateButtonVisibility();
        CheckAndSaveScoreOnPhaseChange();
    }
    
    private GameStateManager.GameState lastSavedState = GameStateManager.GameState.Inicio;
    
    private void CheckAndSaveScoreOnPhaseChange()
    {
        if (gameStateManager == null) return;
        
        GameStateManager.GameState currentState = gameStateManager.GetCurrentState();
        
        if (currentState != lastSavedState && currentState != GameStateManager.GameState.Inicio && currentState != GameStateManager.GameState.Fim)
        {
            SaveCurrentScore();
            lastSavedState = currentState;
        }
        
        if (currentState == GameStateManager.GameState.Inicio || currentState == GameStateManager.GameState.Fim)
        {
            lastSavedState = currentState;
        }
    }
    
    void InitializeReferences()
    {
        gameStateManager = GameStateManager.Instance;
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        sprintPlanningManager = FindObjectOfType<SprintPlanningManager>();
        dailyScrumManager = FindObjectOfType<DailyScrumManager>();
        imprevistoManager = ImprevistoManager.Instance;
        escolhaManager = EscolhaManager.Instance;
        realizacaoTarefaManager = FindObjectOfType<RealizacaoTarefaManager>();
        sprintReviewManager = FindObjectOfType<SprintReviewManager>();
        sprintRetrospectiveManager = SprintRetrospectiveManager.Instance;
        canvasManager = CanvasManager.Instance;
        timerManager = TimerManager.Instance;
        scoreManager = ScoreManager.Instance;
    }
    
    private void SaveCurrentScore()
    {
        scoreBackup.Clear();
        if (scoreManager != null && scoreManager.scoreboard != null)
        {
            foreach (var kvp in scoreManager.scoreboard)
            {
                scoreBackup[kvp.Key] = kvp.Value;
            }
            Debug.Log("Pontuação salva para backup");
        }
    }
    
    private void RestoreScore()
    {
        if (scoreManager != null && scoreBackup.Count > 0)
        {
            foreach (var kvp in scoreBackup)
            {
                if (scoreManager.scoreboard.ContainsKey(kvp.Key))
                {
                    scoreManager.scoreboard[kvp.Key] = kvp.Value;
                }
            }
            
            scoreManager.UpdateScore("Entrosamento", 0);
            
            Debug.Log("Pontuação restaurada do backup");
        }
    }
    
    void SetupUI()
    {
        if (restartPhaseButton != null)
        {
            restartPhaseButton.onClick.AddListener(OnRestartPhaseButtonClicked);
        }
        
        if (popupConfirmacaoContainer != null)
        {
            popupConfirmacaoContainer.SetActive(false);
        }
        
        if (confirmarRestartButton != null)
        {
            confirmarRestartButton.onClick.AddListener(OnConfirmarRestartClicked);
        }
        
        if (cancelarRestartButton != null)
        {
            cancelarRestartButton.onClick.AddListener(OnCancelarRestartClicked);
        }
    }
    
    private void UpdateButtonVisibility()
    {
        if (restartPhaseButton == null || productOwnerManager == null || gameStateManager == null) return;
        
        bool isLocalPlayerPO = productOwnerManager.IsLocalPlayerProductOwner();
        bool isInPhase = gameStateManager.GetCurrentState() != GameStateManager.GameState.Inicio && 
                         gameStateManager.GetCurrentState() != GameStateManager.GameState.Fim;
        bool shouldShowButton = isLocalPlayerPO && PhotonNetwork.InRoom && isInPhase;
        
        restartPhaseButton.transform.parent.gameObject.SetActive(shouldShowButton);
    }
    
    private void OnRestartPhaseButtonClicked()
    {
        if (gameStateManager == null) return;
        
        currentPhaseToRestart = gameStateManager.GetCurrentState();
        string phaseName = GetPhaseDisplayName(currentPhaseToRestart);
        
        if (popupTexto != null)
        {
            popupTexto.text = $"Tem certeza que deseja reiniciar a fase atual?\n\n<b>{phaseName}</b>\n\nTodos os jogadores voltarão ao início desta fase.";
        }
        
        IsRestartPopupOpen = true;
        if (popupConfirmacaoContainer != null)
        {
            popupConfirmacaoContainer.SetActive(true);
        }
        
        Debug.Log($"Botão de reiniciar fase clicado - Fase: {phaseName}");
    }
    
    private void OnConfirmarRestartClicked()
    {
        IsRestartPopupOpen = false;
        if (popupConfirmacaoContainer != null)
        {
            popupConfirmacaoContainer.SetActive(false);
        }
        
        Debug.Log($"Confirmando reinício da fase: {currentPhaseToRestart}");
        
        photonView.RPC("RestartCurrentPhase", RpcTarget.All, (int)currentPhaseToRestart);
    }
    
    private void OnCancelarRestartClicked()
    {
        IsRestartPopupOpen = false;
        if (popupConfirmacaoContainer != null)
        {
            popupConfirmacaoContainer.SetActive(false);
        }
        
        Debug.Log("Cancelando reinício de fase");
    }
    
    [PunRPC]
    void RestartCurrentPhase(int phaseIndex)
    {
        GameStateManager.GameState phase = (GameStateManager.GameState)phaseIndex;
        Debug.Log($"Reiniciando fase: {phase}");
        
        switch (phase)
        {
            case GameStateManager.GameState.SprintPlanning:
                RestartSprintPlanning();
                break;
                
            case GameStateManager.GameState.DailyScrum:
                RestartDailyScrum();
                break;
                
            case GameStateManager.GameState.Imprevisto:
                RestartImprevisto();
                break;
                
            case GameStateManager.GameState.Escolha:
                RestartEscolha();
                break;
                
            case GameStateManager.GameState.RealizacaoTarefa:
                RestartRealizacaoTarefa();
                break;
                
            case GameStateManager.GameState.SprintReview:
                RestartSprintReview();
                break;
                
            case GameStateManager.GameState.SprintRetrospective:
                RestartSprintRetrospective();
                break;
                
            default:
                Debug.LogWarning($"Fase {phase} não possui lógica de reinício");
                break;
        }
    }
    
    private void RestartSprintPlanning()
    {
        Debug.Log("Reiniciando Sprint Planning...");
        
        if (timerManager != null)
        {
            timerManager.StopTimer();
        }
        
        if (canvasManager != null)
        {
            canvasManager.ClearCanvasForAll();
            canvasManager.DeactivateCanvasForAll();
        }
        
        CommandSaveSystem commandSaveSystem = FindObjectOfType<CommandSaveSystem>();
        if (commandSaveSystem != null)
        {
            commandSaveSystem.ClearAllSavedSessions();
        }
        
        CommandRecorder commandRecorder = FindObjectOfType<CommandRecorder>();
        if (commandRecorder != null)
        {
            commandRecorder.ClearCurrentSession();
            commandRecorder.StartNewSession();
        }
        
        if (sprintPlanningManager != null)
        {
            sprintPlanningManager.ResetSprintPlanning();
        }
        
        if (gameStateManager != null)
        {
            gameStateManager.ChangeState(GameStateManager.GameState.SprintPlanning);
        }
    }
    
    private void RestartDailyScrum()
    {
        Debug.Log("Reiniciando Daily Scrum...");
        
        if (timerManager != null)
        {
            timerManager.StopTimer();
        }
        
        if (canvasManager != null)
        {
            canvasManager.ClearCanvasForAll();
            canvasManager.DeactivateCanvasForAll();
        }
        
        CommandSaveSystem commandSaveSystem = FindObjectOfType<CommandSaveSystem>();
        if (commandSaveSystem != null)
        {
            commandSaveSystem.ClearPlayerNameDisplay();
            commandSaveSystem.HideAllLoadButtons();
        }
        
        if (dailyScrumManager != null)
        {
            dailyScrumManager.ResetDailyScrum();
        }
        
        if (gameStateManager != null)
        {
            gameStateManager.ChangeState(GameStateManager.GameState.DailyScrum);
        }
    }
    
    private void RestartImprevisto()
    {
        Debug.Log("Reiniciando Imprevisto...");
        
        RestoreScore();
        
        if (imprevistoManager != null)
        {
            imprevistoManager.ResetImprevistoManager();
        }
        
        if (gameStateManager != null)
        {
            gameStateManager.ChangeState(GameStateManager.GameState.Imprevisto);
        }
    }
    
    private void RestartEscolha()
    {
        Debug.Log("Reiniciando Escolha...");
        
        RestoreScore();
        
        if (escolhaManager != null)
        {
            escolhaManager.ResetEscolhaManager();
        }
        
        if (gameStateManager != null)
        {
            gameStateManager.ChangeState(GameStateManager.GameState.Escolha);
        }
    }
    
    private void RestartRealizacaoTarefa()
    {
        Debug.Log("Reiniciando Realização da Tarefa...");
        
        if (timerManager != null)
        {
            timerManager.StopTimer();
        }
        
        if (canvasManager != null)
        {
            canvasManager.ClearCanvasForAll();
            canvasManager.DeactivateCanvasForAll();
        }
        
        if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            Hashtable props = new Hashtable();
            props["RealizacaoIniciada"] = null;
            props["CurrentPlayerIndex"] = null;
            props["PlayersOrder"] = null;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        
        if (realizacaoTarefaManager != null)
        {
            PhotonView pv = realizacaoTarefaManager.GetComponent<PhotonView>();
            if (pv != null)
            {
                pv.RPC("ResetRealizacaoTarefaRPC", RpcTarget.All);
            }
            else
            {
                realizacaoTarefaManager.ResetRealizacaoTarefa();
            }
        }
        
        if (gameStateManager != null)
        {
            gameStateManager.ChangeState(GameStateManager.GameState.RealizacaoTarefa);
        }
    }
    
    private void RestartSprintReview()
    {
        Debug.Log("Reiniciando Sprint Review...");
        
        if (timerManager != null)
        {
            timerManager.StopTimer();
        }
        
        RestoreScore();
        
        if (sprintReviewManager != null)
        {
            sprintReviewManager.ResetSprintReviewManager();
        }
        
        if (gameStateManager != null)
        {
            gameStateManager.ChangeState(GameStateManager.GameState.SprintReview);
        }
    }
    
    private void RestartSprintRetrospective()
    {
        Debug.Log("Reiniciando Sprint Retrospective...");
        
        if (sprintRetrospectiveManager != null)
        {
            sprintRetrospectiveManager.ResetSprintRetrospectiveManager();
        }
        
        if (gameStateManager != null)
        {
            gameStateManager.ChangeState(GameStateManager.GameState.SprintRetrospective);
        }
    }
    
    private string GetPhaseDisplayName(GameStateManager.GameState state)
    {
        switch (state)
        {
            case GameStateManager.GameState.SprintPlanning: return "Sprint Planning";
            case GameStateManager.GameState.DailyScrum: return "Daily Scrum";
            case GameStateManager.GameState.Imprevisto: return "Imprevisto";
            case GameStateManager.GameState.Escolha: return "Escolha";
            case GameStateManager.GameState.RealizacaoTarefa: return "Realização da Tarefa";
            case GameStateManager.GameState.SprintReview: return "Sprint Review";
            case GameStateManager.GameState.SprintRetrospective: return "Sprint Retrospective";
            default: return state.ToString();
        }
    }
    
    #region Photon Callbacks
    
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("IsProductOwner"))
        {
            UpdateButtonVisibility();
        }
    }
    
    #endregion
}
