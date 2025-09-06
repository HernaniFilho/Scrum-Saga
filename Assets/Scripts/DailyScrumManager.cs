using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class DailyScrumManager : MonoBehaviourPun
{
    [Header("Product Owner Manager")]
    private ProductOwnerManager productOwnerManager;

    [Header("UI Elements")]
    public UnityEngine.UI.Button startDailyButton;
    public TMP_Text waitingText;

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
            UpdateWaitingText();

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
            
            // Esconder botões de load até carregar desenhos
            HideLoadButtons();
            
            // Tentar carregar desenhos, se não conseguir, ficar tentando
            StartCoroutine(EnsureDrawingsLoad());
        }

        photonView.RPC("DailyIniciada", RpcTarget.All);
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
        }

        // Limpar display do nome do player para todos
        photonView.RPC("ClearPlayerNameForAll", RpcTarget.All);

        if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            gameStateManager.NextState();
        }

        hasStartedDaily = false;
        photonView.RPC("DailyTerminada", RpcTarget.All);
    }

    [PunRPC]
    void DailyIniciada()
    {
        hasStartedDaily = true;
    }

    [PunRPC]
    void DailyTerminada()
    {
        hasStartedDaily = false;
    }

    private System.Collections.IEnumerator EnsureDrawingsLoad()
    {
        int tentativas = 0;
        
        while (tentativas < 20) // Máximo 10 segundos tentando (20 * 0.5s)
        {
            CommandSaveSystem commandSaveSystem = FindObjectOfType<CommandSaveSystem>();
            int savedCount = commandSaveSystem != null ? commandSaveSystem.SavedSessions.Count : 0;
            
            if (savedCount > 0)
            {
                CanvasManager.Instance.ReplayAllPlayerDrawings();
                
                ShowLoadButtons();
                
                yield break;
            }
            
            tentativas++;
            yield return new UnityEngine.WaitForSeconds(0.5f);
        }
    }

    private void HideLoadButtons()
    {
        CommandSaveSystem commandSaveSystem = FindObjectOfType<CommandSaveSystem>();
        if (commandSaveSystem != null)
        {
            // Esconder todos os botões de load temporariamente
            commandSaveSystem.HideAllLoadButtons();
        }
    }

    private void ShowLoadButtons()
    {
        CommandSaveSystem commandSaveSystem = FindObjectOfType<CommandSaveSystem>();
        if (commandSaveSystem != null)
        {
            // Mostrar botões de load baseado nos desenhos carregados
            commandSaveSystem.RefreshSlotVisibility();
        }
    }

    [PunRPC]
    void ClearPlayerNameForAll()
    {
        CommandSaveSystem commandSaveSystem = FindObjectOfType<CommandSaveSystem>();
        if (commandSaveSystem != null)
        {
            commandSaveSystem.ClearPlayerNameDisplay();
        }
    }

    private void UpdateWaitingText()
    {
        if (waitingText == null || productOwnerManager == null) return;

        bool isLocalPlayerPO = productOwnerManager.IsLocalPlayerProductOwner();

        if (!isLocalPlayerPO && !hasStartedDaily)
        {
            waitingText.gameObject.SetActive(true);
            waitingText.text = "Aguardando PO começar a Daily...";
        }
        else
        {
            waitingText.text = "";
            waitingText.gameObject.SetActive(false);
        }
    }
}
