using UnityEngine;

public class DeckImprevistos : MonoBehaviour
{
    [Header("Carta para ser instanciada")]
    public GameObject prefabToSpawn;
    [Header("Distância de spawn da carta em relação ao jogador")]
    public float spawnDistance = 5.0f;
    [Header("Câmera principal")]
    private Camera playerCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("Câmera principal não encontrada. Certifique-se de que há uma câmera com a tag 'MainCamera'.");
        }
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
            ImprevistoManager.Instance.NotifyCartaPega(spawnedCard, spawnPosition, rotation);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
