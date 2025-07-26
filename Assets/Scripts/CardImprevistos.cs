using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CardImprevistos : MonoBehaviour
{
    [Header("Caminho dos textos usados na descrição")]
    public string textFolder = "Texts/Imprevistos";
    [Header("Texto que será atualizado com o texto aleatório")]
    public TMP_Text textUI;

    [Header("Caminho das imagens de natureza usadas no card")]
    public string imageFolder = "Images/Naturezas";
    [Header("Caminho do material branco usado para preencher espaços vazios")]
    public string whiteMaterialPath = "Materials/White";
    public MeshRenderer[] meshRenderers;

    [Header("Caminho do arquivo de variáveis de pontuação")]
    public string scoreVarPath = "ScoreVars/scoreVars";
    [Header("Pontuação máxima para atribuição aleatória")]
    public int maxScore = -1;

    public Dictionary<string, int> scores = new Dictionary<string, int>();
    private string[] selectedScoreVars = new string[2];
    [Header("Botoes com os handlers para as pontuações")]
    public Button scoreButton_1;
    public Button scoreButton_2;

    public static event Action<string, int> OnScoreButtonClicked;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        randomText();
        randomImages();
        loadScoreVariables();
        assignRandomScores(2);
        if (scoreButton_1 == null || scoreButton_2 == null)
        {
            Debug.LogWarning("Botões de pontuação não atribuídos. Verifique se estão configurados no Inspector.");
            return;
        }
        setupScoreButton(scoreButton_1, selectedScoreVars[0]);
        setupScoreButton(scoreButton_2, selectedScoreVars[1]);
    }
    // TODO: Remover este método se não for necessário
    void OnMouseDown()
    {
        //Destroy(gameObject); // Destroi o objeto quando clicado
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

    void randomImages()
    {
        if (meshRenderers == null)
        {
            Debug.LogWarning("Nenhum MeshRenderer atribuído para receber as imagens aleatórias.");
            return;
        }
        Texture[] textures = Resources.LoadAll<Texture>(imageFolder);
        Material whiteMaterial = Resources.Load<Material>(whiteMaterialPath);

        if (textures.Length == 0)
        {
            Debug.LogWarning("Nenhuma textura encontrada em: " + imageFolder);
            return;
        }

        if (whiteMaterial == null)
        {
            Debug.LogWarning("Material branco não encontrado em: " + whiteMaterialPath);
            return;
        }

        int quantity = Random.Range(1, 3); // 1 ou 2


        System.Random random = new System.Random();
        Texture[] randomTextures = textures.OrderBy(x => random.Next()).ToArray();

        for (int i = 0; i < meshRenderers.Length; i++)
    {
        if (i < quantity)
        {
            // Aplica textura aleatória
            Material mat = new Material(meshRenderers[i].material);
            mat.mainTexture = randomTextures[i % randomTextures.Length];
            meshRenderers[i].material = mat;
        }
        else
        {
            // Preenche com branco
            meshRenderers[i].material = whiteMaterial;
        }
    }
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

    void setupScoreButton(Button button, string varName)
    {
        int value = scores[varName];
        var buttonText = button.GetComponentInChildren<TMP_Text>();
        buttonText.text = (value > 0 ? "+" : "") + value + " " + varName;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            Debug.Log($"Clicou no botão de pontuação: {varName} com valor {value}");
            OnScoreButtonClicked?.Invoke(varName, value);
            Destroy(gameObject); // Exemplo de ação ao clicar no botão
        });
    }
}
