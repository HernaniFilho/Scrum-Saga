using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class EscolhaManager : MonoBehaviourPunCallbacks
{
    [Header("Deck References")]
    public DeckEscolhas deckEscolhas;
    
    [Header("Result UI")]
    public TMP_Text resultadoEscolhaText;

    [Header("Configuration")]
    public float resultShowDuration = 5f;

    [Header("Game References")]
    private ProductOwnerManager productOwnerManager;
    private GameStateManager gameStateManager;
    
    [Header("Network Keys")]
    private const string CARTA_PEGA_KEY = "CartaPega";
    private const string CARTA_POSITION_KEY = "CartaPosition";
    private const string CARTA_ROTATION_KEY = "CartaRotation";
    private const string CARTA_TEXTO_KEY = "CartaTexto";
    private const string CARTA_TRILHAS_KEY = "CartaTrilhas";
    private const string CARTA_PONTOS_KEY = "CartaPontos";
    private const string TRILHA_ESCOLHIDA_KEY = "TrilhaEscolhida";
    private const string ESCOLHA_FEITA_KEY = "EscolhaFeita";

    private bool cartaJaCriada = false;
    private bool hasStartedEscolhaPhase = false;

    public static EscolhaManager Instance { get; private set; }

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
        ResetEscolhaState();
    }

    void Update()
    {
        if (productOwnerManager == null)
            productOwnerManager = FindObjectOfType<ProductOwnerManager>();

        if (gameStateManager == null) return;

        var currentState = gameStateManager.GetCurrentState();

        if (currentState == GameStateManager.GameState.Escolha)
        {
            if (!hasStartedEscolhaPhase)
            {
                StartEscolhaPhase();
                hasStartedEscolhaPhase = true;
            }
        }
        else
        {
            if (hasStartedEscolhaPhase)
            {
                hasStartedEscolhaPhase = false;
            }
        }
    }

    void InitializeReferences()
    {
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        gameStateManager = FindObjectOfType<GameStateManager>();
        
        if (deckEscolhas == null)
            deckEscolhas = FindObjectOfType<DeckEscolhas>();
        if (gameStateManager == null)
            Debug.LogError("GameStateManager não encontrado!");
        if (deckEscolhas == null)
            Debug.LogError("DeckEscolhas não encontrado!");
    }

    void SetupUI()
    {
        if (resultadoEscolhaText != null)
        {
            resultadoEscolhaText.gameObject.SetActive(false);
        }
    }

    public void StartEscolhaPhase()
    {
        ResetEscolhaState();
        
        if (resultadoEscolhaText != null)
        {
            resultadoEscolhaText.gameObject.SetActive(true);
            resultadoEscolhaText.text = "Coletem uma carta de Escolha!";
        }
    }

    public bool CanPegarCarta()
    {
        if (!PhotonNetwork.InRoom) return false;
        
        bool cartaJaPega = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CARTA_PEGA_KEY);
        bool escolhaJaFeita = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ESCOLHA_FEITA_KEY);
        
        return !cartaJaPega && !escolhaJaFeita;
    }

    public void NotifyCartaPega(GameObject cartaInstanciada, Vector3 position, Quaternion rotation)
    {
        if (!PhotonNetwork.InRoom) return;
        
        // Pegar informações da carta para sincronizar
        CardEscolhas cardComponent = cartaInstanciada.GetComponent<CardEscolhas>();
        if (cardComponent == null)
        {
            Debug.LogError("CardEscolhas component não encontrado!");
            return;
        }

        string texto = cardComponent.textUI.text;
        string[] trilhas = cardComponent.scores.Keys.ToArray();
        int[] pontos = cardComponent.scores.Values.ToArray();
        
        // Converter posição e rotação para arrays para enviar via RPC
        float[] pos = {position.x, position.y, position.z};
        float[] rot = {rotation.x, rotation.y, rotation.z, rotation.w};
        
        photonView.RPC("BroadcastCartaPega", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName, pos, rot, texto, trilhas, pontos);
    }

    [PunRPC]
    void BroadcastCartaPega(string playerName, float[] position, float[] rotation, string texto, string[] trilhas, int[] pontos)
    {
        // Definir propriedade da sala para indicar que a carta foi pega
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props[CARTA_PEGA_KEY] = playerName;
            props[CARTA_POSITION_KEY] = position;
            props[CARTA_ROTATION_KEY] = rotation;
            props[CARTA_TEXTO_KEY] = texto;
            props[CARTA_TRILHAS_KEY] = trilhas;
            props[CARTA_PONTOS_KEY] = pontos;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        Debug.Log($"Carta de escolha pega por: {playerName}");
        
        // Atualizar UI baseado no papel do jogador
        if (resultadoEscolhaText != null)
        {
            bool isPO = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
            
            if (isPO)
            {
                resultadoEscolhaText.gameObject.SetActive(false);
            }
            else
            {
                resultadoEscolhaText.gameObject.SetActive(true);
                resultadoEscolhaText.text = "Aguardando PO realizar a escolha...";
            }
        }
        
        // Sincronizar carta para todos os outros jogadores (evitar duplicação)
        if (PhotonNetwork.LocalPlayer.NickName != playerName && deckEscolhas != null && !cartaJaCriada)
        {
            cartaJaCriada = true;
            CreateSyncedCard(position, rotation, texto, trilhas, pontos);
        }
    }

    void CreateSyncedCard(float[] position, float[] rotation, string texto, string[] trilhas, int[] pontos)
    {
        Vector3 pos = new Vector3(position[0], position[1], position[2]);
        Quaternion rot = new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]);
        
        GameObject carta = Instantiate(deckEscolhas.prefabToSpawn, pos, rot);
        
        // Configurar carta com dados sincronizados
        CardEscolhas cardComponent = carta.GetComponent<CardEscolhas>();
        if (cardComponent != null)
        {
            bool isPO = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
            bool escolhaJaFeita = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ESCOLHA_FEITA_KEY);
            
            // Usar o novo método da CardEscolhas
            cardComponent.SetupSyncedCard(texto, trilhas, pontos, isPO && !escolhaJaFeita);
            
            // Configurar listeners apenas para PO
            if (isPO && !escolhaJaFeita && trilhas.Length >= 2)
            {
                cardComponent.scoreButton_1.onClick.AddListener(() => OnTrilhaEscolhida(trilhas[0], pontos[0], cardComponent));
                cardComponent.scoreButton_2.onClick.AddListener(() => OnTrilhaEscolhida(trilhas[1], pontos[1], cardComponent));
            }
        }
    }

    public void ConfigureCardForNetworking(CardEscolhas card)
    {
        if (productOwnerManager == null)
            productOwnerManager = FindObjectOfType<ProductOwnerManager>();

        if (productOwnerManager == null)
            Debug.LogError("ProductOwnerManager não encontrado!");

        // Verificar se é PO e se escolha ainda não foi feita
        bool isPO = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        bool escolhaJaFeita = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ESCOLHA_FEITA_KEY);

        // SEMPRE configurar visual dos botões (independente se é carta sincronizada)
        if (card.scoreButton_1 != null && card.scoreButton_2 != null)
        {
            // Configurar interatividade
            card.scoreButton_1.interactable = isPO && !escolhaJaFeita;
            card.scoreButton_2.interactable = isPO && !escolhaJaFeita;

            // SEMPRE desabilitar highlight para não-PO
            if (!isPO)
            {
                DisableButtonHighlight(card.scoreButton_1);
                DisableButtonHighlight(card.scoreButton_2);
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
        if (card.scoreButton_1 != null && card.scoreButton_2 != null)
        {
            // Remover listeners existentes
            card.scoreButton_1.onClick.RemoveAllListeners();
            card.scoreButton_2.onClick.RemoveAllListeners();

            if (isPO && !escolhaJaFeita)
            {
                var trilha1 = card.scores.Keys.ElementAt(0);
                var trilha2 = card.scores.Keys.ElementAt(1);

                card.scoreButton_1.onClick.AddListener(() => OnTrilhaEscolhida(trilha1, card.scores[trilha1], card));
                card.scoreButton_2.onClick.AddListener(() => OnTrilhaEscolhida(trilha2, card.scores[trilha2], card));
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
        
        // Desabilitar Event Triggers (OnPointerEnter, OnPointerExit, etc.)
        EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            eventTrigger.enabled = false;
            Debug.Log("Event Trigger desabilitado para botão");
        }
        
        // Alternativamente, remover todos os triggers
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            trigger.triggers.Clear();
            Debug.Log("Event Triggers removidos do botão");
        }
    }

    public void OnTrilhaEscolhida(string trilhaEscolhida, int pontos, CardEscolhas card)
    {
        if (!productOwnerManager.IsLocalPlayerProductOwner())
        {
            Debug.LogWarning("Apenas o Product Owner pode escolher a trilha!");
            return;
        }

        // Aplicar pontuação via ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.UpdateScore(trilhaEscolhida, pontos);
        }

        // Broadcast da escolha para todos os jogadores
        photonView.RPC("BroadcastEscolhaFeita", RpcTarget.All, trilhaEscolhida, pontos);
        
        // Destruir todas as cartas
        DestroyAllCards();
    }

    [PunRPC]
    void BroadcastEscolhaFeita(string trilhaEscolhida, int pontos)
    {
        // Definir propriedade da sala para indicar que a escolha foi feita
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props[TRILHA_ESCOLHIDA_KEY] = trilhaEscolhida;
            props[ESCOLHA_FEITA_KEY] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        Debug.Log($"Trilha escolhida: {trilhaEscolhida} (+{pontos} pontos)");
        
        // Destruir todas as cartas para todos
        DestroyAllCards();
        
        // Mostrar resultado para todos
        MostrarResultadoEscolha(trilhaEscolhida, pontos);
    }

    void MostrarResultadoEscolha(string trilhaEscolhida, int pontos)
    {
        Debug.Log($"Mostrando resultado: {trilhaEscolhida} +{pontos}");
        
        // Mostrar resultado
        if (resultadoEscolhaText != null)
        {
            resultadoEscolhaText.gameObject.SetActive(true);
            resultadoEscolhaText.text = $"Trilha escolhida: {trilhaEscolhida}\n+{pontos} ponto";
            Debug.Log("Resultado UI ativado");
        }
        else
        {
            Debug.LogError("ResultadoEscolhaText não atribuído!");
        }

        // Iniciar timer de auto-remoção usando TimerManager (apenas Master Client)
        if (TimerManager.Instance != null && PhotonNetwork.IsMasterClient)
        {
            TimerManager.Instance.StartTimer(resultShowDuration, onTimerComplete, "ResultadoEscolhaTimer");
        }
    }

    void onTimerComplete()
    {
        photonView.RPC("EsconderResultadoEscolha", RpcTarget.All);

        if (gameStateManager != null)
        {
            gameStateManager.NextState();
            Debug.Log("Avançando para o próximo estado do jogo: " + gameStateManager.GetCurrentState());
        }
    }

    [PunRPC]
    void EsconderResultadoEscolha()
    {
        if (resultadoEscolhaText != null)
        {
            resultadoEscolhaText.gameObject.SetActive(false);
        }
    }

    void DestroyAllCards()
    {
        // Encontrar e destruir todas as cartas de escolha na cena
        CardEscolhas[] cartas = FindObjectsOfType<CardEscolhas>();
        foreach (var carta in cartas)
        {
            Destroy(carta.gameObject);
        }
    }

    public void ResetEscolhaState()
    {
        cartaJaCriada = false; // Reset da flag de duplicação
        hasStartedEscolhaPhase = false; // Reset da flag de fase

        // Parar timer se estiver ativo
        if (TimerManager.Instance != null && PhotonNetwork.InRoom)
        {
            TimerManager.Instance.StopTimer();
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            // Limpar propriedades da sala relacionadas à escolha
            Hashtable props = new Hashtable();
            props[CARTA_PEGA_KEY] = null;
            props[CARTA_POSITION_KEY] = null;
            props[CARTA_ROTATION_KEY] = null;
            props[CARTA_TEXTO_KEY] = null;
            props[CARTA_TRILHAS_KEY] = null;
            props[CARTA_PONTOS_KEY] = null;
            props[TRILHA_ESCOLHIDA_KEY] = null;
            props[ESCOLHA_FEITA_KEY] = null;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        // Reset da UI local
        if (resultadoEscolhaText != null)
            resultadoEscolhaText.gameObject.SetActive(false);
            
        // Destruir cartas restantes
        DestroyAllCards();
    }

    #region Photon Callbacks

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Recriar carta se chegamos atrasado na sala (evitar duplicação)
        if (propertiesThatChanged.ContainsKey(CARTA_PEGA_KEY) && 
            propertiesThatChanged.ContainsKey(CARTA_POSITION_KEY) && 
            propertiesThatChanged.ContainsKey(CARTA_ROTATION_KEY) &&
            propertiesThatChanged.ContainsKey(CARTA_TEXTO_KEY) &&
            propertiesThatChanged.ContainsKey(CARTA_TRILHAS_KEY) &&
            propertiesThatChanged.ContainsKey(CARTA_PONTOS_KEY))
        {
            object cartaPegaObj = propertiesThatChanged[CARTA_PEGA_KEY];
            object positionObj = propertiesThatChanged[CARTA_POSITION_KEY];
            object rotationObj = propertiesThatChanged[CARTA_ROTATION_KEY];
            object textoObj = propertiesThatChanged[CARTA_TEXTO_KEY];
            object trilhasObj = propertiesThatChanged[CARTA_TRILHAS_KEY];
            object pontosObj = propertiesThatChanged[CARTA_PONTOS_KEY];
            
            if (cartaPegaObj != null && positionObj != null && rotationObj != null && 
                textoObj != null && trilhasObj != null && pontosObj != null)
            {
                string playerName = (string)cartaPegaObj;
                float[] position = (float[])positionObj;
                float[] rotation = (float[])rotationObj;
                string texto = (string)textoObj;
                string[] trilhas = (string[])trilhasObj;
                int[] pontos = (int[])pontosObj;
                
                // Se não foi este jogador que pegou e ainda não criamos a carta
                if (PhotonNetwork.LocalPlayer.NickName != playerName && deckEscolhas != null && !cartaJaCriada)
                {
                    cartaJaCriada = true;
                    CreateSyncedCard(position, rotation, texto, trilhas, pontos);
                }
            }
        }
    }

    #endregion
}
