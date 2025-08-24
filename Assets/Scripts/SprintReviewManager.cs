using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class SprintReviewManager : MonoBehaviourPunCallbacks
{
    [Header("Card Display")]
    public GameObject cardTarefasPrefab;
    private GameObject displayedCard;
    
    [Header("UI Elements")]
    public UnityEngine.UI.Button highSatisfactionButton;
    public UnityEngine.UI.Button mediumSatisfactionButton;
    public UnityEngine.UI.Button lowSatisfactionButton;
    public UnityEngine.UI.Button toggleViewButton;
    public TMP_Text displayText; // Unified text for waiting, result, and hover messages
    
    private GameObject buttonsContainer;
    private bool showingCard = true; // Start showing card

    [Header("Configuration")]
    public float resultShowDuration = 5f;
    
    [Header("Camera Reference")]
    private Camera playerCamera;
    
    [Header("Product Owner Manager")]
    private ProductOwnerManager productOwnerManager;
    
    [Header("Game Board")]
    public GameObject tabuleiro;
    private GameObject shapesContainer;
    
    [Header("Positioning")]
    public float cardSpawnDistance = 0.67f; // Same as selected card in SprintPlanningManager
    
    // Photon synchronization keys
    private const string SELECTED_CARD_KEY = "SprintReview_SelectedCard";
    private const string SATISFACTION_RESULT_KEY = "SprintReview_Result";
    
    private GameStateManager gameStateManager;
    private bool hasDisplayedCard = false;
    private bool hasVoted = false;
    private bool isShowingResult = false;
    private SelectedCardData networkCardData;
    
    // Satisfaction levels
    public enum SatisfactionLevel
    {
        High,    // Máximo: +2 em uma trilha ou +1 em duas
        Medium,  // Médio: +1 se era +2, ou +1 apenas na segunda trilha
        Low      // Mínimo: carta reprovada (sem ação por enquanto)
    }
    
    void Start()
    {
        playerCamera = Camera.main;
        productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        gameStateManager = GameStateManager.Instance;
        
        if (playerCamera == null)
        {
            Debug.LogError("Câmera principal não encontrada!");
        }
        
        InitializeUI();
    }
    
    void Update()
    {
        if (productOwnerManager == null)
        {
            productOwnerManager = FindObjectOfType<ProductOwnerManager>();
        }

        if (shapesContainer == null)
        {
            shapesContainer = GameObject.Find("ShapesContainer");
        }
        
        if (gameStateManager == null) return;
        
        var currentState = gameStateManager.GetCurrentState();
        
        if (currentState == GameStateManager.GameState.SprintReview)
        {
            if (!hasDisplayedCard)
            {
                // PO synchronizes selected card data for all players
                if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
                {
                    SynchronizeSelectedCard();
                }
                
                DisplaySelectedCard();
                ShowUI();
                
                // Initialize view state: start showing card
                InitializeViewState();
                hasDisplayedCard = true;
            }
        }
        else
        {
            hasDisplayedCard = false;
            hasVoted = false;
            isShowingResult = false;
            showingCard = true;
            ClearDisplayedCard();
        }
    }
    
    private void InitializeUI()
    {
        // Get buttons container from any button's parent
        if (highSatisfactionButton != null)
        {
            buttonsContainer = highSatisfactionButton.transform.parent.gameObject;
            buttonsContainer.SetActive(false);
        }
        
        // Setup satisfaction buttons with hover events
        if (highSatisfactionButton != null)
        {
            highSatisfactionButton.onClick.AddListener(() => OnSatisfactionButtonClicked(SatisfactionLevel.High));
            AddHoverEvents(highSatisfactionButton, "Muito satisfeito!");
        }
        
        if (mediumSatisfactionButton != null)
        {
            mediumSatisfactionButton.onClick.AddListener(() => OnSatisfactionButtonClicked(SatisfactionLevel.Medium));
            AddHoverEvents(mediumSatisfactionButton, "Pouco satisfeito!");
        }
        
        if (lowSatisfactionButton != null)
        {
            lowSatisfactionButton.onClick.AddListener(() => OnSatisfactionButtonClicked(SatisfactionLevel.Low));
            AddHoverEvents(lowSatisfactionButton, "Necessário aprimorar!");
        }
        
        // Setup toggle view button
        if (toggleViewButton != null)
        {
            toggleViewButton.onClick.AddListener(OnToggleViewClicked);
            UpdateToggleButtonText();
        }
        
        // Setup unified display text
        if (displayText != null)
        {
            displayText.gameObject.SetActive(false);
        }
    }
    
    private void AddHoverEvents(UnityEngine.UI.Button button, string hoverMessage)
    {
        UnityEngine.EventSystems.EventTrigger trigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }
        
        // Mouse enter
        var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { OnButtonHover(hoverMessage); });
        trigger.triggers.Add(enterEntry);
        
        // Mouse exit
        var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { OnButtonExit(); });
        trigger.triggers.Add(exitEntry);
    }
    
    private void OnButtonHover(string message)
    {
        bool isProductOwner = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        if (isProductOwner && !hasVoted && displayText != null)
        {
            displayText.text = message;
            displayText.gameObject.SetActive(true);
        }
    }
    
    private void OnButtonExit()
    {
        bool isProductOwner = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        if (isProductOwner && !hasVoted && displayText != null)
        {
            displayText.text = "";
            displayText.gameObject.SetActive(false);
        }
    }
    
    private void OnToggleViewClicked()
    {
        showingCard = !showingCard;
        UpdateToggleButtonText();
        ToggleView();
    }
    
    private void UpdateToggleButtonText()
    {
        if (toggleViewButton != null)
        {
            TMP_Text buttonText = toggleViewButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = showingCard ? "Ver desenho" : "Ver carta";
            }
        }
    }
    
    private void ToggleView()
    {
        if (showingCard)
        {
            // Showing card: hide canvas, shapes, and tabuleiro
            if (CanvasManager.Instance != null) CanvasManager.Instance.DeactivateCanvas();

            if (shapesContainer != null) shapesContainer.SetActive(false);
            
            if (tabuleiro != null) tabuleiro.SetActive(false);
            
            if (displayedCard != null) displayedCard.SetActive(true);
        }
        else
        {
            // Showing drawing: hide card, show canvas and shapes (no toolbar/drawing ability)
            if (displayedCard != null) displayedCard.SetActive(false);
            
            if (CanvasManager.Instance != null)
            {
                CanvasManager.Instance.ActivateCanvas();
                CanvasManager.Instance.DeactivateToolbar();
            }

            if (shapesContainer != null) shapesContainer.SetActive(true);
            
            if (tabuleiro != null) tabuleiro.SetActive(true);
        }
    }
    
    private void InitializeViewState()
    {
        // Start showing card (showingCard = true), so disable canvas and shapes
        showingCard = true;
        UpdateToggleButtonText();
        ToggleView();
    }
    
    private void SynchronizeSelectedCard()
    {
        if (SelectedCardStorage.Instance && SelectedCardStorage.Instance.HasSelectedCard())
        {
            SelectedCardData cardData = SelectedCardStorage.Instance.GetSelectedCardData();
            
            // Create serializable data for network sync including texture info
            var syncData = new Dictionary<string, object>()
            {
                ["imageName"] = cardData.imageName ?? "",
                ["scores"] = cardData.scores ?? new Dictionary<string, int>()
            };
            
            // Add full texture path for better synchronization
            if (cardData.cardMaterial != null && cardData.cardMaterial.mainTexture != null)
            {
                // Store just the texture name, others will load from Images/Tarefas/Cartas
                syncData["textureName"] = cardData.cardMaterial.mainTexture.name;
            }
            
            Hashtable props = new Hashtable();
            props[SELECTED_CARD_KEY] = syncData;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            
            Debug.Log("Carta selecionada sincronizada para todos os jogadores");
        }
    }
    
    private void SynchronizeResult(SatisfactionLevel level)
    {
        Hashtable props = new Hashtable();
        props[SATISFACTION_RESULT_KEY] = (int)level;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        Debug.Log($"Resultado de satisfação sincronizado: {level}");
    }
    
    private void DisplaySelectedCard()
    {
        SelectedCardData cardData = GetCardDataForDisplay();
        
        if (cardData == null)
        {
            Debug.LogError("Nenhuma carta selecionada encontrada para Sprint Review!");
            return;
        }
        
        if (cardTarefasPrefab == null || playerCamera == null)
        {
            Debug.LogError("Prefab da carta ou câmera não encontrados!");
            return;
        }
        
        // Clear any existing card
        ClearDisplayedCard();
        
        // Calculate position (center of screen)
        Vector3 screenCenter = new Vector3(Screen.width / 2f + 160f, Screen.height / 2f, cardSpawnDistance);
        Vector3 centerPosition = playerCamera.ScreenToWorldPoint(screenCenter);
        
        // Create card
        Quaternion rotation = Quaternion.Euler(-90, 0, 180);
        displayedCard = Instantiate(cardTarefasPrefab, centerPosition, rotation);
        displayedCard.transform.localScale = new Vector3(0.51f, 0.0001f, 0.65f);
        
        // Apply card data (CardTarefas.Start() now respects existing materials)
        ApplyCardData(displayedCard, cardData);
        
        Debug.Log("Carta selecionada exibida para Sprint Review!");
    }
    
    private SelectedCardData GetCardDataForDisplay()
    {
        // For PO: use local SelectedCardStorage
        if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            if (SelectedCardStorage.Instance && SelectedCardStorage.Instance.HasSelectedCard())
            {
                return SelectedCardStorage.Instance.GetSelectedCardData();
            }
        }
        // For other players: use network synchronized data
        else if (networkCardData != null)
        {
            return networkCardData;
        }
        
        return null;
    }
    
    private void ApplyCardData(GameObject card, SelectedCardData cardData)
    {
        if (card == null || cardData == null) return;
        
        // Get components
        CardTarefas cardTarefas = card.GetComponent<CardTarefas>();
        MeshRenderer meshRenderer = card.GetComponent<MeshRenderer>();
        
        // Set card as selected and apply scores
        if (cardTarefas != null)
        {
            cardTarefas.isSelected = true;
            cardTarefas.scores = new Dictionary<string, int>(cardData.scores);
        }
        
        // Apply the original material from SelectedCardStorage
        if (meshRenderer != null && cardData.cardMaterial != null)
        {
            // Create a copy to avoid modifying the original
            Material preservedMaterial = new Material(cardData.cardMaterial);
            meshRenderer.material = preservedMaterial;
            
            Debug.Log($"Texture preservada no Sprint Review: {preservedMaterial.mainTexture?.name}");
        }
        else if (meshRenderer != null && cardData.cardTexture != null)
        {
            // Fallback: recreate material from texture info
            Material fallbackMaterial = new Material(Shader.Find("Unlit/Texture"));
            fallbackMaterial.mainTexture = cardData.cardTexture;
            fallbackMaterial.SetTexture("_MainTex", cardData.cardTexture);
            meshRenderer.material = fallbackMaterial;
            
            Debug.Log($"Material recriado no Sprint Review: {cardData.cardTexture.name}");
        }
        
        // Update score texts
        if (cardTarefas != null)
        {
            cardTarefas.UpdateScoreTexts();
        }
        
        // Deactivate censura since we want to show the full card
        Transform censuraTransform = FindChildByName(card.transform, "Censura");
        if (censuraTransform != null)
        {
            censuraTransform.gameObject.SetActive(false);
        }
    }
    
    private void ShowUI()
    {
        bool isProductOwner = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        
        // Always show toggle button
        if (toggleViewButton != null) toggleViewButton.gameObject.SetActive(true);
        
        if (isProductOwner && !hasVoted)
        {
            // Show buttons for Product Owner
            if (buttonsContainer != null) buttonsContainer.SetActive(true);
            
            if (displayText != null) displayText.gameObject.SetActive(false);
        }
        else if (!isProductOwner && !hasVoted)
        {
            // Show waiting text for other players
            if (displayText != null)
            {
                displayText.text = "Aguardando feedback do PO...";
                displayText.gameObject.SetActive(true);
            }
            
            // Hide buttons
            if (buttonsContainer != null) buttonsContainer.SetActive(false);
        }
    }

    private void OnSatisfactionButtonClicked(SatisfactionLevel level)
    {
        if (hasVoted || isShowingResult) return;

        bool isProductOwner = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        if (!isProductOwner) return; // Only PO can vote

        hasVoted = true;

        Debug.Log($"Nível de satisfação selecionado: {level}");

        // Apply score changes based on satisfaction level
        ApplyScoreChanges(level);

        // Hide buttons
        if (buttonsContainer != null) buttonsContainer.SetActive(false);

        if (toggleViewButton != null) toggleViewButton.gameObject.SetActive(false);

        // Synchronize result for all players
        SynchronizeResult(level);
        
        // Show result to all players
        ShowResult(level);
        
        // Start 5-second timer before phase transition
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.StartTimer(resultShowDuration, () => {
                onTimerComplete(level);
            }, "ResultadoSprintReviewTimer");
        }
    }
    
    private void ApplyScoreChanges(SatisfactionLevel level)
    {
        SelectedCardData cardData = GetCardDataForDisplay();
        if (cardData == null || cardData.scores == null)
        {
            Debug.LogError("Nenhuma carta selecionada para aplicar mudanças de pontuação!");
            return;
        }
        
        Dictionary<string, int> originalScores = new Dictionary<string, int>(cardData.scores);
        Dictionary<string, int> finalScores = new Dictionary<string, int>();
        
        switch (level)
        {
            case SatisfactionLevel.High:
                // Máximo: mantém pontuação original (+2 em uma trilha ou +1 em duas)
                finalScores = new Dictionary<string, int>(originalScores);
                Debug.Log("Satisfação Alta: Pontuação máxima mantida!");
                break;
                
            case SatisfactionLevel.Medium:
                // Médio: reduz pontuação pela metade
                foreach (var score in originalScores)
                {
                    if (score.Value == 2)
                    {
                        finalScores[score.Key] = 1; // +2 vira +1
                    }
                    else if (score.Value == 1)
                    {
                        // Se há duas trilhas com +1, mantém apenas a primeira
                        if (finalScores.Count == 0)
                        {
                            finalScores[score.Key] = 1;
                        }
                        // Se já adicionamos uma trilha, pula as outras
                    }
                }
                Debug.Log("Satisfação Média: Pontuação reduzida pela metade!");
                break;
                
            case SatisfactionLevel.Low:
                // Mínimo: carta reprovada (nenhuma pontuação)
                finalScores.Clear();
                Debug.Log("Satisfação Baixa: Carta reprovada (sem pontuação)!");
                break;
        }
        
        // Apply scores to ScoreManager instead of modifying card
        if (ScoreManager.Instance != null)
        {
            foreach (var score in finalScores)
            {
                ScoreManager.Instance.UpdateScore(score.Key, score.Value);
                Debug.Log($"Pontuação adicionada ao ScoreManager: {score.Key} +{score.Value}");
            }
        }
        else
        {
            Debug.LogError("ScoreManager não encontrado!");
        }
    }
    
    private void ShowResult(SatisfactionLevel level)
    {
        string resultMessage = "";
        
        switch (level)
        {
            case SatisfactionLevel.High:
                resultMessage = "O PO ficou muito satisfeito!\nPontuação máxima obtida!";
                break;
            case SatisfactionLevel.Medium:
                resultMessage = "Poderia ser melhor...\nPontuação reduzida pela metade!";
                break;
            case SatisfactionLevel.Low:
                resultMessage = "Necessário aprimorar!\nCarta reprovada!";
                break;
        }
        
        if (displayText != null)
        {
            displayText.text = resultMessage;
            displayText.gameObject.SetActive(true);
        }
        
        isShowingResult = true;
    }
    
    private void onTimerComplete(SatisfactionLevel level)
    {
        // Hide result text
        if (displayText != null)
        {
            displayText.gameObject.SetActive(false);
        }
        
        isShowingResult = false;

        if (CanvasManager.Instance != null)
        {
            CanvasManager.Instance.ClearCanvasForAll();
            CanvasManager.Instance.ClearAllSavedDrawings();
            CanvasManager.Instance.DeactivateCanvasForAll();
        }
        
        if (productOwnerManager != null)
        {
            gameStateManager.NextState();
        }
    }
    
    private void ClearDisplayedCard()
    {
        if (displayedCard != null)
        {
            Destroy(displayedCard);
            displayedCard = null;
        }
    }
    
    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
    
    // Photon network callbacks
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        foreach (var property in propertiesThatChanged)
        {
            string key = property.Key.ToString();
            
            if (key == SELECTED_CARD_KEY)
            {
                var syncData = (Dictionary<string, object>)property.Value;
                networkCardData = new SelectedCardData();
                networkCardData.imageName = syncData["imageName"].ToString();
                networkCardData.scores = (Dictionary<string, int>)syncData["scores"];
                
                // Load texture from Images/Tarefas/Cartas folder like CardTarefas does
                if (syncData.ContainsKey("textureName"))
                {
                    string textureName = syncData["textureName"].ToString();
                    
                    // Load all textures from the same folder CardTarefas uses
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
                        networkCardData.cardTexture = foundTexture;
                        // Use same shader as CardTarefas
                        networkCardData.cardMaterial = new Material(Shader.Find("Unlit/Texture"));
                        networkCardData.cardMaterial.mainTexture = foundTexture;
                        networkCardData.cardMaterial.SetTexture("_MainTex", foundTexture);
                        
                        Debug.Log($"Texture '{textureName}' carregada com sucesso para sincronização");
                    }
                    else
                    {
                        Debug.LogWarning($"Texture '{textureName}' não encontrada em Images/Tarefas/Cartas");
                    }
                }
                
                Debug.Log("Dados da carta sincronizados pela rede");
            }
            else if (key == SATISFACTION_RESULT_KEY)
            {
                if (showingCard) OnToggleViewClicked();
                
                SatisfactionLevel level = (SatisfactionLevel)(int)property.Value;
                if (!productOwnerManager.IsLocalPlayerProductOwner())
                {
                    if (toggleViewButton != null) toggleViewButton.gameObject.SetActive(false);
                    ShowResult(level);
                    StartCoroutine(ResultDisplayTimerForPlayers(level));
                }
            }
        }
    }
    
    private IEnumerator ResultDisplayTimerForPlayers(SatisfactionLevel level)
    {
        yield return new WaitForSeconds(5f);
        
        if (displayText != null)
        {
            displayText.gameObject.SetActive(false);
        }
        
        isShowingResult = false;
    }
    
    void OnDestroy()
    {
        ClearDisplayedCard();
    }
}
