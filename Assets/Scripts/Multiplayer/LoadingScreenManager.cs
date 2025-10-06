using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("UI Components - Manual Assignment")]
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingText;

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
}
