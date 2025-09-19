using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

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

    [Header("Textos dos Nomes das Pontuações")]
    public TMP_Text scoreNameText_1; // Entrosamento
    public TMP_Text scoreNameText_2; // Produtividade
    public TMP_Text scoreNameText_3; // Expectativa
    
    [Header("Textos dos Valores das Pontuações (formato ?/{max})")]
    public TMP_Text scoreValueText_1; // Entrosamento
    public TMP_Text scoreValueText_2; // Produtividade
    public TMP_Text scoreValueText_3; // Expectativa
    
    [Header("Sliders das Pontuações")]
    public Slider scoreSlider_1; // Entrosamento
    public Slider scoreSlider_2; // Produtividade
    public Slider scoreSlider_3; // Expectativa
    
    [Header("Referências de Natureza")]
    public TMP_Text natureText_1;
    public TMP_Text natureText_2;
    public TMP_Text natureText_3;

    [Header("Referências aos Textos de Natureza e Pontuação na UI")]
    public Dictionary<string, TMP_Text> scoreTexts = new Dictionary<string, TMP_Text>();
    private Dictionary<string, TMP_Text> scoreNameTexts = new Dictionary<string, TMP_Text>();
    private Dictionary<string, TMP_Text> scoreValueTexts = new Dictionary<string, TMP_Text>();
    private Dictionary<string, Slider> scoreSliders = new Dictionary<string, Slider>();
    
    // Cores para animação dos sliders
    private Color defaultColor = new Color(0f, 0.588f, 1f, 1f); // #0096FF
    private Color gainColor = new Color(0.098f, 0.82f, 0.02f, 1f); // #19D205;
    private Color lossColor = new Color(1f, 0f, 0f, 1f); // #FF0000
    
    // Dicionário para armazenar valores anteriores dos sliders
    private Dictionary<string, float> previousSliderValues = new Dictionary<string, float>();
    
    // Dicionário para armazenar as imagens fill dos sliders
    private Dictionary<string, Image> sliderFillImages = new Dictionary<string, Image>();

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

    void Start()
    {
        // Verificar referências dos textos de nomes
        if (scoreNameText_1 == null || scoreNameText_2 == null || scoreNameText_3 == null)
        {
            Debug.LogError("Referências dos textos de nomes das pontuações não atribuídas!");
            return;
        }
        
        // Verificar referências dos textos de valores
        if (scoreValueText_1 == null || scoreValueText_2 == null || scoreValueText_3 == null)
        {
            Debug.LogError("Referências dos textos de valores das pontuações não atribuídas!");
            return;
        }
        
        // Verificar referências dos sliders
        if (scoreSlider_1 == null || scoreSlider_2 == null || scoreSlider_3 == null)
        {
            Debug.LogError("Referências dos sliders das pontuações não atribuídas!");
            return;
        }
        
        // Verificar referências de natureza
        if (natureText_1 == null || natureText_2 == null || natureText_3 == null)
        {
            Debug.LogError("Referências de natureza da UI não atribuídas!");
            return;
        }

        // Configurar dicionários para ScoreTypes
        scoreNameTexts[ScoreType.Entrosamento.ToString()] = scoreNameText_1;
        scoreNameTexts[ScoreType.Produtividade.ToString()] = scoreNameText_2;
        scoreNameTexts[ScoreType.Expectativa.ToString()] = scoreNameText_3;
        
        scoreValueTexts[ScoreType.Entrosamento.ToString()] = scoreValueText_1;
        scoreValueTexts[ScoreType.Produtividade.ToString()] = scoreValueText_2;
        scoreValueTexts[ScoreType.Expectativa.ToString()] = scoreValueText_3;
        
        scoreSliders[ScoreType.Entrosamento.ToString()] = scoreSlider_1;
        scoreSliders[ScoreType.Produtividade.ToString()] = scoreSlider_2;
        scoreSliders[ScoreType.Expectativa.ToString()] = scoreSlider_3;
        
        // Configurar dicionários para Natures
        scoreTexts[Natures.Adaptacao.ToString()] = natureText_1;
        scoreTexts[Natures.Inspecao.ToString()] = natureText_2;
        scoreTexts[Natures.Transparencia.ToString()] = natureText_3;

        // Configurar sliders com valores mínimo e máximo e capturar images fill
        foreach (var kvp in scoreSliders)
        {
            string key = kvp.Key;
            Slider slider = kvp.Value;
            
            slider.minValue = 0f;
            slider.maxValue = 10f;
            
            // Capturar a imagem fill do slider para mudanças de cor
            Image fillImage = slider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                sliderFillImages[key] = fillImage;
                fillImage.color = defaultColor;
            }
            
            // Inicializar valores anteriores
            previousSliderValues[key] = 0f;
        }

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
            
            // Para loadScore, definir o valor anterior igual ao atual para evitar animação desnecessária
            previousSliderValues[key] = value;
            
            // Atualizar display sem animação no carregamento inicial
            if (scoreNameTexts.ContainsKey(key))
                scoreNameTexts[key].text = type.ToString();
            if (scoreValueTexts.ContainsKey(key))
                scoreValueTexts[key].text = $"{value}/10";
            if (scoreSliders.ContainsKey(key))
                scoreSliders[key].value = value;
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
            else if (scoreboard[varName] + value > 10)
            {
                if (scoreboard[varName] == 10)
                {
                    Debug.LogWarning($"Tentativa de aumentar '{varName}' acima de 10. Operação ignorada.");
                    return false;
                }
                else
                {
                    scoreboard[varName] = 10;
                }
            }
            else
            {
                scoreboard[varName] += value;
            }

            Debug.Log($"Pontuação atualizada: {varName} = {scoreboard[varName]}");
            
            // Para ScoreTypes usar display animado, para Natures usar UpdateScoreTexts
            if (Enum.TryParse<ScoreType>(varName, out ScoreType scoreType))
            {
                UpdateScoreDisplay(varName, scoreboard[varName]);
            }
            else
            {
                UpdateScoreTexts(varName, scoreboard[varName]);
            }

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

    public void UpdateScoreDisplay(string varName, int value)
    {
        // Apenas atualiza displays dos ScoreTypes, não das Natures
        if (Enum.TryParse<ScoreType>(varName, out ScoreType scoreType))
        {
            // Atualizar texto do nome
            if (scoreNameTexts.ContainsKey(varName))
            {
                scoreNameTexts[varName].text = scoreType.ToString();
            }
            
            // Animar slider com cor e movimento gradual (texto será atualizado durante animação)
            if (scoreSliders.ContainsKey(varName))
            {
                StartCoroutine(AnimateSlider(varName, value));
            }
            
            Debug.Log($"Display atualizado para {varName}: {value}/10");
        }
    }

    private IEnumerator AnimateSlider(string varName, int newValue)
    {
        if (!scoreSliders.ContainsKey(varName))
            yield break;
            
        Slider slider = scoreSliders[varName];
        float previousValue = previousSliderValues.ContainsKey(varName) ? previousSliderValues[varName] : slider.value;
        float targetValue = newValue;
        
        // Se não há mudança, apenas atualizar o valor sem animação
        if (Mathf.Approximately(previousValue, targetValue))
        {
            slider.value = targetValue;
            previousSliderValues[varName] = targetValue;
            yield break;
        }
        
        // Determinar se é ganho ou perda
        bool isGain = targetValue > previousValue;
        bool isLoss = targetValue < previousValue;
        
        // Mudar cor do slider baseado no tipo de mudança
        if (sliderFillImages.ContainsKey(varName))
        {
            Image fillImage = sliderFillImages[varName];
            
            if (isGain)
            {
                fillImage.color = gainColor;
            }
            else if (isLoss)
            {
                fillImage.color = lossColor;
            }
        }
        
        // Calcular duração da animação: 2 segundos por ponto
        float pointDifference = Mathf.Abs(targetValue - previousValue);
        float animationDuration = pointDifference * 2f;
        
        // Atualizar valor anterior imediatamente para evitar múltiplas animações
        previousSliderValues[varName] = targetValue;
        
        // Animar o valor gradualmente
        float elapsedTime = 0f;
        int lastDisplayedValue = Mathf.RoundToInt(previousValue);
        
        while (elapsedTime < animationDuration)
        {
            float progress = elapsedTime / animationDuration;
            float currentValue = Mathf.Lerp(previousValue, targetValue, progress);
            slider.value = currentValue;
            
            // Atualizar texto do valor apenas quando passar por um valor inteiro
            int currentIntValue = Mathf.RoundToInt(currentValue);
            if (currentIntValue != lastDisplayedValue && scoreValueTexts.ContainsKey(varName))
            {
                scoreValueTexts[varName].text = $"{currentIntValue}/10";
                lastDisplayedValue = currentIntValue;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Garantir valor final exato
        slider.value = targetValue;
        if (scoreValueTexts.ContainsKey(varName))
        {
            scoreValueTexts[varName].text = $"{newValue}/10";
        }
        
        // Aguardar 0.2 segundos antes de começar o fade (reduzido de 0.5s)
        yield return new WaitForSeconds(0.2f);
        
        // Fade de cor apenas se ainda temos a referência
        if (sliderFillImages.ContainsKey(varName))
        {
            Image fillImage = sliderFillImages[varName];
            
            // Fade gradual de volta para a cor padrão em 0.5 segundos (reduzido de 1s)
            Color startColor = fillImage.color;
            float fadeDuration = 0.5f;
            float fadeElapsedTime = 0f;
            
            while (fadeElapsedTime < fadeDuration)
            {
                // Garantir que o slider mantém o valor correto durante o fade
                slider.value = targetValue;
                
                float fadeProgress = fadeElapsedTime / fadeDuration;
                fillImage.color = Color.Lerp(startColor, defaultColor, fadeProgress);
                fadeElapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Garantir valores finais exatos
            slider.value = targetValue;
            fillImage.color = defaultColor;
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
        
        // Atualizar displays após reset
        foreach (ScoreType type in Enum.GetValues(typeof(ScoreType)))
        {
            string key = type.ToString();
            if (scoreboard.ContainsKey(key))
            {
                UpdateScoreDisplay(key, scoreboard[key]);
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
