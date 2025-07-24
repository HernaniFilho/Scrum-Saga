using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ButtonStateManager : MonoBehaviour
{
    [Header("Button References")]
    public Button rectangleButton;
    public Button circleButton;
    public Button ellipseButton;
    public Button lineButton;
    public Button bucketButton;
    
    [Header("Visual States")]
    public Color selectedColor = new Color(0.3f, 0.7f, 1f, 1f);
    public Color normalColor = Color.white;
    public float selectedScale = 1.1f;
    public float normalScale = 1f;
    
    private Dictionary<ShapeType, Button> buttonMap;
    private Button currentSelectedButton;
    private ShapeDrawer shapeDrawer;
    
    void Start()
    {
        shapeDrawer = FindObjectOfType<ShapeDrawer>();
        InitializeButtonMap();
        SetSelectedButton(rectangleButton);
        SetupButtonListeners();
    }
    
    private void InitializeButtonMap()
    {
        buttonMap = new Dictionary<ShapeType, Button>
        {
            { ShapeType.Rectangle, rectangleButton },
            { ShapeType.Circle, circleButton },
            { ShapeType.Ellipse, ellipseButton },
            { ShapeType.Line, lineButton },
            { ShapeType.Bucket, bucketButton }
        };
    }
    
    private void SetupButtonListeners()
    {
        if (rectangleButton != null)
            rectangleButton.onClick.AddListener(() => OnShapeButtonClicked(ShapeType.Rectangle));
        if (circleButton != null)
            circleButton.onClick.AddListener(() => OnShapeButtonClicked(ShapeType.Circle));
        if (ellipseButton != null)
            ellipseButton.onClick.AddListener(() => OnShapeButtonClicked(ShapeType.Ellipse));
        if (lineButton != null)
            lineButton.onClick.AddListener(() => OnShapeButtonClicked(ShapeType.Line));
        if (bucketButton != null)
            bucketButton.onClick.AddListener(() => OnShapeButtonClicked(ShapeType.Bucket));
    }
    
    private void OnShapeButtonClicked(ShapeType shapeType)
    {
        if (shapeDrawer != null)
        {
            shapeDrawer.SetShape(shapeType);
        }
        
        if (buttonMap.ContainsKey(shapeType))
        {
            SetSelectedButton(buttonMap[shapeType]);
        }
    }
    
    public void SetSelectedButton(Button selectedButton)
    {
        if (currentSelectedButton != null)
        {
            SetButtonVisualState(currentSelectedButton, false);
        }
        
        currentSelectedButton = selectedButton;
        if (currentSelectedButton != null)
        {
            SetButtonVisualState(currentSelectedButton, true);
        }
    }
    
    private void SetButtonVisualState(Button button, bool isSelected)
    {
        if (button == null) return;
        
        ColorBlock colors = button.colors;
        colors.normalColor = isSelected ? selectedColor : normalColor;
        colors.selectedColor = isSelected ? selectedColor : normalColor;
        colors.highlightedColor = isSelected ? selectedColor * 1.2f : normalColor * 1.2f;
        button.colors = colors;
        
        Transform buttonTransform = button.transform;
        buttonTransform.localScale = Vector3.one * (isSelected ? selectedScale : normalScale);
    }
    
    public void UpdateSelection(ShapeType currentShape)
    {
        if (buttonMap.ContainsKey(currentShape))
        {
            SetSelectedButton(buttonMap[currentShape]);
        }
    }
    
    void Update()
    {
        if (shapeDrawer != null)
        {
            UpdateSelection(shapeDrawer.currentShape);
        }
    }
}
