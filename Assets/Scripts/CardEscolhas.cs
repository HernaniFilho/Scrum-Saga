using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CardEscolhas : MonoBehaviour
{
    [Header("Caminho dos textos usados na descrição")]
    public string textFolder = "Texts/Escolhas";
    [Header("Texto que será atualizado com o texto aleatório")]
    public TMP_Text textUI;
    [Header("Pontuação máxima para atribuição aleatória")]
    public int maxScore = 1;

    public Dictionary<string, int> scores = new Dictionary<string, int>();
    [Header("Botoes com os handlers para as pontuações")]
    public Button scoreButton_1;
    public Button scoreButton_2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        randomText();
        loadScoreVariables();
        if (scoreButton_1 == null || scoreButton_2 == null)
        {
            Debug.LogWarning("Botões de pontuação não atribuídos. Verifique se estão configurados no Inspector.");
            return;
        }
        setupScoreButton(scoreButton_1, scores.Keys.ElementAt(0));
        setupScoreButton(scoreButton_2, scores.Keys.ElementAt(1));
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
        ScoreManager.ScoreType[] randomTypes = GetTwoRandomScoreTypes();
        if (randomTypes.Length < 2)
        {
            Debug.LogWarning("Não há pontuações suficientes para selecionar aleatoriamente.");
            return;
        }
        scores.Clear();
        foreach (var type in randomTypes)
        {
            string varName = type.ToString();
            if (!scores.ContainsKey(varName))
            {
                scores.Add(varName, maxScore);
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

        // Embaralha a lista
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]); // Troca
        }

        // Pega os dois primeiros elementos
        return new ScoreManager.ScoreType[] { list[0], list[1] };
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
            ScoreManager.Instance.UpdateScore(varName, value);
            Destroy(gameObject); // Exemplo de ação ao clicar no botão
        });
    }

    
}
