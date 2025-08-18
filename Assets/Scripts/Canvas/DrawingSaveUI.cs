using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DrawingSaveUI : MonoBehaviour
{
    [Header("UI Settings")]
    public bool createUI = true;
    public float padding = 12f;
    public float spacing = 10f;
    public Vector2 buttonSize = new Vector2(60f, 60f);
    public Vector2 saveButtonSize = new Vector2(80f, 60f);
    
    private DrawingSaveSystem saveSystem;
    private Button[] slotButtons = new Button[4];
    private Button saveButton;
    private GameObject uiPanel;
    private Canvas uiCanvas;
    
    void Start()
    {
        saveSystem = FindObjectOfType<DrawingSaveSystem>();

        if (createUI)
        {
            CreateSimpleUI();
        }
    }
    
    public void CreateSimpleUI()
    {
        try
        {
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                Debug.LogError("Canvas principal não encontrado!");
                return;
            }
            
            // Cria Canvas para a UI
            GameObject canvasGO = new GameObject("DrawingSaveCanvas");
            canvasGO.transform.SetParent(mainCanvas.transform, false);
            
            uiCanvas = canvasGO.AddComponent<Canvas>();
            uiCanvas.overrideSorting = true;
            uiCanvas.sortingOrder = 100;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            
            // Cria painel principal
            CreateMainPanel(canvasGO);
            
            // Cria elementos da UI
            CreateUIElements();
            
            UpdateSlotsVisual();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erro ao criar UI: {e.Message}");
        }
    }
    
    private void CreateMainPanel(GameObject parent)
    {
        uiPanel = new GameObject("SaveSlotsPanel");
        uiPanel.transform.SetParent(parent.transform, false);
        
        Image panelImage = uiPanel.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // Calcula tamanho do painel
        float totalWidth = saveButtonSize.x + (4 * buttonSize.x) + (4 * spacing) + (padding * 2);
        float totalHeight = Mathf.Max(saveButtonSize.y, buttonSize.y) + (padding * 2);
        
        RectTransform panelRect = uiPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f); // Canto superior esquerdo
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(totalWidth, totalHeight);
        panelRect.anchoredPosition = new Vector2(20f, -20f); // 20px da borda superior e esquerda
    }
    
    private void CreateUIElements()
    {
        // Container para organizar elementos
        GameObject container = new GameObject("ButtonContainer");
        container.transform.SetParent(uiPanel.transform, false);
        
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = new Vector2(padding, padding);
        containerRect.offsetMax = new Vector2(-padding, -padding);
        
        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        
        // Cria botão Salvar
        CreateSaveButton(container.transform);
        
        // Cria 4 slots numerados
        for (int i = 0; i < 4; i++)
        {
            CreateSlot(i, container.transform);
        }
    }
    
    private void CreateSaveButton(Transform parent)
    {
        GameObject saveObj = new GameObject("SaveButton");
        saveObj.transform.SetParent(parent, false);
        
        saveButton = saveObj.AddComponent<Button>();
        Image saveImg = saveObj.AddComponent<Image>();
        saveImg.color = new Color(0.2f, 0.8f, 0.2f); // Verde
        
        // Outline para o botão
        Outline saveOutline = saveObj.AddComponent<Outline>();
        saveOutline.effectColor = Color.black;
        saveOutline.effectDistance = new Vector2(2f, 2f);
        
        // Texto
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(saveObj.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "SALVAR";
        text.fontSize = 14f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        
        // Tamanho do botão
        RectTransform saveRT = saveObj.GetComponent<RectTransform>();
        saveRT.sizeDelta = saveButtonSize;
        
        // Tamanho do texto
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        
        saveButton.onClick.AddListener(SaveDrawing);
    }
    
    private void CreateSlot(int slotIndex, Transform parent)
    {
        GameObject slotObj = new GameObject($"Slot{slotIndex + 1}");
        slotObj.transform.SetParent(parent, false);
        
        Button slotBtn = slotObj.AddComponent<Button>();
        Image slotImg = slotObj.AddComponent<Image>();
        slotImg.color = new Color(0.3f, 0.3f, 0.3f); // Cinza (vazio)
        
        // Outline para o slot
        Outline slotOutline = slotObj.AddComponent<Outline>();
        slotOutline.effectColor = Color.black;
        slotOutline.effectDistance = new Vector2(1f, 1f);
        
        // Texto com número
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(slotObj.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = (slotIndex + 1).ToString();
        text.fontSize = 24f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        
        // Tamanho do slot
        RectTransform slotRT = slotObj.GetComponent<RectTransform>();
        slotRT.sizeDelta = buttonSize;
        
        // Tamanho do texto
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        
        slotButtons[slotIndex] = slotBtn;
        
        // Adiciona listener
        int capturedIndex = slotIndex;
        slotBtn.onClick.AddListener(() => LoadDrawing(capturedIndex));
    }
    
    public void SaveDrawing()
    {
        if (saveSystem == null) return;
        
        saveSystem.SaveCurrentDrawing($"Desenho {saveSystem.GetSavedDrawingsCount() + 1}");
        UpdateSlotsVisual();
    }
    
    private void LoadDrawing(int slotIndex)
    {
        if (saveSystem == null || slotIndex >= saveSystem.GetSavedDrawingsCount()) return;
        
        saveSystem.LoadDrawing(slotIndex);
    }
    
    private void UpdateSlotsVisual()
    {
        if (saveSystem == null) return;
        
        int savedCount = saveSystem.GetSavedDrawingsCount();
        
        for (int i = 0; i < 4; i++)
        {
            if (slotButtons[i] != null)
            {
                Image img = slotButtons[i].GetComponent<Image>();
                if (i < savedCount)
                {
                    img.color = new Color(0.2f, 0.6f, 0.8f); // Azul (tem desenho)
                }
                else
                {
                    img.color = new Color(0.3f, 0.3f, 0.3f); // Cinza (vazio)
                }
            }
        }
    }
}
