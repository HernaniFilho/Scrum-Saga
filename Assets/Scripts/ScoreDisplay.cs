using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ScoreDisplay : MonoBehaviour
{
    [Header("Textos dos Nomes das Pontuações")]
    public TMP_Text scoreNameText_1;
    public TMP_Text scoreNameText_2;
    public TMP_Text scoreNameText_3;
    
    [Header("Textos dos Valores das Pontuações")]
    public TMP_Text scoreValueText_1;
    public TMP_Text scoreValueText_2;
    public TMP_Text scoreValueText_3;
    
    [Header("Sliders das Pontuações")]
    public Slider scoreSlider_1;
    public Slider scoreSlider_2;
    public Slider scoreSlider_3;
    
    [Header("GameObjects das Cartas de Natureza")]
    public GameObject natureCardObject_1;
    public GameObject natureCardObject_2;
    public GameObject natureCardObject_3;
    
    // Cores para animação dos sliders
    private Color defaultColor = new Color(0f, 0.588f, 1f, 1f); // #0096FF
    private Color gainColor = new Color(0.098f, 0.82f, 0.02f, 1f); // #19D205
    private Color lossColor = new Color(1f, 0f, 0f, 1f); // #FF0000
    
    // Valores anteriores para animação
    private Dictionary<string, float> previousValues = new Dictionary<string, float>();
    private bool isFirstUpdate = true;
    
    // Referências aos componentes
    private Dictionary<string, TMP_Text> nameTexts = new Dictionary<string, TMP_Text>();
    private Dictionary<string, TMP_Text> valueTexts = new Dictionary<string, TMP_Text>();
    private Dictionary<string, Slider> sliders = new Dictionary<string, Slider>();
    private Dictionary<string, Image> sliderFillImages = new Dictionary<string, Image>();
    private Image[] natureCardImages;

    void Start()
    {
        InitializeReferences();
        
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterDisplay(this);
        }
        else
        {
            Debug.LogError("ScoreManager.Instance não encontrado!");
        }
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.UnregisterDisplay(this);
        }
    }
    
    private void InitializeReferences()
    {
        // Configurar dicionários de ScoreTypes
        nameTexts[ScoreManager.ScoreType.Entrosamento.ToString()] = scoreNameText_1;
        nameTexts[ScoreManager.ScoreType.Produtividade.ToString()] = scoreNameText_2;
        nameTexts[ScoreManager.ScoreType.Expectativa.ToString()] = scoreNameText_3;
        
        valueTexts[ScoreManager.ScoreType.Entrosamento.ToString()] = scoreValueText_1;
        valueTexts[ScoreManager.ScoreType.Produtividade.ToString()] = scoreValueText_2;
        valueTexts[ScoreManager.ScoreType.Expectativa.ToString()] = scoreValueText_3;
        
        sliders[ScoreManager.ScoreType.Entrosamento.ToString()] = scoreSlider_1;
        sliders[ScoreManager.ScoreType.Produtividade.ToString()] = scoreSlider_2;
        sliders[ScoreManager.ScoreType.Expectativa.ToString()] = scoreSlider_3;
        
        // Configurar sliders e capturar fill images
        foreach (var kvp in sliders)
        {
            if (kvp.Value != null)
            {
                kvp.Value.minValue = 0f;
                kvp.Value.maxValue = 10f;
                
                Image fillImage = kvp.Value.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    sliderFillImages[kvp.Key] = fillImage;
                    fillImage.color = defaultColor;
                }
                
                previousValues[kvp.Key] = 0f;
            }
        }
        
        // Configurar cartas de natureza
        GameObject[] cardObjects = new GameObject[] { natureCardObject_1, natureCardObject_2, natureCardObject_3 };
        natureCardImages = new Image[3];
        
        for (int i = 0; i < 3; i++)
        {
            if (cardObjects[i] != null)
            {
                natureCardImages[i] = cardObjects[i].GetComponent<Image>();
            }
        }
    }
    
    public void UpdateDisplay()
    {
        if (ScoreManager.Instance == null) return;

        UpdateScoreType(ScoreManager.ScoreType.Entrosamento);
        UpdateScoreType(ScoreManager.ScoreType.Produtividade);
        UpdateScoreType(ScoreManager.ScoreType.Expectativa);
        
        UpdateNatureCards();
        
        isFirstUpdate = false;
    }

    private void UpdateScoreType(ScoreManager.ScoreType type)
    {
        string key = type.ToString();
        if (!ScoreManager.Instance.scoreboard.ContainsKey(key)) return;
        
        int value = ScoreManager.Instance.scoreboard[key];
        
        // Atualizar nome
        if (nameTexts.ContainsKey(key) && nameTexts[key] != null)
        {
            nameTexts[key].text = type.ToString();
        }
        
        // Animar slider
        if (sliders.ContainsKey(key) && sliders[key] != null)
        {
            StartCoroutine(AnimateSlider(key, value));
        }
    }
    
    private IEnumerator AnimateSlider(string key, int newValue)
    {
        if (!sliders.ContainsKey(key)) yield break;
        
        Slider slider = sliders[key];
        float targetValue = newValue;

        if (isFirstUpdate)
        {
            slider.value = targetValue;
            if (valueTexts.ContainsKey(key) && valueTexts[key] != null)
            {
                valueTexts[key].text = $"{newValue}/10";
            }
            previousValues[key] = targetValue;
            yield break;
        }
        
        float previousValue = previousValues.ContainsKey(key) ? previousValues[key] : newValue;
        
        // Se não há mudança, apenas atualizar
        if (Mathf.Approximately(previousValue, targetValue))
        {
            slider.value = targetValue;
            if (valueTexts.ContainsKey(key) && valueTexts[key] != null)
            {
                valueTexts[key].text = $"{newValue}/10";
            }
            previousValues[key] = targetValue;
            yield break;
        }
        
        // Determinar tipo de mudança
        bool isGain = targetValue > previousValue;
        bool isLoss = targetValue < previousValue;
        
        // Mudar cor
        if (sliderFillImages.ContainsKey(key))
        {
            Image fillImage = sliderFillImages[key];
            if (isGain)
                fillImage.color = gainColor;
            else if (isLoss)
                fillImage.color = lossColor;
        }
        
        // Calcular duração: 2 segundos por ponto
        float pointDifference = Mathf.Abs(targetValue - previousValue);
        float animationDuration = pointDifference * 2f;
        
        previousValues[key] = targetValue;
        
        // Animar
        float elapsedTime = 0f;
        int lastDisplayedValue = Mathf.RoundToInt(previousValue);
        
        while (elapsedTime < animationDuration)
        {
            float progress = elapsedTime / animationDuration;
            float currentValue = Mathf.Lerp(previousValue, targetValue, progress);
            slider.value = currentValue;
            
            int currentIntValue = Mathf.RoundToInt(currentValue);
            if (currentIntValue != lastDisplayedValue && valueTexts.ContainsKey(key) && valueTexts[key] != null)
            {
                valueTexts[key].text = $"{currentIntValue}/10";
                lastDisplayedValue = currentIntValue;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Valor final
        slider.value = targetValue;
        if (valueTexts.ContainsKey(key) && valueTexts[key] != null)
        {
            valueTexts[key].text = $"{newValue}/10";
        }
        
        // Aguardar antes do fade
        yield return new WaitForSeconds(0.2f);
        
        // Fade de cor
        if (sliderFillImages.ContainsKey(key))
        {
            Image fillImage = sliderFillImages[key];
            Color startColor = fillImage.color;
            float fadeDuration = 0.5f;
            float fadeElapsedTime = 0f;
            
            while (fadeElapsedTime < fadeDuration)
            {
                slider.value = targetValue;
                
                float fadeProgress = fadeElapsedTime / fadeDuration;
                fillImage.color = Color.Lerp(startColor, defaultColor, fadeProgress);
                fadeElapsedTime += Time.deltaTime;
                yield return null;
            }
            
            slider.value = targetValue;
            fillImage.color = defaultColor;
        }
    }

    private void UpdateNatureCards()
    {
        if (natureCardImages == null || ScoreManager.Instance == null) return;
        
        var cards = ScoreManager.Instance.GetAvailableNatureCards();
        
        for (int i = 0; i < 3; i++)
        {
            if (natureCardImages[i] != null)
            {
                if (i < cards.Count)
                {
                    Sprite sprite = ScoreManager.Instance.GetNatureSprite(cards[i]);
                    if (sprite != null)
                    {
                        natureCardImages[i].sprite = sprite;
                    }
                }
                else
                {
                    if (ScoreManager.Instance.emptySlotSprite != null)
                    {
                        natureCardImages[i].sprite = ScoreManager.Instance.emptySlotSprite;
                    }
                }
            }
        }
    }
}