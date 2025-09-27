using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class SprintPlanningManager : MonoBehaviourPun
{
  [Header("Card Prefab")]
  public GameObject cardTarefasPrefab;

  [Header("Card Spawn Positions")]
  public Transform[] cardSpawnPositions = new Transform[4];

  [Header("Camera Reference")]
  private Camera playerCamera;

  [Header("Product Owner Manager")]
  private ProductOwnerManager productOwnerManager;

  [Header("Game Board")]
  private GameObject tabuleiro;

  [Header("UI Elements")]
  public UnityEngine.UI.Button startDraftButton;
  public GameObject stopDraftContainer;
  public UnityEngine.UI.Button stopDraftButton;
  public TMP_Text draftText;
  public GameObject legendaContainer;
  
  [Header("Draft Completion Elements")]
  public GameObject finishDraftContainer;
  public UnityEngine.UI.Button finishDraftButton;
  public GameObject allPlayersFinishedPopup;
  public UnityEngine.UI.Button closeAllFinishedButton;
  public UnityEngine.UI.Button endSprintPlanningButton;

  [Header("Positioning")]
  public float spawnDistance = 1f; // Câmera: -297f, Cards: -296f
  public float selectedCardSpawnDistance = 0.67f; // Câmera: -297f, Card: -296.33
  public float cardSpacing = 0.8f;
  public float centeredCardSpacing = 0.6f; // Espaçamento entre rejected e selected quando centralizadas

  private List<GameObject> spawnedCards = new List<GameObject>();
  private GameStateManager gameStateManager;
  private bool hasSpawnedCardsThisPhase = false;
  private GameObject selectedCard = null;
  private bool cardSelectionCompleted = false;
  private CardTarefas selectedCardData = null;
  private bool hasStartedDraft = false;
  private bool hasLocalPlayerFinished = false;
  private NetworkManager networkManager;

  void Start()
  {
    playerCamera = Camera.main;
    productOwnerManager = FindObjectOfType<ProductOwnerManager>();
    gameStateManager = GameStateManager.Instance;
    tabuleiro = GameObject.Find("Tabuleiro");
    networkManager = FindObjectOfType<NetworkManager>();

    if (playerCamera == null)
    {
      Debug.LogError("Câmera principal não encontrada!");
    }

    if (startDraftButton != null)
    {
      startDraftButton.gameObject.SetActive(false);
      startDraftButton.onClick.AddListener(OnStartButtonClicked);
    }

    if (stopDraftContainer != null && stopDraftButton != null)
    {
      stopDraftContainer.gameObject.SetActive(false);
      stopDraftButton.onClick.AddListener(OnStopDraftButtonClicked);
    }

    if (legendaContainer != null)
    {
      legendaContainer.SetActive(false);
    }

    // Setup dos elementos de conclus\u00e3o do rascunho
    if (finishDraftContainer != null && finishDraftButton != null)
    {
      finishDraftContainer.SetActive(false);
      finishDraftButton.onClick.AddListener(OnFinishDraftButtonClicked);
    }

    if (allPlayersFinishedPopup != null)
    {
      allPlayersFinishedPopup.SetActive(false);
    }

    if (closeAllFinishedButton != null)
    {
      closeAllFinishedButton.onClick.AddListener(OnCloseAllFinishedButtonClicked);
    }

    if (endSprintPlanningButton != null)
    {
      endSprintPlanningButton.onClick.AddListener(OnEndSprintPlanningButtonClicked);
    }
  }

  void Update()
  {
    if (productOwnerManager == null)
    {
      productOwnerManager = FindObjectOfType<ProductOwnerManager>();
    }

    if (networkManager == null)
    {
      networkManager = FindObjectOfType<NetworkManager>();
    }

    if (gameStateManager == null) return;

    var currentState = gameStateManager.GetCurrentState();

    if (currentState == GameStateManager.GameState.SprintPlanning)
    {
      if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner() && tabuleiro != null)
      {
        tabuleiro.SetActive(false);
      }

      if (draftText != null && productOwnerManager != null)
      {
        bool isLocalPlayerPO = productOwnerManager.IsLocalPlayerProductOwner();
        if (!isLocalPlayerPO)
        {
          if (hasStartedDraft)
          {
            draftText.gameObject.SetActive(false);
            
            // Mostrar container "Terminar Rascunho" para jogadores n\u00e3o-PO durante o rascunho
            if (finishDraftContainer != null && !hasLocalPlayerFinished)
            {
              finishDraftContainer.SetActive(true);
            }
          }
          else
          {
            draftText.gameObject.SetActive(true);
            draftText.text = "Aguardando PO escolher a carta e começar o rascunho...";
            
            if (finishDraftContainer != null)
            {
              finishDraftContainer.SetActive(false);
            }
          }
        }
        else
        {
          if (finishDraftContainer != null)
          {
            finishDraftContainer.SetActive(false);
          }
        }
      }

      if (!hasSpawnedCardsThisPhase &&
          spawnedCards.Count == 0 &&
          productOwnerManager != null &&
          productOwnerManager.IsLocalPlayerProductOwner())
      {
        SpawnTaskCards();
        hasSpawnedCardsThisPhase = true;
      }
    }
    else
    {
      if (hasSpawnedCardsThisPhase)
      {
        hasSpawnedCardsThisPhase = false;
      }

      cardSelectionCompleted = false;
      selectedCard = null;
      selectedCardData = null;

      if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner() && tabuleiro != null)
      {
        // Don't reactivate tabuleiro if we're in SprintReview phase (let SprintReviewManager handle it)
        if (currentState != GameStateManager.GameState.SprintReview && currentState != GameStateManager.GameState.Fim)
        {
          tabuleiro.SetActive(true);
        }
      }

      if (startDraftButton != null)
      {
        startDraftButton.gameObject.SetActive(false);
      }

      if (stopDraftContainer != null)
      {
        stopDraftContainer.gameObject.SetActive(false);
      }

      if (legendaContainer != null)
      {
        legendaContainer.SetActive(false);
      }

      if (finishDraftContainer != null)
      {
        finishDraftContainer.SetActive(false);
      }

      if (allPlayersFinishedPopup != null)
      {
        allPlayersFinishedPopup.SetActive(false);
      }
    }
  }

  private void SpawnTaskCards()
  {
    if (cardTarefasPrefab == null)
    {
      Debug.LogError("Prefab de carta não atribuído!");
      return;
    }

    ClearSpawnedCards();

    if (CardScoreManager.Instance == null)
    {
      GameObject scoreManagerGO = new GameObject("CardScoreManager");
      scoreManagerGO.AddComponent<CardScoreManager>();
    }

    List<Dictionary<string, int>> uniqueScores = CardScoreManager.Instance.GenerateUniqueCardScores();

    for (int i = 0; i < 4; i++)
    {
      Vector3 spawnPosition;

      if (cardSpawnPositions[i] != null)
      {
        spawnPosition = cardSpawnPositions[i].position;
      }
      else
      {
        spawnPosition = CalculateCardPosition(i);
      }

      Quaternion rotation = Quaternion.Euler(-90, 0, 180);

      GameObject newCard = Instantiate(cardTarefasPrefab, spawnPosition, rotation);

      newCard.transform.localScale = new Vector3(0.51f, 0.0001f, 0.65f);

      // Configurar pontuações únicas para esta carta
      CardTarefas cardTarefas = newCard.GetComponent<CardTarefas>();
      if (cardTarefas != null && i < uniqueScores.Count)
      {
        cardTarefas.SetPredefinedScores(uniqueScores[i]);
        Debug.Log($"Carta {i + 1} configurada com pontuações únicas");
      }

      // Ativar o componente Censura quando mostrar as 4 cartas
      Transform censuraTransform = FindChildByName(newCard.transform, "Censura");
      if (censuraTransform != null)
      {
        censuraTransform.gameObject.SetActive(true);
        Debug.Log($"Censura ativada para carta {i + 1}");
      }

      CardSelector cardSelector = newCard.AddComponent<CardSelector>();
      cardSelector.Initialize(this, i);

      spawnedCards.Add(newCard);

      Debug.Log($"Carta de tarefa {i + 1} spawnada na posição: {spawnPosition}");
    }
  }

  private Vector3 CalculateCardPosition(int cardIndex)
  {
    if (playerCamera == null) return Vector3.zero;

    // Calcula a posição para spawnar: na frente da câmera + 160px para a direita
    float xOffset = OffsetManager.Instance != null ? OffsetManager.Instance.xOffset : 160f;
    Vector3 screenCenter = new Vector3(Screen.width / 2f + xOffset, Screen.height / 2f, spawnDistance);
    Vector3 centerPosition = playerCamera.ScreenToWorldPoint(screenCenter);

    // 0 = top-left, 1 = top-right, 2 = bottom-left, 3 = bottom-right
    float offsetX = (cardIndex % 2 == 0) ? -cardSpacing / 2f : cardSpacing / 2f; // Left/Right
    float offsetY = (cardIndex < 2) ? cardSpacing / 2f : -cardSpacing / 2f;     // Top/Bottom

    Vector3 rightOffset = playerCamera.transform.right * offsetX;
    Vector3 upOffset = playerCamera.transform.up * offsetY;

    return centerPosition + rightOffset + upOffset;
  }

  private Vector3 CalculateCenteredPosition(bool isLeftCard)
  {
    if (playerCamera == null) return Vector3.zero;

    float xOffset = OffsetManager.Instance != null ? OffsetManager.Instance.xOffset : 160f;
    Vector3 screenCenter = new Vector3(Screen.width / 2f + xOffset, Screen.height / 2f, selectedCardSpawnDistance);
    Vector3 centerPosition = playerCamera.ScreenToWorldPoint(screenCenter);

    float offset = centeredCardSpacing / 2f;
    float horizontalOffset = isLeftCard ? -offset : offset;

    Vector3 rightOffset = playerCamera.transform.right * horizontalOffset;
    return centerPosition + rightOffset;
  }

  public void OnCardSelected(int cardIndex, GameObject selectedCardObj)
  {
    if (selectedCardObj == null || cardSelectionCompleted) return;

    CardTarefas cardTarefas = selectedCardObj.GetComponent<CardTarefas>();
    if (cardTarefas != null)
    {
      selectedCardData = cardTarefas;

      if (SelectedCardStorage.Instance == null)
      {
        GameObject storageGO = new GameObject("SelectedCardStorage");
        storageGO.AddComponent<SelectedCardStorage>();
      }

      // Registrar a textura da carta selecionada como utilizada
      if (UsedCardsManager.Instance == null)
      {
        GameObject usedCardsGO = new GameObject("UsedCardsManager");
        usedCardsGO.AddComponent<UsedCardsManager>();
      }
      
      if (cardTarefas.meshRenderer != null && cardTarefas.meshRenderer.material != null && cardTarefas.meshRenderer.material.mainTexture != null)
      {
        string textureName = cardTarefas.meshRenderer.material.mainTexture.name;
        UsedCardsManager.Instance.AddUsedCard(textureName);
      }

      SelectedCardStorage.Instance.StoreSelectedCard(cardTarefas);
      Debug.Log($"Carta {cardIndex + 1} selecionada e armazenada!");
    }

    cardSelectionCompleted = true;

    ClearSpawnedCards();
    RecreateSelectedCards();

    ShowStartButton();
    ShowLegenda();
  }

  private void RecreateSelectedCards()
  {
    if (cardTarefasPrefab == null || playerCamera == null || selectedCardData == null) return;

    bool hasRejectedCard = SelectedCardStorage.Instance != null && SelectedCardStorage.Instance.HasRejectedCard();
    
    if (hasRejectedCard)
    {
      CreateRejectedCardDisplay();
      CreateSelectedCardDisplay(true);
    }
    else
    {
      CreateSelectedCardDisplay(false);
    }
  }

  private void CreateRejectedCardDisplay()
  {
    if (SelectedCardStorage.Instance == null || !SelectedCardStorage.Instance.HasRejectedCard()) return;

    CardTarefas storedRejectedCard = SelectedCardStorage.Instance.GetRejectedCard();
    if (storedRejectedCard == null) return;

    Vector3 position = CalculateCenteredPosition(true);
    Quaternion rotation = Quaternion.Euler(-90, 0, 180);
    
    bool prefabWasActive = cardTarefasPrefab.activeSelf;
    cardTarefasPrefab.SetActive(false);
    
    GameObject rejectedCard = Instantiate(cardTarefasPrefab, position, rotation);
    rejectedCard.transform.localScale = new Vector3(0.51f, 0.0001f, 0.65f);
    
    cardTarefasPrefab.SetActive(prefabWasActive);
    
    ApplyCardData(rejectedCard, storedRejectedCard);
    
    rejectedCard.SetActive(true);

    CardSelector selector = rejectedCard.GetComponent<CardSelector>();
    if (selector != null)
    {
      Destroy(selector);
    }

    spawnedCards.Add(rejectedCard);
    Debug.Log("Carta reprovada exibida na posição 0");
  }

  private void CreateSelectedCardDisplay(bool showingWithRejected)
  {
    Vector3 centerPosition;
    
    if (!showingWithRejected)
    {
      float xOffset = OffsetManager.Instance != null ? OffsetManager.Instance.xOffset : 160f;
      Vector3 screenCenter = new Vector3(Screen.width / 2f + xOffset, Screen.height / 2f, selectedCardSpawnDistance);
      centerPosition = playerCamera.ScreenToWorldPoint(screenCenter);
    }
    else
    {
      centerPosition = CalculateCenteredPosition(false);
    }

    Quaternion rotation = Quaternion.Euler(-90, 0, 180);
    selectedCard = Instantiate(cardTarefasPrefab, centerPosition, rotation);
    selectedCard.transform.localScale = new Vector3(0.51f, 0.0001f, 0.65f);

    ApplyCardData(selectedCard, selectedCardData);

    CardSelector selector = selectedCard.GetComponent<CardSelector>();
    if (selector != null)
    {
      Destroy(selector);
    }

    spawnedCards.Add(selectedCard);
  }

  private void ClearSpawnedCards()
  {
    foreach (GameObject card in spawnedCards)
    {
      if (card != null)
      {
        Destroy(card);
      }
    }
    spawnedCards.Clear();
  }

  private void ApplyCardData(GameObject card, CardTarefas sourceCardData)
  {
    if (card == null || sourceCardData == null) return;

    MeshRenderer sourceMeshRenderer = sourceCardData.meshRenderer;
    MeshRenderer targetMeshRenderer = card.GetComponent<MeshRenderer>();
    if (sourceMeshRenderer != null && targetMeshRenderer != null && sourceMeshRenderer.material != null)
    {
      targetMeshRenderer.material = new Material(sourceMeshRenderer.material);
    }

    CardTarefas targetCardTarefas = card.GetComponent<CardTarefas>();
    if (targetCardTarefas != null)
    {
      targetCardTarefas.scores = new Dictionary<string, int>(sourceCardData.scores);
      targetCardTarefas.maxScore = sourceCardData.maxScore;
      targetCardTarefas.isSelected = true;
      targetCardTarefas.InitializeWithCustomData(sourceCardData.scores, targetMeshRenderer?.material, sourceCardData.maxScore);
    }
  }

  private void ShowStartButton()
  {
    if (startDraftButton != null)
    {
      startDraftButton.gameObject.SetActive(true);
    }
  }

  private void ShowLegenda()
  {
    if (legendaContainer != null)
    {
      legendaContainer.SetActive(true);
    }
  }

  private void OnStartButtonClicked()
  {
    Debug.Log("Botão de 'Começar' clicado!");

    hasStartedDraft = true;

    if (startDraftButton != null)
    {
      startDraftButton.gameObject.SetActive(false);
    }

    // Mostrar botão de parar rascunho
    if (stopDraftContainer != null)
    {
      stopDraftContainer.gameObject.SetActive(true);
    }

    // Momento do rascunho - iniciar timer de 1:40 para todos os players
    if (TimerManager.Instance != null)
    {
      TimerManager.Instance.StartTimer(100f, OnDraftTimeComplete, "DraftTimer");
    }

    if (CanvasManager.Instance != null)
    {
      CanvasManager.Instance.ActivateCanvasForOthers();
      CanvasManager.Instance.ActivateDrawingForAll();
    }

    draftText.gameObject.SetActive(true);
    draftText.text = "Descreva o desenho, o Dev Team está rascunhando...";
    photonView.RPC("RascunhoIniciado", RpcTarget.All);
  }

  private void OnStopDraftButtonClicked()
  {
    Debug.Log("Botão 'Parar Rascunho' clicado!");

    if (stopDraftContainer != null)
    {
      stopDraftContainer.gameObject.SetActive(false);
    }

    // Limpar textos "Terminou!" de todos os jogadores
    ClearAllPlayerFinishedTexts();

    if (TimerManager.Instance != null)
    {
      TimerManager.Instance.EndTimer();
    }
  }

  private void OnDraftTimeComplete()
  {
    Debug.Log("Timer do rascunho encerrado!");

    if (CanvasManager.Instance != null)
    {
      CanvasManager.Instance.SaveAndSyncAllPlayerDrawings();
      CanvasManager.Instance.ClearCanvasForAll();
      CanvasManager.Instance.DeactivateCanvasForAll();
    }

    ClearAllPlayerFinishedTexts();

    if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
    {
      ClearSpawnedCards();
      draftText.gameObject.SetActive(false);
      gameStateManager.NextState();
    }

    hasStartedDraft = false;
    photonView.RPC("RascunhoTerminado", RpcTarget.All);
  }

  [PunRPC]
  void RascunhoIniciado()
  {
    hasStartedDraft = true;
  }

  [PunRPC]
  void RascunhoTerminado()
  {
    hasStartedDraft = false;
    hasLocalPlayerFinished = false;
  }

  private void OnFinishDraftButtonClicked()
  {
    Debug.Log($"Jogador {PhotonNetwork.LocalPlayer.NickName} (ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}) terminou o rascunho!");
    
    hasLocalPlayerFinished = true;
    
    if (finishDraftContainer != null)
    {
      finishDraftContainer.SetActive(false);
    }
    
    UpdatePlayerPOText("Terminou!");
    
    photonView.RPC("PlayerFinishedDraft", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
  }

  private void OnCloseAllFinishedButtonClicked()
  {
    Debug.Log("Fechando popup de todos terminaram");
    
    if (allPlayersFinishedPopup != null)
    {
      allPlayersFinishedPopup.SetActive(false);
    }
  }

  private void OnEndSprintPlanningButtonClicked()
  {
    Debug.Log("Encerrando Sprint Planning via popup!");
    
    if (allPlayersFinishedPopup != null)
    {
      allPlayersFinishedPopup.SetActive(false);
    }
    
    ClearAllPlayerFinishedTexts();
    
    if (TimerManager.Instance != null)
    {
      TimerManager.Instance.EndTimer();
    }
    
    if (CanvasManager.Instance != null)
    {
      CanvasManager.Instance.SaveAndSyncAllPlayerDrawings();
      CanvasManager.Instance.ClearCanvasForAll();
      CanvasManager.Instance.DeactivateCanvasForAll();
    }
    
    if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
    {
      ClearSpawnedCards();
      if (draftText != null)
        draftText.gameObject.SetActive(false);
      gameStateManager.NextState();
    }
    
    hasStartedDraft = false;
    photonView.RPC("RascunhoTerminado", RpcTarget.All);
  }

  private void UpdatePlayerPOText(string text)
  {
    if (networkManager == null) 
    {
      Debug.LogError("SprintPlanningManager: NetworkManager \u00e9 null no UpdatePlayerPOText!");
      return;
    }
    
    int playerIndex = GetPlayerIndex(PhotonNetwork.LocalPlayer.ActorNumber);
    Debug.Log($"SprintPlanningManager: UpdatePlayerPOText - ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}, PlayerIndex: {playerIndex}, Text: '{text}'");
    
    if (playerIndex >= 0 && playerIndex < 5)
    {
      photonView.RPC("UpdatePlayerPOTextRPC", RpcTarget.All, playerIndex, text);
    }
    else
    {
      Debug.LogError($"SprintPlanningManager: PlayerIndex inv\u00e1lido: {playerIndex}");
    }
  }

  private int GetPlayerIndex(int actorNumber)
  {
    Player[] players = PhotonNetwork.PlayerList;
    for (int i = 0; i < players.Length && i < 5; i++)
    {
      if (players[i].ActorNumber == actorNumber)
      {
        return i;
      }
    }
    return -1;
  }

  [PunRPC]
  void UpdatePlayerPOTextRPC(int playerIndex, string text)
  {
    if (networkManager == null)
    {
      networkManager = FindObjectOfType<NetworkManager>();
    }
    
    if (networkManager != null)
    {
      networkManager.UpdatePlayerPOText(playerIndex, text);
    }
    else
    {
      Debug.LogError("SprintPlanningManager: NetworkManager n\u00e3o encontrado!");
    }
  }

  [PunRPC]
  void PlayerFinishedDraft(int actorNumber)
  {
    Debug.Log($"Jogador {actorNumber} terminou o rascunho");
    
    if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
    {
      CheckIfAllPlayersFinished();
    }
  }

  private void CheckIfAllPlayersFinished()
  {
    Player[] players = PhotonNetwork.PlayerList;
    int totalNonPOPlayers = 0;
    int finishedPlayers = 0;
    
    foreach (Player player in players)
    {
      bool isPlayerPO = false;
      if (player.CustomProperties.ContainsKey("IsProductOwner"))
      {
        isPlayerPO = (bool)player.CustomProperties["IsProductOwner"];
      }
      
      if (!isPlayerPO)
      {
        totalNonPOPlayers++;
        
        int playerIndex = GetPlayerIndex(player.ActorNumber);
        bool hasFinished = PlayerHasFinished(playerIndex);
        Debug.Log($"Jogador {player.NickName} (Index: {playerIndex}) - Terminou: {hasFinished}");
        
        if (hasFinished)
        {
          finishedPlayers++;
        }
      }
      else
      {
        Debug.Log($"Jogador {player.NickName} \u00e9 PO - ignorando");
      }
    }
    
    if (totalNonPOPlayers > 0 && finishedPlayers >= totalNonPOPlayers)
    {
      Debug.Log("TODOS TERMINARAM! Mostrando popup para o PO");
      ShowAllPlayersFinishedPopup();
    }
    else
    {
      Debug.Log("Nem todos terminaram ainda");
    }
  }

  private bool PlayerHasFinished(int playerIndex)
  {
    if (networkManager == null) 
    {
      Debug.LogError("SprintPlanningManager: NetworkManager \u00e9 null no PlayerHasFinished!");
      return false;
    }
    
    string playerText = networkManager.GetPlayerPOText(playerIndex);
    bool hasFinished = playerText == "Terminou!";
    Debug.Log($"PlayerHasFinished({playerIndex}): texto='{playerText}', terminou={hasFinished}");
    return hasFinished;
  }

  private void ShowAllPlayersFinishedPopup()
  {
    Debug.Log("Todos os jogadores terminaram! Mostrando popup para o PO.");
    
    if (allPlayersFinishedPopup != null)
    {
      allPlayersFinishedPopup.SetActive(true);
    }
  }

  private void ClearAllPlayerFinishedTexts()
  {
    Debug.Log("Limpando textos 'Terminou!' de todos os jogadores via RPC");
    
    photonView.RPC("ClearFinishedTextsRPC", RpcTarget.All);
  }

  [PunRPC]
  void ClearFinishedTextsRPC()
  {
    Debug.Log("RPC: Limpando textos 'Terminou!' localmente");
    
    if (networkManager != null)
    {
      networkManager.ClearFinishedPlayerTexts();
    }
    else
    {
      Debug.LogError("RPC: NetworkManager \u00e9 null na limpeza!");
      networkManager = FindObjectOfType<NetworkManager>();
      if (networkManager != null)
      {
        networkManager.ClearFinishedPlayerTexts();
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

  public void ResetSprintPlanning()
  {
    ClearSpawnedCards();
    hasStartedDraft = false;
    cardSelectionCompleted = false;
    selectedCard = null;
    selectedCardData = null;
    hasSpawnedCardsThisPhase = false;
    hasLocalPlayerFinished = false;
    
    ClearAllPlayerFinishedTexts();
    
    // Esconder elementos de UI
    if (startDraftButton != null)
      startDraftButton.gameObject.SetActive(false);
    if (stopDraftContainer != null)  
      stopDraftContainer.gameObject.SetActive(false);
    if (draftText != null)
      draftText.gameObject.SetActive(false);
    if (legendaContainer != null)
      legendaContainer.SetActive(false);
    if (finishDraftContainer != null)
      finishDraftContainer.SetActive(false);
    if (allPlayersFinishedPopup != null)
      allPlayersFinishedPopup.SetActive(false);
      
    Debug.Log("SprintPlanningManager resetado - cartas e UI limpos");
  }

  void OnDestroy()
  {
    ClearSpawnedCards();
  }
}
