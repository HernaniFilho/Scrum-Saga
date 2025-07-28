using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardAprendizagens : MonoBehaviour
{
    [Header("Caminho dos textos usados na descrição")]
    public string textFolder = "Texts/Aprendizagens";
    [Header("Texto que será atualizado com o texto aleatório")]
    public TMP_Text textUI;

    [Header("Caminho das imagens de natureza usadas no card")]
    public string imageFolder = "Images/Naturezas";
    [Header("Caminho do material branco usado para preencher espaços vazios")]
    public string whiteMaterialPath = "Materials/White";
    [Header("Natureza que será atualizada com a imagem aleatória")]
    public MeshRenderer meshRenderer;
    [Header("Natureza do card")]
    public string nature;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        randomText();
        randomImages();
    }
    // TODO: Remover este método se não for necessário
    void OnMouseDown()
    {
        ScoreManager.Instance.UpdateScore(nature, 1);
        Debug.Log("Pontuação de " + nature + " aumentada em 1.");
        Destroy(gameObject); // Destroi o objeto quando clicado
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
        if (meshRenderer == null)
        {
            Debug.LogWarning("Nenhum MeshRenderer atribuído para receber as imagens aleatórias.");
            return;
        }
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

        // Seleciona primeira textura aleatória
        Texture selectedTexture = randomTextures[0];

        // Cria novo material com essa textura
        Material material = new Material(meshRenderer.material);
        material.mainTexture = selectedTexture;
        meshRenderer.material = material;

        // Salva o nome da imagem na variável 'nature', sem extensão
        nature = selectedTexture.name;
        Debug.Log("Natureza selecionada: " + nature);
    }
}
