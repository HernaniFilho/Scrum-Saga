using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[System.Serializable]
public class SelectedCardData
{
    public string imageName;
    public Dictionary<string, int> scores;
    public Sprite cardSprite;
    
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

        // Store sprite information
        Image image = cardTarefas.image;
        if (image != null && image.sprite != null)
        {
            selectedCardData.cardSprite = image.sprite;
            selectedCardData.imageName = image.sprite.name;
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

        Image image = cardTarefas.image;
        if (image != null && image.sprite != null)
        {
            rejectedCardData.cardSprite = image.sprite;
            rejectedCardData.imageName = image.sprite.name;
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
        
        if (selectedCardData.cardSprite != null)
        {
            syncData["textureName"] = selectedCardData.cardSprite.name;
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
        
        if (rejectedCardData.cardSprite != null)
        {
            syncData["textureName"] = rejectedCardData.cardSprite.name;
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
        
        if (selectedCard.image != null && selectedCard.image.sprite != null)
        {
            syncData["textureName"] = selectedCard.image.sprite.name;
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
        
        if (rejectedCard.image != null && rejectedCard.image.sprite != null)
        {
            syncData["textureName"] = rejectedCard.image.sprite.name;
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
            string spriteName = syncData["textureName"].ToString();
            
            Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Tarefas/Cartas_Novas");
            Sprite foundSprite = null;
            
            foreach (Sprite spr in sprites)
            {
                if (spr.name == spriteName)
                {
                    foundSprite = spr;
                    break;
                }
            }
            
            if (foundSprite != null)
            {
                cardData.cardSprite = foundSprite;
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
            string spriteName = syncData["textureName"].ToString();
            
            Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Tarefas/Cartas_Novas");
            Sprite foundSprite = null;
            
            foreach (Sprite spr in sprites)
            {
                if (spr.name == spriteName)
                {
                    foundSprite = spr;
                    break;
                }
            }
            
            if (foundSprite != null)
            {
                Image image = tempCardObj.AddComponent<Image>();
                image.sprite = foundSprite;
                cardTarefas.image = image;
            }
        }
        
        DontDestroyOnLoad(tempCardObj);
        
        return cardTarefas;
    }
}
