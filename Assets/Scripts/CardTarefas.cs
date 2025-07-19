using UnityEngine;

public class CardTarefas : MonoBehaviour
{
    public string imageFolder = "Images/Tarefas";
    public MeshRenderer meshRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        randomTexture();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void randomTexture()
    {
        Texture[] textures = Resources.LoadAll<Texture>(imageFolder);
        if (textures.Length == 0)
        {
            Debug.LogWarning("Nenhuma textura encontrada em: " + imageFolder);
            return;
        }

        Texture randomTexture = textures[Random.Range(0, textures.Length)];
        meshRenderer.material.mainTexture = randomTexture;
    }
}
