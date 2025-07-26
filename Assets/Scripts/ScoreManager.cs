using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public enum ScoreType
    {
        Entrosamento,
        Produtividade,
        Expectativa
    }
    public enum Difficulty
    {
        Easy = 7,
        Medium = 6,
        Hard = 5
    }

    [Header("Dificuldade do jogo")]
    public Difficulty gameDifficulty = Difficulty.Easy;
    public Dictionary<string, int> scoreboard = new Dictionary<string, int>();
    public Dictionary<string, TMP_Text> scoreTexts = new Dictionary<string, TMP_Text>();

    // TODO: Melhorar a forma de referenciar os scores na UI
    [Header("Referências aos scores na UI")]
    public TMP_Text scoreText_1;
    public TMP_Text scoreText_2;
    public TMP_Text scoreText_3;
    public static ScoreManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Garante que só haverá uma instância
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Se quiser manter entre cenas
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (scoreText_1 == null || scoreText_2 == null || scoreText_3 == null)
        {
            Debug.LogError("Referências de texto da UI não atribuídas!");
            return;
        }
        scoreTexts[ScoreType.Entrosamento.ToString()] = scoreText_1;
        scoreTexts[ScoreType.Produtividade.ToString()] = scoreText_2;
        scoreTexts[ScoreType.Expectativa.ToString()] = scoreText_3;

        loadScore(gameDifficulty);
        Debug.Log("ScoreManager iniciado com dificuldade: " + gameDifficulty);
        foreach (var score in scoreboard)
        {
            Debug.Log($"Pontuação inicial: {score.Key} = {score.Value}");
        }
    }

    void loadScore(Difficulty difficulty)
    {
        scoreboard.Clear();
        foreach (ScoreType type in Enum.GetValues(typeof(ScoreType)))
        {
            string key = type.ToString();
            int value = (int)difficulty; // Atribui a dificuldade como valor inicial
            scoreboard[key] = value;
            UpdateScoreTexts(key, value);
        }
    }

    public void UpdateScore(string varName, int value)
    {
        if (scoreboard.ContainsKey(varName))
        {
            scoreboard[varName] += value;
            Debug.Log($"Pontuação atualizada: {varName} = {scoreboard[varName]}");
            UpdateScoreTexts(varName, scoreboard[varName]);
        }
        else
        {
            Debug.LogWarning($"Variável de pontuação '{varName}' não encontrada.");
        }
    }

    public void UpdateScoreTexts(string varName, int value)
    {
        if (scoreTexts.ContainsKey(varName))
        {
            scoreTexts[varName].text = $"{varName}: {value}";
            Debug.Log($"Texto atualizado para {varName}: {value}");
        }
        else
        {
            Debug.LogWarning($"Texto de pontuação '{varName}' não encontrado.");
        }
    }
}
