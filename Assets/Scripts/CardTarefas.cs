using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class CardTarefas : MonoBehaviour
{
    public string imageFolder = "Images/Tarefas";
    public MeshRenderer meshRenderer;
    [Header("Pontuação máxima para atribuição aleatória")]
    public int maxScore = 1;

    public Dictionary<string, int> scores = new Dictionary<string, int>();
    [Header("Textos para exibir as pontuações")]
    public TMP_Text scoreText_1;
    public TMP_Text scoreText_2;
    public TMP_Text scoreText_3;
    public TMP_Text scoreText_4;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        randomTexture();
        loadScoreVariables();
        if (scoreText_1 == null || scoreText_2 == null || scoreText_3 == null || scoreText_4 == null)
        {
            Debug.LogWarning("Textos de pontuação não atribuídos. Verifique se estão configurados no Inspector.");
            return;
        }
        
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
            setupScoreText(scoreText_4, scoreKeys[1]);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnMouseDown()
    {
        Destroy(gameObject); // Destroi o objeto quando clicado
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
        Texture2D[] textures = Resources.LoadAll<Texture2D>(imageFolder);
        if (textures.Length == 0)
        {
            Debug.LogWarning("Nenhuma textura encontrada em: " + imageFolder);
            return;
        }

        Texture2D randomTexture = textures[Random.Range(0, textures.Length)];
        
        Material material = new Material(Shader.Find("Unlit/Texture"));
        material.mainTexture = randomTexture;
        material.SetTexture("_MainTex", randomTexture);
        meshRenderer.material = material;
    }
}
