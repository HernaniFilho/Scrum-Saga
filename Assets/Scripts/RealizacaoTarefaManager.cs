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

    [Header("View Draft Button")]
    public UnityEngine.UI.Button viewDraftButton;
    public TMP_Text viewDraftButtonText;

    [Header("Game References")]
    private ProductOwnerManager productOwnerManager;
    private GameStateManager gameStateManager;
    private CanvasManager canvasManager;
    private TimerManager timerManager;
    private GameObject drawingArea;
    private UndoSystem undoSystem;

    [Header("Configuration")]
    public float realizacaoTimer = 105f; // 1m20 = 80 segundos

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
    private ShapeType lastSelectedShape = ShapeType.Rectangle; // Para restaurar o shape após cancelamento
    private bool isViewingDraft = false; // Para controlar se está vendo rascunho ou tarefa
    private List<DrawingCommand> taskCommands = new List<DrawingCommand>(); // Para salvar comandos da tarefa atual
    private CommandSaveSystem commandSaveSystem;
    private CommandReplaySystem commandReplaySystem;
    private int taskCommandStartIndex = 0; // Índice onde começaram os comandos da tarefa

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
                // Clear room properties when leaving RealizacaoTarefa phase
                ResetRealizacaoState();
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
            if (viewDraftButton != null)
                viewDraftButton.gameObject.SetActive(false);
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
        commandSaveSystem = FindObjectOfType<CommandSaveSystem>();
        commandReplaySystem = FindObjectOfType<CommandReplaySystem>();

        if (gameStateManager == null)
            Debug.LogError("GameStateManager não encontrado!");
        if (canvasManager == null)
            Debug.LogError("CanvasManager não encontrado!");
        if (timerManager == null)
            Debug.LogError("TimerManager não encontrado!");
        if (undoSystem == null)
            Debug.LogError("UndoSystem não encontrado!");
        if (commandSaveSystem == null)
            Debug.LogError("CommandSaveSystem não encontrado!");
        if (commandReplaySystem == null)
            Debug.LogError("CommandReplaySystem não encontrado!");
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

        if (viewDraftButton != null)
        {
            viewDraftButton.onClick.AddListener(OnViewDraftButtonClicked);
            viewDraftButton.gameObject.SetActive(false);
        }
    }

    public void StartRealizacaoPhase()
    {
        // Reset local state but don't clear room properties yet
        hasStartedRealizacao = false;
        timerRunning = false;
        currentPlayerIndex = 0;
        playersOrder.Clear();

        // Ativar canvas para todos, mas sem toolbar e desenho desabilitado
        if (canvasManager != null)
        {
            canvasManager.ActivateCanvasForAll();
            canvasManager.DeactivateToolbarForAll();
            canvasManager.DeactivateDrawingForAll();
        }

        bool isPO = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        bool realizacaoJaIniciada = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(REALIZACAO_INICIADA_KEY) &&
                                   (bool)PhotonNetwork.CurrentRoom.CustomProperties[REALIZACAO_INICIADA_KEY];

        if (isPO && !realizacaoJaIniciada)
        {
            // Mostrar botão para PO
            if (startRealizacaoButton != null)
                startRealizacaoButton.gameObject.SetActive(true);
                
            if (aguardandoText != null)
                aguardandoText.gameObject.SetActive(false);
        }
        else if (!isPO)
        {
            // Mostrar texto de aguardo para outros players
            if (aguardandoText != null)
            {
                aguardandoText.gameObject.SetActive(true);
                aguardandoText.text = "Aguardando PO começar...";
            }
            
            if (startRealizacaoButton != null)
                startRealizacaoButton.gameObject.SetActive(false);
        }

        // Se já iniciou, continuar processo
        if (realizacaoJaIniciada)
        {
            ContinueRealizacaoProcess();
            ShowViewDraftButton();
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

        // Limpar comandos da tarefa anterior
        taskCommands.Clear();

        // Continuar processo
        ContinueRealizacaoProcess();
        
        // Mostrar botão "Ver rascunho" se não for PO
        ShowViewDraftButton();
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
            taskCommandStartIndex = commandCountBeforeTurn; // Marcar onde começam os comandos deste player
        }

        // Habilitar desenho apenas para o player atual
        if (canvasManager != null)
        {
            if (isMyTurn && !isViewingDraft)
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
        
        ShowViewDraftButton();
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

        // Salvar o shape atual antes de desativar
        ShapeDrawer shapeDrawer = FindObjectOfType<ShapeDrawer>();
        if (shapeDrawer != null)
        {
            lastSelectedShape = shapeDrawer.currentShape;
        }

        // IMEDIATAMENTE desativar desenho para evitar múltiplas ações
        if (canvasManager != null)
        {
            canvasManager.DeactivateDrawingLocal();
            canvasManager.DeactivateToolbar();
        }

        // Esconder botão ver rascunho durante confirmação
        if (viewDraftButton != null)
        {
            viewDraftButton.gameObject.SetActive(false);
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

        // Salvar e sincronizar comandos da tarefa atual antes de confirmar
        SaveAndSyncCurrentPlayerTaskCommands();

        onConfirmFunction();
        
        // Confirmar desenho e passar a vez
        photonView.RPC("ProcessPlayerFinished", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        
        // Mostrar botão ver rascunho novamente
        ShowViewDraftButton();
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

        // Restaurar o shape que estava selecionado
        ShapeDrawer shapeDrawer = FindObjectOfType<ShapeDrawer>();
        ButtonStateManager buttonManager = FindObjectOfType<ButtonStateManager>();
        
        if (shapeDrawer != null && buttonManager != null)
        {
            shapeDrawer.SetShape(lastSelectedShape);
            buttonManager.UpdateSelection(lastSelectedShape);
        }
        
        // Mostrar botão ver rascunho novamente
        ShowViewDraftButton();
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

    [PunRPC]
    void HandleTimeUp()
    {
        // Se há um popup de confirmação aberto (player fez desenho mas não confirmou)
        if (confirmationPopupContainer != null && confirmationPopupContainer.activeSelf)
        {
            Debug.Log("Confirmando desenho automaticamente devido ao fim do tempo");
            
            // Fechar popup de confirmação
            confirmationPopupContainer.SetActive(false);
            
            // Executar confirmação automática
            if (onConfirmFunction != null)
            {
                // Salvar e sincronizar comandos da tarefa atual antes de confirmar
                SaveAndSyncCurrentPlayerTaskCommands();
                onConfirmFunction();
                
                // Confirmar desenho e passar a vez
                photonView.RPC("ProcessPlayerFinished", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
    }

    private void OnRealizacaoTimeComplete()
    {
        Debug.Log("Timer da Realização da Tarefa encerrado!");

        // Notificar todos os jogadores que o tempo acabou para confirmação automática
        photonView.RPC("HandleTimeUp", RpcTarget.All);

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

    #region View Draft Methods

    private void ShowViewDraftButton()
    {
        if (viewDraftButton == null || productOwnerManager == null) return;

        // Não mostrar para PO
        if (productOwnerManager.IsLocalPlayerProductOwner())
        {
            viewDraftButton.gameObject.SetActive(false);
            return;
        }

        // Só mostrar se tem rascunho do player
        if (HasPlayerDraft())
        {
            viewDraftButton.gameObject.SetActive(true);
            UpdateViewDraftButtonText();
        }
        else
        {
            viewDraftButton.gameObject.SetActive(false);
        }
    }

    private void UpdateViewDraftButtonText()
    {
        if (viewDraftButtonText == null) return;

        if (isViewingDraft)
        {
            viewDraftButtonText.text = "Ver tarefa";
        }
        else
        {
            viewDraftButtonText.text = "Ver rascunho";
        }
    }

    private bool HasPlayerDraft()
    {
        if (commandSaveSystem == null) return false;

        // Verificar se há rascunho do player atual
        var savedSessions = commandSaveSystem.SavedSessions;
        string localPlayerId = PhotonNetwork.LocalPlayer.UserId;
        
        string localPlayerName = PhotonNetwork.LocalPlayer.NickName;
        if (string.IsNullOrEmpty(localPlayerName))
        {
            localPlayerName = $"Player_{PhotonNetwork.LocalPlayer.ActorNumber}";
        }
        
        for (int i = 0; i < savedSessions.Count; i++)
        {
            var session = savedSessions[i];
            
            // Pegar dados do primeiro comando, igual ao GetPlayerNameFromSession
            string sessionPlayerId = GetPlayerIdFromSession(session);
            string sessionPlayerName = GetPlayerNameFromSession(session);
            
            // Primeiro tenta por playerId
            if (sessionPlayerId == localPlayerId)
            {
                return true;
            }
        }
        
        return false;
    }

    private string GetPlayerIdFromSession(DrawingSession session)
    {
        // Primeiro tenta pegar do primeiro comando
        if (session.commands != null && session.commands.Count > 0)
        {
            var firstCommand = session.commands[0];
            if (!string.IsNullOrEmpty(firstCommand.playerId))
            {
                return firstCommand.playerId;
            }
        }
        
        // Fallback para o playerId da sessão
        return session.playerId;
    }

    private string GetPlayerNameFromSession(DrawingSession session)
    {
        // Primeiro tenta pegar do primeiro comando
        if (session.commands != null && session.commands.Count > 0)
        {
            var firstCommand = session.commands[0];
            if (!string.IsNullOrEmpty(firstCommand.playerName) && firstCommand.playerName != "Local Player")
            {
                return firstCommand.playerName;
            }
        }
        
        // Fallback para o playerName da sessão
        return session.playerName;
    }

    private void OnViewDraftButtonClicked()
    {
        if (isViewingDraft)
        {
            // Está vendo rascunho, voltar para tarefa
            ShowCurrentTask();
        }
        else
        {
            // Está vendo tarefa, mostrar rascunho
            ShowPlayerDraft();
        }
    }

    private void ShowPlayerDraft()
    {
        if (canvasManager == null || commandSaveSystem == null) return;

        // Salvar o shape atual antes de desativar (como no cancelar desenho)
        ShapeDrawer shapeDrawer = FindObjectOfType<ShapeDrawer>();
        if (shapeDrawer != null)
        {
            lastSelectedShape = shapeDrawer.currentShape;
        }

        // Limpar canvas antes
        canvasManager.ClearCanvasLocal();

        // Desabilitar drawing e toolbar
        canvasManager.DeactivateDrawingLocal();
        canvasManager.DeactivateToolbar();

        // Encontrar e carregar rascunho do player
        var savedSessions = commandSaveSystem.SavedSessions;
        string localPlayerId = PhotonNetwork.LocalPlayer.UserId;
        string localPlayerName = PhotonNetwork.LocalPlayer.NickName;
        if (string.IsNullOrEmpty(localPlayerName))
        {
            localPlayerName = $"Player_{PhotonNetwork.LocalPlayer.ActorNumber}";
        }
        
        for (int i = 0; i < savedSessions.Count; i++)
        {
            var session = savedSessions[i];
            
            // Pegar dados do primeiro comando, igual ao GetPlayerNameFromSession
            string sessionPlayerId = GetPlayerIdFromSession(session);
            string sessionPlayerName = GetPlayerNameFromSession(session);
            
            // Primeiro tenta por playerId
            if (sessionPlayerId == localPlayerId)
            {
                commandSaveSystem.LoadDrawing(i, false);
                break;
            }
            
            // Se não encontrar por playerId, tenta por playerName (fallback para sessões locais)
            if (sessionPlayerId == "local" && sessionPlayerName == localPlayerName)
            {
                commandSaveSystem.LoadDrawing(i, false);
                break;
            }
        }

        isViewingDraft = true;
        UpdateViewDraftButtonText();
    }

    private void ShowCurrentTask()
    {
        if (canvasManager == null) return;

        // Limpar canvas antes
        canvasManager.ClearCanvasLocal();

        // Reproduzir comandos da tarefa atual
        ReplayTaskCommands();

        // Reativar drawing e toolbar apenas se for sua vez
        Player currentPlayer = playersOrder.Count > 0 ? playersOrder[currentPlayerIndex] : null;
        bool isMyTurn = currentPlayer != null && PhotonNetwork.LocalPlayer == currentPlayer;

        if (isMyTurn && timerRunning)
        {
            canvasManager.ActivateDrawingLocal();
            canvasManager.ActivateToolbar();
            
            // Restaurar o shape que estava selecionado (como no cancelar desenho)
            ShapeDrawer shapeDrawer = FindObjectOfType<ShapeDrawer>();
            ButtonStateManager buttonManager = FindObjectOfType<ButtonStateManager>();
            
            if (shapeDrawer != null && buttonManager != null)
            {
                shapeDrawer.SetShape(lastSelectedShape);
                buttonManager.UpdateSelection(lastSelectedShape);
            }
        }

        isViewingDraft = false;
        UpdateViewDraftButtonText();
    }

    private void SaveAndSyncCurrentPlayerTaskCommands()
    {
        if (CommandRecorder.Instance == null) return;

        DrawingSession session = CommandRecorder.Instance.GetCurrentSession();
        if (session != null && session.commands.Count > taskCommandStartIndex)
        {
            List<DrawingCommand> playerCommands = new List<DrawingCommand>();
            
            // Salvar apenas comandos do player atual da tarefa (do taskCommandStartIndex em diante)
            for (int i = taskCommandStartIndex; i < session.commands.Count; i++)
            {
                playerCommands.Add(session.commands[i]);
            }
            
            // Sincronizar comandos via RPC para todos os players
            if (playerCommands.Count > 0)
            {
                photonView.RPC("AddPlayerTaskCommands", RpcTarget.All, JsonUtility.ToJson(new SerializableCommandList(playerCommands)));
            }
        }
    }

    [PunRPC]
    void AddPlayerTaskCommands(string commandsJson)
    {
        try
        {
            SerializableCommandList commandList = JsonUtility.FromJson<SerializableCommandList>(commandsJson);
            
            // Adicionar comandos à lista geral da tarefa (acumular, não substituir)
            taskCommands.AddRange(commandList.commands);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AddPlayerTaskCommands] Erro ao deserializar comandos: {e.Message}");
        }
    }

    [System.Serializable]
    public class SerializableCommandList
    {
        public List<DrawingCommand> commands = new List<DrawingCommand>();
        
        public SerializableCommandList(List<DrawingCommand> commandList)
        {
            commands = commandList;
        }
    }

    private void ReplayTaskCommands()
    {
        if (taskCommands.Count == 0 || commandReplaySystem == null) return;

        // Reproduzir comandos da tarefa
        foreach (var command in taskCommands)
        {
            commandReplaySystem.ReplayCommand(command);
        }
    }

    public bool GetIsViewingDraft()
    {
        return isViewingDraft;
    }

    #endregion

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
                ShowViewDraftButton();
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
