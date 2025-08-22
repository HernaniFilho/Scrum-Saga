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
    
    [Header("Networking")]
    public bool isSyncedCard = false; // Flag para cartas sincronizadas

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Apenas gerar dados aleatórios se não for carta sincronizada
        if (!isSyncedCard)
        {
            randomText();
            randomImages();
        }
    }
    
    // Método para configurar carta sincronizada
    public void SetupSyncedCard(string texto, string natureza)
    {
        Debug.Log($"SetupSyncedCard chamado - Texto: '{texto}', Natureza: '{natureza}'");
        
        isSyncedCard = true;
        
        // Configurar texto
        if (textUI != null)
        {
            textUI.text = texto;
            Debug.Log($"Texto configurado: '{textUI.text}'");
        }
        else
        {
            Debug.LogError("textUI é null!");
        }
            
        // Configurar natureza
        nature = natureza;
        Debug.Log($"Natureza configurada: '{nature}'");
        
        // Aplicar textura correspondente à natureza
        ApplySyncedNatureTexture(natureza);
    }
    
    void ApplySyncedNatureTexture(string natureza)
    {
        if (meshRenderer == null) 
        {
            Debug.LogError("MeshRenderer não encontrado para aplicar textura sincronizada!");
            return;
        }
        
        Debug.Log($"Tentando carregar textura sincronizada para natureza: '{natureza}'");
        
        string imageFolder = "Images/Naturezas";
        string texturePath = imageFolder + "/" + natureza;
        Texture2D texture = Resources.Load<Texture2D>(texturePath);
        
        if (texture != null)
        {
            Debug.Log($"Textura carregada com sucesso: {texture.name} - Tamanho: {texture.width}x{texture.height}");
            
            Material material = new Material(Shader.Find("Unlit/Texture"));
            if (material.shader != null)
            {
                material.mainTexture = texture;
                material.SetTexture("_MainTex", texture);
                meshRenderer.material = material;
                
                Debug.Log($"Textura sincronizada aplicada com sucesso: {natureza}");
                Debug.Log($"Material aplicado - Textura principal: {(material.mainTexture != null ? material.mainTexture.name : "NULL")}");
            }
            else
            {
                Debug.LogError("Shader 'Unlit/Texture' não encontrado!");
            }
        }
        else
        {
            Debug.LogError($"Textura não encontrada no caminho: {texturePath}");
            
            // Listar texturas disponíveis para debug
            Texture2D[] availableTextures = Resources.LoadAll<Texture2D>(imageFolder);
            Debug.Log($"Texturas disponíveis em {imageFolder}:");
            foreach (var tex in availableTextures)
            {
                Debug.Log($"- {tex.name}");
            }
        }
    }
    void OnMouseDown()
    {
        // Durante a fase de Sprint Retrospective, apenas o PO pode clicar na carta
        if (GameStateManager.Instance != null && 
            GameStateManager.Instance.GetCurrentState() == GameStateManager.GameState.SprintRetrospective &&
            SprintRetrospectiveManager.Instance != null)
        {
            // O clique será tratado pelo POClickHandler adicionado pelo SprintRetrospectiveManager
            return;
        }
        
        // Comportamento padrão para outras fases
        ScoreManager.Instance.UpdateScore(nature, 1);
        Debug.Log("Pontuação de " + nature + " aumentada em 1.");
        Destroy(gameObject);
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

    public void randomImages()
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
