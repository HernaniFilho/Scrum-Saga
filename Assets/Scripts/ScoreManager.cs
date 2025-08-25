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
    public enum Natures
    {
        Adaptacao,
        Inspecao,
        Transparencia
    }
    public enum Difficulty
    {
        Easy = 6,
        Medium = 5,
        Hard = 4
    }

    private static readonly Dictionary<Natures, string> natureDisplayNames = new Dictionary<Natures, string>
    {
        { Natures.Adaptacao, "Adaptação" },
        { Natures.Inspecao, "Inspeção" },
        { Natures.Transparencia, "Transparência" }
    };

    [Header("Dificuldade do jogo")]
    public Difficulty gameDifficulty = Difficulty.Easy;
    // Vai ter uma pontuação inicial baseada na dificuldade
    // e será atualizada conforme o jogo avança.
    // Também terá uma pontuação para cada tipo de natureza.
    public Dictionary<string, int> scoreboard = new Dictionary<string, int>();

    [Header("Referências aos Textos de Natureza e Pontuação na UI")]
    public Dictionary<string, TMP_Text> scoreTexts = new Dictionary<string, TMP_Text>();
    [Header("Referências aos ScoresTypes na UI")]
    public TMP_Text scoreText_1;
    public TMP_Text scoreText_2;
    public TMP_Text scoreText_3;
    [Header("Referências de Natureza")]
    public TMP_Text natureText_1;
    public TMP_Text natureText_2;
    public TMP_Text natureText_3;

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

        if (natureText_1 == null || natureText_2 == null || natureText_3 == null)
        {
            Debug.LogError("Referências de natureza da UI não atribuídas!");
            return;
        }
        scoreTexts[Natures.Adaptacao.ToString()] = natureText_1;
        scoreTexts[Natures.Inspecao.ToString()] = natureText_2;
        scoreTexts[Natures.Transparencia.ToString()] = natureText_3;

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

        // Valores específicos para cada dificuldade e tipo de score
        Dictionary<(Difficulty, ScoreType), int> difficultyValues = new Dictionary<(Difficulty, ScoreType), int>
        {
            // Fácil: Prod 6, Entrosa 7, Expec 5
            { (Difficulty.Easy, ScoreType.Produtividade), 6 },
            { (Difficulty.Easy, ScoreType.Entrosamento), 7 },
            { (Difficulty.Easy, ScoreType.Expectativa), 5 },
            
            // Médio: Prod 5, Entrosa 6, Expec 4
            { (Difficulty.Medium, ScoreType.Produtividade), 5 },
            { (Difficulty.Medium, ScoreType.Entrosamento), 6 },
            { (Difficulty.Medium, ScoreType.Expectativa), 4 },
            
            // Difícil: Prod 4, Entrosa 5, Expec 4
            { (Difficulty.Hard, ScoreType.Produtividade), 4 },
            { (Difficulty.Hard, ScoreType.Entrosamento), 5 },
            { (Difficulty.Hard, ScoreType.Expectativa), 4 }
        };

        foreach (ScoreType type in Enum.GetValues(typeof(ScoreType)))
        {
            string key = type.ToString();
            int value = difficultyValues[(difficulty, type)];
            scoreboard[key] = value;
            UpdateScoreTexts(key, value);
        }
        foreach (Natures nature in Enum.GetValues(typeof(Natures)))
        {
            string key = nature.ToString();
            int value = 0; // Inicializa as naturezas com 0
            scoreboard[key] = value;
            UpdateScoreTexts(key, value);
        }
    }

    public bool UpdateScore(string varName, int value)
    {
        if (scoreboard.ContainsKey(varName))
        {
            if (scoreboard[varName] + value < 0)
            {
                if (scoreboard[varName] == 0)
                {
                    Debug.LogWarning($"Tentativa de reduzir '{varName}' abaixo de zero. Operação ignorada.");
                    return false;
                }
                else
                {
                    scoreboard[varName] = 0;
                }
            }
            else
            {
                scoreboard[varName] += value;
            }

            Debug.Log($"Pontuação atualizada: {varName} = {scoreboard[varName]}");
            UpdateScoreTexts(varName, scoreboard[varName]);

            // Sincronizar com outros jogadores se NetworkScoreManager existir
            if (NetworkScoreManager.Instance != null)
            {
                NetworkScoreManager.Instance.BroadcastScoreUpdate(varName, scoreboard[varName]);
            }

            return true;
        }
        else
        {
            Debug.LogWarning($"Variável de pontuação '{varName}' não encontrada.");
        }

        return false;
    }

    string GetNatureDisplayText(Natures nature)
    {
        return natureDisplayNames.TryGetValue(nature, out string displayName) ? displayName : nature.ToString();
    }

    public void UpdateScoreTexts(string varName, int value)
    {
        if (scoreTexts.ContainsKey(varName))
        {
            string displayName = varName;

            // Se for uma natureza, usar o nome com acento
            if (Enum.TryParse<Natures>(varName, out Natures nature))
            {
                displayName = GetNatureDisplayText(nature);
            }

            scoreTexts[varName].text = $"{displayName}: {value}";
            Debug.Log($"Texto atualizado para {varName}: {value}");
        }
        else
        {
            Debug.LogWarning($"Texto de pontuação '{varName}' não encontrado.");
        }
    }

    public void ResetScore()
    {
        loadScore(gameDifficulty);

        if (NetworkScoreManager.Instance != null)
        {
            foreach (var entry in scoreboard)
            {
                NetworkScoreManager.Instance.BroadcastScoreUpdate(entry.Key, entry.Value);
            }
        }
    }

    public int GetLowestScoreValue()
    {
        if (scoreboard.Count == 0)
            return 0; // ou outro valor que faça sentido no seu jogo

        int lowest = int.MaxValue;

        foreach (ScoreType type in Enum.GetValues(typeof(ScoreType)))
        {
            string key = type.ToString();
            if (scoreboard.TryGetValue(key, out int value))
            {
                if (value < lowest)
                {
                    lowest = value;
                }
            }
        }

        return lowest;
    }

}
