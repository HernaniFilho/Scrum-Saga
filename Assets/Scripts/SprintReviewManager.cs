using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    public TMP_Text displayText; // Unified text for waiting and result
    public TMP_Text satisfactionText; // Hover messages
    
    [Header("Round Info Text")]
    public TMP_Text roundInfoText;
    
    [Header("Pause Button")]
    public UnityEngine.UI.Button pauseButton;
    
    private GameObject buttonsContainer;
    private bool showingCard = true; // Start showing card

    [Header("Configuration")]
    public float resultShowDuration = 10f;
    
    [Header("Camera Reference")]
    private Camera playerCamera;
    
    private List<GameObject> spawnedCards = new List<GameObject>();
    
    private Vector3 originalTogglePosition;
    private bool hasMovedToggleButton = false;
    
    [Header("Product Owner Manager")]
    private ProductOwnerManager productOwnerManager;
    
    [Header("Game Board")]
    public GameObject tabuleiro;
    private GameObject shapesContainer;
    
    [Header("Positioning")]
    public float cardSpawnDistance = 0.67f; // Same as selected card in SprintPlanningManager
    public float evaluationUIOffset = 25f; // Offset for UI during PO evaluation
    
    // Photon synchronization keys
    private const string SATISFACTION_RESULT_KEY = "SprintReview_Result";
    private const string SHOW_APPROVED_CARDS_KEY = "SprintReview_ShowApprovedCards";
    private const string FORCE_DRAWING_VIEW_KEY = "SprintReview_ForceDrawingView";
    private const string RESULT_TIMER_COMPLETE_KEY = "SprintReview_ResultTimerComplete";
    
    private GameStateManager gameStateManager;
    private bool hasDisplayedCard = false;
    private bool hasVoted = false;
    private bool isShowingResult = false;
    
    private bool isMultiRound = false;
    private int currentRound = 0;
    private int totalRounds = 1;
    private List<SelectedCardData> cardsToReview = new List<SelectedCardData>();
    private List<SatisfactionLevel> roundResults = new List<SatisfactionLevel>();
    private bool hasBeenPrepared = false;
    
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
                // Only prepare rounds once per Sprint Review session
                if (!hasBeenPrepared)
                {
                    PrepareReviewRounds();
                    hasBeenPrepared = true;
                }
                
                
                DisplayCurrentRoundCard();
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
            isMultiRound = false;
            currentRound = 0;
            totalRounds = 1;
            cardsToReview.Clear();
            roundResults.Clear();
            hasBeenPrepared = false;
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
            
            RectTransform buttonRect = toggleViewButton.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                originalTogglePosition = buttonRect.anchoredPosition;
            }
        }
        
        // Setup unified display text
        if (displayText != null)
        {
            displayText.gameObject.SetActive(false);
        }

        if (satisfactionText != null)
        {
            satisfactionText.gameObject.SetActive(false);
        }
        
        SetupPauseButton();
    }
    
    void SetupPauseButton()
    {
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
            pauseButton.onClick.RemoveAllListeners();
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
        if (isProductOwner && !hasVoted && satisfactionText != null)
        {
            satisfactionText.text = message;
            satisfactionText.gameObject.SetActive(true);
        }
    }
    
    private void OnButtonExit()
    {
        bool isProductOwner = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        if (isProductOwner && !hasVoted && satisfactionText != null)
        {
            satisfactionText.text = "";
            satisfactionText.gameObject.SetActive(false);
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
            
            foreach (GameObject card in spawnedCards)
            {
                if (card != null) card.SetActive(true);
            }
        }
        else
        {
            if (displayedCard != null) displayedCard.SetActive(false);
            
            foreach (GameObject card in spawnedCards)
            {
                if (card != null) card.SetActive(false);
            }
            
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
        bool isProductOwner = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        
        if (isProductOwner)
        {
            showingCard = true;
            UpdateToggleButtonText();
            ToggleView();
        }
        else
        {
            showingCard = false;
            if (toggleViewButton != null) toggleViewButton.gameObject.SetActive(false);
            ShowDrawingViewForPlayers();
        }
    }

    private void ShowDrawingViewForPlayers()
    {
        if (CanvasManager.Instance != null)
        {
            CanvasManager.Instance.ActivateCanvas();
            CanvasManager.Instance.DeactivateToolbar();
        }

        if (shapesContainer != null) shapesContainer.SetActive(true);
        if (tabuleiro != null) tabuleiro.SetActive(true);
    }
    

    
    private void SynchronizeResult(SatisfactionLevel level)
    {
        Hashtable props = new Hashtable();
        props[SATISFACTION_RESULT_KEY] = (int)level;
        
        int[] roundResultsArray = new int[roundResults.Count];
        for (int i = 0; i < roundResults.Count; i++)
        {
            roundResultsArray[i] = (int)roundResults[i];
        }
        props["RoundResults"] = roundResultsArray;
        props["CurrentRound"] = currentRound;
        props["TotalRounds"] = totalRounds;
        props["IsMultiRound"] = isMultiRound;
        
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        Debug.Log($"Resultado de satisfação sincronizado: {level} - RoundResults compartilhados");
    }
    
    private void PrepareReviewRounds()
    {
        cardsToReview.Clear();
        roundResults.Clear();
        
        bool hasSelectedCard = SelectedCardStorage.Instance != null && SelectedCardStorage.Instance.HasSelectedCard();
        bool hasRejectedCard = SelectedCardStorage.Instance != null && SelectedCardStorage.Instance.HasRejectedCard();
        
        if (hasSelectedCard && hasRejectedCard)
        {
            isMultiRound = true;
            totalRounds = 2;
            cardsToReview.Add(SelectedCardStorage.Instance.GetRejectedCardData());
            cardsToReview.Add(SelectedCardStorage.Instance.GetSelectedCardData());
            Debug.Log("Sprint Review preparado para 2 rodadas (carta reprovada + nova carta)");
        }
        else if (hasSelectedCard)
        {
            isMultiRound = false;
            totalRounds = 1;
            cardsToReview.Add(SelectedCardStorage.Instance.GetSelectedCardData());
            Debug.Log("Sprint Review preparado para 1 rodada (apenas nova carta)");
        }
        
        currentRound = 0;
    }

    private void DisplayCurrentRoundCard()
    {
        if (currentRound >= cardsToReview.Count)
        {
            return;
        }
        
        ClearDisplayedCard();
        
        bool isProductOwner = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        if (isProductOwner)
        {
            DisplayCardForPO();
        }
        
        ShowRoundInfo();
        
        string roundType = (isMultiRound && currentRound == 0) ? "reprovada" : "nova";
        Debug.Log($"Preparando para avaliar carta {roundType} (Rodada {currentRound + 1}/{totalRounds})");
    }

    private void DisplayCardForPO()
    {
        SelectedCardData cardData = cardsToReview[currentRound];
        
        if (cardData == null)
        {
            Debug.LogError("Nenhuma carta encontrada para a rodada atual!");
            return;
        }
        
        if (cardTarefasPrefab == null || playerCamera == null)
        {
            Debug.LogError("Prefab da carta ou câmera não encontrados!");
            return;
        }
        
        // Calculate position (center of screen)
        float xOffset = OffsetManager.Instance != null ? OffsetManager.Instance.xOffset : 160f;
        Vector3 screenCenter = new Vector3(Screen.width / 2f + xOffset, Screen.height / 2f, cardSpawnDistance);
        Vector3 centerPosition = playerCamera.ScreenToWorldPoint(screenCenter);
        
        // Create card for PO
        Quaternion rotation = Quaternion.Euler(-90, 0, 180);
        displayedCard = Instantiate(cardTarefasPrefab, centerPosition, rotation);
        displayedCard.transform.localScale = new Vector3(0.51f, 0.0001f, 0.65f);
        
        // Apply card data
        ApplyCardData(displayedCard, cardData);
        
        Debug.Log("Carta exibida para o PO");
    }

    private void ShowRoundInfo()
    {
        if (roundInfoText != null && totalRounds > 1)
        {
            roundInfoText.text = "Carta Reprovada";
            
            if (roundInfoText.transform.parent != null)
            {
                roundInfoText.transform.parent.gameObject.SetActive(true);
            }
        }
    }

    private void HideRoundInfo()
    {
        if (roundInfoText != null && roundInfoText.transform.parent != null)
        {
            roundInfoText.transform.parent.gameObject.SetActive(false);
        }
    }
    
    private SelectedCardData GetCardDataForDisplay()
    {
        if (SelectedCardStorage.Instance && SelectedCardStorage.Instance.HasSelectedCard())
        {
            return SelectedCardStorage.Instance.GetSelectedCardData();
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
        
        if (isProductOwner)
        {
            if (toggleViewButton != null) 
            {
                toggleViewButton.gameObject.SetActive(true);
                
                RectTransform buttonRect = toggleViewButton.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.anchoredPosition = originalTogglePosition;
                }
            }
            
            if (!hasVoted)
            {
                if (buttonsContainer != null) buttonsContainer.SetActive(true);

                if (displayText != null) displayText.gameObject.SetActive(false);
                if (satisfactionText != null) satisfactionText.gameObject.SetActive(false);

                MoveElementsDown();
            }
        }
        else
        {
            if (toggleViewButton != null) toggleViewButton.gameObject.SetActive(false);
            
            if (!hasVoted)
            {
                if (displayText != null)
                {
                    displayText.text = "Aguardando feedback do PO...";
                    displayText.gameObject.SetActive(true);
                }
                
                if (buttonsContainer != null) buttonsContainer.SetActive(false);
            }
        }
    }

    private void OnSatisfactionButtonClicked(SatisfactionLevel level)
    {
        if (hasVoted || isShowingResult) return;

        bool isProductOwner = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        if (!isProductOwner) return; // Only PO can vote

        hasVoted = true;
        roundResults.Add(level);

        Debug.Log($"Nível de satisfação selecionado: {level} (Rodada {currentRound + 1}/{totalRounds})");

        HandleRoundSatisfaction(level);

        // Hide buttons
        if (buttonsContainer != null) buttonsContainer.SetActive(false);

        if (toggleViewButton != null) toggleViewButton.gameObject.SetActive(false);
        
        RestoreElementsPosition();

        // Synchronize result for all players
        SynchronizeResult(level);
        
        // Hide round info during result display
        HideRoundInfo();
        
        // Show result to all players
        ShowResult(level);
        
        // Only show approved cards if the current card was not rejected
        if (level != SatisfactionLevel.Low)
        {
            ShowApprovedCards();
        }
        else
        {
            if (isProductOwner)
            {
                Hashtable props = new Hashtable();
                props["FORCE_DRAWING_NO_WAITING"] = System.DateTime.Now.Ticks;
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                Debug.Log("Carta rejeitada - forçando visualização apenas do desenho sem texto de espera");
            }
        }
        
        // Enable toggle button for players if there are approved cards
        EnablePlayerToggleIfNeeded();
        
        // Start timer before next round or phase transition
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.StartTimer(resultShowDuration, () => {
                onTimerComplete(level);
            }, "ResultadoSprintReviewTimer");
        }
        
        // Atualizar botão de pause para o novo timer
        UpdatePauseButtonVisibility();
    }

    private void HandleRoundSatisfaction(SatisfactionLevel level)
    {
        bool isRejectedCardRound = isMultiRound && currentRound == 0;
        
        if (isRejectedCardRound)
        {
            if (level == SatisfactionLevel.Low)
            {
                Debug.Log("Carta reprovada foi reprovada novamente - será removida permanentemente");
            }
            else
            {
                ApplyScoreChanges(level);
                Debug.Log("Carta reprovada foi aprovada - pontuação aplicada");
            }
        }
        else
        {
            if (level == SatisfactionLevel.Low)
            {
                if (SelectedCardStorage.Instance != null && SelectedCardStorage.Instance.HasSelectedCard())
                {
                    SelectedCardStorage.Instance.MoveSelectedToRejected();
                    Debug.Log("Nova carta foi reprovada - movida para cartas reprovadas");
                }
            }
            else
            {
                ApplyScoreChanges(level);
                Debug.Log("Nova carta foi aprovada - pontuação aplicada");
            }
        }
    }
    
    private void ApplyScoreChanges(SatisfactionLevel level)
    {
        SelectedCardData cardData = cardsToReview[currentRound];
        if (cardData == null || cardData.scores == null)
        {
            Debug.LogError("Nenhuma carta encontrada para aplicar mudanças de pontuação!");
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
                resultMessage = "Poderia ser melhor...\nPontuação média obtida!";
                break;
            case SatisfactionLevel.Low:
                resultMessage = "Necessário aprimorar!\nCarta reprovada!";
                break;
        }

        if (displayText != null)
        {
            displayText.text = resultMessage;
            displayText.gameObject.SetActive(true);

            LayoutRebuilder.ForceRebuildLayoutImmediate(displayText.rectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(displayText.transform.parent as RectTransform);
        }
        
        isShowingResult = true;
    }

    private void ShowApprovedCards()
    {
        List<SelectedCardData> approvedCards = new List<SelectedCardData>();
        
        for (int i = 0; i <= currentRound && i < roundResults.Count; i++)
        {
            if (roundResults[i] != SatisfactionLevel.Low)
            {
                if (i < cardsToReview.Count)
                {
                    approvedCards.Add(cardsToReview[i]);
                }
            }
        }
        
        if (approvedCards.Count == 0)
        {
            Debug.Log("Nenhuma carta aprovada para mostrar aos jogadores");
            return;
        }
        
        var cardsList = new List<Dictionary<string, object>>();
        
        foreach (var card in approvedCards)
        {
            var cardData = new Dictionary<string, object>()
            {
                ["imageName"] = card.imageName ?? "",
                ["scores"] = card.scores ?? new Dictionary<string, int>()
            };
            
            if (card.cardMaterial != null && card.cardMaterial.mainTexture != null)
            {
                cardData["textureName"] = card.cardMaterial.mainTexture.name;
            }
            
            cardsList.Add(cardData);
        }
        
        Hashtable props = new Hashtable();
        props[SHOW_APPROVED_CARDS_KEY] = cardsList.ToArray();
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        
        Debug.Log($"Comando para mostrar {approvedCards.Count} carta(s) aprovada(s) enviado para todos os players");
        
        DisplayApprovedCardsLocally(approvedCards);
    }
    

    
    private void DisplayApprovedCardsLocally(List<SelectedCardData> approvedCards)
    {
        if (approvedCards == null || approvedCards.Count == 0) return;
        
        ClearDisplayedCard();
        
        DisplaySingleCard(approvedCards[approvedCards.Count - 1]); // Show the last (most recent) approved card
    }

    private void EnablePlayerToggleIfNeeded()
    {
        bool isProductOwner = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        
        if (!isProductOwner)
        {
            int approvedCount = 0;
            for (int i = 0; i <= currentRound && i < roundResults.Count; i++)
            {
                if (roundResults[i] != SatisfactionLevel.Low)
                {
                    approvedCount++;
                }
            }
            
            if (approvedCount > 0 && toggleViewButton != null)
            {
                toggleViewButton.gameObject.SetActive(true);
                
                if (!hasMovedToggleButton)
                {
                    RectTransform buttonRect = toggleViewButton.GetComponent<RectTransform>();
                    if (buttonRect != null)
                    {
                        Vector3 currentPos = buttonRect.anchoredPosition;
                        buttonRect.anchoredPosition = new Vector3(currentPos.x, currentPos.y - 35, currentPos.z);
                        hasMovedToggleButton = true;
                    }
                }
                
                showingCard = true;
                UpdateToggleButtonText();
                
                ToggleView();
            }
            else
            {
                if (toggleViewButton != null)
                {
                    toggleViewButton.gameObject.SetActive(false);
                }
            }
        }
    }

    private void DisplaySingleCard(SelectedCardData cardData)
    {
        if (cardTarefasPrefab == null || playerCamera == null || cardData == null) return;
        
        float xOffset = OffsetManager.Instance != null ? OffsetManager.Instance.xOffset : 160f;
        Vector3 screenCenter = new Vector3(Screen.width / 2f + xOffset, Screen.height / 2f, cardSpawnDistance);
        Vector3 centerPosition = playerCamera.ScreenToWorldPoint(screenCenter);
        
        Quaternion rotation = Quaternion.Euler(-90, 0, 180);
        displayedCard = Instantiate(cardTarefasPrefab, centerPosition, rotation);
        displayedCard.transform.localScale = new Vector3(0.51f, 0.0001f, 0.65f);
        
        ApplyCardData(displayedCard, cardData);
    }


    
    private void onTimerComplete(SatisfactionLevel level)
    {
        bool isProductOwner = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        if (isProductOwner)
        {
            Hashtable props = new Hashtable();
            props[RESULT_TIMER_COMPLETE_KEY] = System.DateTime.Now.Ticks; // Use timestamp to ensure property change
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        
        // Hide result text for PO
        if (displayText != null)
        {
            displayText.gameObject.SetActive(false);
        }
        
        isShowingResult = false;

        // Check if we need to proceed to next round
        if (currentRound + 1 < totalRounds)
        {
            currentRound++;
            hasVoted = false;
            
            if (currentRound == 1 && roundResults.Count > 0 && roundResults[0] == SatisfactionLevel.Low)
            {
                if (SelectedCardStorage.Instance != null)
                {
                    SelectedCardStorage.Instance.ClearRejectedCard();
                    Debug.Log("Carta reprovada novamente foi removida do storage");
                }
            }
            
            if (isProductOwner)
            {
                Hashtable props = new Hashtable();
                props[FORCE_DRAWING_VIEW_KEY] = System.DateTime.Now.Ticks; // Use timestamp to ensure property change
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                Debug.Log("Comando enviado para todos retornarem à visualização do desenho");
            }
            
            DisplayCurrentRoundCard();
            
            ShowUI();
            InitializeViewState();
            
            HideRoundInfo();
            
            Debug.Log($"Iniciando rodada {currentRound + 1}/{totalRounds}");
        }
        else
        {
            // All rounds completed - handle card storage based on results
            if (SelectedCardStorage.Instance != null)
            {
                if (isMultiRound && roundResults.Count >= 2)
                {
                    SatisfactionLevel rejectedCardResult = roundResults[0]; // Resultado da carta reprovada
                    SatisfactionLevel selectedCardResult = roundResults[1]; // Resultado da nova carta
                    
                    Debug.LogWarning($"DEBUG MULTI: Rejected result: {rejectedCardResult}, Selected result: {selectedCardResult}");
                    
                    // Primeiro, lidar com a nova carta
                    if (selectedCardResult == SatisfactionLevel.Low)
                    {
                        // Nova carta foi reprovada - mover para rejected (substitui a antiga)
                        Debug.LogWarning($"DEBUG: Antes de mover carta - HasSelected: {SelectedCardStorage.Instance.HasSelectedCard()}, HasRejected: {SelectedCardStorage.Instance.HasRejectedCard()}");
                        SelectedCardStorage.Instance.MoveSelectedToRejected();
                        Debug.LogWarning($"DEBUG: Depois de mover carta - HasSelected: {SelectedCardStorage.Instance.HasSelectedCard()}, HasRejected: {SelectedCardStorage.Instance.HasRejectedCard()}");
                        Debug.LogWarning("Nova carta foi reprovada - movida para rejected storage (substitui a anterior)");
                    }
                    else
                    {
                        // Nova carta foi aprovada - limpar selected
                        SelectedCardStorage.Instance.ClearSelectedCard();
                        Debug.LogWarning("Nova carta foi aprovada - removida do storage");
                        
                        // Se a carta reprovada anterior também foi aprovada, limpar rejected
                        if (rejectedCardResult != SatisfactionLevel.Low)
                        {
                            SelectedCardStorage.Instance.ClearRejectedCard();
                            Debug.LogWarning("Carta reprovada anterior também foi aprovada - removida do storage");
                        }
                        // Se a carta reprovada anterior foi reprovada novamente, ela fica no rejected storage
                    }
                }
                else if (!isMultiRound && roundResults.Count >= 1)
                {
                    SatisfactionLevel result = roundResults[0];
                    
                    if (result == SatisfactionLevel.Low)
                    {
                        Debug.LogWarning($"DEBUG SINGLE: Antes de mover carta - HasSelected: {SelectedCardStorage.Instance.HasSelectedCard()}, HasRejected: {SelectedCardStorage.Instance.HasRejectedCard()}");
                        SelectedCardStorage.Instance.MoveSelectedToRejected();
                        Debug.LogWarning($"DEBUG SINGLE: Depois de mover carta - HasSelected: {SelectedCardStorage.Instance.HasSelectedCard()}, HasRejected: {SelectedCardStorage.Instance.HasRejectedCard()}");
                        Debug.LogWarning("Carta selecionada foi reprovada - movida para rejected storage");
                    }
                    else
                    {
                        SelectedCardStorage.Instance.ClearSelectedCard();
                        Debug.LogWarning("Carta selecionada foi aprovada - removida do storage");
                    }
                }
            }
            
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
            
            HideRoundInfo();
            
            Debug.Log("Sprint Review completo - todas as rodadas finalizadas");
        }
        
        // Atualizar botão de pause quando timer acaba
        UpdatePauseButtonVisibility();
    }
    
    private void ClearDisplayedCard()
    {
        if (displayedCard != null)
        {
            Destroy(displayedCard);
            displayedCard = null;
        }
        
        foreach (GameObject card in spawnedCards)
        {
            if (card != null && card != displayedCard)
            {
                Destroy(card);
            }
        }
        spawnedCards.Clear();
    }
    
    public void ResetSprintReviewManager()
    {
        hasDisplayedCard = false;
        hasVoted = false;
        isShowingResult = false;
        showingCard = true;
        
        isMultiRound = false;
        currentRound = 0;
        totalRounds = 1;
        cardsToReview.Clear();
        roundResults.Clear();
        hasBeenPrepared = false;
        
        HideRoundInfo();
        
        // Esconder e resetar UI
        if (displayText != null)
            displayText.gameObject.SetActive(false);
        if (satisfactionText != null)
            satisfactionText.gameObject.SetActive(false);
            
        // Esconder botões de avaliação
            if (highSatisfactionButton != null)
                highSatisfactionButton.transform.parent.gameObject.SetActive(false);
        if (mediumSatisfactionButton != null)
            mediumSatisfactionButton.transform.parent.gameObject.SetActive(false);
        if (lowSatisfactionButton != null)
            lowSatisfactionButton.transform.parent.gameObject.SetActive(false);
            
        // Esconder botão de toggle view
        if (toggleViewButton != null)
            toggleViewButton.gameObject.SetActive(false);
            
        // Esconder botão de pause
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);
            
        // Limpar carta exibida
        ClearDisplayedCard();
        
        // Mostrar tabuleiro se estava escondido
        if (tabuleiro != null)
            tabuleiro.SetActive(true);
            
        // Ativar shapesContainer se estava desativado
        if (shapesContainer != null)
            shapesContainer.SetActive(true);
    }

    // Método para atualizar visibilidade do botão de pause
    void UpdatePauseButtonVisibility()
    {
        if (pauseButton == null) return;
        
        bool isPO = productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner();
        bool hasActiveTimer = TimerManager.Instance != null && TimerManager.Instance.IsTimerActive();
        bool isSprintReviewPhase = gameStateManager != null && gameStateManager.GetCurrentState() == GameStateManager.GameState.SprintReview;
        
        // Mostrar botão se é PO, tem timer ativo E está na fase de Sprint Review
        if (isPO && hasActiveTimer && isSprintReviewPhase)
        {
            pauseButton.gameObject.SetActive(true);
            
            // Configurar onClick baseado no estado atual
            pauseButton.onClick.RemoveAllListeners();
            
            if (TimerManager.Instance.IsPaused())
            {
                pauseButton.GetComponentInChildren<TMP_Text>().text = "Despausar";
                pauseButton.onClick.AddListener(() => UnpauseTimers());
            }
            else
            {
                pauseButton.GetComponentInChildren<TMP_Text>().text = "Pausar";
                pauseButton.onClick.AddListener(() => PauseTimers());
            }
        }
        else
        {
            pauseButton.gameObject.SetActive(false);
        }
    }
    
    // Métodos para controle de pause pelo PO
    public void PauseTimers()
    {
        if (TimerManager.Instance != null && productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            TimerManager.Instance.PauseTimer();
            UpdatePauseButtonVisibility();
            
            PreserveResultTextOnPause();
        }
    }
    
    public void UnpauseTimers()
    {
        if (TimerManager.Instance != null && productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
        {
            TimerManager.Instance.UnpauseTimer();
            UpdatePauseButtonVisibility();
        }
    }

    private void PreserveResultTextOnPause()
    {
        if (isShowingResult && displayText != null && displayText.gameObject.activeInHierarchy)
        {
            StartCoroutine(KeepResultTextActive());
        }
    }

    private IEnumerator KeepResultTextActive()
    {
        while (TimerManager.Instance != null && TimerManager.Instance.IsPaused() && isShowingResult)
        {
            if (displayText != null && !displayText.gameObject.activeInHierarchy)
            {
                displayText.gameObject.SetActive(true);
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void MoveElementsDown()
    {
        MobileScaler mobileScaler = FindObjectOfType<MobileScaler>();
        bool isMobile = mobileScaler != null && mobileScaler.isMobile;
        if (!isMobile) return;

        if (buttonsContainer != null)
        {
            RectTransform buttonsRect = buttonsContainer.GetComponent<RectTransform>();
            if (buttonsRect != null)
            {
                buttonsRect.anchoredPosition += new Vector2(0, -evaluationUIOffset);
            }
        }
        
        if (satisfactionText != null)
        {
            RectTransform satisfactionTextRect = satisfactionText.GetComponent<RectTransform>();
            if (satisfactionTextRect != null)
            {
                satisfactionTextRect.anchoredPosition += new Vector2(0, -evaluationUIOffset);
            }
        }
    }

    private void RestoreElementsPosition()
    {
        MobileScaler mobileScaler = FindObjectOfType<MobileScaler>();
        bool isMobile = mobileScaler != null && mobileScaler.isMobile;
        if (!isMobile) return;

        if (buttonsContainer != null)
        {
            RectTransform buttonsRect = buttonsContainer.GetComponent<RectTransform>();
            if (buttonsRect != null)
            {
                buttonsRect.anchoredPosition += new Vector2(0, evaluationUIOffset);
            }
        }
        
        if (satisfactionText != null)
        {
            RectTransform satisfactionTextRect = satisfactionText.GetComponent<RectTransform>();
            if (satisfactionTextRect != null)
            {
                satisfactionTextRect.anchoredPosition += new Vector2(0, evaluationUIOffset);
            }
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
            
            if (key == SATISFACTION_RESULT_KEY)
            {
                if (showingCard) OnToggleViewClicked();
                
                SatisfactionLevel level = (SatisfactionLevel)(int)property.Value;
                if (!productOwnerManager.IsLocalPlayerProductOwner())
                {
                    if (toggleViewButton != null) toggleViewButton.gameObject.SetActive(false);
                    ShowResult(level);
                    
                    EnablePlayerToggleIfNeeded();
                }
            }
            else if (key == "RoundResults")
            {
                if (!productOwnerManager.IsLocalPlayerProductOwner())
                {
                    int[] roundResultsArray = (int[])property.Value;
                    roundResults.Clear();
                    
                    for (int i = 0; i < roundResultsArray.Length; i++)
                    {
                        roundResults.Add((SatisfactionLevel)roundResultsArray[i]);
                    }
                    
                    Debug.Log($"RoundResults sincronizados para player - {roundResults.Count} resultados");
                }
            }
            else if (key == "CurrentRound")
            {
                if (!productOwnerManager.IsLocalPlayerProductOwner())
                {
                    currentRound = (int)property.Value;
                }
            }
            else if (key == "TotalRounds")
            {
                if (!productOwnerManager.IsLocalPlayerProductOwner())
                {
                    totalRounds = (int)property.Value;
                }
            }
            else if (key == "IsMultiRound")
            {
                if (!productOwnerManager.IsLocalPlayerProductOwner())
                {
                    isMultiRound = (bool)property.Value;
                }
            }
            else if (key == SHOW_APPROVED_CARDS_KEY)
            {
                if (!productOwnerManager.IsLocalPlayerProductOwner())
                {
                    var cardsArray = (Dictionary<string, object>[])property.Value;
                    List<SelectedCardData> approvedCards = new List<SelectedCardData>();
                    
                    foreach (var cardSync in cardsArray)
                    {
                        approvedCards.Add(CreateCardDataFromSync(cardSync));
                    }
                    
                    if (!showingCard)
                    {
                        showingCard = true;
                        UpdateToggleButtonText();
                        ToggleView();
                    }
                    
                    DisplayApprovedCardsLocally(approvedCards);
                    
                    EnablePlayerToggleIfNeeded();
                    
                    Debug.Log($"Comando recebido para mostrar {approvedCards.Count} carta(s) aprovada(s)");
                }
            }
            else if (key == FORCE_DRAWING_VIEW_KEY)
            {
                // Force all players (except PO) back to drawing view for new round
                if (!productOwnerManager.IsLocalPlayerProductOwner())
                {
                    showingCard = false;
                    if (toggleViewButton != null) 
                    {
                        toggleViewButton.gameObject.SetActive(false);
                        UpdateToggleButtonText();
                    }
                    ShowDrawingViewForPlayers();
                    
                    if (displayText != null)
                    {
                        displayText.text = "Aguardando feedback do PO...";
                        displayText.gameObject.SetActive(true);
                    }
                    
                    Debug.Log("Forçado a retornar para visualização do desenho para nova rodada");
                }
            }
            else if (key == "FORCE_DRAWING_NO_WAITING")
            {
                if (!productOwnerManager.IsLocalPlayerProductOwner())
                {
                    showingCard = false;
                    if (toggleViewButton != null) 
                    {
                        toggleViewButton.gameObject.SetActive(false);
                        UpdateToggleButtonText();
                    }
                    ShowDrawingViewForPlayers();
                    
                    if (displayText != null && !isShowingResult)
                    {
                        displayText.gameObject.SetActive(false);
                    }
                    
                    Debug.Log("Forçado a retornar para visualização do desenho (carta rejeitada - sem texto de espera)");
                }
            }
            else if (key == RESULT_TIMER_COMPLETE_KEY)
            {
                if (!productOwnerManager.IsLocalPlayerProductOwner())
                {
                    if (displayText != null)
                    {
                        displayText.gameObject.SetActive(false);
                    }

                    if (toggleViewButton != null)
                    {
                        toggleViewButton.gameObject.SetActive(false);
                    }

                    isShowingResult = false;
                }
                else
                {
                    if (satisfactionText != null)
                    {
                        satisfactionText.gameObject.SetActive(false);
                    }
                }
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
    
    void OnDestroy()
    {
        ClearDisplayedCard();
    }
}
