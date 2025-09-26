using System;
using System.Collections.Generic;
using UnityEngine;

public class CardScoreManager : MonoBehaviour
{
    public static CardScoreManager Instance { get; private set; }

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

    public List<Dictionary<string, int>> GenerateUniqueCardScores()
    {
        List<Dictionary<string, int>> uniqueScores = new List<Dictionary<string, int>>();
        HashSet<string> usedScoreCombinations = new HashSet<string>();

        ScoreManager.ScoreType[] allScoreTypes = (ScoreManager.ScoreType[])Enum.GetValues(typeof(ScoreManager.ScoreType));
        
        if (allScoreTypes.Length < 2)
        {
            Debug.LogError("Não há tipos de pontuação suficientes para gerar cartas únicas!");
            return uniqueScores;
        }

        int attempts = 0;
        int maxAttempts = 100;

        while (uniqueScores.Count < 4 && attempts < maxAttempts)
        {
            Dictionary<string, int> newScoreSet = GenerateRandomScoreSet(allScoreTypes);
            string scoreKey = GenerateScoreKey(newScoreSet);

            if (!usedScoreCombinations.Contains(scoreKey))
            {
                uniqueScores.Add(newScoreSet);
                usedScoreCombinations.Add(scoreKey);
                Debug.Log($"Pontuação única gerada: {scoreKey}");
            }

            attempts++;
        }

        while (uniqueScores.Count < 4)
        {
            Dictionary<string, int> fallbackScore = GenerateRandomScoreSet(allScoreTypes);
            uniqueScores.Add(fallbackScore);
            Debug.LogWarning($"Usando pontuação de fallback para carta {uniqueScores.Count}");
        }

        Debug.Log($"Geradas {uniqueScores.Count} pontuações únicas para as cartas");
        return uniqueScores;
    }

    private Dictionary<string, int> GenerateRandomScoreSet(ScoreManager.ScoreType[] availableTypes)
    {
        Dictionary<string, int> scores = new Dictionary<string, int>();
        
        bool isDublePointSingle = UnityEngine.Random.Range(0f, 1f) > 0.5f;
        
        if (isDublePointSingle)
        {
            // +2 em uma trilha
            ScoreManager.ScoreType selectedType = availableTypes[UnityEngine.Random.Range(0, availableTypes.Length)];
            scores.Add(selectedType.ToString(), 2);
        }
        else
        {
            // +1 em duas trilhas
            ScoreManager.ScoreType[] twoRandomTypes = GetTwoRandomScoreTypes(availableTypes);
            
            foreach (var type in twoRandomTypes)
            {
                string varName = type.ToString();
                if (!scores.ContainsKey(varName))
                {
                    scores.Add(varName, 1);
                }
            }
        }
        
        return scores;
    }

    private ScoreManager.ScoreType[] GetTwoRandomScoreTypes(ScoreManager.ScoreType[] availableTypes)
    {
        if (availableTypes.Length < 2)
        {
            Debug.LogWarning("Não há tipos suficientes para selecionar dois únicos");
            return new ScoreManager.ScoreType[0];
        }

        List<ScoreManager.ScoreType> list = new List<ScoreManager.ScoreType>(availableTypes);

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return new ScoreManager.ScoreType[] { list[0], list[1] };
    }

    private string GenerateScoreKey(Dictionary<string, int> scores)
    {
        List<string> parts = new List<string>();
        
        var sortedKeys = new List<string>(scores.Keys);
        sortedKeys.Sort();
        
        foreach (string key in sortedKeys)
        {
            parts.Add($"{key}:{scores[key]}");
        }
        
        return string.Join("|", parts);
    }
}
