using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class SprintRetrospectiveManager : MonoBehaviourPunCallbacks
{
    [Header("Deck References")]
    public DeckAprendizagens deckAprendizagens;
    
    [Header("Result UI")]
    public TMP_Text resultadoAprendizagemText;
    
    [Header("Pause Button")]
    public Button pauseButton;

    [Header("Configuration")]
    public float cardShowDuration = 7f;

    [Header("Game References")]
    private ProductOwnerManager productOwnerManager;
    private GameStateManager gameStateManager;
    
    [Header("Network Keys")]
    private const string CARTA_PEGA_KEY = "CartaAprendizagemPega";
    private const string CARTA_SPAWN_DISTANCE_KEY = "CartaAprendizagemSpawnDistance";
    private const string CARTA_ROTATION_KEY = "CartaAprendizagemRotation";
    private const string CARTA_TEXTO_KEY = "CartaAprendizagemTexto";
    private const string CARTA_NATURE_KEY = "CartaAprendizagemNature";
    private const string CARTA_REMOVIDA_KEY = "CartaAprendizagemRemovida";

    private bool cartaJaCriada = false;
    private bool hasStartedRetrospectivePhase = false;
    private bool cartaLocalmenteColetada = false;

    public static SprintRetrospectiveManager Instance { get; private set; }

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
        ResetRetrospectiveState();
    }

    void Update()
    {
        if (productOwnerManager == null)
            productOwnerManager = FindObjectOfType<ProductOwnerManager>();

        if (gameStateManager == null) return;

        var currentState = gameStateManager.GetCurrentState();

        if (currentState == GameStateManager.GameState.SprintRetrospective)
        {
            if (!hasStartedRetrospectivePhase)
            {
                StartRetrospectivePhase();
                hasStartedRetrospectivePhase = true;
            }
        }
        else
        {
            if (hasStartedRetrospectivePhase)
            {
                hasStartedRetrospectivePhase = false;
            }
        }
    }

    void InitializeReferences()
    {
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        gameStateManager = FindObjectOfType<GameStateManager>();
        
        if (deckAprendizagens == null)
            deckAprendizagens = FindObjectOfType<DeckAprendizagens>();
        if (gameStateManager == null)
            Debug.LogError("GameStateManager não encontrado!");
        if (deckAprendizagens == null)
            Debug.LogError("DeckAprendizagens não encontrado!");
    }

    void SetupUI()
    {
        if (resultadoAprendizagemText != null)
        {
            resultadoAprendizagemText.gameObject.SetActive(false);
        }
        
        SetupPauseButton();
    }
    
    void SetupPauseButton()
    {
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
            pauseButton.onClick.RemoveAllListeners();
        }
    }

    public void StartRetrospectivePhase()
    {
        ResetRetrospectiveState();
        
        if (resultadoAprendizagemText != null)
        {
            resultadoAprendizagemText.gameObject.SetActive(true);
            resultadoAprendizagemText.text = "PO, colete uma carta de Aprendizagem!";
        }
    }

    public bool CanPegarCarta()
    {
        if (!PhotonNetwork.InRoom) return false;
        
        // Verificação local instantânea
        if (cartaLocalmenteColetada) return false;
        
        bool cartaJaPega = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CARTA_PEGA_KEY);
        bool cartaJaRemovida = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CARTA_REMOVIDA_KEY);
        
        return !cartaJaPega && !cartaJaRemovida;
    }

    public void NotifyCartaPega(GameObject cartaInstanciada, float spawnDistance, Quaternion rotation)
    {
        if (!PhotonNetwork.InRoom) return;
        
        // Marcar localmente que a carta foi coletada (instantâneo)
        cartaLocalmenteColetada = true;
        
        // Desabilitar collider por 2 segundos
        Collider col = cartaInstanciada.GetComponent<Collider>();
        if (col != null)
        {
            StartCoroutine(EnableColliderAfterDelay(col));
        }
        
        // Aguardar inicialização completa da carta
        StartCoroutine(WaitForCardInitialization(cartaInstanciada, spawnDistance, rotation));
    }

    private IEnumerator EnableColliderAfterDelay(Collider col)
    {
        col.enabled = false;
        yield return new WaitForSeconds(1.2f);
        if (col != null)
            col.enabled = true;
    }

    private IEnumerator WaitForCardInitialization(GameObject cartaInstanciada, float spawnDistance, Quaternion rotation)
    {
        CardAprendizagens cardComponent = cartaInstanciada.GetComponent<CardAprendizagens>();
        if (cardComponent == null)
        {
            Debug.LogError("CardAprendizagens component não encontrado!");
            yield break;
        }

        // Aguardar até que o texto e natureza estejam inicializados
        int maxAttempts = 10;
        int attempts = 0;
        
        while ((string.IsNullOrEmpty(cardComponent.textUI.text) || cardComponent.textUI.text == "Descricao" || 
                string.IsNullOrEmpty(cardComponent.nature)) && attempts < maxAttempts)
        {
            yield return new WaitForSeconds(0.1f);
            attempts++;
            Debug.Log($"Aguardando inicialização da carta... Tentativa {attempts}/10");
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogError("Timeout aguardando inicialização da carta!");
            yield break;
        }

        string texto = cardComponent.textUI.text;
        string nature = cardComponent.nature;
        
        Debug.Log($"Carta inicializada com sucesso - Texto: '{texto}', Nature: '{nature}'");
        
        // Converter posição e rotação para arrays para enviar via RPC
        float[] rot = {rotation.x, rotation.y, rotation.z, rotation.w};
        
        photonView.RPC("BroadcastCartaPega", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName, spawnDistance, rot, texto, nature);
    }

    [PunRPC]
    void BroadcastCartaPega(string playerName, float spawnDistance, float[] rotation, string texto, string nature)
    {
        // Definir propriedade da sala para indicar que a carta foi pega
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props[CARTA_PEGA_KEY] = playerName;
            props[CARTA_SPAWN_DISTANCE_KEY] = spawnDistance;
            props[CARTA_ROTATION_KEY] = rotation;
            props[CARTA_TEXTO_KEY] = texto;
            props[CARTA_NATURE_KEY] = nature;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        Debug.Log($"Carta de aprendizagem pega por: {playerName}");
        
        // Esconder UI quando carta for pega
        if (resultadoAprendizagemText != null)
        {
            resultadoAprendizagemText.gameObject.SetActive(false);
        }
        
        // Sincronizar carta para todos os outros jogadores (evitar duplicação)
        if (PhotonNetwork.LocalPlayer.NickName != playerName && deckAprendizagens != null && !cartaJaCriada)
        {
            cartaJaCriada = true;
            StartCoroutine(CreateSyncedCardWithDelay(spawnDistance, rotation, texto, nature));
        }

        // Iniciar timer de auto-remoção usando TimerManager (apenas Master Client)
        if (TimerManager.Instance != null && PhotonNetwork.IsMasterClient)
        {
            TimerManager.Instance.StartTimer(cardShowDuration, () => {
                photonView.RPC("BroadcastCartaRemovida", RpcTarget.All);
            }, "CartaAprendizagemTimer");
        }
        
        // Mostrar botão de pause para PO
        UpdatePauseButtonVisibility();
    }

    IEnumerator CreateSyncedCardWithDelay(float spawnDistance, float[] rotation, string texto, string nature)
    {
        GameObject carta = CreateSyncedCard(spawnDistance, rotation, texto, nature);
        
        if (carta != null)
        {
            Collider col = carta.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
                yield return new WaitForSeconds(1.2f);
                if (col != null)
                    col.enabled = true;
            }
        }
    }

    GameObject CreateSyncedCard(float spawnDistance, float[] rotation, string texto, string nature)
    {
        Camera playerCamera = Camera.main;
        Vector3 screenPos = playerCamera.WorldToScreenPoint(playerCamera.transform.position + playerCamera.transform.forward * spawnDistance);
        screenPos.x += OffsetManager.Instance != null ? OffsetManager.Instance.xOffset : 160f;
        Vector3 pos = playerCamera.ScreenToWorldPoint(screenPos);
        Quaternion rot = new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]);
        
        GameObject carta = Instantiate(deckAprendizagens.prefabToSpawn, pos, rot);
        
        // Configurar carta com dados sincronizados
        CardAprendizagens cardComponent = carta.GetComponent<CardAprendizagens>();
        if (cardComponent != null)
        {
            // Usar o método SetupSyncedCard para garantir sincronização
            cardComponent.SetupSyncedCard(texto, nature);
            
            // Configurar clique para PO
            bool isPO = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
            
            // CardAprendizagens já tem OnMouseDown que chama OnPOClickCard diretamente durante SprintRetrospective
            // Garantir que há collider para o clique funcionar
            if (carta.GetComponent<Collider>() == null)
            {
                carta.AddComponent<BoxCollider>();
            }
        }
        
        return carta;
    }

    public void OnPOClickCard()
    {
        if (!productOwnerManager.IsLocalPlayerProductOwner()) return;
        
        // Parar o timer usando TimerManager
        if (TimerManager.Instance != null && PhotonNetwork.InRoom)
        {
            TimerManager.Instance.StopTimer();
        }

        // Broadcast da remoção para todos os jogadores
        photonView.RPC("BroadcastCartaRemovida", RpcTarget.All);
    }

    [PunRPC]
    void BroadcastCartaRemovida()
    {
        // Definir propriedade da sala para indicar que a carta foi removida
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props[CARTA_REMOVIDA_KEY] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        Debug.Log("Carta removida");
        
        // Aumentar pontuação apenas uma vez (Master Client)
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateScoreFromExistingCards();
        }
        
        // Destruir todas as cartas para todos
        DestroyAllCards();
        
        // Esconder UI
        if (resultadoAprendizagemText != null)
        {
            resultadoAprendizagemText.gameObject.SetActive(false);
        }
        
        // Esconder botão de pause
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);
        
        // Avançar para próximo estado (apenas o PO pode fazer isso)
        if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            AdvanceToNextState();
        }
    }

    void AdvanceToNextState()
    {
        if (gameStateManager != null)
        {
            gameStateManager.NextState();
            Debug.Log("Avançando para o próximo estado do jogo: " + gameStateManager.GetCurrentState());
        }
        else
        {
            Debug.LogError("GameStateManager não encontrado para avançar estado!");
        }
        
        // Atualizar botão de pause quando avança estado
        UpdatePauseButtonVisibility();
    }

    void UpdateScoreFromExistingCards()
    {
        // Encontrar cartas de aprendizagem na cena e atualizar pontuação
        CardAprendizagens[] cartas = FindObjectsOfType<CardAprendizagens>();
        foreach (var carta in cartas)
        {
            if (!string.IsNullOrEmpty(carta.nature))
            {
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.UpdateScore(carta.nature, 1);
                }
                Debug.Log($"Enviando atualização de pontuação: {carta.nature} +1");
            }
        }
    }

    void DestroyAllCards()
    {
        // Encontrar e destruir todas as cartas de aprendizagem na cena
        CardAprendizagens[] cartas = FindObjectsOfType<CardAprendizagens>();
        foreach (var carta in cartas)
        {
            Destroy(carta.gameObject);
        }
    }

    public void ResetRetrospectiveState()
    {
        cartaJaCriada = false;
        hasStartedRetrospectivePhase = false;
        cartaLocalmenteColetada = false;
        
        // Parar timer se estiver ativo
        if (TimerManager.Instance != null && PhotonNetwork.InRoom)
        {
            TimerManager.Instance.StopTimer();
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            // Limpar propriedades da sala relacionadas à retrospectiva
            Hashtable props = new Hashtable();
            props[CARTA_PEGA_KEY] = null;
            props[CARTA_SPAWN_DISTANCE_KEY] = null;
            props[CARTA_ROTATION_KEY] = null;
            props[CARTA_TEXTO_KEY] = null;
            props[CARTA_NATURE_KEY] = null;
            props[CARTA_REMOVIDA_KEY] = null;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        // Reset da UI local
        if (resultadoAprendizagemText != null)
            resultadoAprendizagemText.gameObject.SetActive(false);
            
        // Esconder botão de pause
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);
            
        // Destruir cartas restantes
        DestroyAllCards();
    }

    public void ResetSprintRetrospectiveManager()
    {
        cartaJaCriada = false;
        hasStartedRetrospectivePhase = false;
        cartaLocalmenteColetada = false;
        
        // Parar timer se estiver ativo
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.StopTimer("CartaAprendizagemTimer");
        }
        
        // Esconder UI
        if (resultadoAprendizagemText != null)
            resultadoAprendizagemText.gameObject.SetActive(false);
            
        // Destruir todas as cartas
        DestroyAllCards();
    }
    
    // Método para atualizar visibilidade do botão de pause
    void UpdatePauseButtonVisibility()
    {
        if (pauseButton == null) return;
        
        bool isPO = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        bool hasActiveTimer = TimerManager.Instance != null && TimerManager.Instance.IsTimerActive();
        bool isRetrospectivePhase = gameStateManager != null && gameStateManager.GetCurrentState() == GameStateManager.GameState.SprintRetrospective;
        
        // Mostrar botão se é PO, tem timer ativo E está na fase de Sprint Retrospective
        if (isPO && hasActiveTimer && isRetrospectivePhase)
        {
            pauseButton.gameObject.SetActive(true);
            
            // Configurar onClick baseado no estado atual
            pauseButton.onClick.RemoveAllListeners();
            
            if (TimerManager.Instance.IsPaused())
            {
                pauseButton.GetComponentInChildren<TMP_Text>().text = "Despausar";
                pauseButton.onClick.AddListener(() => UnpauseTimers());
            }
            else
            {
                pauseButton.GetComponentInChildren<TMP_Text>().text = "Pausar";
                pauseButton.onClick.AddListener(() => PauseTimers());
            }
        }
        else
        {
            pauseButton.gameObject.SetActive(false);
        }
    }
    
    // Métodos para controle de pause pelo PO
    public void PauseTimers()
    {
        if (TimerManager.Instance != null && productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            TimerManager.Instance.PauseTimer();
            UpdatePauseButtonVisibility();
        }
    }
    
    public void UnpauseTimers()
    {
        if (TimerManager.Instance != null && productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            TimerManager.Instance.UnpauseTimer();
            UpdatePauseButtonVisibility();
        }
    }

    #region Photon Callbacks

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Recriar carta se chegamos atrasado na sala (evitar duplicação)
        if (propertiesThatChanged.ContainsKey(CARTA_PEGA_KEY) && 
            propertiesThatChanged.ContainsKey(CARTA_SPAWN_DISTANCE_KEY) && 
            propertiesThatChanged.ContainsKey(CARTA_ROTATION_KEY) &&
            propertiesThatChanged.ContainsKey(CARTA_TEXTO_KEY) &&
            propertiesThatChanged.ContainsKey(CARTA_NATURE_KEY))
        {
            object cartaPegaObj = propertiesThatChanged[CARTA_PEGA_KEY];
            object spawnDistanceObj = propertiesThatChanged[CARTA_SPAWN_DISTANCE_KEY];
            object rotationObj = propertiesThatChanged[CARTA_ROTATION_KEY];
            object textoObj = propertiesThatChanged[CARTA_TEXTO_KEY];
            object natureObj = propertiesThatChanged[CARTA_NATURE_KEY];
            
            if (cartaPegaObj != null && spawnDistanceObj != null && rotationObj != null && 
                textoObj != null && natureObj != null)
            {
                string playerName = (string)cartaPegaObj;
                float spawnDistance = (float)spawnDistanceObj;
                float[] rotation = (float[])rotationObj;
                string texto = (string)textoObj;
                string nature = (string)natureObj;
                
                // Se não foi este jogador que pegou e ainda não criamos a carta
                if (PhotonNetwork.LocalPlayer.NickName != playerName && deckAprendizagens != null && !cartaJaCriada)
                {
                    cartaJaCriada = true;
                    StartCoroutine(CreateSyncedCardWithDelay(spawnDistance, rotation, texto, nature));
                }
            }
        }
    }

    #endregion
}
