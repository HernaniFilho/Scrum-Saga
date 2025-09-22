using UnityEngine;

public class DeckImprevistos : MonoBehaviour
{
    [Header("Carta para ser instanciada")]
    public GameObject prefabToSpawn;
    [Header("Distância de spawn da carta em relação ao jogador")]
    public float spawnDistance = 5.0f;
    [Header("Glow effect do deck")]
    public GameObject glowEffect;
    [Header("Câmera principal")]
    private Camera playerCamera;
    private Renderer glowRenderer;
    private bool lastCanPegarState = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("Câmera principal não encontrada. Certifique-se de que há uma câmera com a tag 'MainCamera'.");
        }
        
        if (glowEffect != null)
        {
            glowRenderer = glowEffect.GetComponent<Renderer>();
            if (glowRenderer == null)
                Debug.LogWarning("GlowEffect não possui Renderer para controlar visibilidade!");
        }
        
        UpdateDeckVisibility();
    }

    void Update()
    {
        UpdateDeckVisibility();
    }

    void OnMouseDown()
    {
        if (prefabToSpawn == null || playerCamera == null)
        {
            Debug.LogError("Prefab de imprevistos ou câmera principal não está configurado.");
            return;
        }

        if (GameStateManager.Instance == null)
        {
            Debug.LogError("GameStateManager não encontrado!");
            return;
        }

        if (GameStateManager.Instance.GetCurrentState() != GameStateManager.GameState.Imprevisto)
        {
            Debug.Log("Cartas de imprevisto só podem ser pegas durante o estado de Imprevisto!");
            return;
        }

        // Verificar se já pode pegar carta
        if (ImprevistoManager.Instance != null && !ImprevistoManager.Instance.CanPegarCarta())
        {
            Debug.Log("Não é possível pegar carta de imprevisto no momento!");
            return;
        }

        Debug.Log("Deck de imprevistos clicada. Comprando carta de imprevisto...");
        
        // Calcula a posição para spawnar: na frente da câmera + 160px para a direita
        Vector3 screenPos = playerCamera.WorldToScreenPoint(playerCamera.transform.position + playerCamera.transform.forward * spawnDistance);
        screenPos.x += 160;
        Vector3 spawnPosition = playerCamera.ScreenToWorldPoint(screenPos);

        Quaternion rotation = Quaternion.Euler(-90, 0, 180);
        // Instancia o prefab nessa posição com a rotação padrão (sem rotações extras)
        GameObject spawnedCard = Instantiate(prefabToSpawn, spawnPosition, rotation);
        spawnedCard.transform.localScale = new Vector3(6f, 0.1f, 6f);
        Debug.Log("Carta de imprevisto comprada e instanciada na posição: " + spawnPosition);
        
        // Configurar carta para networking
        CardImprevistos cardComponent = spawnedCard.GetComponent<CardImprevistos>();
        if (cardComponent != null && ImprevistoManager.Instance != null)
        {
            ImprevistoManager.Instance.ConfigureCardForNetworking(cardComponent);
        }
        
        // Notificar o ImprevistoManager
        if (ImprevistoManager.Instance != null)
        {
            ImprevistoManager.Instance.NotifyCartaPega(spawnedCard, spawnDistance, rotation);
        }
    }

    void UpdateDeckVisibility()
    {
        if (glowEffect == null) return;
        
        bool isImprevistoPhase = GameStateManager.Instance != null && 
                                GameStateManager.Instance.GetCurrentState() == GameStateManager.GameState.Imprevisto;
        
        bool hasActiveCards = FindObjectsOfType<CardImprevistos>().Length > 0;
        
        bool shouldShowGlow = isImprevistoPhase && (ImprevistoManager.Instance != null && 
                             (ImprevistoManager.Instance.CanPegarCarta() || hasActiveCards));
        
        if (shouldShowGlow != lastCanPegarState)
        {
            lastCanPegarState = shouldShowGlow;
            
            if (glowRenderer != null)
            {
                StartCoroutine(FadeGlow(shouldShowGlow));
            }
            else
            {
                glowEffect.SetActive(shouldShowGlow);
            }
        }
    }
    
    System.Collections.IEnumerator FadeGlow(bool fadeIn)
    {
        Material material = glowRenderer.material;
        float startAlpha = fadeIn ? 0f : 1f;
        float targetAlpha = fadeIn ? 1f : 0f;
        float duration = 0.5f;
        
        if (fadeIn)
            glowEffect.SetActive(true);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            
            Color color = material.color;
            color.a = alpha;
            material.color = color;
            
            yield return null;
        }
        
        if (!fadeIn)
            glowEffect.SetActive(false);
    }
}
