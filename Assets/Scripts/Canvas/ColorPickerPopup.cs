using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class ColorPickerPopup : MonoBehaviour
{
    public static ColorPickerPopup Instance { get; private set; }
    
    [Header("UI Components")]
    public GameObject popupContainer;
    public Button[] colorButtons;
    public TMP_Text colorNameText;
    
    [Header("Visual Settings")]
    public Color buttonOutlineColor = Color.black;
    public Color selectedButtonOutlineColor = Color.black;
    public Vector2 buttonOutlineSize = new Vector2(2f, 2f);
    public Vector2 hoverButtonOutlineSize = new Vector2(3f, 3f);
    public Vector2 selectedButtonOutlineSize = new Vector2(4f, 4f);
    
    public event Action<Color> OnColorSelected;
    public event Action OnPopupClosed;
    
    public Button selectedColorButton;
    private Color currentSelectedColor = Color.red;
    private bool isVisible = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeColorPicker();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeColorPicker()
    {
        if (colorButtons == null || colorButtons.Length == 0)
        {
            Debug.LogError("Color buttons array is empty! Please assign buttons in the inspector.");
            return;
        }
        
        // Configura os botões usando a cor do Image de cada botão
        for (int i = 0; i < colorButtons.Length; i++)
        {
            SetupColorButton(colorButtons[i], i == 0);
        }
        
        // Define o primeiro botão como selecionado por padrão
        if (colorButtons.Length > 0)
        {
            selectedColorButton = colorButtons[0];
            Image firstButtonImage = colorButtons[0].GetComponent<Image>();
            if (firstButtonImage != null)
            {
                currentSelectedColor = firstButtonImage.color;
            }
            UpdateColorNameText(colorButtons[0].name);
        }
        
        // Inicia com o popup invisível
        if (popupContainer != null)
        {
            popupContainer.SetActive(false);
        }
    }
    
    private void SetupColorButton(Button button, bool isSelected = false)
    {
        if (button == null) return;
        
        // Pega a cor do botão do componente Image
        Image buttonImage = button.GetComponent<Image>();
        Color buttonColor = buttonImage != null ? buttonImage.color : Color.white;
        
        // Adiciona ou atualiza o Outline
        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }
        
        outline.effectColor = isSelected ? selectedButtonOutlineColor : buttonOutlineColor;
        outline.effectDistance = isSelected ? selectedButtonOutlineSize : buttonOutlineSize;
        
        // Adiciona ou atualiza o ColorButtonHover
        ColorButtonHover hoverHandler = button.GetComponent<ColorButtonHover>();
        if (hoverHandler == null)
        {
            hoverHandler = button.gameObject.AddComponent<ColorButtonHover>();
        }
        
        hoverHandler.Initialize(outline, buttonOutlineSize, hoverButtonOutlineSize, selectedButtonOutlineSize, isSelected);
        hoverHandler.SetColorPicker(this);
        
        // Remove listeners antigos e adiciona o novo
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {
            SelectColor(buttonColor, button);
        });
    }
    
    public void ShowPopup()
    {
        if (popupContainer == null) 
        {
            Debug.LogError("PopupContainer is null!");
            return;
        }
        
        popupContainer.SetActive(true);
        isVisible = true;
    }
    
    public void ClosePopup()
    {
        if (popupContainer == null) return;
        
        popupContainer.SetActive(false);
        isVisible = false;
        OnPopupClosed?.Invoke();
    }
    
    private void SelectColor(Color selectedColor, Button buttonComponent)
    {
        // Remove seleção do botão anterior
        if (selectedColorButton != null)
        {
            Outline prevOutline = selectedColorButton.GetComponent<Outline>();
            ColorButtonHover prevHover = selectedColorButton.GetComponent<ColorButtonHover>();
            if (prevOutline != null && prevHover != null)
            {
                prevHover.SetSelected(false);
                prevOutline.effectColor = buttonOutlineColor;
                prevOutline.effectDistance = buttonOutlineSize;
            }
        }
        
        // Define o novo botão selecionado
        selectedColorButton = buttonComponent;
        currentSelectedColor = selectedColor;
        
        // Aplica visual de selecionado
        Outline newOutline = buttonComponent.GetComponent<Outline>();
        ColorButtonHover newHover = buttonComponent.GetComponent<ColorButtonHover>();
        if (newOutline != null && newHover != null)
        {
            newHover.SetSelected(true);
            newOutline.effectColor = selectedButtonOutlineColor;
            newOutline.effectDistance = selectedButtonOutlineSize;
        }
        
        // Atualiza o texto da cor usando o nome do botão
        UpdateColorNameText(buttonComponent.name);
        
        OnColorSelected?.Invoke(selectedColor);
    }
    
    public void UpdateColorNameText(string colorName)
    {
        if (colorNameText != null)
        {
            colorNameText.text = colorName;
        }
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
    
    private ColorPickerPopup colorPicker;
    
    public void Initialize(Outline outlineComponent, Vector2 normalSize, Vector2 hoverSize, Vector2 selectedSize, bool selected = false)
    {
        outline = outlineComponent;
        normalOutlineSize = normalSize;
        hoverOutlineSize = hoverSize;
        selectedOutlineSize = selectedSize;
        isSelected = selected;
    }
    
    public void SetColorPicker(ColorPickerPopup picker)
    {
        colorPicker = picker;
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
        
        // Mostra o nome da cor (nome do botão) quando o mouse está em cima
        if (colorPicker != null)
        {
            colorPicker.UpdateColorNameText(gameObject.name);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (outline != null && !isSelected)
        {
            outline.effectDistance = normalOutlineSize;
        }
        
        // Volta para a cor selecionada quando o mouse sai
        if (colorPicker != null && colorPicker.selectedColorButton != null)
        {
            colorPicker.UpdateColorNameText(colorPicker.selectedColorButton.name);
        }
    }
}
