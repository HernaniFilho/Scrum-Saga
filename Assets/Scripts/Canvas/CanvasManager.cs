using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Linq;
using System.Collections.Generic;
using System;

[System.Serializable]
public class SessionsListWrapper
{
    public DrawingSession[] sessions;
}

public class CanvasManager : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Game Board")]
    [SerializeField] private GameObject tabuleiro;
    
    [Header("Canvas Components")]
    [SerializeField] private GameObject canvasContainer;
    [SerializeField] private GameObject canvasDrawingBoard;
    [SerializeField] private GameObject canvasToolbar;
    [SerializeField] private GameObject drawingSaveSlotsContainer;
    
    private const string CANVAS_STATE_KEY = "CanvasActive";
    private const string TOOLBAR_STATE_KEY = "ToolbarActive";
    private const string CLEAR_CANVAS_KEY = "ClearCanvas";
    private const string RESET_SHAPE_KEY = "ResetShape";
    private const string DRAWING_DISABLED_KEY = "DrawingDisabled";
    private const string SAVE_UI_KEY = "SaveUI";
    
    [Header("Network Sync")]  
    private List<DrawingSession> receivedSessions = new List<DrawingSession>();
    private List<DrawingSession> finalSessionsList = new List<DrawingSession>();
    private int expectedSessionsCount = 0;
    
    [Header("RPC Chunking")]
    private Dictionary<string, Dictionary<int, string>> receivedChunks = new Dictionary<string, Dictionary<int, string>>();
    private Dictionary<string, int> expectedChunks = new Dictionary<string, int>();
    
    public static CanvasManager Instance { get; private set; }
    public List<DrawingSession> FinalSessionsList => finalSessionsList;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Inicializa estruturas de sincronização
        receivedSessions = new List<DrawingSession>();
        finalSessionsList = new List<DrawingSession>();
        expectedSessionsCount = 0;
        receivedChunks = new Dictionary<string, Dictionary<int, string>>();
        expectedChunks = new Dictionary<string, int>();
    }

    void Start()
    {
        if (tabuleiro == null)
            tabuleiro = GameObject.Find("Tabuleiro");
        if (canvasContainer == null)
            canvasContainer = GameObject.Find("Canvas Container");

        if (canvasContainer != null)
        {
            if (canvasToolbar == null)
                canvasToolbar = FindChildByName(canvasContainer.transform, "Toolbar")?.gameObject;
            if (drawingSaveSlotsContainer == null)
                drawingSaveSlotsContainer = FindChildByName(canvasContainer.transform, "DrawingSave Slots Container")?.gameObject;
        }

        LoadStatesFromRoom();
    }

    public void ActivateCanvas()
    {
        ApplyCanvasState(true);
    }
    
    public void ActivateCanvasForAll()
    {
        BroadcastCanvasState(true);
    }
    
    public void ActivateCanvasForOthers()
    {
        BroadcastCanvasStateExcludingSelf(true);
    }
    
    public void BroadcastCanvasState(bool isActive)
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[CANVAS_STATE_KEY] = isActive;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log($"Estado do canvas {(isActive ? "ativado" : "desativado")} enviado para todos os jogadores");
        }
        else
        {
            ApplyCanvasState(isActive);
        }
    }
    
    public void BroadcastCanvasStateExcludingSelf(bool isActive)
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[CANVAS_STATE_KEY] = isActive;
            props[CANVAS_STATE_KEY + "_exclude"] = PhotonNetwork.LocalPlayer.UserId;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log($"Estado do canvas {(isActive ? "ativado" : "desativado")} enviado para outros jogadores");
        }
    }
    
    private void ApplyCanvasState(bool isActive)
    {
        if (isActive)
        {
            if (tabuleiro != null)
            {
                tabuleiro.SetActive(false);
            }

            if (canvasDrawingBoard != null)
            {
                canvasDrawingBoard.SetActive(true);
            }

            if (canvasToolbar != null)
            {
                canvasToolbar.SetActive(true);
            }
            
            Debug.Log("Canvas ativado!");
        }
        else
        {
            if (tabuleiro != null)
            {
                tabuleiro.SetActive(true);
            }
            
            if (canvasDrawingBoard != null)
            {
                canvasDrawingBoard.SetActive(false);
            }

            if (canvasToolbar != null)
            {
                canvasToolbar.SetActive(false);
            }
            
            Debug.Log("Canvas desativado!");
        }
    }

    public void DeactivateCanvas()
    {
        ApplyCanvasState(false);
    }
    
    public void DeactivateCanvasForAll()
    {
        BroadcastCanvasState(false);
    }

    public void ActivateToolbar()
    {
        ApplyToolbarState(true);
    }
    
    public void ActivateToolbarForAll()
    {
        BroadcastToolbarState(true);
    }

    public void DeactivateToolbar()
    {
        ApplyToolbarState(false);
    }
    
    public void DeactivateToolbarForAll()
    {
        BroadcastToolbarState(false);
    }
    
    public void BroadcastToolbarState(bool isActive)
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[TOOLBAR_STATE_KEY] = isActive;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log($"Estado da toolbar {(isActive ? "ativada" : "desativada")} enviado para todos os jogadores");
        }
        else
        {
            ApplyToolbarState(isActive);
        }
    }

    public void ClearCanvasForAll()
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[CLEAR_CANVAS_KEY] = System.DateTime.Now.Ticks; // Usa timestamp para forçar atualização
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log("Comando de limpar canvas enviado para todos os jogadores");
        }
        else
        {
            ClearCanvasLocal();
        }
    }

    private void ClearCanvasLocal()
    {
        ShapeDrawer shapeDrawer = FindObjectOfType<ShapeDrawer>();
        if (shapeDrawer != null)
        {
            shapeDrawer.ClearAll();
            Debug.Log("Canvas limpo localmente");
        }
    }

    public void ResetForAll()
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[RESET_SHAPE_KEY] = System.DateTime.Now.Ticks;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log("Comando de reset para Rectangle enviado para todos os jogadores");
        }
        else
        {
            ResetShapeLocal();
        }
    }

    private void ResetShapeLocal()
    {
        ShapeDrawer shapeDrawer = FindObjectOfType<ShapeDrawer>();
        if (shapeDrawer != null)
        {
            shapeDrawer.SetShapeToRectangle();
            Debug.Log("Shape resetado para Rectangle localmente");
        }
    }

    public void DeactivateDrawingForAll()
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[DRAWING_DISABLED_KEY] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log("Desenho desativado para todos os jogadores");
        }
        else
        {
            DeactivateDrawingLocal();
        }
    }

    public void ActivateDrawingForAll()
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[DRAWING_DISABLED_KEY] = false;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log("Desenho ativado para todos os jogadores");
        }
        else
        {
            ActivateDrawingLocal();
        }
    }

    private void DeactivateDrawingLocal()
    {
        ShapeDrawer shapeDrawer = FindObjectOfType<ShapeDrawer>();
        if (shapeDrawer != null)
        {
            shapeDrawer.enabled = false;
            Debug.Log("Desenho desativado localmente");
        }
    }

    private void ActivateDrawingLocal()
    {
        ShapeDrawer shapeDrawer = FindObjectOfType<ShapeDrawer>();
        if (shapeDrawer != null)
        {
            shapeDrawer.enabled = true;
            Debug.Log("Desenho ativado localmente");
        }
    }

    public void DeactivateDrawingSaveUIForAll()
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[SAVE_UI_KEY] = false;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log("Comando de desativar DrawingSaveUI enviado para todos os jogadores");
        }
        else
        {
            DeactivateDrawingSaveUILocal();
        }
    }

    public void ActivateDrawingSaveUIForAll()
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[SAVE_UI_KEY] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log("Comando de ativar DrawingSaveUI enviado para todos os jogadores");
        }
        else
        {
            ActivateDrawingSaveUILocal();
        }
    }

    private void DeactivateDrawingSaveUILocal()
    {
        if (drawingSaveSlotsContainer != null)
        {
            drawingSaveSlotsContainer.SetActive(false);
            Debug.Log("DrawingSave Slots desativado localmente");
        }
        else
        {
            Debug.LogWarning("DrawingSave Slots não encontrado!");
        }
    }

    private void ActivateDrawingSaveUILocal()
    {
        if (drawingSaveSlotsContainer != null)
        {
            drawingSaveSlotsContainer.SetActive(true);
            Debug.Log("DrawingSave Slots ativado localmente");
        }
        else
        {
            Debug.LogWarning("DrawingSave Slots não encontrado!");
        }
    }
    
    private void ApplyToolbarState(bool isActive)
    {
        if (canvasToolbar != null)
        {
            canvasToolbar.SetActive(isActive);
        }
        
        Debug.Log($"Canvas Toolbar {(isActive ? "ativada" : "desativada")}!");
    }
    
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(CANVAS_STATE_KEY))
        {
            bool isActive = (bool)propertiesThatChanged[CANVAS_STATE_KEY];
            
            // Verifica se a propriedade tem exclusão (para o Product Owner)
            string excludePlayerKey = CANVAS_STATE_KEY + "_exclude";
            if (propertiesThatChanged.ContainsKey(excludePlayerKey))
            {
                string excludedPlayerId = (string)propertiesThatChanged[excludePlayerKey];
                if (PhotonNetwork.LocalPlayer.UserId == excludedPlayerId)
                {
                    // Este jogador deve ser excluído, então não aplica o estado
                    return;
                }
            }
            
            ApplyCanvasState(isActive);
        }
        
        if (propertiesThatChanged.ContainsKey(TOOLBAR_STATE_KEY))
        {
            bool isActive = (bool)propertiesThatChanged[TOOLBAR_STATE_KEY];
            ApplyToolbarState(isActive);
        }

        if (propertiesThatChanged.ContainsKey(CLEAR_CANVAS_KEY))
        {
            ClearCanvasLocal();
        }

        if (propertiesThatChanged.ContainsKey(RESET_SHAPE_KEY))
        {
            ResetShapeLocal();
        }

        if (propertiesThatChanged.ContainsKey(DRAWING_DISABLED_KEY))
        {
            bool isDisabled = (bool)propertiesThatChanged[DRAWING_DISABLED_KEY];
            if (isDisabled)
            {
                DeactivateDrawingLocal();
            }
            else
            {
                ActivateDrawingLocal();
            }
        }

        if (propertiesThatChanged.ContainsKey(SAVE_UI_KEY))
        {
            bool isActive = (bool)propertiesThatChanged[SAVE_UI_KEY];
            if (isActive)
            {
                ActivateDrawingSaveUILocal();
            }
            else
            {
                DeactivateDrawingSaveUILocal();
            }
        }
    }
    
    private void LoadStatesFromRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CANVAS_STATE_KEY))
            {
                bool currentCanvasState = (bool)PhotonNetwork.CurrentRoom.CustomProperties[CANVAS_STATE_KEY];
                ApplyCanvasState(currentCanvasState);
                Debug.Log($"Estado do canvas carregado da sala: {(currentCanvasState ? "ativado" : "desativado")}");
            }
            
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(TOOLBAR_STATE_KEY))
            {
                bool currentToolbarState = (bool)PhotonNetwork.CurrentRoom.CustomProperties[TOOLBAR_STATE_KEY];
                ApplyToolbarState(currentToolbarState);
                Debug.Log($"Estado da toolbar carregado da sala: {(currentToolbarState ? "ativada" : "desativada")}");
            }
        }
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log("Entrou na sala, carregando estados atuais...");
        LoadStatesFromRoom();
    }

    public void SaveAndSyncAllPlayerDrawings()
    {
        UnityEngine.Debug.Log("SaveAndSyncAllPlayerDrawings iniciado!");
        
        // Limpa estruturas anteriores
        receivedSessions.Clear();
        finalSessionsList.Clear();
        
        ProductOwnerManager poManager = FindObjectOfType<ProductOwnerManager>();
        if (poManager != null && poManager.IsLocalPlayerProductOwner())
        {
            UnityEngine.Debug.Log("PO iniciando coleta centralizada de desenhos");
            
            // PO define quantos players esperar
            expectedSessionsCount = PhotonNetwork.PlayerList.Length;
            UnityEngine.Debug.Log($"PO esperando {expectedSessionsCount} sessões");
            
            // Solicita que todos os players (incluindo ele mesmo) salvem e enviem
            GetComponent<PhotonView>().RPC("RequestDrawingSave", RpcTarget.All);
        }
    }

    // Métodos de chunking removidos - agora sincronização é centralizada no PO
    
    // RPCs para sincronização de desenhos
    [PunRPC]
    void RequestDrawingSave()
    {
        UnityEngine.Debug.Log("RPC RequestDrawingSave recebido!");
        
        CommandRecorder commandRecorder = FindObjectOfType<CommandRecorder>();
        if (commandRecorder != null)
        {
            DrawingSession currentSession = commandRecorder.GetCurrentSession();
            if (currentSession != null && currentSession.GetCommandCount() > 0)
            {
                // Envia a sessão diretamente para o PO
                ProductOwnerManager poManager = FindObjectOfType<ProductOwnerManager>();
                if (poManager != null)
                {
                    var poPlayer = poManager.GetCurrentProductOwner();
                    if (poPlayer != null)
                    {
                        string sessionJson = currentSession.ToJson();
                        UnityEngine.Debug.Log($"Enviando sessão para PO: {currentSession.GetCommandCount()} comandos, {sessionJson.Length} bytes");
                        
                        // Envia para o PO especificamente
                        GetComponent<PhotonView>().RPC("ReceiveSessionForPO", poPlayer, PhotonNetwork.LocalPlayer.UserId, sessionJson);
                    }
                }
            }
            else
            {
                // Se não tem comandos, envia sessão vazia com nome do player
                string playerName = PhotonNetwork.LocalPlayer.NickName;
                if (string.IsNullOrEmpty(playerName))
                {
                    playerName = $"Player_{PhotonNetwork.LocalPlayer.ActorNumber}";
                }
                DrawingSession emptySession = new DrawingSession(playerName, PhotonNetwork.LocalPlayer.UserId);
                string emptyJson = emptySession.ToJson();
                
                ProductOwnerManager poManager = FindObjectOfType<ProductOwnerManager>();
                if (poManager != null)
                {
                    var poPlayer = poManager.GetCurrentProductOwner();
                    if (poPlayer != null)
                    {
                        GetComponent<PhotonView>().RPC("ReceiveSessionForPO", poPlayer, PhotonNetwork.LocalPlayer.UserId, emptyJson);
                    }
                }
            }
        }
    }
    
    [PunRPC]
    void ReceiveSessionForPO(string playerId, string sessionJson)
    {
        UnityEngine.Debug.Log($"PO recebeu sessão de {playerId}");
        
        // Apenas o PO deve processar isso
        ProductOwnerManager poManager = FindObjectOfType<ProductOwnerManager>();
        if (poManager == null || !poManager.IsLocalPlayerProductOwner())
        {
            return;
        }
        
        try
        {
            DrawingSession receivedSession = DrawingSession.FromJson(sessionJson);
            receivedSessions.Add(receivedSession);
            
            UnityEngine.Debug.Log($"=== PO RECEBEU SESSÃO ===");
            UnityEngine.Debug.Log($"De PlayerId: {playerId}");
            UnityEngine.Debug.Log($"SessionId: {receivedSession.sessionId}");
            UnityEngine.Debug.Log($"PlayerName na sessão: '{receivedSession.playerName}'");
            UnityEngine.Debug.Log($"PlayerId na sessão: '{receivedSession.playerId}'");
            UnityEngine.Debug.Log($"Comandos: {receivedSession.GetCommandCount()}");
            UnityEngine.Debug.Log($"Total coletado: {receivedSessions.Count}/{expectedSessionsCount}");
            
            // Verifica se coletou todas as sessões
            if (receivedSessions.Count >= expectedSessionsCount)
            {
                UnityEngine.Debug.Log("PO coletou todas as sessões! Enviando lista final para todos...");
                SendFinalSessionsList();
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Erro ao receber sessão de {playerId}: {e.Message}");
        }
    }
    
    private void SendFinalSessionsList()
    {
        // Cria JSON da lista completa de sessões
        string finalListJson = JsonUtility.ToJson(new SessionsListWrapper { sessions = receivedSessions.ToArray() });
        
        UnityEngine.Debug.Log($"PO enviando lista final com {receivedSessions.Count} sessões (tamanho: {finalListJson.Length} bytes)");
        
        // Envia para todos os players
        GetComponent<PhotonView>().RPC("ReceiveFinalSessionsList", RpcTarget.All, finalListJson);
    }
    
    [PunRPC]
    void ReceiveFinalSessionsList(string finalListJson)
    {
        UnityEngine.Debug.Log("Recebida lista final de sessões do PO");
        
        try
        {
            SessionsListWrapper wrapper = JsonUtility.FromJson<SessionsListWrapper>(finalListJson);
            finalSessionsList.Clear();
            finalSessionsList.AddRange(wrapper.sessions);
            
            UnityEngine.Debug.Log($"Lista final carregada com {finalSessionsList.Count} sessões");
            
            // Atualiza o sistema local
            UpdateLocalWithFinalList();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Erro ao receber lista final: {e.Message}");
        }
    }
    
    // Métodos antigos removidos - sistema centralizado é mais simples
    
    private void UpdateLocalWithFinalList()
    {
        CommandSaveSystem commandSaveSystem = FindObjectOfType<CommandSaveSystem>();
        if (commandSaveSystem != null)
        {
            UnityEngine.Debug.Log("Atualizando sistema local com lista final do PO");
            
            // Limpa tudo e usa apenas a lista final do PO (ordem determinística)
            commandSaveSystem.ClearSavedSessions();
            
            // Adiciona apenas sessões com comandos (remove vazias) 
            int addedCount = 0;
            foreach (DrawingSession session in finalSessionsList)
            {
                if (session != null && session.GetCommandCount() > 0)
                {
                    // Corrige nomes incorretos de sessões offline
                    if (session.playerName == "Local Player" || session.playerName == "Empty")
                    {
                        // Procura o player real pelo playerId
                        if (PhotonNetwork.InRoom)
                        {
                            var player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.UserId == session.playerId);
                            if (player != null && !string.IsNullOrEmpty(player.NickName))
                            {
                                session.playerName = player.NickName;
                                UnityEngine.Debug.Log($"Nome corrigido de '{session.playerName}' para '{player.NickName}'");
                            }
                        }
                    }
                    
                    commandSaveSystem.AddSavedSession(session);
                    UnityEngine.Debug.Log($"Adicionada sessão no slot {addedCount}: {session.playerName} ({session.GetCommandCount()} comandos)");
                    addedCount++;
                }
                else
                {
                    UnityEngine.Debug.Log($"Sessão vazia ignorada: {session?.playerName ?? "null"}");
                }
            }
            
            UnityEngine.Debug.Log($"Sistema local atualizado com {addedCount} sessões válidas");
            
            // Atualiza visibilidade dos slots
            commandSaveSystem.RefreshSlotVisibility();
            
            // NÃO carrega automaticamente aqui - apenas atualiza a lista
            // O carregamento será feito quando o PO clicar em "Começar Daily Scrum"
        }
    }
    
    private System.Collections.IEnumerator DelayedAutoLoad(CommandSaveSystem commandSaveSystem)
    {
        // Aguarda um frame para garantir que a UI foi atualizada
        yield return new WaitForEndOfFrame();
        
        if (commandSaveSystem != null && commandSaveSystem.GetSavedSessionsCount() > 0)
        {
            UnityEngine.Debug.Log("Executando auto-load do slot 0 após delay");
            commandSaveSystem.LoadDrawing(0);
        }
    }
    
    public void ReplayAllPlayerDrawings()
    {
        UnityEngine.Debug.Log("ReplayAllPlayerDrawings chamado pelo PO - enviando comando para todos");
        
        // PO envia comando via RPC para TODOS carregarem o slot 0
        if (PhotonNetwork.InRoom)
        {
            GetComponent<PhotonView>().RPC("LoadFirstDrawingForAll", RpcTarget.All);
        }
    }
    
    [PunRPC]
    void LoadFirstDrawingForAll()
    {
        UnityEngine.Debug.Log("RPC LoadFirstDrawingForAll recebido - carregando slot 0");
        
        CommandSaveSystem commandSaveSystem = FindObjectOfType<CommandSaveSystem>();
        
        if (commandSaveSystem != null)
        {
            UnityEngine.Debug.Log($"CommandSaveSystem encontrado com {commandSaveSystem.GetSavedSessionsCount()} sessões");
            
            // Se não tem sessões ainda, tenta forçar atualização da lista final
            if (commandSaveSystem.GetSavedSessionsCount() == 0 && finalSessionsList.Count > 0)
            {
                UnityEngine.Debug.Log("Forçando atualização da lista final");
                UpdateLocalWithFinalList();
            }
            
            // Carrega o primeiro desenho
            if (commandSaveSystem.GetSavedSessionsCount() > 0)
            {
                UnityEngine.Debug.Log("Auto-carregando primeiro desenho (slot 0) via RPC");
                StartCoroutine(DelayedAutoLoad(commandSaveSystem));
            }
            else
            {
                UnityEngine.Debug.Log("Nenhuma sessão disponível para carregar");
            }
        }
        else
        {
            UnityEngine.Debug.LogError("CommandSaveSystem não encontrado!");
        }
    }
    
    public void ClearAllSavedDrawings()
    {
        UnityEngine.Debug.Log("PO enviando comando de limpeza para todos os players");
        
        // PO envia comando via RPC para TODOS limparem
        if (PhotonNetwork.InRoom)
        {
            GetComponent<PhotonView>().RPC("ClearAllDrawingsForEveryone", RpcTarget.All);
        }
    }
    
    [PunRPC]
    void ClearAllDrawingsForEveryone()
    {
        UnityEngine.Debug.Log("RPC ClearAllDrawingsForEveryone recebido - limpando tudo");
        
        // Limpa estruturas de sincronização
        receivedSessions.Clear();
        finalSessionsList.Clear();
        expectedSessionsCount = 0;
        
        // Limpa o sistema local
        CommandSaveSystem commandSaveSystem = FindObjectOfType<CommandSaveSystem>();
        if (commandSaveSystem != null)
        {
            commandSaveSystem.ClearAllSavedSessions();
            UnityEngine.Debug.Log("CommandSaveSystem limpo");
        }
        
        // Limpa o canvas
        ShapeDrawer shapeDrawer = FindObjectOfType<ShapeDrawer>();
        if (shapeDrawer != null)
        {
            shapeDrawer.ClearAll();
            UnityEngine.Debug.Log("Canvas limpo");
        }
        
        // Inicia nova sessão no CommandRecorder
        CommandRecorder commandRecorder = FindObjectOfType<CommandRecorder>();
        if (commandRecorder != null)
        {
            commandRecorder.StartNewSession();
            UnityEngine.Debug.Log("Nova sessão iniciada no CommandRecorder");
        }
    }
    
    // Método necessário para IPunObservable (não usado neste caso)
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Não precisamos sincronizar dados aqui - usamos RPCs
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}
