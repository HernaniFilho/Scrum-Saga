using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class CardEscolhas : MonoBehaviour
{
    [Header("Caminho dos textos usados na descrição")]
    public string textFolder = "Texts/Escolhas";
    [Header("Texto que será atualizado com o texto aleatório")]
    public TMP_Text textUI;

    [Header("Caminho do arquivo de variáveis de pontuação")]
    public string scoreVarPath = "ScoreVars/scoreVars";
    [Header("Pontuação máxima para atribuição aleatória")]
    public int maxScore = 1;

    private Dictionary<string, int> scores = new Dictionary<string, int>();
    [SerializeField] private string[] selectedScoreVars = new string[2];
    [Header("Textos de pontuação que será atualizado com as pontuações aleatórias")]
    public TMP_Text scoreTextUI_1;
    public TMP_Text scoreTextUI_2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        randomText();
        randomScoreText();
    }
    // TODO: Remover este método se não for necessário
    void OnMouseDown()
    {
        Destroy(gameObject); // Destroi o objeto quando clicado
    }

    // Update is called once per frame
    void Update()
    {

    }

    void randomText()
    {
        TextAsset[] texts = Resources.LoadAll<TextAsset>(textFolder);
        if (texts.Length == 0)
        {
            Debug.LogWarning("Nenhum texto encontrado em: " + textFolder);
            return;
        }

        TextAsset randomText = texts[Random.Range(0, texts.Length)];
        textUI.text = randomText.text;
    }

    void loadScoreVariables()
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

        foreach (string line in lines)
        {
            string varName = line.Trim();
            if (!scores.ContainsKey(varName))
            {
                scores.Add(varName, 0);
            }
        }
    }

    void assignRandomScores(int count)
    {
        if (scores.Count < count)
        {
            Debug.LogWarning("Não há pontuações suficientes para atribuir.");
            return;
        }

        if (count <= 0)
        {
            Debug.LogWarning("O número de pontuações a serem atribuídas deve ser maior que zero.");
            return;
        }

        // Randomiza as chaves do dicionário e seleciona 'count' chaves
        System.Random random = new System.Random();
        var selectedKeys = scores.Keys.OrderBy(x => random.Next()).Take(count).ToArray();
        foreach (var key in selectedKeys)
        {
            scores[key] = maxScore;
        }

        selectedScoreVars = selectedKeys;
    }

    void randomScoreText()
    {
        loadScoreVariables();
        assignRandomScores(2);

        if (scoreTextUI_1 != null)
        {
            if (scores[selectedScoreVars[0]] > 0)
            {
                scoreTextUI_1.text = $"+{scores[selectedScoreVars[0]]} {selectedScoreVars[0]}";
            }
            else
            {
                scoreTextUI_1.text = $"{scores[selectedScoreVars[0]]} {selectedScoreVars[0]}";
            }
        }
        
        if (scoreTextUI_2 != null)
        {
            if (scores[selectedScoreVars[1]] > 0)
            {
                scoreTextUI_2.text = $"+{scores[selectedScoreVars[1]]} {selectedScoreVars[1]}";
            }
            else
            {
                scoreTextUI_2.text = $"{scores[selectedScoreVars[1]]} {selectedScoreVars[1]}";
            }
        }
    }
}
