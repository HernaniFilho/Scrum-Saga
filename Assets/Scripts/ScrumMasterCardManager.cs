using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// Gerencia a carta Scrum Master durante as fases DailyScrum, Imprevisto, Escolha e Realização da Tarefa
/// Permite que jogadores que não são PO usem a carta uma única vez
/// </summary>
public class ScrumMasterCardManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements - Assignable in Unity Editor")]
    [SerializeField] private Button usarCartaScrumMasterButton;
    [SerializeField] private GameObject popupCartaContainer;
    [SerializeField] private Button confirmarCartaButton;
    [SerializeField] private Button cancelarCartaButton;
    [SerializeField] private GameObject avisoCartaContainer;
    [SerializeField] private Button fecharAvisoButton;
    [SerializeField] private GameObject avisoFimCartaContainer;
    [SerializeField] private Button fecharAvisoFimButton;
    
    [Header("Text Elements")]
    [SerializeField] private TMP_Text popupCartaTexto;
    [SerializeField] private TMP_Text avisoCartaTexto;
    [SerializeField] private TMP_Text avisoFimCartaTexto;
    
    [Header("Game References")]
    private ProductOwnerManager productOwnerManager;
    private GameStateManager gameStateManager;
    
    [Header("Network Keys")]
    private const string CARTA_SCRUM_MASTER_USADA_KEY = "CartaScrumMasterUsada";
    private const string CARTA_USADA_POR_KEY = "CartaScrumMasterUsadaPor";
    
    [Header("Configuration")]
    [SerializeField] private string textoConfirmacao = "A Carta Scrum Master pode ser usada para auxiliar a equipe durante as fases do jogo, permitindo tirar dúvidas com o PO.\n\nUma vez usada, não poderá ser utilizada novamente nesta partida.\n\nDeseja usar a carta agora?";
    [SerializeField] private string textoAvisoUsada = "A Carta Scrum Master foi utilizada por {NOME_DO_PLAYER}!\n\nAproveitem para tirar dúvidas com o PO até o fim da Realização da Tarefa!";
    [SerializeField] private string textoAvisoFim = "O tempo tempo de utilização da Carta Scrum Master acabou!\n\nA fase de Realização da Tarefa foi finalizada e não é mais possível tirar dúvidas com o PO usando a carta.";
    
    private bool cartaJaUsada = false;
    private bool avisoFimJaMostrado = false;
    private GameStateManager.GameState estadoAnterior = GameStateManager.GameState.Inicio;

    public static ScrumMasterCardManager Instance { get; private set; }

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
        CheckCartaStatus();
    }

    void Update()
    {
        if (productOwnerManager == null)
            productOwnerManager = FindObjectOfType<ProductOwnerManager>();

        if (gameStateManager == null) return;

        var currentState = gameStateManager.GetCurrentState();

        // Verificar mudança de estado para detectar fim da Realização da Tarefa
        CheckForRealizacaoTarefaEnd(currentState);
        
        // Verificar se está em uma das fases onde a carta pode ser usada
        bool isValidPhase = currentState == GameStateManager.GameState.DailyScrum ||
                           currentState == GameStateManager.GameState.Imprevisto ||
                           currentState == GameStateManager.GameState.Escolha ||
                           currentState == GameStateManager.GameState.RealizacaoTarefa;

        UpdateButtonVisibility(isValidPhase);
        
        estadoAnterior = currentState;
    }

    void InitializeReferences()
    {
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        gameStateManager = GameStateManager.Instance;
        
        if (gameStateManager == null)
            Debug.LogError("GameStateManager não encontrado!");
    }

    void SetupUI()
    {
        // Setup do botão principal
        if (usarCartaScrumMasterButton != null)
        {
            usarCartaScrumMasterButton.transform.parent.gameObject.SetActive(false);
            usarCartaScrumMasterButton.onClick.AddListener(OnUsarCartaButtonClicked);
        }

        // Setup do popup
        if (popupCartaContainer != null)
        {
            popupCartaContainer.SetActive(false);
            
            if (popupCartaTexto != null)
                popupCartaTexto.text = textoConfirmacao;
        }

        if (confirmarCartaButton != null)
        {
            confirmarCartaButton.onClick.AddListener(OnConfirmarCartaButtonClicked);
        }

        if (cancelarCartaButton != null)
        {
            cancelarCartaButton.onClick.AddListener(OnCancelarCartaButtonClicked);
        }

        // Setup do aviso
        if (avisoCartaContainer != null)
        {
            avisoCartaContainer.SetActive(false);
            
            if (avisoCartaTexto != null)
                avisoCartaTexto.text = textoAvisoUsada;
        }

        if (fecharAvisoButton != null)
        {
            fecharAvisoButton.onClick.AddListener(OnFecharAvisoButtonClicked);
        }

        // Setup do aviso de fim
        if (avisoFimCartaContainer != null)
        {
            avisoFimCartaContainer.SetActive(false);
            
            if (avisoFimCartaTexto != null)
                avisoFimCartaTexto.text = textoAvisoFim;
        }

        if (fecharAvisoFimButton != null)
        {
            fecharAvisoFimButton.onClick.AddListener(OnFecharAvisoFimButtonClicked);
        }
    }

    private void UpdateButtonVisibility(bool isValidPhase)
    {
        if (usarCartaScrumMasterButton == null || productOwnerManager == null) return;

        bool isLocalPlayerPO = productOwnerManager.IsLocalPlayerProductOwner();
        bool cartaDisponivel = !cartaJaUsada && !IsCartaJaUsadaGlobalmente();

        bool shouldShowButton = isValidPhase && !isLocalPlayerPO && cartaDisponivel;

        usarCartaScrumMasterButton.transform.parent.gameObject.SetActive(shouldShowButton);
    }

    private void CheckForRealizacaoTarefaEnd(GameStateManager.GameState currentState)
    {
        // Verificar se saímos da fase de Realização da Tarefa, a carta foi usada e ainda não mostramos o aviso
        if (estadoAnterior == GameStateManager.GameState.RealizacaoTarefa && 
            currentState != GameStateManager.GameState.RealizacaoTarefa &&
            IsCartaJaUsadaGlobalmente() && !avisoFimJaMostrado)
        {
            MostrarAvisoFimCarta();
            avisoFimJaMostrado = true;
        }
    }

    private void MostrarAvisoFimCarta()
    {
        if (avisoFimCartaContainer != null)
        {
            avisoFimCartaContainer.SetActive(true);
        }
    }

    private bool IsCartaJaUsadaGlobalmente()
    {
        if (!PhotonNetwork.InRoom) return false;
        
        return PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CARTA_SCRUM_MASTER_USADA_KEY) &&
               (bool)PhotonNetwork.CurrentRoom.CustomProperties[CARTA_SCRUM_MASTER_USADA_KEY];
    }

    private void OnUsarCartaButtonClicked()
    {
        Debug.Log("Botão 'Usar Carta Scrum Master' clicado!");

        if (IsCartaJaUsadaGlobalmente())
        {
            Debug.LogWarning("Carta Scrum Master já foi usada!");
            return;
        }

        if (popupCartaContainer != null)
        {
            popupCartaContainer.SetActive(true);
        }
    }

    private void OnConfirmarCartaButtonClicked()
    {
        Debug.Log("Confirmando uso da Carta Scrum Master");

        if (popupCartaContainer != null)
        {
            popupCartaContainer.SetActive(false);
        }

        cartaJaUsada = true;

        string playerName = PhotonNetwork.LocalPlayer.NickName;
        photonView.RPC("BroadcastCartaScrumMasterUsada", RpcTarget.All, playerName);
    }

    private void OnCancelarCartaButtonClicked()
    {
        Debug.Log("Cancelando uso da Carta Scrum Master");

        if (popupCartaContainer != null)
        {
            popupCartaContainer.SetActive(false);
        }
    }

    private void OnFecharAvisoButtonClicked()
    {
        Debug.Log("Fechando aviso da Carta Scrum Master");

        if (avisoCartaContainer != null)
        {
            avisoCartaContainer.SetActive(false);
        }
    }

    private void OnFecharAvisoFimButtonClicked()
    {
        Debug.Log("Fechando aviso de fim da Carta Scrum Master");

        if (avisoFimCartaContainer != null)
        {
            avisoFimCartaContainer.SetActive(false);
        }
    }

    [PunRPC]
    void BroadcastCartaScrumMasterUsada(string playerName)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props[CARTA_SCRUM_MASTER_USADA_KEY] = true;
            props[CARTA_USADA_POR_KEY] = playerName;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        Debug.Log($"Carta Scrum Master usada por: {playerName}");
        
        cartaJaUsada = true;

        if (usarCartaScrumMasterButton != null)
        {
            usarCartaScrumMasterButton.transform.parent.gameObject.SetActive(false);
        }

        if (avisoCartaContainer != null)
        {
            avisoCartaContainer.SetActive(true);
            
            if (avisoCartaTexto != null)
            {
                string textoComNome = textoAvisoUsada.Replace("{NOME_DO_PLAYER}", playerName);
                avisoCartaTexto.text = textoComNome;
            }
        }
    }

    private void CheckCartaStatus()
    {
        if (IsCartaJaUsadaGlobalmente())
        {
            cartaJaUsada = true;
            
            if (usarCartaScrumMasterButton != null)
            {
                usarCartaScrumMasterButton.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    public void ResetCartaStatus()
    {
        // RPC para resetar para todos os jogadores
        photonView.RPC("ResetCartaStatusRPC", RpcTarget.All);
        
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props[CARTA_SCRUM_MASTER_USADA_KEY] = null;
            props[CARTA_USADA_POR_KEY] = null;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    [PunRPC]
    void ResetCartaStatusRPC()
    {
        cartaJaUsada = false;
        avisoFimJaMostrado = false;

        if (popupCartaContainer != null)
            popupCartaContainer.SetActive(false);
            
        if (avisoCartaContainer != null)
            avisoCartaContainer.SetActive(false);
            
        if (avisoFimCartaContainer != null)
            avisoFimCartaContainer.SetActive(false);
    }

    public bool IsCartaUsada()
    {
        return cartaJaUsada || IsCartaJaUsadaGlobalmente();
    }

    public string GetPlayerQueUsouCarta()
    {
        if (!PhotonNetwork.InRoom) return null;
        
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CARTA_USADA_POR_KEY))
        {
            return (string)PhotonNetwork.CurrentRoom.CustomProperties[CARTA_USADA_POR_KEY];
        }
        
        return null;
    }

    #region Photon Callbacks

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(CARTA_SCRUM_MASTER_USADA_KEY))
        {
            bool cartaUsada = propertiesThatChanged[CARTA_SCRUM_MASTER_USADA_KEY] != null &&
                             (bool)propertiesThatChanged[CARTA_SCRUM_MASTER_USADA_KEY];

            if (cartaUsada)
            {
                cartaJaUsada = true;
                
                if (usarCartaScrumMasterButton != null)
                {
                    usarCartaScrumMasterButton.transform.parent.gameObject.SetActive(false);
                }
            }
        }
    }

    public override void OnJoinedRoom()
    {
        CheckCartaStatus();
    }

    #endregion
}
