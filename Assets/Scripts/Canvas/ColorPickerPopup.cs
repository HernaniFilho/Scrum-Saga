using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ColorPickerPopup : MonoBehaviour
{
    public static ColorPickerPopup Instance { get; private set; }
    
    [Header("Popup Settings")]
    private GameObject popupPanel;
    private Canvas popupCanvas;
    private bool isVisible = false;

    public float padding = 12f;
    public float spacing = 12f;
    public Vector2 buttonSize = new Vector2(50f, 50f);
    public Color buttonOutlineColor = Color.black;
    public Color selectedButtonOutlineColor = Color.black;
    public Vector2 buttonOutlineSize = new Vector2(2f, 2f);
    public Vector2 hoverButtonOutlineSize = new Vector2(3f, 3f);
    public Vector2 selectedButtonOutlineSize = new Vector2(4f, 4f);
    
    private readonly Color[] availableColors = new Color[]
    {
        new Color(1f, 0f, 0f, 1f),      // Vermelho
        new Color(1f, 0.5f, 0f, 1f),    // Laranja
        new Color(1f, 1f, 0f, 1f),      // Amarelo
        new Color(0f, 1f, 0f, 1f),      // Verde
        new Color(0.3f, 0.7f, 1f, 1f),  // Azul claro
        new Color(0.561f, 0f, 1f, 1f),  // Violeta
        Color.white,                    // Branco
    };
    
    public event Action<Color> OnColorSelected;
    public event Action OnPopupClosed;
    
    private GameObject selectedColorIndicator;
    private Color currentSelectedColor = Color.red;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            CreatePopupUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void CreatePopupUI()
    {
        try
        {
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                return;
            }
            
            GameObject canvasGO = new GameObject("ColorPickerCanvas");
            canvasGO.transform.SetParent(mainCanvas.transform, false);
            
            popupCanvas = canvasGO.AddComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 1000;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            
            popupPanel = new GameObject("ColorPickerPanel");
            popupPanel.transform.SetParent(canvasGO.transform, false);
            
            Image panelImage = popupPanel.AddComponent<Image>();
            panelImage.color = new Color(0.4339623f, 0.4278213f, 0.4278213f, 1f);
            
            float containerWidth = (availableColors.Length * buttonSize.x) + ((availableColors.Length - 1) * spacing) + (padding * 2);
            float containerHeight = buttonSize.y + (padding * 2);
            
            RectTransform panelRect = popupPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(containerWidth, containerHeight);
            panelRect.anchoredPosition = new Vector2(0, 110);
            
            CreateColorButtons();
            
            canvasGO.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating popup UI: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private void CreateColorButtons()
    {        
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(popupPanel.transform, false);
        
        RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = new Vector2(padding, padding);
        containerRect.offsetMax = new Vector2(-padding, -padding);
        
        HorizontalLayoutGroup horizontalLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.spacing = spacing;
        horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
        horizontalLayout.childControlWidth = false;
        horizontalLayout.childControlHeight = false;
        horizontalLayout.childForceExpandWidth = false;
        horizontalLayout.childForceExpandHeight = false;
        
        for (int i = 0; i < availableColors.Length; i++)
        {
            CreateColorButton(availableColors[i], buttonContainer.transform, i == 0);
        }
    }
    
    private void CreateColorButton(Color color, Transform parent, bool isSelected = false)
    {
        GameObject buttonGO = new GameObject($"ColorButton_{color.ToString()}");
        buttonGO.transform.SetParent(parent, false);
        
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.sizeDelta = buttonSize;
        
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = color;
        
        Outline outline = buttonGO.AddComponent<Outline>();
        outline.effectColor = isSelected ? selectedButtonOutlineColor : buttonOutlineColor;
        outline.effectDistance = isSelected ? selectedButtonOutlineSize : buttonOutlineSize;
        
        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        
        ColorButtonHover hoverHandler = buttonGO.AddComponent<ColorButtonHover>();
        hoverHandler.Initialize(outline, buttonOutlineSize, hoverButtonOutlineSize, selectedButtonOutlineSize, isSelected);
        
        if (isSelected)
        {
            selectedColorIndicator = buttonGO;
            currentSelectedColor = color;
        }
        
        button.onClick.AddListener(() => {
            SelectColor(color, buttonGO);
        });
    }
    
    public void ShowPopup()
    {
        if (popupCanvas == null) 
        {
            Debug.LogError("PopupCanvas is null!");
            return;
        }
        
        popupCanvas.gameObject.SetActive(true);
        isVisible = true;
        
        Canvas.ForceUpdateCanvases();
    }
    
    public void ClosePopup()
    {
        if (popupCanvas == null) return;
        
        popupCanvas.gameObject.SetActive(false);
        isVisible = false;
        OnPopupClosed?.Invoke();
    }
    
    private void SelectColor(Color selectedColor, GameObject buttonGO)
    {
        if (selectedColorIndicator != null)
        {
            Outline prevOutline = selectedColorIndicator.GetComponent<Outline>();
            ColorButtonHover prevHover = selectedColorIndicator.GetComponent<ColorButtonHover>();
            if (prevOutline != null && prevHover != null)
            {
                prevHover.SetSelected(false);
                prevOutline.effectColor = buttonOutlineColor;
                prevOutline.effectDistance = buttonOutlineSize;
            }
        }
        
        selectedColorIndicator = buttonGO;
        currentSelectedColor = selectedColor;
        Outline newOutline = buttonGO.GetComponent<Outline>();
        ColorButtonHover newHover = buttonGO.GetComponent<ColorButtonHover>();
        if (newOutline != null && newHover != null)
        {
            newHover.SetSelected(true);
            newOutline.effectColor = selectedButtonOutlineColor;
            newOutline.effectDistance = selectedButtonOutlineSize;
        }
        
        OnColorSelected?.Invoke(selectedColor);
    }
    
    public bool IsVisible => isVisible;
    public Color GetCurrentSelectedColor() => currentSelectedColor;
    
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

public class ColorButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Outline outline;
    private Vector2 normalOutlineSize;
    private Vector2 hoverOutlineSize;
    private Vector2 selectedOutlineSize;
    private bool isSelected = false;
    
    public void Initialize(Outline outlineComponent, Vector2 normalSize, Vector2 hoverSize, Vector2 selectedSize, bool selected = false)
    {
        outline = outlineComponent;
        normalOutlineSize = normalSize;
        hoverOutlineSize = hoverSize;
        selectedOutlineSize = selectedSize;
        isSelected = selected;
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (outline != null && !isSelected)
        {
            outline.effectDistance = hoverOutlineSize;
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (outline != null && !isSelected)
        {
            outline.effectDistance = normalOutlineSize;
        }
    }
}
