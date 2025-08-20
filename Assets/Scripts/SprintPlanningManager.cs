using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using TMPro;

public class SprintPlanningManager : MonoBehaviour
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
  public TMP_Text draftText;

  [Header("Positioning")]
  public float spawnDistance = 1f; // Câmera: -297f, Cards: -296f
  public float selectedCardSpawnDistance = 0.67f; // Câmera: -297f, Card: -296.33
  public float cardSpacing = 0.8f;

  private List<GameObject> spawnedCards = new List<GameObject>();
  private GameStateManager gameStateManager;
  private bool hasSpawnedCardsThisPhase = false;
  private GameObject selectedCard = null;
  private bool cardSelectionCompleted = false;
  private CardTarefas selectedCardData = null;

  void Start()
  {
    playerCamera = Camera.main;
    productOwnerManager = FindObjectOfType<ProductOwnerManager>();
    gameStateManager = GameStateManager.Instance;
    tabuleiro = GameObject.Find("Tabuleiro");

    if (playerCamera == null)
    {
      Debug.LogError("Câmera principal não encontrada!");
    }

    if (startDraftButton != null)
    {
      startDraftButton.gameObject.SetActive(false);
      startDraftButton.onClick.AddListener(OnStartButtonClicked);
    }
  }

  void Update()
  {
    if (productOwnerManager == null)
    {
      productOwnerManager = FindObjectOfType<ProductOwnerManager>();
    }

    if (gameStateManager == null) return;

    var currentState = gameStateManager.GetCurrentState();

    if (currentState == GameStateManager.GameState.SprintPlanning)
    {
      if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner() && tabuleiro != null)
      {
        tabuleiro.SetActive(false);
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
        tabuleiro.SetActive(true);
      }

      if (startDraftButton != null)
      {
        startDraftButton.gameObject.SetActive(false);
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
    Vector3 screenCenter = new Vector3(Screen.width / 2f + 160f, Screen.height / 2f, spawnDistance);
    Vector3 centerPosition = playerCamera.ScreenToWorldPoint(screenCenter);

    // 0 = top-left, 1 = top-right, 2 = bottom-left, 3 = bottom-right
    float offsetX = (cardIndex % 2 == 0) ? -cardSpacing / 2f : cardSpacing / 2f; // Left/Right
    float offsetY = (cardIndex < 2) ? cardSpacing / 2f : -cardSpacing / 2f;     // Top/Bottom

    Vector3 rightOffset = playerCamera.transform.right * offsetX;
    Vector3 upOffset = playerCamera.transform.up * offsetY;

    return centerPosition + rightOffset + upOffset;
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

      SelectedCardStorage.Instance.StoreSelectedCard(cardTarefas);
      Debug.Log($"Carta {cardIndex + 1} selecionada e armazenada!");
    }

    cardSelectionCompleted = true;

    ClearSpawnedCards();
    RecreateSelectedCard();

    ShowStartButton();
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

  private void RecreateSelectedCard()
  {
    if (cardTarefasPrefab == null || playerCamera == null || selectedCardData == null) return;

    Vector3 screenCenter = new Vector3(Screen.width / 2f + 160f, Screen.height / 2f, selectedCardSpawnDistance);
    Vector3 centerPosition = playerCamera.ScreenToWorldPoint(screenCenter);

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

    Debug.Log("Carta selecionada recriada e centralizada!");
  }

  private void ApplyCardData(GameObject card, SelectedCardData cardData)
  {
    if (card == null || cardData == null) return;

    MeshRenderer meshRenderer = card.GetComponent<MeshRenderer>();
    if (meshRenderer != null && cardData.cardMaterial != null)
    {
      meshRenderer.material = cardData.cardMaterial;
    }

    CardTarefas cardTarefas = card.GetComponent<CardTarefas>();
    if (cardTarefas != null)
    {
      cardTarefas.scores = new Dictionary<string, int>(cardData.scores);
    }
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
      targetCardTarefas.isSelected = true;
      targetCardTarefas.UpdateScoreTexts();
    }
  }

  private void ShowStartButton()
  {
    if (startDraftButton != null)
    {
      startDraftButton.gameObject.SetActive(true);
    }
  }

  private void OnStartButtonClicked()
  {
    Debug.Log("Botão de 'Começar' clicado!");

    if (startDraftButton != null)
    {
      startDraftButton.gameObject.SetActive(false);
    }

    // Momento do rascunho - iniciar timer de 1:40 para todos os players
    if (TimerManager.Instance != null)
    {
      TimerManager.Instance.StartTimer(10f, OnDraftTimeComplete, "DraftTimer");
    }

    if (CanvasManager.Instance != null)
    {
      CanvasManager.Instance.ActivateCanvasForOthers();
    }

    draftText.gameObject.SetActive(true);
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

    if (productOwnerManager != null && productOwnerManager.IsLocalPlayerProductOwner())
    {
      ClearSpawnedCards();
      draftText.gameObject.SetActive(false);
      gameStateManager.NextState();
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

  void OnDestroy()
  {
    ClearSpawnedCards();
  }
}
