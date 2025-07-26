using UnityEngine;

public class CardEventManager : MonoBehaviour
{
    public ScoreManager scoreManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CardEscolhas.OnScoreButtonClicked += HandleScoreButtonClicked;
        CardImprevistos.OnScoreButtonClicked += HandleScoreButtonClicked;
        if (scoreManager == null)
        {
            Debug.LogWarning("ScoreManager não atribuído. Verifique se está configurado no Inspector.");
            return;
        }

    }

    // Update is called once per frame
    void onDisable()
    {
        CardEscolhas.OnScoreButtonClicked -= HandleScoreButtonClicked;
        CardImprevistos.OnScoreButtonClicked -= HandleScoreButtonClicked;
    }

    private void HandleScoreButtonClicked(string varName, int value)
    {
        Debug.Log($"Evento de pontuação recebido: {varName} com valor {value}");
        // Lógica adicional para lidar com o evento de pontuação
        if (scoreManager != null)
        {
            scoreManager.UpdateScore(varName, value);
        }
        else
        {
            Debug.LogWarning("ScoreManager não está atribuído. Não foi possível atualizar a pontuação.");
        }
    }
}
