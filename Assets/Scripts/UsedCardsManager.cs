using System.Collections.Generic;
using UnityEngine;

public class UsedCardsManager : MonoBehaviour
{
    public static UsedCardsManager Instance { get; private set; }
    
    private HashSet<string> usedCardTextures = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddUsedCard(string textureName)
    {
        if (!string.IsNullOrEmpty(textureName))
        {
            usedCardTextures.Add(textureName);
            Debug.Log($"Carta adicionada às usadas: {textureName}. Total: {usedCardTextures.Count}");
        }
    }

    public bool IsCardUsed(string textureName)
    {
        return usedCardTextures.Contains(textureName);
    }

    public void ClearUsedCards()
    {
        int count = usedCardTextures.Count;
        usedCardTextures.Clear();
        Debug.Log($"Cartas utilizadas limpas. {count} cartas foram removidas da lista.");
    }

    public int GetUsedCardsCount()
    {
        return usedCardTextures.Count;
    }

    public List<string> GetUsedCardsList()
    {
        return new List<string>(usedCardTextures);
    }
}
