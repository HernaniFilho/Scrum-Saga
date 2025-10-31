using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CardTarefas : MonoBehaviour
{
    public string imageFolder = "Images/Tarefas/Cartas_Novas";
    public Image image;
    [Header("Pontuação máxima para atribuição aleatória")]
    public int maxScore = 1;

    public Dictionary<string, int> scores = new Dictionary<string, int>();
    [Header("Textos para exibir as pontuações")]
    public TMP_Text scoreText_1;
    public TMP_Text scoreText_2;
    public TMP_Text scoreText_3;
    public TMP_Text scoreText_4;
    
    [Header("Estado da carta")]
    public bool isSelected = false;
    private bool customInitialized = false;
    private bool hasPredefinedScores = false;

    public void InitializeWithCustomData(Dictionary<string, int> customScores, Sprite customSprite, int customMaxScore = 1)
    {
        customInitialized = true;
        isSelected = true;
        
        scores = new Dictionary<string, int>(customScores);
        maxScore = customMaxScore;
        
        if (customSprite != null && image != null)
        {
            image.sprite = customSprite;
        }
    }

    public void SetPredefinedScores(Dictionary<string, int> predefinedScores)
    {
        if (predefinedScores != null && predefinedScores.Count > 0)
        {
            scores = new Dictionary<string, int>(predefinedScores);
            hasPredefinedScores = true;
            
            maxScore = 0;
            foreach (var score in scores.Values)
            {
                if (score > maxScore) maxScore = score;
            }
            
            Debug.Log($"Pontuações pré-definidas aplicadas: {string.Join(", ", scores.Keys)}");
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (customInitialized)
        {
            if (scoreText_1 != null && scoreText_2 != null && scoreText_3 != null && scoreText_4 != null)
            {
                UpdateScoreTexts();
            }
            return;
        }
        
        // Só aplica sprite aleatório se não houver sprite definido (evita sobrescrever cartas selecionadas)
        if (image != null && image.sprite == null)
        {
            randomTexture();
        }
        
        // Só gera novas pontuações se a carta não foi selecionada e não tem pontuações pré-definidas
        if (!isSelected && scores.Count == 0 && !hasPredefinedScores)
        {
            loadScoreVariables();
        }
        
        if (scoreText_1 == null || scoreText_2 == null || scoreText_3 == null || scoreText_4 == null)
        {
            Debug.LogWarning("Textos de pontuação não atribuídos. Verifique se estão configurados no Inspector.");
            return;
        }
        
        UpdateScoreTexts();
    }
    
    public void UpdateScoreTexts()
    {
        scoreText_1.text = "";
        scoreText_2.text = "";
        scoreText_3.text = "";
        scoreText_4.text = "";
        
        var scoreKeys = scores.Keys.ToArray();
        if (scores.Count == 1)
        {
            // +2 em uma trilha
            setupScoreText(scoreText_3, scoreKeys[0]);
            setupScoreTextWithCustomValue(scoreText_4, scoreKeys[0], 1);
        }
        else if (scores.Count == 2)
        {
            // +1 em duas trilhas
            setupScoreText(scoreText_1, scoreKeys[0]);
            setupScoreText(scoreText_2, scoreKeys[1]);
            setupScoreText(scoreText_4, scoreKeys[0]);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnMouseDown()
    {
        if (ResetGameManager.IsResetPopupOpen || RestartPhaseManager.IsRestartPopupOpen)
        {
            return;
        }
        
        // Só destrói se não foi selecionada
        if (!isSelected)
        {
            Destroy(gameObject);
        }
    }

    void loadScoreVariables()
    {
        scores.Clear();
        
        // Decide se será +1 em 2 trilhas ou +2 em 1 trilha
        bool isDublePointSingle = Random.Range(0f, 1f) > 0.5f;
        
        if (isDublePointSingle)
        {
            // +2 em uma trilha
            ScoreManager.ScoreType[] allTypes = (ScoreManager.ScoreType[])Enum.GetValues(typeof(ScoreManager.ScoreType));
            ScoreManager.ScoreType selectedType = allTypes[Random.Range(0, allTypes.Length)];
            scores.Add(selectedType.ToString(), 2);
        }
        else
        {
            // +1 em duas trilhas
            ScoreManager.ScoreType[] randomTypes = GetTwoRandomScoreTypes();
            if (randomTypes.Length < 2)
            {
                Debug.LogWarning("Não há pontuações suficientes para selecionar aleatoriamente.");
                return;
            }
            
            foreach (var type in randomTypes)
            {
                string varName = type.ToString();
                if (!scores.ContainsKey(varName))
                {
                    scores.Add(varName, 1);
                }
            }
        }
    }

    ScoreManager.ScoreType[] GetTwoRandomScoreTypes()
    {
        ScoreManager.ScoreType[] values = (ScoreManager.ScoreType[])Enum.GetValues(typeof(ScoreManager.ScoreType));
        if (values.Length < 2)
        {
            Debug.LogWarning("Não há pontuações suficientes para selecionar aleatoriamente.");
            return new ScoreManager.ScoreType[0];
        }
        List<ScoreManager.ScoreType> list = new List<ScoreManager.ScoreType>(values);

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return new ScoreManager.ScoreType[] { list[0], list[1] };
    }

    void setupScoreText(TMP_Text textComponent, string varName)
    {
        int value = scores[varName];
        textComponent.text = (value > 0 ? "+" : "") + value + " " + varName;
    }

    void setupScoreTextWithCustomValue(TMP_Text textComponent, string varName, int customValue)
    {
        textComponent.text = (customValue > 0 ? "+" : "") + customValue + " " + varName;
    }

    void randomTexture()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(imageFolder);
        if (sprites.Length == 0)
        {
            Debug.LogWarning("Nenhum sprite encontrado em: " + imageFolder);
            return;
        }

        // Criar instância do UsedCardsManager se não existir
        if (UsedCardsManager.Instance == null)
        {
            GameObject usedCardsGO = new GameObject("UsedCardsManager");
            usedCardsGO.AddComponent<UsedCardsManager>();
        }

        Sprite selectedSprite = null;
        int maxAttempts = sprites.Length * 2; // Evita loop infinito
        int attempts = 0;

        do
        {
            selectedSprite = sprites[Random.Range(0, sprites.Length)];
            attempts++;
            
            if (attempts >= maxAttempts)
            {
                Debug.LogWarning($"Todos os sprites podem estar sendo usados. Usando sprite: {selectedSprite.name}");
                break;
            }
        }
        while (UsedCardsManager.Instance.IsCardUsed(selectedSprite.name));
        
        image.sprite = selectedSprite;
    }
}
