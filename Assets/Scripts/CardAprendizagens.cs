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
        
        // Teste direto com nomes das imagens
        string[] imageNames = { "Adaptacao", "Inspecao", "Transparencia" };
        string selectedImageName = imageNames[Random.Range(0, imageNames.Length)];
        
        Debug.Log("Tentando carregar: " + imageFolder + "/" + selectedImageName);
        Texture2D selectedTexture = Resources.Load<Texture2D>(imageFolder + "/" + selectedImageName);
        
        if (selectedTexture == null)
        {
            Debug.LogError("Textura não encontrada: " + imageFolder + "/" + selectedImageName);
            return;
        }
        
        Debug.Log("Textura carregada: " + selectedTexture.name + " - Tamanho: " + selectedTexture.width + "x" + selectedTexture.height);
        
        // Criar material novo com shader simples para WebGL
        Material material = new Material(Shader.Find("Unlit/Texture"));
        
        Debug.Log("Shader encontrado: " + (material.shader != null ? material.shader.name : "NULL"));
        
        // Aplicar textura
        material.mainTexture = selectedTexture;
        material.SetTexture("_MainTex", selectedTexture);
        
        // Aplicar ao renderer
        meshRenderer.material = material;
        
        Debug.Log("Material aplicado - Textura principal: " + (material.mainTexture != null ? material.mainTexture.name : "NULL"));
        
        nature = selectedImageName;
        Debug.Log("Natureza selecionada: " + nature);
    }
}
