using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardAprendizagens : MonoBehaviour
{
    public string textFolder = "Texts/Aprendizagens";
    public TMP_Text textUI;

    public string imageFolder = "Images/Naturezas";
    public string whiteMaterialPath = "Materials/White";
    public MeshRenderer[] meshRenderers;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        randomText();
        randomImages();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void randomText()
    {
        TextAsset[] texts = Resources.LoadAll<TextAsset>(textFolder);
        if (texts.Length == 0)
        {
            Debug.LogWarning("Nenhum texto encontrado em: " + textFolder);
            return;
        }

        TextAsset randomText = texts[Random.Range(0, texts.Length)];
        textUI.text = randomText.text;
    }

    void randomImages()
    {
        int quantity = Random.Range(1, 4);
        Texture[] textures = Resources.LoadAll<Texture>(imageFolder);
        Material whiteMaterial = Resources.Load<Material>(whiteMaterialPath);

        if (textures.Length == 0)
        {
            Debug.LogWarning("Nenhuma textura encontrada em: " + imageFolder);
            return;
        }

        if (whiteMaterial == null)
        {
            Debug.LogWarning("Material branco não encontrado em: " + whiteMaterialPath);
            return;
        }

        System.Random random = new System.Random();
        Texture[] randomTextures = textures.OrderBy(x => random.Next()).ToArray();

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (i < quantity)
            {
                // Cria uma instância do material para não sobrescrever outros objetos
                Material mat = new Material(meshRenderers[i].material);
                mat.mainTexture = randomTextures[i % randomTextures.Length];
                meshRenderers[i].material = mat;
            }
            else
            {
                meshRenderers[i].material = whiteMaterial;
            }
        }

    }
}
