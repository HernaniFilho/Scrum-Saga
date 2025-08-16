using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SelectedCardData
{
    public string imageName;
    public Dictionary<string, int> scores;
    public Texture2D cardTexture;
    public Material cardMaterial;
    
    public SelectedCardData()
    {
        scores = new Dictionary<string, int>();
    }
}

public class SelectedCardStorage : MonoBehaviour
{
    public static SelectedCardStorage Instance { get; private set; }
    
    [Header("Selected Card Data")]
    public SelectedCardData selectedCardData;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        selectedCardData = new SelectedCardData();
    }

    public void StoreSelectedCard(CardTarefas cardTarefas)
    {
        if (cardTarefas == null)
        {
            Debug.LogError("Não é possível armazenar carta nula!");
            return;
        }

        // Clear previous data
        selectedCardData = new SelectedCardData();

        // Store scores (tracks that the card improves)
        selectedCardData.scores = new Dictionary<string, int>(cardTarefas.scores);

        // Store texture/material information
        MeshRenderer meshRenderer = cardTarefas.meshRenderer;
        if (meshRenderer != null && meshRenderer.material != null)
        {
            selectedCardData.cardMaterial = new Material(meshRenderer.material);
            
            Texture2D texture = meshRenderer.material.mainTexture as Texture2D;
            if (texture != null)
            {
                selectedCardData.cardTexture = texture;
                selectedCardData.imageName = texture.name;
            }
        }

        Debug.Log("Dados da carta selecionada armazenados com sucesso!");
        LogSelectedCardInfo();
    }

    public SelectedCardData GetSelectedCardData()
    {
        return selectedCardData;
    }

    public bool HasSelectedCard()
    {
        return selectedCardData != null && selectedCardData.scores.Count > 0;
    }

    public void ClearSelectedCard()
    {
        selectedCardData = new SelectedCardData();
        Debug.Log("Dados da carta selecionada limpos.");
    }

    private void LogSelectedCardInfo()
    {
        if (selectedCardData == null) return;

        Debug.Log("=== Informações da Carta Selecionada ===");
        Debug.Log($"Nome da Imagem: {selectedCardData.imageName}");
        Debug.Log("Pontuações/Trilhas melhoradas:");
        
        foreach (var kvp in selectedCardData.scores)
        {
            Debug.Log($"  {kvp.Key}: +{kvp.Value}");
        }
    }
}
