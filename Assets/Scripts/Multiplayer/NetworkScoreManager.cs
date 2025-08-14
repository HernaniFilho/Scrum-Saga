using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class NetworkScoreManager : MonoBehaviourPunCallbacks
{
    public static NetworkScoreManager Instance { get; private set; }
    
    private ScoreManager scoreManager;
    
    // Keys para armazenar as pontuações nas propriedades da sala
    private const string SCORE_PREFIX = "Score_";
    
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
        scoreManager = ScoreManager.Instance;
        
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager não encontrado! NetworkScoreManager precisa de um ScoreManager na cena.");
            return;
        }

        // Carregar pontuações atuais se já estiver em uma sala
        LoadScoresFromRoom();

        Debug.Log("NetworkScoreManager conectado ao ScoreManager");
    }

    public void BroadcastScoreUpdate(string scoreKey, int newValue)
    {
        if (PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props[SCORE_PREFIX + scoreKey] = newValue;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log($"Pontuação {scoreKey}: {newValue} sincronizada com outros jogadores");
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        foreach (var property in propertiesThatChanged)
        {
            string key = property.Key.ToString();
            if (key.StartsWith(SCORE_PREFIX))
            {
                string scoreKey = key.Substring(SCORE_PREFIX.Length);
                int newValue = (int)property.Value;
                ApplyScoreChange(scoreKey, newValue);
            }
        }
    }

    private void ApplyScoreChange(string scoreKey, int newValue)
    {
        if (scoreManager != null && scoreManager.scoreboard.ContainsKey(scoreKey))
        {
            // Temporariamente desativar NetworkScoreManager para evitar loop
            var temp = Instance;
            Instance = null;
            
            // Aplicar valor diretamente sem somar
            scoreManager.scoreboard[scoreKey] = newValue;
            scoreManager.UpdateScoreTexts(scoreKey, newValue);
            
            // Reativar NetworkScoreManager
            Instance = temp;
            
            Debug.Log($"Pontuação sincronizada da rede: {scoreKey} = {newValue}");
        }
    }

    private void LoadScoresFromRoom()
    {
        if (PhotonNetwork.InRoom && scoreManager != null)
        {
            var roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
            
            foreach (var property in roomProperties)
            {
                string key = property.Key.ToString();
                if (key.StartsWith(SCORE_PREFIX))
                {
                    string scoreKey = key.Substring(SCORE_PREFIX.Length);
                    int value = (int)property.Value;
                    ApplyScoreChange(scoreKey, value);
                    Debug.Log($"Pontuação carregada da sala: {scoreKey} = {value}");
                }
            }
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Entrou na sala, carregando pontuações atuais...");
        LoadScoresFromRoom();
        
        // Se for o primeiro jogador, inicializar pontuações na sala
        if (PhotonNetwork.PlayerList.Length == 1 && scoreManager != null)
        {
            InitializeRoomScores();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Jogador {newPlayer.NickName} entrou na sala");
        // Novos jogadores automaticamente recebem as pontuações via propriedades da sala
    }

    private void InitializeRoomScores()
    {
        if (scoreManager != null)
        {
            Hashtable props = new Hashtable();
            
            // Inicializar pontuações na sala com os valores atuais do ScoreManager
            foreach (var score in scoreManager.scoreboard)
            {
                props[SCORE_PREFIX + score.Key] = score.Value;
            }
            
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log("Pontuações inicializadas na sala para outros jogadores");
        }
    }
}
