using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public enum Difficulty
    {
        Easy = 7,
        Medium = 6,
        Hard = 5
    }

    [Header("Caminho do arquivo de variáveis de pontuação")]
    public string scoreVarPath = "ScoreVars/scoreVars";

    [Header("Dificuldade do jogo")]
    public Difficulty gameDifficulty = Difficulty.Easy;
    private Dictionary<string, int> scores = new Dictionary<string, int>();

    [Header("Referências aos cards")]
    public CardEscolhas cardEscolhas;
    public CardImprevistos cardImprevistos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        loadScoreVariables(gameDifficulty);
        Debug.Log("Pontuações carregadas com sucesso.");
        foreach (var varName in scores.Keys)
        {
            Debug.Log($"Pontuação inicial: {varName} = {scores[varName]}");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void loadScoreVariables(Difficulty difficulty)
    {
        TextAsset scoreFile = Resources.Load<TextAsset>(scoreVarPath);
        if (scoreFile == null)
        {
            Debug.LogWarning("Arquivo de pontuação não encontrado em: " + scoreVarPath);
            return;
        }

        string[] lines = scoreFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
        {
            Debug.LogWarning("Arquivo de pontuação está vazio: " + scoreVarPath);
            return;
        }

        int scorePoints = (int)difficulty;
        foreach (string line in lines)
        {
            string varName = line.Trim();
            if (!scores.ContainsKey(varName))
            {
                scores.Add(varName, scorePoints);
            }
        }
    }

    public Dictionary<string, int> GetScoreVariables()
    {
        return new Dictionary<string, int>(scores);
    }
    
    public int GetScore(string varName)
    {
        if (scores.TryGetValue(varName, out int score))
        {
            return score;
        }
        else
        {
            Debug.LogWarning($"Variável de pontuação '{varName}' não encontrada.");
            return 0;
        }
    }
    
    public void UpdateScore(string varName, int value)
    {
        if (scores.ContainsKey(varName))
        {
            scores[varName] += value;
            Debug.Log($"Pontuação atualizada: {varName} = {scores[varName]}");
        }
        else
        {
            Debug.LogWarning($"Variável de pontuação '{varName}' não encontrada.");
        }
    }
}
