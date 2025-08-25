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

    [Header("Configuration")]
    public float fimPhaseDuration = 10f;

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
    }

    public void StartFimPhase()
    {
      ResetFimState();
      
      if (tabuleiro != null)
      tabuleiro.SetActive(true);

      if (fimText != null && feedbackText != null)
      {
        feedbackText.text = "";
        fimText.text = "";

        fimText.gameObject.SetActive(true);
        feedbackText.gameObject.SetActive(true);

        if (startButtonManager != null && startButtonManager.GetCurrentSprint() != startButtonManager.sprintsMax)
        {
          fimText.text = "Fim da Sprint! Prepare-se para a próxima!";
        }
        else if (ScoreManager.Instance != null)
        {
          int lowestScore = ScoreManager.Instance.GetLowestScoreValue();
      
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
        else
        {
          fimText.text = "Fim da Sprint! Prepare-se para a próxima!";
        }
      }
      
      if (TimerManager.Instance != null && PhotonNetwork.IsMasterClient)
      {
        TimerManager.Instance.StartTimer(fimPhaseDuration, onTimerComplete, "FimTimer");
      }
    }

    void onTimerComplete()
    {
      photonView.RPC("EsconderTextos", RpcTarget.All);

      if (tabuleiro != null)
        tabuleiro.SetActive(true);

      if (gameStateManager != null)
      {
        gameStateManager.NextState();
      }
        
      if (startButtonManager != null && startButtonManager.GetCurrentSprint() == startButtonManager.sprintsMax)
      {
        if (productOwnerManager != null)
        {
          productOwnerManager.ClearProductOwner();
        }
        
        if (ScoreManager.Instance != null)
        {
          ScoreManager.Instance.ResetScore();
        }
      }
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

    public void ResetFimState()
    {
      hasStartedFimPhase = false;

      if (TimerManager.Instance != null && PhotonNetwork.InRoom)
      {
        TimerManager.Instance.StopTimer();
      }

      // Reset da UI local
      if (fimText != null)
        fimText.gameObject.SetActive(false);
      if (feedbackText != null)
        feedbackText.gameObject.SetActive(false);
    }
}
