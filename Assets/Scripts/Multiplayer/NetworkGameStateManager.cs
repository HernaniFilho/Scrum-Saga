using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class NetworkGameStateManager : MonoBehaviourPunCallbacks
{
    public static NetworkGameStateManager Instance { get; private set; }
    
    private GameStateManager gameStateManager;
    private const string GAME_STATE_KEY = "GameState";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        gameStateManager = FindObjectOfType<GameStateManager>();
        
        if (gameStateManager == null)
        {
            Debug.LogError("GameStateManager não encontrado! NetworkGameStateManager precisa de um GameStateManager na cena.");
            return;
        }

        // Carregar estado atual se já estiver em uma sala
        LoadCurrentStateFromRoom();

        Debug.Log("NetworkGameStateManager conectado ao GameStateManager");
    }

    public void BroadcastStateChange(GameStateManager.GameState newState)
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[GAME_STATE_KEY] = (int)newState;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log($"Estado {newState} enviado para outros jogadores");
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(GAME_STATE_KEY))
        {
            int newStateValue = (int)propertiesThatChanged[GAME_STATE_KEY];
            ApplyStateChange(newStateValue);
        }
    }

    private void ApplyStateChange(int newStateValue)
    {
        if (gameStateManager != null)
        {
            GameStateManager.GameState newState = (GameStateManager.GameState)newStateValue;
            
            // Temporariamente desativar NetworkGameStateManager para evitar loop
            var temp = Instance;
            Instance = null;
            
            gameStateManager.ChangeState(newState);
            
            // Reativar NetworkGameStateManager
            Instance = temp;
            
            Debug.Log($"Estado sincronizado da rede: {newState}");
        }
    }

    private void LoadCurrentStateFromRoom()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(GAME_STATE_KEY))
        {
            int currentStateValue = (int)PhotonNetwork.CurrentRoom.CustomProperties[GAME_STATE_KEY];
            ApplyStateChange(currentStateValue);
            Debug.Log($"Estado carregado da sala: {(GameStateManager.GameState)currentStateValue}");
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Entrou na sala, carregando estado atual...");
        LoadCurrentStateFromRoom();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Jogador {newPlayer.NickName} entrou na sala");
        
        // Novos jogadores automaticamente recebem o estado via propriedades da sala
        // Não precisamos fazer nada extra aqui
    }
}
