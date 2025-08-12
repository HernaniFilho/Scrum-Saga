using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameStateManager : MonoBehaviour
{
    public enum GameState
    {
        Inicio,
        SprintPlanning,
        DailyScrum,
        Imprevisto,
        Escolha,
        RealizacaoTarefa,
        SprintReview,
        SprintRetrospective,
        Fim
    }

    [Header("Estado atual do jogo")]
    public GameState currentState = GameState.Inicio;

    [Header("Referência ao Texto de Estado na UI")]
    public TMP_Text stateText;
    
    [Header("Referência ao Tabuleiro")]
    public GameObject tabuleiro;

    private static readonly Dictionary<GameState, string> stateDisplayNames = new Dictionary<GameState, string>
    {
        { GameState.Inicio,              "Início da Sprint" },
        { GameState.SprintPlanning,      "Sprint Planning" },
        { GameState.DailyScrum,          "Daily Scrum" },
        { GameState.Imprevisto,          "Imprevisto" },
        { GameState.Escolha,             "Escolha" },
        { GameState.RealizacaoTarefa,    "Realização da Tarefa" },
        { GameState.SprintReview,        "Sprint Review" },
        { GameState.SprintRetrospective, "Sprint Retrospective" },
        { GameState.Fim,                 "Fim da Sprint" }
    };

    public static GameStateManager Instance { get; private set; }

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
        if (stateText == null)
        {
            Debug.LogError("Referência de texto do estado da UI não atribuída!");
            return;
        }

        PositionTabuleiro();
        UpdateStateText();
        Debug.Log("GameStateManager iniciado com estado: " + currentState);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        UpdateStateText();
        Debug.Log($"Estado alterado para: {currentState}");

        // Se NetworkGameStateManager existir, notificar outros jogadores
        if (NetworkGameStateManager.Instance != null)
        {
            NetworkGameStateManager.Instance.BroadcastStateChange(newState);
        }
    }

    public void ChangeState(string stateName)
    {
        if (Enum.TryParse<GameState>(stateName, out GameState newState))
        {
            ChangeState(newState);
        }
        else
        {
            Debug.LogWarning($"Estado '{stateName}' não encontrado.");
        }
    }

    public GameState GetCurrentState()
    {
        return currentState;
    }

    public string GetCurrentStateName()
    {
        return currentState.ToString();
    }

    string GetStateDisplayText(GameState state)
    {
        return stateDisplayNames.TryGetValue(state, out string displayName) ? displayName : state.ToString();
    }

    void UpdateStateText()
    {
        if (stateText != null)
        {
            string displayText = GetStateDisplayText(currentState);
            stateText.text = displayText;
            Debug.Log($"Texto do estado atualizado para: {displayText}");
        }
    }

    public void NextState()
    {
        PositionTabuleiro();
        GameState[] states = (GameState[])Enum.GetValues(typeof(GameState));
        int currentIndex = Array.IndexOf(states, currentState);
        
        if (currentIndex < states.Length - 1)
        {
            ChangeState(states[currentIndex + 1]);
        }
        else
        {
            ChangeState(states[0]);
            Debug.Log("Estava no último estado, voltando ao primeiro.");
        }
    }

    public void PreviousState()
    {
        GameState[] states = (GameState[])Enum.GetValues(typeof(GameState));
        int currentIndex = Array.IndexOf(states, currentState);
        
        if (currentIndex > 0)
        {
            ChangeState(states[currentIndex - 1]);
        }
        else
        {
            Debug.Log("Já está no primeiro estado!");
        }
    }

    void PositionTabuleiro()
    {
        return;
        if (tabuleiro == null)
        {
            // Tenta encontrar o tabuleiro automaticamente se não foi atribuído
            tabuleiro = GameObject.Find("Tabuleiro");
            if (tabuleiro == null)
            {
                Debug.LogWarning("Tabuleiro não encontrado!");
                return;
            }
        }

        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("Câmera principal não encontrada. Certifique-se de que há uma câmera com a tag 'MainCamera'.");
        }

        Vector3 tabuleiroPosition = new Vector3(
            (-10.4f + 17.4f) / 2,
            -1083.62f,
            -512f
        );

        float pixelsToUnits = 160f / Camera.main.pixelWidth * Camera.main.orthographicSize * 2f;

        // Adicione ao eixo X (direita)
        tabuleiroPosition.x -= pixelsToUnits;

        // Aplique a nova posição
        // tabuleiro.transform.position = tabuleiroPosition;
    }
}
