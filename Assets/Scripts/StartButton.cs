using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class StartButton : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public Button startPhaseButton;

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

        gameStateManager.NextState();
    }
}
