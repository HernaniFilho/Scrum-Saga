using UnityEngine;

public class DeckAprendizagens : MonoBehaviour
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
        screenPos.x += 160;
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
