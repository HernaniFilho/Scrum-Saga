using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class LeaveRoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements - Assignable in Unity Editor")]
    [SerializeField] private Button leaveRoomButton;

    [Header("Popup Confirmation")]
    [SerializeField] private GameObject popupLeaveRoomContainer;
    [SerializeField] private Button confirmarLeaveRoomButton;
    [SerializeField] private Button cancelarLeaveRoomButton;

    private ProductOwnerManager productOwnerManager;

    void Start()
    {
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();

        if (leaveRoomButton != null)
        {
            leaveRoomButton.onClick.AddListener(OnLeaveRoomButtonClicked);
        }

        SetupPopup();

        UpdateButtonVisibility();
    }

    void SetupPopup()
    {
        if (popupLeaveRoomContainer != null)
        {
            popupLeaveRoomContainer.SetActive(false);
        }

        if (confirmarLeaveRoomButton != null)
        {
            confirmarLeaveRoomButton.onClick.AddListener(OnConfirmarLeaveRoomClicked);
        }

        if (cancelarLeaveRoomButton != null)
        {
            cancelarLeaveRoomButton.onClick.AddListener(OnCancelarLeaveRoomClicked);
        }
    }

    void Update()
    {
        UpdateButtonVisibility();
    }

    private void UpdateButtonVisibility()
    {
        if (leaveRoomButton == null) return;

        bool shouldShowButton = PhotonNetwork.InRoom;
        leaveRoomButton.transform.parent.gameObject.SetActive(shouldShowButton);
    }

    private void OnLeaveRoomButtonClicked()
    {
        if (!PhotonNetwork.InRoom) return;

        if (popupLeaveRoomContainer != null)
        {
            popupLeaveRoomContainer.SetActive(true);
        }
    }

    private void OnConfirmarLeaveRoomClicked()
    {
        if (popupLeaveRoomContainer != null)
        {
            popupLeaveRoomContainer.SetActive(false);
        }

        if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            productOwnerManager.ClearProductOwner();
        }

        Debug.Log("Saindo da sala...");
        PhotonNetwork.LeaveRoom();
    }

    private void OnCancelarLeaveRoomClicked()
    {
        if (popupLeaveRoomContainer != null)
        {
            popupLeaveRoomContainer.SetActive(false);
        }
    }

    #region Photon Callbacks

    public override void OnLeftRoom()
    {
        Debug.Log("Você saiu da sala");
        UpdateButtonVisibility();
    }

    public override void OnJoinedRoom()
    {
        UpdateButtonVisibility();
    }

    #endregion
}
