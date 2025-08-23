using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RealizacaoTarefaManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public UnityEngine.UI.Button startRealizacaoButton;
    public GameObject stopRealizacaoContainer;
    public UnityEngine.UI.Button stopRealizacaoButton;
    public TMP_Text aguardandoText;
    public TMP_Text vezDoPlayerText;
    
    [Header("Confirmation Popup")]
    public GameObject confirmationPopupContainer;
    public UnityEngine.UI.Button confirmDrawingButton;
    public UnityEngine.UI.Button cancelDrawingButton;

    [Header("Game References")]
    private ProductOwnerManager productOwnerManager;
    private GameStateManager gameStateManager;
    private CanvasManager canvasManager;
    private TimerManager timerManager;
    private GameObject drawingArea;
    private UndoSystem undoSystem;

    [Header("Configuration")]
    public float realizacaoTimer = 80f; // 1m20 = 80 segundos

    [Header("Network Keys")]
    private const string REALIZACAO_INICIADA_KEY = "RealizacaoIniciada";
    private const string CURRENT_PLAYER_INDEX_KEY = "CurrentPlayerIndex";
    private const string PLAYERS_ORDER_KEY = "PlayersOrder";

    private bool hasStartedRealizacao = false;
    private bool timerRunning = false;
    private List<Player> playersOrder = new List<Player>();
    private int currentPlayerIndex = 0;
    private int commandCountBeforeTurn = 0; // Para rastrear comandos da vez atual
    private Action onConfirmFunction = null;

    public static RealizacaoTarefaManager Instance { get; private set; }

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
        ResetRealizacaoState();
    }

    void Update()
    {
        if (productOwnerManager == null)
            productOwnerManager = FindObjectOfType<ProductOwnerManager>();

        if (gameStateManager == null) return;

        var currentState = gameStateManager.GetCurrentState();

        if (currentState == GameStateManager.GameState.RealizacaoTarefa)
        {
            if (!hasStartedRealizacao)
            {
                StartRealizacaoPhase();
                hasStartedRealizacao = true;
            }
        }
        else
        {
            if (hasStartedRealizacao)
            {
                hasStartedRealizacao = false;
                timerRunning = false;
            }

            if (startRealizacaoButton != null)
                startRealizacaoButton.gameObject.SetActive(false);
            if (stopRealizacaoContainer != null)
                stopRealizacaoContainer.gameObject.SetActive(false);
            if (aguardandoText != null)
                aguardandoText.gameObject.SetActive(false);
            if (vezDoPlayerText != null)
                vezDoPlayerText.transform.parent.gameObject.SetActive(false);
            if (confirmationPopupContainer != null)
                confirmationPopupContainer.SetActive(false);
        }
    }

    void InitializeReferences()
    {
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        gameStateManager = GameStateManager.Instance;
        canvasManager = CanvasManager.Instance;
        timerManager = TimerManager.Instance;
        drawingArea = GameObject.Find("DrawingArea");
        undoSystem = drawingArea.GetComponent<UndoSystem>();

        if (gameStateManager == null)
            Debug.LogError("GameStateManager não encontrado!");
        if (canvasManager == null)
            Debug.LogError("CanvasManager não encontrado!");
        if (timerManager == null)
            Debug.LogError("TimerManager não encontrado!");
        if (undoSystem == null)
            Debug.LogError("UndoSystem não encontrado!");
    }

    void SetupUI()
    {
        if (startRealizacaoButton != null)
        {
            startRealizacaoButton.gameObject.SetActive(false);
            startRealizacaoButton.onClick.AddListener(OnStartRealizacaoButtonClicked);
        }

        if (stopRealizacaoContainer != null && stopRealizacaoButton != null)
        {
            stopRealizacaoContainer.gameObject.SetActive(false);
            stopRealizacaoButton.onClick.AddListener(OnStopRealizacaoButtonClicked);
        }

        if (aguardandoText != null)
        {
            aguardandoText.gameObject.SetActive(false);
        }

        if (vezDoPlayerText != null)
        {
            vezDoPlayerText.transform.parent.gameObject.SetActive(false);
        }

        if (confirmationPopupContainer != null)
        {
            confirmationPopupContainer.SetActive(false);
        }

        if (confirmDrawingButton != null)
        {
            confirmDrawingButton.onClick.AddListener(OnConfirmDrawing);
        }

        if (cancelDrawingButton != null)
        {
            cancelDrawingButton.onClick.AddListener(OnCancelDrawing);
        }
    }

    public void StartRealizacaoPhase()
    {
        ResetRealizacaoState();

        // Ativar canvas para todos, mas sem toolbar e desenho desabilitado
        if (canvasManager != null)
        {
            canvasManager.ActivateCanvasForAll();
            canvasManager.DeactivateToolbarForAll();
            canvasManager.DeactivateDrawingForAll();
        }

        bool isPO = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        bool realizacaoJaIniciada = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(REALIZACAO_INICIADA_KEY);

        if (isPO && !realizacaoJaIniciada)
        {
            // Mostrar botão para PO
            if (startRealizacaoButton != null)
                startRealizacaoButton.gameObject.SetActive(true);
        }
        else
        {
            // Mostrar texto de aguardo para outros players
            if (aguardandoText != null)
            {
                aguardandoText.gameObject.SetActive(true);
                aguardandoText.text = "Aguardando PO começar...";
            }
        }

        // Se já iniciou, continuar processo
        if (realizacaoJaIniciada)
        {
            ContinueRealizacaoProcess();
        }
    }

    private void OnStartRealizacaoButtonClicked()
    {
        Debug.Log("Botão 'Começar Realização' clicado!");

        if (startRealizacaoButton != null)
            startRealizacaoButton.gameObject.SetActive(false);

        // Definir ordem dos players (excluindo PO)
        SetupPlayersOrder();
            
        if (stopRealizacaoContainer != null)
            stopRealizacaoContainer.gameObject.SetActive(true);

        // Iniciar processo via RPC
        photonView.RPC("BroadcastRealizacaoIniciada", RpcTarget.All);
    }

    private void OnStopRealizacaoButtonClicked()
    {
        Debug.Log("Botão 'Parar Realização' clicado!");

        if (stopRealizacaoContainer != null)
            stopRealizacaoContainer.gameObject.SetActive(false);

        if (timerManager != null)
            timerManager.EndTimer();
    }

    private void SetupPlayersOrder()
    {
        playersOrder.Clear();

        foreach (var player in PhotonNetwork.PlayerList)
        {
            // Excluir PO da ordem de desenho
            if (productOwnerManager != null && productOwnerManager.IsPlayerProductOwner(player))
                continue;

            playersOrder.Add(player);
        }

        // Sincronizar ordem dos players
        if (PhotonNetwork.IsMasterClient)
        {
            int[] playerActorNumbers = new int[playersOrder.Count];
            for (int i = 0; i < playersOrder.Count; i++)
            {
                playerActorNumbers[i] = playersOrder[i].ActorNumber;
            }

            Hashtable props = new Hashtable();
            props[PLAYERS_ORDER_KEY] = playerActorNumbers;
            props[CURRENT_PLAYER_INDEX_KEY] = 0;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    [PunRPC]
    void BroadcastRealizacaoIniciada()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props[REALIZACAO_INICIADA_KEY] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        Debug.Log("Realização da Tarefa iniciada!");
        
        // Esconder texto de aguardo
        if (aguardandoText != null)
            aguardandoText.gameObject.SetActive(false);

        // Iniciar timer de 1m20
        if (timerManager != null)
        {
            timerManager.StartTimer(realizacaoTimer, OnRealizacaoTimeComplete, "RealizacaoTimer");
            timerRunning = true;
        }

        // Continuar processo
        ContinueRealizacaoProcess();
    }

    private void ContinueRealizacaoProcess()
    {
        // Reconstruir ordem dos players a partir das propriedades da sala
        LoadPlayersOrderFromRoom();

        // Ativar desenho para o primeiro player
        SetCurrentPlayer();
    }

    private void LoadPlayersOrderFromRoom()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PLAYERS_ORDER_KEY))
        {
            int[] playerActorNumbers = (int[])PhotonNetwork.CurrentRoom.CustomProperties[PLAYERS_ORDER_KEY];
            playersOrder.Clear();

            foreach (int actorNumber in playerActorNumbers)
            {
                Player player = GetPlayerByActorNumber(actorNumber);
                if (player != null)
                    playersOrder.Add(player);
            }
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CURRENT_PLAYER_INDEX_KEY))
        {
            currentPlayerIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties[CURRENT_PLAYER_INDEX_KEY];
        }
    }

    private Player GetPlayerByActorNumber(int actorNumber)
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber == actorNumber)
                return player;
        }
        return null;
    }

    private void SetCurrentPlayer()
    {
        if (playersOrder.Count == 0) return;

        Player currentPlayer = playersOrder[currentPlayerIndex];
        bool isMyTurn = PhotonNetwork.LocalPlayer == currentPlayer;

        // Mostrar de quem é a vez
        if (vezDoPlayerText != null)
        {
            vezDoPlayerText.transform.parent.gameObject.SetActive(true);
            if (isMyTurn)
            {
                vezDoPlayerText.text = "Sua vez!";
            }
            else
            {
                vezDoPlayerText.text = $"Vez de: {currentPlayer.NickName}";
            }
        }

        // Salvar contagem de comandos antes da vez começar
        if (isMyTurn && CommandRecorder.Instance != null)
        {
            DrawingSession session = CommandRecorder.Instance.GetCurrentSession();
            commandCountBeforeTurn = session != null ? session.GetCommandCount() : 0;
        }

        // Habilitar desenho apenas para o player atual
        if (canvasManager != null)
        {
            if (isMyTurn)
            {
                canvasManager.ActivateToolbar();
                canvasManager.ActivateDrawingLocal();
            }
            else
            {
                canvasManager.DeactivateToolbar();
                canvasManager.DeactivateDrawingLocal();
            }
        }

        Debug.Log($"Vez do player: {currentPlayer.NickName} (Local: {isMyTurn})");
    }

    public void SyncDrawingInRealTime(GameObject shape)
    {
        if (!timerRunning) return;

        // Usar o sistema de CommandRecorder para sincronizar
        if (CommandRecorder.Instance != null && canvasManager != null)
        {
            // Solicitar que todos apliquem o último comando gravado
            canvasManager.ReplayLastCommandForAll();
        }
    }

    public void SyncFloodFillInRealTime(Vector2 point, Color color)
    {
        if (!timerRunning) return;

        // Usar o sistema de CommandRecorder para sincronizar flood fill
        if (CommandRecorder.Instance != null && canvasManager != null)
        {
            // Solicitar que todos apliquem o último comando gravado
            canvasManager.ReplayLastCommandForAll();
        }
    }

    public void OnPlayerFinishedDrawing(Action onConfirmFn)
    {
        // Chamado quando o player atual termina de desenhar
        if (!timerRunning) return; // Só funciona durante o timer ativo
        
        Player currentPlayer = playersOrder[currentPlayerIndex];
        bool isMyTurn = PhotonNetwork.LocalPlayer == currentPlayer;
        
        // Apenas o player atual pode finalizar seu turno
        if (!isMyTurn) return;

        // IMEDIATAMENTE desativar desenho para evitar múltiplas ações
        if (canvasManager != null)
        {
            canvasManager.DeactivateDrawingLocal();
            canvasManager.DeactivateToolbar();
        }

        // Mostrar popup de confirmação
        if (confirmationPopupContainer != null)
        {
            onConfirmFunction = onConfirmFn;
            confirmationPopupContainer.SetActive(true);
        }
    }

    public void OnConfirmDrawing()
    {
        // Esconder popup
        if (confirmationPopupContainer != null)
        {
            confirmationPopupContainer.SetActive(false);
        }

        onConfirmFunction();
        
        // Confirmar desenho e passar a vez
        photonView.RPC("ProcessPlayerFinished", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public void OnCancelDrawing()
    {
        // Esconder popup
        if (confirmationPopupContainer != null)
        {
            confirmationPopupContainer.SetActive(false);
        }

        // Desfazer comandos da vez atual
        if (undoSystem != null)
        {
            undoSystem.UndoLastAction();
        }

        // Reativar desenho para o mesmo player tentar novamente
        if (canvasManager != null)
        {
            canvasManager.ActivateToolbar();
            canvasManager.ActivateDrawingLocal();
        }
    }

    [PunRPC]
    void ProcessPlayerFinished(int playerActorNumber)
    {
        // Apenas Master Client processa mudanças de turno
        if (!PhotonNetwork.IsMasterClient) return;

        // Verificar se é realmente o player atual
        Player currentPlayer = playersOrder[currentPlayerIndex];
        if (currentPlayer.ActorNumber != playerActorNumber) return;

        Debug.Log($"Player {currentPlayer.NickName} terminou seu turno");

        // Passar para próximo player
        currentPlayerIndex = (currentPlayerIndex + 1) % playersOrder.Count;

        Hashtable props = new Hashtable();
        props[CURRENT_PLAYER_INDEX_KEY] = currentPlayerIndex;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        // Notificar mudança de turno
        photonView.RPC("BroadcastTurnChange", RpcTarget.All, currentPlayerIndex);
    }

    [PunRPC]
    void BroadcastTurnChange(int newPlayerIndex)
    {
        currentPlayerIndex = newPlayerIndex;
        SetCurrentPlayer();
    }

    private void OnRealizacaoTimeComplete()
    {
        Debug.Log("Timer da Realização da Tarefa encerrado!");

        timerRunning = false;

        if (canvasManager != null)
        {
            canvasManager.DeactivateToolbarForAll();
            canvasManager.DeactivateDrawingForAll();
        }

        if (vezDoPlayerText != null)
            vezDoPlayerText.transform.parent.gameObject.SetActive(false);
        if (stopRealizacaoContainer != null)
            stopRealizacaoContainer.gameObject.SetActive(false);

        if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            gameStateManager.NextState();
        }
    }

    public void ResetRealizacaoState()
    {
        hasStartedRealizacao = false;
        timerRunning = false;
        currentPlayerIndex = 0;
        playersOrder.Clear();

        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props[REALIZACAO_INICIADA_KEY] = null;
            props[CURRENT_PLAYER_INDEX_KEY] = null;
            props[PLAYERS_ORDER_KEY] = null;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        if (startRealizacaoButton != null)
            startRealizacaoButton.gameObject.SetActive(false);
        if (aguardandoText != null)
            aguardandoText.gameObject.SetActive(false);
        if (vezDoPlayerText != null)
            vezDoPlayerText.transform.parent.gameObject.SetActive(false);
        if (confirmationPopupContainer != null)
            confirmationPopupContainer.SetActive(false);
    }

    #region Photon Callbacks

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(REALIZACAO_INICIADA_KEY))
        {
            bool realizacaoIniciada = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(REALIZACAO_INICIADA_KEY) &&
                                     (bool)PhotonNetwork.CurrentRoom.CustomProperties[REALIZACAO_INICIADA_KEY];

            if (realizacaoIniciada && !timerRunning)
            {
                ContinueRealizacaoProcess();
            }
        }

        if (propertiesThatChanged.ContainsKey(CURRENT_PLAYER_INDEX_KEY))
        {
            LoadPlayersOrderFromRoom();
            SetCurrentPlayer();
        }
    }

    #endregion
}
