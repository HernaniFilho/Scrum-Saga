using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("UI Components")]
    private GameObject loadingPanel;
    private TextMeshProUGUI loadingText;
    private Canvas loadingCanvas;
    
    private void Awake()
    {
        CreateLoadingUI();
        HideLoadingScreen();
    }

    public void ShowLoadingScreen(string message = "Conectando...")
    {
        if (loadingPanel != null)
        {
            loadingText.text = message;
            loadingPanel.SetActive(true);
        }
    }

    public void HideLoadingScreen()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    public void UpdateLoadingMessage(string message)
    {
        if (loadingText != null)
        {
            loadingText.text = message;
        }
    }

    void CreateLoadingUI()
    {
        if (loadingPanel != null) return;

        // Criar painel principal com fundo branco (semelhante ao NameInput)
        GameObject panelObj = new GameObject("LoadingScreenPanel");

        // Define como filho do CanvasPrincipal
        GameObject canvasPrincipal = GameObject.Find("CanvasPrincipal");
        if (canvasPrincipal == null)
        {
            Debug.LogError("CanvasPrincipal não encontrado na cena!");
            return;
        }
        panelObj.transform.SetParent(canvasPrincipal.transform, false);

        loadingPanel = panelObj;
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = Color.white; // Fundo branco como no NameInput

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Texto de loading centralizado
        CreateLoadingText(panelObj);

        Debug.Log("Loading Screen UI criada");
    }

    void CreateLoadingText(GameObject parent)
    {
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(parent.transform, false);
        
        loadingText = textObj.AddComponent<TextMeshProUGUI>();
        loadingText.text = "Conectando...";
        loadingText.fontSize = 28;
        loadingText.color = Color.black;
        loadingText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = loadingText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(600, 100);
    }
}
