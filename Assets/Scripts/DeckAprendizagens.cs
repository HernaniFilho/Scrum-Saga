using UnityEngine;

public class DeckAprendizagens : MonoBehaviour
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
            Debug.LogError("Prefab de aprendizagem ou câmera principal não está configurado.");
            return;
        }

        if (GameStateManager.Instance == null)
        {
            Debug.LogError("GameStateManager não encontrado!");
            return;
        }

        if (GameStateManager.Instance.GetCurrentState() != GameStateManager.GameState.SprintRetrospective)
        {
            Debug.Log("Cartas de aprendizagem só podem ser pegas durante o Sprint Retrospective!");
            return;
        }

        // Verificar se pode pegar carta através do SprintRetrospectiveManager
        if (SprintRetrospectiveManager.Instance != null && !SprintRetrospectiveManager.Instance.CanPegarCarta())
        {
            Debug.Log("Carta já foi pega ou removida!");
            return;
        }

        Debug.Log("Deck de aprendizagem clicada. Instanciando...");
        
        // Calcula a posição para spawnar: na frente da câmera + 160px para a direita
        Vector3 screenPos = playerCamera.WorldToScreenPoint(playerCamera.transform.position + playerCamera.transform.forward * spawnDistance);
        screenPos.x += OffsetManager.Instance != null ? OffsetManager.Instance.xOffset : 160f;
        Vector3 spawnPosition = playerCamera.ScreenToWorldPoint(screenPos);

        Quaternion rotation = Quaternion.Euler(-90, 0, 180);
        // Instancia o prefab nessa posição com a rotação padrão (sem rotações extras)
        GameObject cartaInstanciada = Instantiate(prefabToSpawn, spawnPosition, rotation);
        
        // Notificar o SprintRetrospectiveManager sobre a carta pega
        if (SprintRetrospectiveManager.Instance != null)
        {
            SprintRetrospectiveManager.Instance.NotifyCartaPega(cartaInstanciada, spawnDistance, rotation);
        }
    }

    void UpdateDeckVisibility()
    {
        if (glowEffect == null) return;
        
        bool isRetrospectivePhase = GameStateManager.Instance != null && 
                                   GameStateManager.Instance.GetCurrentState() == GameStateManager.GameState.SprintRetrospective;
        
        bool hasActiveCards = FindObjectsOfType<CardAprendizagens>().Length > 0;
        
        bool shouldShowGlow = isRetrospectivePhase && (SprintRetrospectiveManager.Instance != null && 
                             (SprintRetrospectiveManager.Instance.CanPegarCarta() || hasActiveCards));
        
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
