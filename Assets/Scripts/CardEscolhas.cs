using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

    [Header("Networking")]
    public bool isSyncedCard = false; // Flag para cartas sincronizadas

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Apenas gerar dados aleatórios se não for carta sincronizada
        if (!isSyncedCard)
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
    }

    // Método para configurar carta sincronizada
    public void SetupSyncedCard(string texto, string[] trilhas, int[] pontos, bool isPO)
    {
        isSyncedCard = true;
        
        // Configurar texto
        if (textUI != null)
            textUI.text = texto;
            
        // Configurar pontuações
        scores.Clear();
        for (int i = 0; i < trilhas.Length && i < pontos.Length; i++)
        {
            scores[trilhas[i]] = pontos[i];
        }
        
        // Configurar botões
        if (scoreButton_1 != null && scoreButton_2 != null && trilhas.Length >= 2)
        {
            // Configurar textos dos botões
            var buttonText1 = scoreButton_1.GetComponentInChildren<TMP_Text>();
            var buttonText2 = scoreButton_2.GetComponentInChildren<TMP_Text>();
            
            buttonText1.text = (pontos[0] > 0 ? "+" : "") + pontos[0] + " " + trilhas[0];
            buttonText2.text = (pontos[1] > 0 ? "+" : "") + pontos[1] + " " + trilhas[1];
            
            // Remover listeners existentes
            scoreButton_1.onClick.RemoveAllListeners();
            scoreButton_2.onClick.RemoveAllListeners();
            
            // Configurar interatividade e visual apenas para PO
            scoreButton_1.interactable = isPO;
            scoreButton_2.interactable = isPO;
            
            // Desabilitar highlight para não-PO
            if (!isPO)
            {
                DisableButtonHighlight(scoreButton_1);
                DisableButtonHighlight(scoreButton_2);
            }
        }
    }
    
    void DisableButtonHighlight(Button button)
    {
        // Desabilitar cores de highlight
        var colors = button.colors;
        colors.highlightedColor = colors.normalColor;
        colors.pressedColor = colors.normalColor;
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
        
        // Desabilitar navegação
        var nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
        
        // Desabilitar Event Triggers (OnPointerEnter, OnPointerExit, etc.)
        EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            eventTrigger.enabled = false;
            Debug.Log("Event Trigger desabilitado para botão na carta");
        }
        
        // Remover todos os triggers
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            trigger.triggers.Clear();
            Debug.Log("Event Triggers removidos do botão na carta");
        }
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
