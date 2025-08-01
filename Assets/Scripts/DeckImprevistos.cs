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

        Debug.Log("Deck de imprevistos clicada. Comprando carta de imprevisto...");
        // Calcula a posição para spawnar: na frente da câmera
        Vector3 spawnPosition = playerCamera.transform.position + playerCamera.transform.forward * spawnDistance;

        Quaternion rotation = Quaternion.Euler(-180, 0, 0);
        // Instancia o prefab nessa posição com a rotação padrão (sem rotações extras)
        Instantiate(prefabToSpawn, spawnPosition, rotation);
        Debug.Log("Carta de imprevisto comprada e instanciada na posição: " + spawnPosition);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
