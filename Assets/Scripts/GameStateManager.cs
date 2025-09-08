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

    public void ResetToInitialState()
    {
        ChangeState(GameState.Inicio);
        Debug.Log("Estado resetado para o inicial (Inicio)");
    }
}
