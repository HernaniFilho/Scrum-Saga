using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ImprevistoManager : MonoBehaviourPunCallbacks
{
    [Header("Deck References")]
    public DeckImprevistos deckImprevistos;
    
    [Header("Result UI")]
    public TMP_Text resultadoImprevistoText;
    
    [Header("Pause Button")]
    public Button pauseButton;

    [Header("Configuration")]
    public float cardShowDuration = 15f;

    [Header("Game References")]
    private ProductOwnerManager productOwnerManager;
    private GameStateManager gameStateManager;
    
    [Header("Network Keys")]
    private const string CARTA_PEGA_KEY = "CartaImprevistoPega";
    private const string CARTA_SPAWN_DISTANCE_KEY = "CartaImprevistoSpawnDistance";
    private const string CARTA_ROTATION_KEY = "CartaImprevistoRotation";
    private const string CARTA_TEXTO_KEY = "CartaImprevistoTexto";
    private const string CARTA_NATURES_KEY = "CartaImprevistoNatures";
    private const string CARTA_DEBUFFS_KEY = "CartaImprevistoDebuffs";
    private const string CARTA_USE_FIRST_KEY = "CartaImprevistoUseFirst";
    private const string CARTA_REMOVIDA_KEY = "CartaImprevistoRemovida";

    private bool cartaJaCriada = false;
    private bool hasStartedImprevistoPhase = false;
    private bool cartaLocalmenteColetada = false;

    public static ImprevistoManager Instance { get; private set; }

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
        ResetImprevistoState();
    }

    void Update()
    {
        if (productOwnerManager == null)
            productOwnerManager = FindObjectOfType<ProductOwnerManager>();

        if (gameStateManager == null) return;

        var currentState = gameStateManager.GetCurrentState();

        if (currentState == GameStateManager.GameState.Imprevisto)
        {
            if (!hasStartedImprevistoPhase)
            {
                StartImprevistoPhase();
                hasStartedImprevistoPhase = true;
            }
        }
        else
        {
            if (hasStartedImprevistoPhase)
            {
                hasStartedImprevistoPhase = false;
            }
        }
    }

    void InitializeReferences()
    {
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        gameStateManager = FindObjectOfType<GameStateManager>();
        
        if (deckImprevistos == null)
            deckImprevistos = FindObjectOfType<DeckImprevistos>();
        if (gameStateManager == null)
            Debug.LogError("GameStateManager não encontrado!");
        if (deckImprevistos == null)
            Debug.LogError("DeckImprevistos não encontrado!");
    }

    void SetupUI()
    {
        if (resultadoImprevistoText != null)
        {
            resultadoImprevistoText.gameObject.SetActive(false);
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

    public void StartImprevistoPhase()
    {
        ResetImprevistoState();
        
        if (resultadoImprevistoText != null)
        {
            resultadoImprevistoText.gameObject.SetActive(true);
            resultadoImprevistoText.text = "PO, colete uma carta de Imprevisto!";
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
        CardImprevistos cardComponent = cartaInstanciada.GetComponent<CardImprevistos>();
        if (cardComponent == null)
        {
            Debug.LogError("CardImprevistos component não encontrado!");
            yield break;
        }

        // Aguardar até que o texto esteja inicializado
        int maxAttempts = 10;
        int attempts = 0;
        
        while ((string.IsNullOrEmpty(cardComponent.textUI.text) || cardComponent.textUI.text == "Descricao") && attempts < maxAttempts)
        {
            yield return new WaitForSeconds(0.1f);
            attempts++;
            Debug.Log($"Aguardando inicialização da carta de imprevisto... Tentativa {attempts}/10");
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogError("Timeout aguardando inicialização da carta de imprevisto!");
            yield break;
        }

        string texto = cardComponent.textUI.text;
        string[] naturesKeys = cardComponent.natures.Keys.ToArray();
        int[] naturesValues = cardComponent.natures.Values.ToArray();
        string[] debuffsKeys = cardComponent.debuffs.Keys.ToArray();
        int[] debuffsValues = cardComponent.debuffs.Values.ToArray();
        bool useFirstOnly = cardComponent.useFirstOnly;
        
        Debug.Log($"Carta de imprevisto inicializada com sucesso - Texto: '{texto}', Natures: {naturesKeys.Length}, Debuffs: {debuffsKeys.Length}");
        
        // Converter posição e rotação para arrays para enviar via RPC
        float[] rot = {rotation.x, rotation.y, rotation.z, rotation.w};
        
        photonView.RPC("BroadcastCartaPega", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName, spawnDistance, rot, texto, naturesKeys, naturesValues, debuffsKeys, debuffsValues, useFirstOnly);
    }

    [PunRPC]
    void BroadcastCartaPega(string playerName, float spawnDistance, float[] rotation, string texto, string[] naturesKeys, int[] naturesValues, string[] debuffsKeys, int[] debuffsValues, bool useFirstOnly)
    {
        // Definir propriedade da sala para indicar que a carta foi pega
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props[CARTA_PEGA_KEY] = playerName;
            props[CARTA_SPAWN_DISTANCE_KEY] = spawnDistance;
            props[CARTA_ROTATION_KEY] = rotation;
            props[CARTA_TEXTO_KEY] = texto;
            props[CARTA_NATURES_KEY] = new object[] {naturesKeys, naturesValues};
            props[CARTA_DEBUFFS_KEY] = new object[] {debuffsKeys, debuffsValues};
            props[CARTA_USE_FIRST_KEY] = useFirstOnly;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        Debug.Log($"Carta de imprevisto pega por: {playerName}");
        
        // Esconder UI quando carta for pega
        if (resultadoImprevistoText != null)
        {
            resultadoImprevistoText.gameObject.SetActive(false);
        }
        
        // Sincronizar carta para todos os outros jogadores (evitar duplicação)
        if (PhotonNetwork.LocalPlayer.NickName != playerName && deckImprevistos != null && !cartaJaCriada)
        {
            cartaJaCriada = true;
            StartCoroutine(CreateSyncedCardWithDelay(spawnDistance, rotation, texto, naturesKeys, naturesValues, debuffsKeys, debuffsValues, useFirstOnly));
        }

        // Iniciar timer de auto-remoção usando TimerManager (apenas Master Client)
        if (TimerManager.Instance != null && PhotonNetwork.IsMasterClient)
        {
            TimerManager.Instance.StartTimer(cardShowDuration, OnCartaTimeoutComplete, "ImprevistoCartaTimer");
        }
        
        // Mostrar botão de pause para PO
        UpdatePauseButtonVisibility();
    }

    IEnumerator CreateSyncedCardWithDelay(float spawnDistance, float[] rotation, string texto, string[] naturesKeys, int[] naturesValues, string[] debuffsKeys, int[] debuffsValues, bool useFirstOnly)
    {
        GameObject carta = CreateSyncedCard(spawnDistance, rotation, texto, naturesKeys, naturesValues, debuffsKeys, debuffsValues, useFirstOnly);
        
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

    GameObject CreateSyncedCard(float spawnDistance, float[] rotation, string texto, string[] naturesKeys, int[] naturesValues, string[] debuffsKeys, int[] debuffsValues, bool useFirstOnly)
    {
        Camera playerCamera = Camera.main;
        Vector3 screenPos = playerCamera.WorldToScreenPoint(playerCamera.transform.position + playerCamera.transform.forward * spawnDistance);
        screenPos.x += OffsetManager.Instance != null ? OffsetManager.Instance.xOffset : 160f;
        Vector3 pos = playerCamera.ScreenToWorldPoint(screenPos);
        Quaternion rot = new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]);
        
        GameObject carta = Instantiate(deckImprevistos.prefabToSpawn, pos, rot);
        carta.transform.localScale = new Vector3(6f, 0.1f, 6f);
        
        // Configurar carta com dados sincronizados
        CardImprevistos cardComponent = carta.GetComponent<CardImprevistos>();
        if (cardComponent != null)
        {
            // Usar o método SetupSyncedCard para garantir sincronização
            cardComponent.SetupSyncedCard(texto, naturesKeys, naturesValues, debuffsKeys, debuffsValues, useFirstOnly);
            
            // Configurar carta para networking
            ConfigureCardForNetworking(cardComponent);
            
            // Garantir que há collider para o clique funcionar
            if (carta.GetComponent<Collider>() == null)
            {
            carta.AddComponent<BoxCollider>();
            }
            }
                
        return carta;
    }

    public void ConfigureCardForNetworking(CardImprevistos card)
    {
        if (productOwnerManager == null)
            productOwnerManager = FindObjectOfType<ProductOwnerManager>();

        if (productOwnerManager == null)
            Debug.LogError("ProductOwnerManager não encontrado!");

        // Verificar se é PO e se carta ainda não foi removida
        bool isPO = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        bool cartaJaRemovida = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CARTA_REMOVIDA_KEY);

        // SEMPRE configurar visual dos botões (independente se é carta sincronizada)
        if (card.natureButton_1 != null && card.natureButton_2 != null)
        {
            // Configurar interatividade
            card.natureButton_1.interactable = isPO && !cartaJaRemovida;
            card.natureButton_2.interactable = isPO && !cartaJaRemovida;

            // SEMPRE desabilitar highlight para não-PO
            if (!isPO)
            {
                DisableButtonHighlight(card.natureButton_1);
                DisableButtonHighlight(card.natureButton_2);
                Debug.Log("Highlight desabilitado para não-PO");
            }
        }

        // Se a carta já foi configurada como sincronizada, não reconfigurar listeners
        if (card.isSyncedCard) 
        {
            Debug.Log("Carta já sincronizada, pulando configuração de listeners");
            return;
        }

        // Sobrescrever os botões da carta para usar o sistema de networking
        if (card.natureButton_1 != null && card.natureButton_2 != null)
        {
            // Remover listeners existentes
            card.natureButton_1.onClick.RemoveAllListeners();
            card.natureButton_2.onClick.RemoveAllListeners();

            if (isPO && !cartaJaRemovida && card.natures.Count > 0)
            {
                var nature1 = card.natures.Keys.ElementAt(0);
                card.natureButton_1.onClick.AddListener(() => OnNatureClicked(nature1, card.natures[nature1], card));
                
                if (card.natures.Count > 1)
                {
                    var nature2 = card.natures.Keys.ElementAt(1);
                    card.natureButton_2.onClick.AddListener(() => OnNatureClicked(nature2, card.natures[nature2], card));
                }
            }
        }
    }

    void DisableButtonHighlight(Button button)
    {
        // Desabilitar cores de highlight
        var colors = button.colors;
        colors.highlightedColor = colors.normalColor;
        colors.pressedColor = colors.normalColor;
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
        
        // Desabilitar navegação
        var nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
        
        // Desabilitar Event Triggers
        EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            eventTrigger.enabled = false;
            Debug.Log("Event Trigger desabilitado para botão de natureza");
        }
        
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            trigger.triggers.Clear();
            Debug.Log("Event Triggers removidos do botão de natureza");
        }
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
        photonView.RPC("BroadcastImprevistoRemovido", RpcTarget.All);
    }

    public void OnNatureClicked(string nature, int value, CardImprevistos card)
    {
        if (!productOwnerManager.IsLocalPlayerProductOwner())
        {
            Debug.LogWarning("Apenas o Product Owner pode neutralizar o imprevisto!");
            return;
        }

        // Verificar se tem pontos suficientes na natureza para neutralizar
        if (ScoreManager.Instance != null)
        {
            bool canNeutralize = ScoreManager.Instance.UpdateScore(nature, value);
            if (!canNeutralize)
            {
                Debug.LogWarning($"Não é possível neutralizar: pontuação insuficiente em {nature}!");
                return;
            }
        }

        // Parar o timer usando TimerManager
        if (TimerManager.Instance != null && PhotonNetwork.InRoom)
        {
            TimerManager.Instance.StopTimer();
        }

        // Broadcast da neutralização para todos os jogadores
        photonView.RPC("BroadcastImprevistoNeutralizado", RpcTarget.All, nature, value);
        
        // Destruir todas as cartas
        DestroyAllCards();
    }

    [PunRPC]
    void BroadcastImprevistoNeutralizado(string nature, int value)
    {
        // Definir propriedade da sala para indicar que o imprevisto foi neutralizado
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props[CARTA_REMOVIDA_KEY] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        Debug.Log($"Imprevisto neutralizado: {nature} ({value} pontos)");
        
        // Destruir todas as cartas para todos
        DestroyAllCards();
        
        // Mostrar resultado para todos
        if (resultadoImprevistoText != null)
        {
            resultadoImprevistoText.gameObject.SetActive(true);
            resultadoImprevistoText.text = "Imprevisto neutralizado!";
            Debug.Log("Resultado neutralização UI ativado");
        }

        // Iniciar timer de 5 segundos antes de avançar (apenas Master Client)
        if (TimerManager.Instance != null && PhotonNetwork.IsMasterClient)
        {
            TimerManager.Instance.StartTimer(5f, OnNeutralizacaoTimerComplete, "ImprevistoNeutralizacaoTimer");
        }
        
        // Atualizar botão de pause para o novo timer
        UpdatePauseButtonVisibility();
    }

    [PunRPC]
    void BroadcastImprevistoRemovido()
    {
        // Definir propriedade da sala para indicar que a carta foi removida
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props[CARTA_REMOVIDA_KEY] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        Debug.Log("Carta de imprevisto removida por tempo ou PO");
        
        // Aplicar debuffs automaticamente (Master Client)
        if (PhotonNetwork.IsMasterClient)
        {
            ApplyDebuffsFromExistingCards();
        }
        
        // Destruir todas as cartas para todos
        DestroyAllCards();
        
        // Mostrar resultado para todos
        if (resultadoImprevistoText != null)
        {
            resultadoImprevistoText.gameObject.SetActive(true);
            resultadoImprevistoText.text = "Imprevisto não foi neutralizado! Pontuação perdida.";
            Debug.Log("Resultado remoção UI ativado");
        }
        
        // Iniciar timer de 5 segundos antes de avançar (apenas Master Client)
        if (TimerManager.Instance != null && PhotonNetwork.IsMasterClient)
        {
            TimerManager.Instance.StartTimer(5f, OnRemovidoTimerComplete, "ImprevistoRemovidoTimer");
        }
        
        // Atualizar botão de pause para o novo timer
        UpdatePauseButtonVisibility();
    }

    void OnCartaTimeoutComplete()
    {
        photonView.RPC("BroadcastImprevistoRemovido", RpcTarget.All);
    }

    void OnNeutralizacaoTimerComplete()
    {
        photonView.RPC("EsconderResultadoImprevisto", RpcTarget.All);
        
        if (gameStateManager != null)
        {
            gameStateManager.NextState();
            Debug.Log("Avançando para o próximo estado do jogo: " + gameStateManager.GetCurrentState());
        }
        
        // Atualizar botão de pause quando timer acaba
        UpdatePauseButtonVisibility();
    }
    
    void OnRemovidoTimerComplete()
    {
        photonView.RPC("EsconderResultadoImprevisto", RpcTarget.All);
        
        if (gameStateManager != null)
        {
            gameStateManager.NextState();
            Debug.Log("Avançando para o próximo estado do jogo: " + gameStateManager.GetCurrentState());
        }
        
        // Atualizar botão de pause quando timer acaba
        UpdatePauseButtonVisibility();
    }

    void ApplyDebuffsFromExistingCards()
    {
        // Obter dados dos debuffs das propriedades da sala para evitar aplicação dupla
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CARTA_DEBUFFS_KEY) && 
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CARTA_USE_FIRST_KEY))
        {
            object[] debuffsData = (object[])PhotonNetwork.CurrentRoom.CustomProperties[CARTA_DEBUFFS_KEY];
            bool useFirstOnly = (bool)PhotonNetwork.CurrentRoom.CustomProperties[CARTA_USE_FIRST_KEY];
            
            string[] debuffsKeys = (string[])debuffsData[0];
            int[] debuffsValues = (int[])debuffsData[1];
            
            if (useFirstOnly && debuffsKeys.Length > 0)
            {
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.UpdateScore(debuffsKeys[0], debuffsValues[0]);
                }
                Debug.Log($"Enviando debuff único: {debuffsKeys[0]} {debuffsValues[0]}");
            }
            else
            {
                // Aplicar todos os debuffs
                for (int i = 0; i < debuffsKeys.Length && i < debuffsValues.Length; i++)
                {
                    if (ScoreManager.Instance != null)
                    {
                        ScoreManager.Instance.UpdateScore(debuffsKeys[i], debuffsValues[i]);
                    }
                    Debug.Log($"Enviando debuff: {debuffsKeys[i]} {debuffsValues[i]}");
                }
            }
        }
    }

    [PunRPC]
    void EsconderResultadoImprevisto()
    {
        if (resultadoImprevistoText != null)
        {
            resultadoImprevistoText.gameObject.SetActive(false);
        }
    }

    void DestroyAllCards()
    {
        // Encontrar e destruir todas as cartas de imprevisto na cena
        CardImprevistos[] cartas = FindObjectsOfType<CardImprevistos>();
        foreach (var carta in cartas)
        {
            Destroy(carta.gameObject);
        }
    }

    public void ResetImprevistoState()
    {
        cartaJaCriada = false;
        hasStartedImprevistoPhase = false;
        cartaLocalmenteColetada = false;
        
        // Parar timer se estiver ativo
        if (TimerManager.Instance != null && PhotonNetwork.InRoom)
        {
            TimerManager.Instance.StopTimer();
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            // Limpar propriedades da sala relacionadas ao imprevisto
            Hashtable props = new Hashtable();
            props[CARTA_PEGA_KEY] = null;
            props[CARTA_SPAWN_DISTANCE_KEY] = null;
            props[CARTA_ROTATION_KEY] = null;
            props[CARTA_TEXTO_KEY] = null;
            props[CARTA_NATURES_KEY] = null;
            props[CARTA_DEBUFFS_KEY] = null;
            props[CARTA_USE_FIRST_KEY] = null;
            props[CARTA_REMOVIDA_KEY] = null;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        // Reset da UI local
        if (resultadoImprevistoText != null)
            resultadoImprevistoText.gameObject.SetActive(false);
            
        // Esconder botão de pause
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);
            
        // Destruir cartas restantes
        DestroyAllCards();
    }

    public void ResetImprevistoManager()
    {
        cartaJaCriada = false;
        hasStartedImprevistoPhase = false;
        cartaLocalmenteColetada = false;
        
        // Parar timers se estiverem ativos
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.StopTimer("ImprevistoCartaTimer");
            TimerManager.Instance.StopTimer("ImprevistoNeutralizacaoTimer");
            TimerManager.Instance.StopTimer("ImprevistoRemovidoTimer");
        }
        
        // Esconder UI
        if (resultadoImprevistoText != null)
            resultadoImprevistoText.gameObject.SetActive(false);
            
        // Destruir todas as cartas
        DestroyAllCards();
    }
    
    // Método para atualizar visibilidade do botão de pause
    void UpdatePauseButtonVisibility()
    {
        if (pauseButton == null) return;
        
        bool isPO = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        bool hasActiveTimer = TimerManager.Instance != null && TimerManager.Instance.IsTimerActive();
        bool isImprevistoPhase = gameStateManager != null && gameStateManager.GetCurrentState() == GameStateManager.GameState.Imprevisto;
        
        // Mostrar botão se é PO, tem timer ativo E está na fase de Imprevisto
        if (isPO && hasActiveTimer && isImprevistoPhase)
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
            propertiesThatChanged.ContainsKey(CARTA_NATURES_KEY) &&
            propertiesThatChanged.ContainsKey(CARTA_DEBUFFS_KEY) &&
            propertiesThatChanged.ContainsKey(CARTA_USE_FIRST_KEY))
        {
            object cartaPegaObj = propertiesThatChanged[CARTA_PEGA_KEY];
            object spawnDistanceObj = propertiesThatChanged[CARTA_SPAWN_DISTANCE_KEY];
            object rotationObj = propertiesThatChanged[CARTA_ROTATION_KEY];
            object textoObj = propertiesThatChanged[CARTA_TEXTO_KEY];
            object naturesObj = propertiesThatChanged[CARTA_NATURES_KEY];
            object debuffsObj = propertiesThatChanged[CARTA_DEBUFFS_KEY];
            object useFirstObj = propertiesThatChanged[CARTA_USE_FIRST_KEY];
            
            if (cartaPegaObj != null && spawnDistanceObj != null && rotationObj != null && 
                textoObj != null && naturesObj != null && debuffsObj != null && useFirstObj != null)
            {
                string playerName = (string)cartaPegaObj;
                float spawnDistance = (float)spawnDistanceObj;
                float[] rotation = (float[])rotationObj;
                string texto = (string)textoObj;
                bool useFirstOnly = (bool)useFirstObj;
                
                object[] naturesData = (object[])naturesObj;
                string[] naturesKeys = (string[])naturesData[0];
                int[] naturesValues = (int[])naturesData[1];
                
                object[] debuffsData = (object[])debuffsObj;
                string[] debuffsKeys = (string[])debuffsData[0];
                int[] debuffsValues = (int[])debuffsData[1];
                
                // Se não foi este jogador que pegou e ainda não criamos a carta
                if (PhotonNetwork.LocalPlayer.NickName != playerName && deckImprevistos != null && !cartaJaCriada)
                {
                    cartaJaCriada = true;
                    StartCoroutine(CreateSyncedCardWithDelay(spawnDistance, rotation, texto, naturesKeys, naturesValues, debuffsKeys, debuffsValues, useFirstOnly));
                }
            }
        }
    }

    #endregion
}
