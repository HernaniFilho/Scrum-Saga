using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class FimManager : MonoBehaviourPun
{
    [Header("Result UI")]
    public TMP_Text fimText;
    public TMP_Text feedbackText;
    
    [Header("Pause Button")]
    public Button pauseButton;

    [Header("Configuration")]
    public float fimPhaseDefaultDuration = 10f;
    public float fimPhaseFeedbackDuration = 20f;

    [Header("Game References")]
    [SerializeField] private GameObject tabuleiro;
    private GameStateManager gameStateManager;
    private StartButtonManager startButtonManager;
    private ProductOwnerManager productOwnerManager;
    
    private bool hasStartedFimPhase = false;

    public static FimManager Instance { get; private set; }

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
      ResetFimState();
    }

    void Update()
    {
      if (productOwnerManager == null)
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();
      if (startButtonManager == null)
        startButtonManager = FindObjectOfType<StartButtonManager>();

      if (gameStateManager == null) return;

      var currentState = gameStateManager.GetCurrentState();

      if (currentState == GameStateManager.GameState.Fim)
      {
        if (!hasStartedFimPhase)
        {
          StartFimPhase();
          hasStartedFimPhase = true;
        }
      }
      else
      {
        if (hasStartedFimPhase)
        {
          hasStartedFimPhase = false;
        }
      }
    }

    void InitializeReferences()
    {
      productOwnerManager = FindObjectOfType<ProductOwnerManager>();
      gameStateManager = FindObjectOfType<GameStateManager>();
      startButtonManager = FindObjectOfType<StartButtonManager>();
      
      if (tabuleiro == null)
        tabuleiro = GameObject.Find("Tabuleiro");
      if (gameStateManager == null)
        Debug.LogError("GameStateManager não encontrado!");
    }

    void SetupUI()
    {
      if (fimText != null)
      {
        fimText.gameObject.SetActive(false);
      }
      
      if (feedbackText != null)
      {
        feedbackText.gameObject.SetActive(false);
      }
      
      SetupPauseButton();
    }
    
    void SetupPauseButton()
    {
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
            pauseButton.onClick.RemoveAllListeners();
        }
    }

    public void StartFimPhase()
    {
      ResetFimState();
      
      if (tabuleiro != null)
        tabuleiro.SetActive(true);

      bool isLastSprint = false;
      if (startButtonManager != null && startButtonManager.GetCurrentSprint() == startButtonManager.sprintsMax)
        isLastSprint = true;

      if (fimText != null && feedbackText != null)
      {
        feedbackText.text = "";
        fimText.text = "";

        fimText.gameObject.SetActive(true);
        feedbackText.gameObject.SetActive(true);

        fimText.text = "Fim da Sprint! Prepare-se para a próxima!";
        if (isLastSprint && ScoreManager.Instance != null)
        {
          int lowestScore = ScoreManager.Instance.GetLowestScoreValue();

          fimText.text = "";
          if (tabuleiro != null)
            tabuleiro.SetActive(false);

          if (lowestScore >= 7)
          {
            feedbackText.text = "Feedback do Cliente: Estou extremamente satisfeito com o trabalho realizado pela sua equipe! Continue assim!";
          }
          else if (lowestScore >= 4)
          {
            feedbackText.text = "Feedback do Cliente: Sua equipe fez um bom trabalho, mas poderia ter se organizado melhor!";
          }
          else
          {
            feedbackText.text = "Feedback do Cliente: Não estou satisfeito com o projeto entregue pela equipe! Que tal tentar de novo?";
          }
        }
      }
      
      if (TimerManager.Instance != null && productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
      {
        TimerManager.Instance.StartTimer(isLastSprint ? fimPhaseFeedbackDuration : fimPhaseDefaultDuration, onTimerComplete, "FimTimer");
      }
      
      // Mostrar botão de pause para PO
      UpdatePauseButtonVisibility();
    }

    void onTimerComplete()
    {
      bool isLastSprint = false;
      if (startButtonManager != null && startButtonManager.GetCurrentSprint() == startButtonManager.sprintsMax)
        isLastSprint = true;

      photonView.RPC("EsconderTextos", RpcTarget.All);
        
      if (isLastSprint)
      {
        photonView.RPC("MostrarTabuleiro", RpcTarget.All);

        if (productOwnerManager != null)
        {
          productOwnerManager.ClearProductOwner();
        }
        
        if (ScoreManager.Instance != null)
        {
          ScoreManager.Instance.ResetScore();
        }

        // Reset da carta Scrum Master quando o jogo reinicia
        if (ScrumMasterCardManager.Instance != null)
        {
          ScrumMasterCardManager.Instance.ResetCartaStatus();
        }

        if (SelectedCardStorage.Instance != null)
        {
          SelectedCardStorage.Instance.ClearSelectedCard();
          SelectedCardStorage.Instance.ClearRejectedCard();
        }
      }

      if (gameStateManager != null)
      {
        gameStateManager.NextState();
      }
      
      // Atualizar botão de pause quando timer acaba
      UpdatePauseButtonVisibility();
    }

    [PunRPC]
    void EsconderTextos()
    {
      if (fimText != null)
      {
        fimText.gameObject.SetActive(false);
      }
      
      if (feedbackText != null)
      {
        feedbackText.gameObject.SetActive(false);
      }
    }

    [PunRPC]
    void MostrarTabuleiro()
    {
      if (tabuleiro != null)
      {
        tabuleiro.SetActive(true);
      }
    }

    public void ResetFimState()
    {
      hasStartedFimPhase = false;

      // Reset da UI local
      if (fimText != null)
        fimText.gameObject.SetActive(false);
      if (feedbackText != null)
        feedbackText.gameObject.SetActive(false);
        
      // Esconder botão de pause
      if (pauseButton != null)
        pauseButton.gameObject.SetActive(false);
    }
    
    // Método para atualizar visibilidade do botão de pause
    void UpdatePauseButtonVisibility()
    {
        if (pauseButton == null) return;
        
        bool isPO = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        bool hasActiveTimer = TimerManager.Instance != null && TimerManager.Instance.IsTimerActive();
        bool isFimPhase = gameStateManager != null && gameStateManager.GetCurrentState() == GameStateManager.GameState.Fim;
        
        // Mostrar botão se é PO, tem timer ativo E está na fase de Fim
        if (isPO && hasActiveTimer && isFimPhase)
        {
            pauseButton.gameObject.SetActive(true);
            
            // Configurar onClick baseado no estado atual
            pauseButton.onClick.RemoveAllListeners();
            
            if (TimerManager.Instance.IsPaused())
            {
                pauseButton.GetComponentInChildren<TMP_Text>().text = "Despausar";
                pauseButton.onClick.AddListener(() => UnpauseTimers());
            }
            else
            {
                pauseButton.GetComponentInChildren<TMP_Text>().text = "Pausar";
                pauseButton.onClick.AddListener(() => PauseTimers());
            }
        }
        else
        {
            pauseButton.gameObject.SetActive(false);
        }
    }
    
    // Métodos para controle de pause pelo PO
    public void PauseTimers()
    {
        if (TimerManager.Instance != null && productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            TimerManager.Instance.PauseTimer();
            UpdatePauseButtonVisibility();
        }
    }
    
    public void UnpauseTimers()
    {
        if (TimerManager.Instance != null && productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            TimerManager.Instance.UnpauseTimer();
            UpdatePauseButtonVisibility();
        }
    }
}
