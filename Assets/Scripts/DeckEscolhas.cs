using UnityEngine;
using System.Collections;

public class DeckEscolhas : MonoBehaviour
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
            Debug.LogError("Prefab de escolhas ou câmera principal não está configurado.");
            return;
        }

        if (GameStateManager.Instance == null)
        {
            Debug.LogError("GameStateManager não encontrado!");
            return;
        }

        if (GameStateManager.Instance.GetCurrentState() != GameStateManager.GameState.Escolha)
        {
            Debug.Log("Cartas de escolha só podem ser pegas durante o estado de Escolha!");
            return;
        }

        // Verificar se pode pegar carta (networking)
        if (EscolhaManager.Instance != null && !EscolhaManager.Instance.CanPegarCarta())
        {
            Debug.Log("Carta já foi pega por outro jogador!");
            return;
        }

        ProductOwnerManager productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        bool isPO = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        if (!isPO)
        {
            Debug.Log("Apenas o PO pode coletar a carta!");
            return;
        }

        Debug.Log("Deck de escolhas clicada. Comprando carta de escolha...");
        
        // Calcula a posição para spawnar: na frente da câmera + 160px para a direita
        Vector3 screenPos = playerCamera.WorldToScreenPoint(playerCamera.transform.position + playerCamera.transform.forward * spawnDistance);
        screenPos.x += OffsetManager.Instance != null ? OffsetManager.Instance.xOffset : 160f;
        Vector3 spawnPosition = playerCamera.ScreenToWorldPoint(screenPos);

        Quaternion rotation = Quaternion.Euler(-90, 0, 180);
        
        // Instancia o prefab localmente
        GameObject carta = Instantiate(prefabToSpawn, spawnPosition, rotation);
        Debug.Log("Carta de escolha comprada e instanciada na posição: " + spawnPosition);
        
        // Aguardar um frame para garantir que a carta foi inicializada
        StartCoroutine(ConfigureCartaAfterFrame(carta, rotation));
    }
    IEnumerator ConfigureCartaAfterFrame(GameObject carta, Quaternion rotation)
    {
        // Aguardar um frame para garantir que Start() da carta foi chamado
        yield return new WaitForEndOfFrame();
        
        // Configurar a carta para networking
        CardEscolhas cardComponent = carta.GetComponent<CardEscolhas>();
        if (cardComponent != null && EscolhaManager.Instance != null)
        {
            EscolhaManager.Instance.ConfigureCardForNetworking(cardComponent);
            
            // Notificar EscolhaManager para sincronizar com outros jogadores
            EscolhaManager.Instance.NotifyCartaPega(carta, spawnDistance, rotation);
        }
    }

    void UpdateDeckVisibility()
    {
        if (glowEffect == null) return;
        
        bool isEscolhaPhase = GameStateManager.Instance != null && 
                             GameStateManager.Instance.GetCurrentState() == GameStateManager.GameState.Escolha;
        
        bool hasActiveCards = FindObjectsOfType<CardEscolhas>().Length > 0;
        
        bool shouldShowGlow = isEscolhaPhase && (EscolhaManager.Instance != null && 
                             (EscolhaManager.Instance.CanPegarCarta() || hasActiveCards));
        
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
    
    IEnumerator FadeGlow(bool fadeIn)
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
