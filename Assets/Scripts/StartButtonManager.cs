using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class StartButtonManager : MonoBehaviourPun
{
    [Header("UI References")]
    public Button startPhaseButton;
    public TMP_Text buttonText;
    private GameObject currentSprintContainer;
    public TMP_Text currentSprintText;

    [Header("Sprint Infos")]
    [SerializeField] private int currentSprint = 0;
    public int sprintsMax = 3;

    private ProductOwnerManager productOwnerManager;
    private GameStateManager gameStateManager;

    void Start()
    {
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        gameStateManager = GameStateManager.Instance;

        if (startPhaseButton != null)
        {
            startPhaseButton.onClick.AddListener(OnStartPhaseClicked);
        }

        if (currentSprintText != null)
        {
            currentSprintContainer = currentSprintText.transform.parent.gameObject;
        }

        UpdateButtonVisibility();
    }

    void Update()
    {
        UpdateButtonVisibility();
        UpdateCurrentSprint();
    }

    private void UpdateButtonVisibility()
    {
        if (productOwnerManager == null)
        {
            productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        }

        if (startPhaseButton == null || productOwnerManager == null || gameStateManager == null)
        {
            return;
        }

        bool shouldShowButton = ShouldShowButton();
        startPhaseButton.gameObject.SetActive(shouldShowButton);

        if (!shouldShowButton) return;

        if (currentSprint == 0 || currentSprint == sprintsMax)
        {
            buttonText.text = "Começar jogo";
        }
        else
        {
            buttonText.text = "Começar sprint";
        }
    }

    private void UpdateCurrentSprint()
    {
        if (currentSprintContainer == null || currentSprintText == null) return;

        if (gameStateManager != null && gameStateManager.GetCurrentState() == GameStateManager.GameState.Inicio)
        {
            currentSprintContainer.SetActive(false);
        }
        else
        {
            currentSprintContainer.SetActive(true);
            currentSprintText.text = $"Sprint: {currentSprint}/{sprintsMax}";
        }
    }

    private bool ShouldShowButton()
    {
        bool isLocalPlayerPO = productOwnerManager.IsLocalPlayerProductOwner();
        bool isStartPhase = gameStateManager.GetCurrentState() == GameStateManager.GameState.Inicio;
        bool isInRoom = PhotonNetwork.InRoom;

        return isLocalPlayerPO && isStartPhase && isInRoom;
    }

    public void OnStartPhaseClicked()
    {
        if (!productOwnerManager.IsLocalPlayerProductOwner())
        {
            return;
        }

        if (gameStateManager.GetCurrentState() != GameStateManager.GameState.Inicio)
        {
            return;
        }

        if ((currentSprint + 1) > sprintsMax)
        {
            setCurrentSprint(1);
        }
        else
        {
            setCurrentSprint(currentSprint + 1);
        }
        gameStateManager.NextState();
    }

    public int GetCurrentSprint()
    {
        return currentSprint;
    }

    private void setCurrentSprint(int sprint)
    {
        currentSprint = sprint;
        photonView.RPC("UpdateCurrentSprint", RpcTarget.All, currentSprint);
    }

    [PunRPC]
    void UpdateCurrentSprint(int sprint)
    {
        currentSprint = sprint;
    }

    public void ResetSprints()
    {
        setCurrentSprint(0);
        Debug.Log("Sprints resetadas para 0");
    }
}
