using UnityEngine;
using TMPro;

public class DailyScrumManager : MonoBehaviour
{
    [Header("Product Owner Manager")]
    private ProductOwnerManager productOwnerManager;

    [Header("UI Elements")]
    public UnityEngine.UI.Button startDailyButton;

    private GameStateManager gameStateManager;
    private bool hasStartedDaily = false;

    void Start()
    {
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        gameStateManager = GameStateManager.Instance;

        if (startDailyButton != null)
        {
            startDailyButton.gameObject.SetActive(false);
            startDailyButton.onClick.AddListener(OnStartDailyButtonClicked);
        }
    }

    void Update()
    {
        if (productOwnerManager == null)
        {
            productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        }

        if (gameStateManager == null) return;

        var currentState = gameStateManager.GetCurrentState();

        if (currentState == GameStateManager.GameState.DailyScrum)
        {
            if (!hasStartedDaily &&
                productOwnerManager != null &&
                productOwnerManager.IsLocalPlayerProductOwner())
            {
                ShowStartButton();
            }
        }
        else
        {
            if (hasStartedDaily)
            {
                hasStartedDaily = false;
            }

            if (startDailyButton != null)
            {
                startDailyButton.gameObject.SetActive(false);
            }
        }
    }

    private void ShowStartButton()
    {
        if (startDailyButton != null)
        {
            startDailyButton.gameObject.SetActive(true);
        }
    }

    private void OnStartDailyButtonClicked()
    {
        Debug.Log("Botão 'Começar Daily' clicado!");

        hasStartedDaily = true;

        if (startDailyButton != null)
        {
            startDailyButton.gameObject.SetActive(false);
        }

        // Iniciar timer de 1 minuto (60 segundos)
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.StartTimer(60f, OnDailyTimeComplete, "DailyTimer");
        }

        // Ativar canvas para todos
        if (CanvasManager.Instance != null)
        {
            CanvasManager.Instance.ActivateCanvasForAll();
            CanvasManager.Instance.DeactivateToolbarForAll();
            CanvasManager.Instance.ResetForAll();
            CanvasManager.Instance.DeactivateDrawingForAll();
            CanvasManager.Instance.ActivateDrawingSaveUIForAll();
            
            // Reproduz todos os desenhos salvos dos players
            CanvasManager.Instance.ReplayAllPlayerDrawings();
        }
    }

    private void OnDailyTimeComplete()
    {
        Debug.Log("Timer da Daily Scrum encerrado!");

        if (CanvasManager.Instance != null)
        {
            CanvasManager.Instance.ActivateDrawingForAll();
            CanvasManager.Instance.DeactivateDrawingSaveUIForAll();
            CanvasManager.Instance.ActivateToolbarForAll();
            CanvasManager.Instance.ClearCanvasForAll();
            CanvasManager.Instance.DeactivateCanvasForAll();
            CanvasManager.Instance.ClearAllSavedDrawings();
        }

        if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            gameStateManager.NextState();
        }

        hasStartedDaily = false;
    }
}
