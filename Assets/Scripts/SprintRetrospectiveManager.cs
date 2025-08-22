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

    [Header("Configuration")]
    public float cardShowDuration = 30f;

    [Header("Game References")]
    private ProductOwnerManager productOwnerManager;
    private GameStateManager gameStateManager;
    
    [Header("Network Keys")]
    private const string CARTA_PEGA_KEY = "CartaAprendizagemPega";
    private const string CARTA_POSITION_KEY = "CartaAprendizagemPosition";
    private const string CARTA_ROTATION_KEY = "CartaAprendizagemRotation";
    private const string CARTA_TEXTO_KEY = "CartaAprendizagemTexto";
    private const string CARTA_NATURE_KEY = "CartaAprendizagemNature";
    private const string CARTA_REMOVIDA_KEY = "CartaAprendizagemRemovida";

    private bool cartaJaCriada = false;
    private bool hasStartedRetrospectivePhase = false;

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
    }

    public void StartRetrospectivePhase()
    {
        ResetRetrospectiveState();
        
        if (resultadoAprendizagemText != null)
        {
            resultadoAprendizagemText.gameObject.SetActive(true);
            resultadoAprendizagemText.text = "Coletem uma carta de Aprendizagem!";
        }
    }

    public bool CanPegarCarta()
    {
        if (!PhotonNetwork.InRoom) return false;
        
        bool cartaJaPega = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CARTA_PEGA_KEY);
        bool cartaJaRemovida = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CARTA_REMOVIDA_KEY);
        
        return !cartaJaPega && !cartaJaRemovida;
    }

    public void NotifyCartaPega(GameObject cartaInstanciada, Vector3 position, Quaternion rotation)
    {
        if (!PhotonNetwork.InRoom) return;
        
        // Aguardar inicialização completa da carta
        StartCoroutine(WaitForCardInitialization(cartaInstanciada, position, rotation));
    }

    private IEnumerator WaitForCardInitialization(GameObject cartaInstanciada, Vector3 position, Quaternion rotation)
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
        float[] pos = {position.x, position.y, position.z};
        float[] rot = {rotation.x, rotation.y, rotation.z, rotation.w};
        
        photonView.RPC("BroadcastCartaPega", RpcTarget.All, PhotonNetwork.LocalPlayer.NickName, pos, rot, texto, nature);
    }

    [PunRPC]
    void BroadcastCartaPega(string playerName, float[] position, float[] rotation, string texto, string nature)
    {
        // Definir propriedade da sala para indicar que a carta foi pega
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            props[CARTA_PEGA_KEY] = playerName;
            props[CARTA_POSITION_KEY] = position;
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
            CreateSyncedCard(position, rotation, texto, nature);
        }

        // Iniciar timer de auto-remoção usando TimerManager (apenas Master Client)
        if (TimerManager.Instance != null && PhotonNetwork.IsMasterClient)
        {
            TimerManager.Instance.StartTimer(cardShowDuration, () => {
                photonView.RPC("BroadcastCartaRemovida", RpcTarget.All);
            }, "CartaAprendizagemTimer");
        }
    }

    void CreateSyncedCard(float[] position, float[] rotation, string texto, string nature)
    {
        Vector3 pos = new Vector3(position[0], position[1], position[2]);
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
            
            if (isPO)
            {
                // Adicionar componente para detectar clique do PO
                if (carta.GetComponent<Collider>() == null)
                {
                    carta.AddComponent<BoxCollider>();
                }
                
                // Adicionar script de clique do PO
                POClickHandler clickHandler = carta.AddComponent<POClickHandler>();
                clickHandler.retrospectiveManager = this;
            }
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
    }

    void UpdateScoreFromExistingCards()
    {
        // Encontrar cartas de aprendizagem na cena e atualizar pontuação
        CardAprendizagens[] cartas = FindObjectsOfType<CardAprendizagens>();
        foreach (var carta in cartas)
        {
            if (!string.IsNullOrEmpty(carta.nature))
            {
                // Usar RPC para sincronizar pontuação para todos os players
                photonView.RPC("BroadcastScoreUpdate", RpcTarget.All, carta.nature, 1);
                Debug.Log($"Enviando atualização de pontuação: {carta.nature} +1");
            }
        }
    }

    [PunRPC]
    void BroadcastScoreUpdate(string nature, int points)
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.UpdateScore(nature, points);
            Debug.Log($"Pontuação atualizada via RPC: {nature} +{points}");
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
            props[CARTA_POSITION_KEY] = null;
            props[CARTA_ROTATION_KEY] = null;
            props[CARTA_TEXTO_KEY] = null;
            props[CARTA_NATURE_KEY] = null;
            props[CARTA_REMOVIDA_KEY] = null;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        // Reset da UI local
        if (resultadoAprendizagemText != null)
            resultadoAprendizagemText.gameObject.SetActive(false);
            
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
            propertiesThatChanged.ContainsKey(CARTA_NATURE_KEY))
        {
            object cartaPegaObj = propertiesThatChanged[CARTA_PEGA_KEY];
            object positionObj = propertiesThatChanged[CARTA_POSITION_KEY];
            object rotationObj = propertiesThatChanged[CARTA_ROTATION_KEY];
            object textoObj = propertiesThatChanged[CARTA_TEXTO_KEY];
            object natureObj = propertiesThatChanged[CARTA_NATURE_KEY];
            
            if (cartaPegaObj != null && positionObj != null && rotationObj != null && 
                textoObj != null && natureObj != null)
            {
                string playerName = (string)cartaPegaObj;
                float[] position = (float[])positionObj;
                float[] rotation = (float[])rotationObj;
                string texto = (string)textoObj;
                string nature = (string)natureObj;
                
                // Se não foi este jogador que pegou e ainda não criamos a carta
                if (PhotonNetwork.LocalPlayer.NickName != playerName && deckAprendizagens != null && !cartaJaCriada)
                {
                    cartaJaCriada = true;
                    CreateSyncedCard(position, rotation, texto, nature);
                }
            }
        }
    }

    #endregion
}

// Classe auxiliar para detectar clique do PO nas cartas
public class POClickHandler : MonoBehaviour
{
    public SprintRetrospectiveManager retrospectiveManager;

    void OnMouseDown()
    {
        if (retrospectiveManager != null)
        {
            retrospectiveManager.OnPOClickCard();
        }
    }
}
