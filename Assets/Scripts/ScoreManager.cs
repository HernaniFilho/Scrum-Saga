using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

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
    
    public Dictionary<string, int> scoreboard = new Dictionary<string, int>();
    
    [Header("Sprites das Cartas de Natureza")]
    public Sprite emptySlotSprite;
    public Sprite adaptacaoSprite;
    public Sprite inspecaoSprite;
    public Sprite transparenciaSprite;

    // Sistema de cartas de natureza
    private List<Natures> availableNatureCards = new List<Natures>();
    
    // Sistema de naturezas embaralhadas por sprint
    private List<string> shuffledNatures = new List<string>();

    public static ScoreManager Instance { get; private set; }
    
    private List<ScoreDisplay> registeredDisplays = new List<ScoreDisplay>();

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
        // Verificar apenas os sprites
        if (emptySlotSprite == null || adaptacaoSprite == null || 
            inspecaoSprite == null || transparenciaSprite == null)
        {
            Debug.LogError("Referências dos sprites de natureza não atribuídas!");
            return;
        }

        loadScore(gameDifficulty);
        ShuffleNatures();
        
        Debug.Log("ScoreManager iniciado com dificuldade: " + gameDifficulty);
    }

    void loadScore(Difficulty difficulty)
    {
        scoreboard.Clear();

        Dictionary<(Difficulty, ScoreType), int> difficultyValues = new Dictionary<(Difficulty, ScoreType), int>
        {
            { (Difficulty.Easy, ScoreType.Produtividade), 6 },
            { (Difficulty.Easy, ScoreType.Entrosamento), 7 },
            { (Difficulty.Easy, ScoreType.Expectativa), 5 },
            { (Difficulty.Medium, ScoreType.Produtividade), 5 },
            { (Difficulty.Medium, ScoreType.Entrosamento), 6 },
            { (Difficulty.Medium, ScoreType.Expectativa), 4 },
            { (Difficulty.Hard, ScoreType.Produtividade), 4 },
            { (Difficulty.Hard, ScoreType.Entrosamento), 5 },
            { (Difficulty.Hard, ScoreType.Expectativa), 4 }
        };

        foreach (ScoreType type in Enum.GetValues(typeof(ScoreType)))
        {
            string key = type.ToString();
            int value = difficultyValues[(difficulty, type)];
            scoreboard[key] = value;
        }
        
        ClearAllNatureCards();
        ShuffleNatures();
        
        foreach (Natures nature in Enum.GetValues(typeof(Natures)))
        {
            string key = nature.ToString();
            scoreboard[key] = 0;
        }
        
        NotifyDisplays();
    }

    public bool UpdateScore(string varName, int value)
    {
        if (scoreboard.ContainsKey(varName))
        {
            int newValue = scoreboard[varName] + value;
            
            if (newValue < 0)
            {
                if (scoreboard[varName] == 0)
                {
                    Debug.LogWarning($"Tentativa de reduzir '{varName}' abaixo de zero. Operação ignorada.");
                    return false;
                }
                scoreboard[varName] = 0;
            }
            else if (newValue > 10)
            {
                if (scoreboard[varName] == 10)
                {
                    Debug.LogWarning($"Tentativa de aumentar '{varName}' acima de 10. Operação ignorada.");
                    return false;
                }
                scoreboard[varName] = 10;
            }
            else
            {
                scoreboard[varName] = newValue;
            }

            Debug.Log($"Pontuação atualizada: {varName} = {scoreboard[varName]}");
            
            // Atualizar sistema de cartas se for natureza
            if (Enum.TryParse<Natures>(varName, out Natures nature))
            {
                UpdateNatureCards(nature, value);
            }

            // Sincronizar com rede
            if (NetworkScoreManager.Instance != null)
            {
                NetworkScoreManager.Instance.BroadcastScoreUpdate(varName, scoreboard[varName]);
            }
            
            NotifyDisplays();
            return true;
        }
        else
        {
            Debug.LogWarning($"Variável de pontuação '{varName}' não encontrada.");
        }

        return false;
    }

    public string GetNatureDisplayText(Natures nature)
    {
        return natureDisplayNames.TryGetValue(nature, out string displayName) ? displayName : nature.ToString();
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
        
        NotifyDisplays();
    }

    public int GetLowestScoreValue()
    {
        if (scoreboard.Count == 0)
            return 0;

        int lowest = int.MaxValue;

        foreach (ScoreType type in Enum.GetValues(typeof(ScoreType)))
        {
            string key = type.ToString();
            if (scoreboard.TryGetValue(key, out int value))
            {
                if (value < lowest)
                    lowest = value;
            }
        }

        return lowest;
    }

    // ============ SISTEMA DE CARTAS DE NATUREZA ============
    
    private void UpdateNatureCards(Natures nature, int deltaValue)
    {
        if (deltaValue > 0)
        {
            for (int i = 0; i < deltaValue; i++)
            {
                AddNatureCard(nature);
            }
        }
        else if (deltaValue < 0)
        {
            int cardsToRemove = Mathf.Abs(deltaValue);
            for (int i = 0; i < cardsToRemove; i++)
            {
                if (HasNatureCard(nature))
                {
                    UseNatureCard(nature);
                }
                else
                {
                    Debug.LogWarning($"Tentou remover carta de {nature}, mas não possui mais cartas!");
                    break;
                }
            }
        }
    }
    
    public Sprite GetNatureSprite(Natures nature)
    {
        switch (nature)
        {
            case Natures.Adaptacao: return adaptacaoSprite;
            case Natures.Inspecao: return inspecaoSprite;
            case Natures.Transparencia: return transparenciaSprite;
            default: return emptySlotSprite;
        }
    }
    
    public void AddNatureCard(Natures nature)
    {
        string key = nature.ToString();
        int currentValue = scoreboard.ContainsKey(key) ? scoreboard[key] : 0;
        int currentCards = availableNatureCards.Count(n => n == nature);
        
        if (currentCards >= currentValue)
        {
            Debug.LogWarning($"Já possui {currentCards} cartas de {nature} para valor {currentValue}.");
            return;
        }
        
        if (availableNatureCards.Count < 3)
        {
            availableNatureCards.Add(nature);
            NotifyDisplays();
            Debug.Log($"Carta de natureza {nature} adicionada. Total: {availableNatureCards.Count}");
        }
        else
        {
            Debug.LogWarning("Já possui 3 cartas de natureza!");
        }
    }
    
    public bool UseNatureCard(Natures nature)
    {
        int index = availableNatureCards.IndexOf(nature);
        if (index >= 0)
        {
            availableNatureCards.RemoveAt(index);
            NotifyDisplays();
            Debug.Log($"Carta de natureza {nature} usada. Restam: {availableNatureCards.Count}");
            return true;
        }
        else
        {
            Debug.LogWarning($"Carta de natureza {nature} não encontrada!");
            return false;
        }
    }
    
    public bool HasNatureCard(Natures nature)
    {
        return availableNatureCards.Contains(nature);
    }
    
    public int GetAvailableNatureCardsCount()
    {
        return availableNatureCards.Count;
    }
    
    public List<Natures> GetAvailableNatureCards()
    {
        return new List<Natures>(availableNatureCards);
    }
    
    public void ClearAllNatureCards()
    {
        availableNatureCards.Clear();
        NotifyDisplays();
        Debug.Log("Todas as cartas de natureza foram removidas.");
    }
    
    public void SyncNatureCardsWithValues()
    {
        availableNatureCards.Clear();
        
        foreach (Natures nature in Enum.GetValues(typeof(Natures)))
        {
            string key = nature.ToString();
            if (scoreboard.ContainsKey(key))
            {
                int value = scoreboard[key];
                for (int i = 0; i < value && availableNatureCards.Count < 3; i++)
                {
                    availableNatureCards.Add(nature);
                }
            }
        }
        
        NotifyDisplays();
        Debug.Log($"Cartas sincronizadas: {availableNatureCards.Count} cartas totais");
    }
    
    public void ShuffleNatures()
    {
        shuffledNatures.Clear();
        
        foreach (Natures nature in Enum.GetValues(typeof(Natures)))
        {
            shuffledNatures.Add(nature.ToString());
        }
        
        for (int i = shuffledNatures.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            string temp = shuffledNatures[i];
            shuffledNatures[i] = shuffledNatures[randomIndex];
            shuffledNatures[randomIndex] = temp;
        }
        
        Debug.Log($"Naturezas embaralhadas: [{string.Join(", ", shuffledNatures)}]");
    }
    
    public string GetNatureForSprint(int sprintNumber)
    {
        int index = sprintNumber - 1;
        
        if (index >= 0 && index < shuffledNatures.Count)
        {
            return shuffledNatures[index];
        }
        else
        {
            Debug.LogError($"Sprint inválido: {sprintNumber}");
            return shuffledNatures.Count > 0 ? shuffledNatures[0] : "Adaptacao";
        }
    }
    
    // ============ SISTEMA DE REGISTRO DE DISPLAYS ============
    
    public void RegisterDisplay(ScoreDisplay display)
    {
        if (!registeredDisplays.Contains(display))
        {
            registeredDisplays.Add(display);
            display.UpdateDisplay();
        }
    }
    
    public void UnregisterDisplay(ScoreDisplay display)
    {
        registeredDisplays.Remove(display);
    }
    
    private void NotifyDisplays()
    {
        foreach (var display in registeredDisplays)
        {
            if (display != null)
            {
                display.UpdateDisplay();
            }
        }
    }
}