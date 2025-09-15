using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

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

public class SelectedCardStorage : MonoBehaviourPunCallbacks
{
    public static SelectedCardStorage Instance { get; private set; }
    
    [Header("Selected Card Data")]
    public SelectedCardData selectedCardData;
    public CardTarefas selectedCard;
    
    [Header("Rejected Card Data")]
    public SelectedCardData rejectedCardData;
    public CardTarefas rejectedCard;
    
    private const string SELECTED_CARD_KEY = "SelectedCard_Data";
    private const string REJECTED_CARD_KEY = "RejectedCard_Data";
    private const string SELECTED_CARDTAREFAS_KEY = "SelectedCard_Tarefas";
    private const string REJECTED_CARDTAREFAS_KEY = "RejectedCard_Tarefas";

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
        rejectedCardData = new SelectedCardData();
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

        selectedCard = cardTarefas;

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
        
        SynchronizeSelectedCard();
        SynchronizeSelectedCardTarefas();
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
        selectedCard = null;
        Debug.Log("Dados da carta selecionada limpos.");
    }

    public void StoreRejectedCard(CardTarefas cardTarefas)
    {
        if (cardTarefas == null)
        {
            Debug.LogError("Não é possível armazenar carta reprovada nula!");
            return;
        }

        rejectedCardData = new SelectedCardData();
        
        rejectedCard = cardTarefas;

        rejectedCardData.scores = new Dictionary<string, int>(cardTarefas.scores);

        MeshRenderer meshRenderer = cardTarefas.meshRenderer;
        if (meshRenderer != null && meshRenderer.material != null)
        {
            rejectedCardData.cardMaterial = new Material(meshRenderer.material);
            
            Texture2D texture = meshRenderer.material.mainTexture as Texture2D;
            if (texture != null)
            {
                rejectedCardData.cardTexture = texture;
                rejectedCardData.imageName = texture.name;
            }
        }

        Debug.Log("Carta reprovada armazenada com sucesso!");
        
        SynchronizeRejectedCard();
        SynchronizeRejectedCardTarefas();
    }

    public void MoveSelectedToRejected()
    {
        if (HasSelectedCard())
        {
            rejectedCardData = selectedCardData;
            rejectedCard = selectedCard;
            selectedCardData = new SelectedCardData();
            selectedCard = null;
            Debug.Log("Carta selecionada movida para reprovada.");
            
            SynchronizeRejectedCard();
            SynchronizeRejectedCardTarefas();
        }
    }

    public SelectedCardData GetRejectedCardData()
    {
        return rejectedCardData;
    }

    public bool HasRejectedCard()
    {
        return rejectedCardData != null && rejectedCardData.scores.Count > 0;
    }

    public void ClearRejectedCard()
    {
        rejectedCardData = new SelectedCardData();
        rejectedCard = null;
        Debug.Log("Dados da carta reprovada limpos.");
    }

    public CardTarefas GetSelectedCard()
    {
        return selectedCard;
    }

    public CardTarefas GetRejectedCard()
    {
        return rejectedCard;
    }

    public bool HasSelectedCardTarefas()
    {
        return selectedCard != null;
    }

    public bool HasRejectedCardTarefas()
    {
        return rejectedCard != null;
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
    
    private void SynchronizeSelectedCard()
    {
        if (selectedCardData == null || !PhotonNetwork.IsConnected) return;
        
        var syncData = new Dictionary<string, object>()
        {
            ["imageName"] = selectedCardData.imageName ?? "",
            ["scores"] = selectedCardData.scores ?? new Dictionary<string, int>()
        };
        
        if (selectedCardData.cardMaterial != null && selectedCardData.cardMaterial.mainTexture != null)
        {
            syncData["textureName"] = selectedCardData.cardMaterial.mainTexture.name;
        }
        
        Hashtable props = new Hashtable();
        props[SELECTED_CARD_KEY] = syncData;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        Debug.Log("Carta selecionada sincronizada para todos os jogadores");
    }
    
    private void SynchronizeRejectedCard()
    {
        if (rejectedCardData == null || !PhotonNetwork.IsConnected) return;
        
        var syncData = new Dictionary<string, object>()
        {
            ["imageName"] = rejectedCardData.imageName ?? "",
            ["scores"] = rejectedCardData.scores ?? new Dictionary<string, int>()
        };
        
        if (rejectedCardData.cardMaterial != null && rejectedCardData.cardMaterial.mainTexture != null)
        {
            syncData["textureName"] = rejectedCardData.cardMaterial.mainTexture.name;
        }
        
        Hashtable props = new Hashtable();
        props[REJECTED_CARD_KEY] = syncData;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        Debug.Log("Carta reprovada sincronizada para todos os jogadores");
    }
    
    private void SynchronizeSelectedCardTarefas()
    {
        if (selectedCard == null || !PhotonNetwork.IsConnected) return;
        
        var syncData = new Dictionary<string, object>()
        {
            ["scores"] = selectedCard.scores ?? new Dictionary<string, int>(),
            ["isSelected"] = selectedCard.isSelected,
            ["maxScore"] = selectedCard.maxScore
        };
        
        if (selectedCard.meshRenderer != null && selectedCard.meshRenderer.material != null && selectedCard.meshRenderer.material.mainTexture != null)
        {
            syncData["textureName"] = selectedCard.meshRenderer.material.mainTexture.name;
        }
        
        Hashtable props = new Hashtable();
        props[SELECTED_CARDTAREFAS_KEY] = syncData;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        Debug.Log("CardTarefas selecionado sincronizado para todos os jogadores");
    }
    
    private void SynchronizeRejectedCardTarefas()
    {
        if (rejectedCard == null || !PhotonNetwork.IsConnected) return;
        
        var syncData = new Dictionary<string, object>()
        {
            ["scores"] = rejectedCard.scores ?? new Dictionary<string, int>(),
            ["isSelected"] = rejectedCard.isSelected,
            ["maxScore"] = rejectedCard.maxScore
        };
        
        if (rejectedCard.meshRenderer != null && rejectedCard.meshRenderer.material != null && rejectedCard.meshRenderer.material.mainTexture != null)
        {
            syncData["textureName"] = rejectedCard.meshRenderer.material.mainTexture.name;
        }
        
        Hashtable props = new Hashtable();
        props[REJECTED_CARDTAREFAS_KEY] = syncData;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        Debug.Log("CardTarefas reprovado sincronizado para todos os jogadores");
    }
    

    
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        foreach (var property in propertiesThatChanged)
        {
            string key = property.Key.ToString();
            
            if (key == SELECTED_CARD_KEY)
            {
                var syncData = (Dictionary<string, object>)property.Value;
                selectedCardData = CreateCardDataFromSync(syncData);
                Debug.Log("Carta selecionada sincronizada pela rede");
            }
            else if (key == REJECTED_CARD_KEY)
            {
                var syncData = (Dictionary<string, object>)property.Value;
                rejectedCardData = CreateCardDataFromSync(syncData);
                Debug.Log("Carta reprovada sincronizada pela rede");
            }
            else if (key == SELECTED_CARDTAREFAS_KEY)
            {
                var syncData = (Dictionary<string, object>)property.Value;
                selectedCard = CreateCardTarefasFromSync(syncData);
                Debug.Log("CardTarefas selecionado sincronizado pela rede");
            }
            else if (key == REJECTED_CARDTAREFAS_KEY)
            {
                var syncData = (Dictionary<string, object>)property.Value;
                rejectedCard = CreateCardTarefasFromSync(syncData);
                Debug.Log("CardTarefas reprovado sincronizado pela rede");
            }

        }
    }
    
    private SelectedCardData CreateCardDataFromSync(Dictionary<string, object> syncData)
    {
        var cardData = new SelectedCardData();
        cardData.imageName = syncData["imageName"].ToString();
        cardData.scores = (Dictionary<string, int>)syncData["scores"];
        
        if (syncData.ContainsKey("textureName"))
        {
            string textureName = syncData["textureName"].ToString();
            
            Texture2D[] textures = Resources.LoadAll<Texture2D>("Images/Tarefas/Cartas");
            Texture2D foundTexture = null;
            
            foreach (Texture2D tex in textures)
            {
                if (tex.name == textureName)
                {
                    foundTexture = tex;
                    break;
                }
            }
            
            if (foundTexture != null)
            {
                cardData.cardTexture = foundTexture;
                cardData.cardMaterial = new Material(Shader.Find("Unlit/Texture"));
                cardData.cardMaterial.mainTexture = foundTexture;
                cardData.cardMaterial.SetTexture("_MainTex", foundTexture);
            }
        }
        
        return cardData;
    }
    
    private CardTarefas CreateCardTarefasFromSync(Dictionary<string, object> syncData)
    {
        GameObject tempCardObj = new GameObject("TempSyncCard");
        CardTarefas cardTarefas = tempCardObj.AddComponent<CardTarefas>();
        
        cardTarefas.scores = (Dictionary<string, int>)syncData["scores"];
        cardTarefas.isSelected = (bool)syncData["isSelected"];
        cardTarefas.maxScore = (int)syncData["maxScore"];
        
        if (syncData.ContainsKey("textureName"))
        {
            string textureName = syncData["textureName"].ToString();
            
            Texture2D[] textures = Resources.LoadAll<Texture2D>("Images/Tarefas/Cartas");
            Texture2D foundTexture = null;
            
            foreach (Texture2D tex in textures)
            {
                if (tex.name == textureName)
                {
                    foundTexture = tex;
                    break;
                }
            }
            
            if (foundTexture != null)
            {
                MeshRenderer meshRenderer = tempCardObj.AddComponent<MeshRenderer>();
                Material material = new Material(Shader.Find("Unlit/Texture"));
                material.mainTexture = foundTexture;
                material.SetTexture("_MainTex", foundTexture);
                meshRenderer.material = material;
                cardTarefas.meshRenderer = meshRenderer;
            }
        }
        
        DontDestroyOnLoad(tempCardObj);
        
        return cardTarefas;
    }
}
