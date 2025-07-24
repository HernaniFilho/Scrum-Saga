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

        Debug.Log("Deck de aprendizagem clicada. Instanciando...");
        // Calcula a posição para spawnar: na frente da câmera
        Vector3 spawnPosition = playerCamera.transform.position + playerCamera.transform.forward * spawnDistance;

        Quaternion rotation = Quaternion.Euler(-180, 0, 0);
        // Instancia o prefab nessa posição com a rotação padrão (sem rotações extras)
        Instantiate(prefabToSpawn, spawnPosition, rotation);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
