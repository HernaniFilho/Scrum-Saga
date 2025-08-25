using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class StartButtonManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public Button startPhaseButton;
    public TMP_Text buttonText;

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

        UpdateButtonVisibility();
    }

    void Update()
    {
        UpdateButtonVisibility();
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

        currentSprint++;
        if (currentSprint > sprintsMax)
        {
            currentSprint = 0;
        }
        gameStateManager.NextState();
    }

    public int GetCurrentSprint()
    {
        return currentSprint;
    }
}
