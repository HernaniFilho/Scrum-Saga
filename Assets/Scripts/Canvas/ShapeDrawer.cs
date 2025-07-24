using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShapeDrawer : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler {
    [Header("Drawing Settings")]
    public ShapeType currentShape = ShapeType.Rectangle;
    public RectTransform drawingArea;
    public RawImage drawingBoard;
    
    [Header("Shape Configuration")]
    public DrawingConfig drawingConfig = new DrawingConfig();
    
    public Color shapeColor 
    { 
        get => drawingConfig.shapeColor; 
        set => drawingConfig.shapeColor = value; 
    }
    
    public float lineThickness 
    { 
        get => drawingConfig.lineThickness; 
        set => drawingConfig.lineThickness = value; 
    }

    private GameObject finalShapeContainer;
    private Canvas canvas;
    private Camera canvasCamera;
    
    private TextureDrawingHandler textureHandler;
    private FloodFillHandler floodFillHandler;
    private ShapePreviewHandler previewHandler;
    
    private ColorPickerPopup colorPickerPopup;
    private bool bucketModeActive = false;
    private Color selectedBucketColor = Color.red;

    void Start()
    {
        InitializeCanvas();
        InitializeShapeContainer();
        InitializeHandlers();
        InitializeColorPicker();
    }
    
    private void InitializeCanvas()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }
    
    private void InitializeShapeContainer()
    {
        finalShapeContainer = new GameObject("ShapesContainer");
        finalShapeContainer.transform.SetParent(drawingArea, false);
        
        RectTransform containerRT = finalShapeContainer.AddComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;
        containerRT.pivot = new Vector2(0.5f, 0.5f);
        containerRT.anchoredPosition = Vector2.zero;
    }
    
    private void InitializeHandlers()
    {
        textureHandler = new TextureDrawingHandler();
        textureHandler.Initialize(drawingArea);
        
        if (drawingBoard != null)
        {
            drawingBoard.texture = textureHandler.DrawingTexture;
        }
        
        floodFillHandler = new FloodFillHandler();
        floodFillHandler.Initialize(textureHandler.DrawingTexture, drawingArea);
        
        previewHandler = new ShapePreviewHandler();
        previewHandler.Initialize(drawingArea);
    }
    
    private void InitializeColorPicker()
    {
        try
        {
            GameObject popupGO = new GameObject("ColorPickerPopup");
            popupGO.transform.SetParent(transform);
            colorPickerPopup = popupGO.AddComponent<ColorPickerPopup>();
            
            colorPickerPopup.OnColorSelected += OnBucketColorSelected;
            colorPickerPopup.OnPopupClosed += OnBucketColorPickerClosed;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing color picker: {e.Message}");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (drawingArea == null || canvas == null) 
        {
            Debug.LogError("DrawingArea or Canvas is null");
            return;
        }
        
        Vector2 localPoint;
        bool validPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawingArea, eventData.position, canvasCamera, out localPoint);
        
        if (!validPoint) 
        {
            Debug.LogWarning("Invalid point in drawing area");
            return;
        }

        if (bucketModeActive)
        {
            PerformFloodFill(localPoint, selectedBucketColor);
            return;
        }
        
        if (currentShape != ShapeType.Bucket)
        {
            previewHandler.CreatePreview(localPoint, currentShape, drawingConfig);
        }
    }
    
    private void OnBucketColorSelected(Color selectedColor)
    {
        selectedBucketColor = selectedColor;
    }
    
    private void OnBucketColorPickerClosed()
    {
        bucketModeActive = false;
        
        currentShape = ShapeType.Rectangle;
    }
    
    private void PerformFloodFill(Vector2 localPoint, Color fillColor)
    {
        Vector2 drawingAreaSize = drawingArea.rect.size;
        float x = (localPoint.x + drawingAreaSize.x / 2) * (textureHandler.DrawingTexture.width / drawingAreaSize.x);
        float y = (localPoint.y + drawingAreaSize.y / 2) * (textureHandler.DrawingTexture.height / drawingAreaSize.y);
        int texX = Mathf.Clamp((int)x, 0, textureHandler.DrawingTexture.width - 1);
        int texY = Mathf.Clamp((int)y, 0, textureHandler.DrawingTexture.height - 1);

        floodFillHandler.FloodFill(texX, texY, fillColor);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentShape == ShapeType.Bucket || bucketModeActive) return;
        if (previewHandler.Preview == null || drawingArea == null) return;

        Vector2 currentPos;
        bool validPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawingArea, eventData.position, canvasCamera, out currentPos);
        
        if (!validPoint) return;

        previewHandler.UpdatePreview(currentPos, currentShape, drawingConfig);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (currentShape == ShapeType.Bucket || bucketModeActive) return;
        if (previewHandler.Preview == null) return;

        if (previewHandler.IsPreviewTooSmall(currentShape))
        {
            previewHandler.DestroyPreview();
            return;
        }

        CreatePermanentShape();
        previewHandler.DestroyPreview();
    }

    private void CreatePermanentShape()
    {
        GameObject shape = ShapeFactory.CreatePermanentShape(
            previewHandler.Preview, 
            currentShape, 
            drawingConfig, 
            finalShapeContainer.transform
        );
        
        RectTransform rt = shape.GetComponent<RectTransform>();
        switch (currentShape)
        {
            case ShapeType.Rectangle:
                textureHandler.DrawRectangleOnTexture(rt, drawingConfig);
                break;
            case ShapeType.Circle:
                textureHandler.DrawCircleOnTexture(rt, drawingConfig);
                break;
            case ShapeType.Ellipse:
                textureHandler.DrawEllipseOnTexture(rt, drawingConfig);
                break;
            case ShapeType.Line:
                textureHandler.DrawLineOnTexture(rt, drawingConfig);
                break;
        }
    }
    
    public void ClearAll()
    {
        if (finalShapeContainer != null)
        {
            foreach (Transform child in finalShapeContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        textureHandler.ClearTexture(Color.white);
        
        bucketModeActive = false;
        if (colorPickerPopup != null && colorPickerPopup.IsVisible)
        {
            colorPickerPopup.ClosePopup();
        }
    }

    public void SetShape(ShapeType shape) 
    {
        if (currentShape == ShapeType.Bucket && shape != ShapeType.Bucket)
        {
            bucketModeActive = false;
            if (colorPickerPopup != null && colorPickerPopup.IsVisible)
            {
                colorPickerPopup.ClosePopup();
            }
        }
        
        currentShape = shape;
        
        if (shape == ShapeType.Bucket)
        {
            ActivateBucketMode();
        }
    }
    
    private void ActivateBucketMode()
    {
        bucketModeActive = true;
        
        if (colorPickerPopup != null)
        {
            selectedBucketColor = colorPickerPopup.GetCurrentSelectedColor();
        }
        
        if (colorPickerPopup != null)
        {
            colorPickerPopup.ShowPopup();
        }
        
    }
    
    public void SetShapeToRectangle() => SetShape(ShapeType.Rectangle);
    public void SetShapeToCircle() => SetShape(ShapeType.Circle);
    public void SetShapeToEllipse() => SetShape(ShapeType.Ellipse);
    public void SetShapeToLine() => SetShape(ShapeType.Line);
    public void SetShapeToBucket() => SetShape(ShapeType.Bucket);
    
    public void SetColor(Color color) => drawingConfig.shapeColor = color;
    public void SetLineThickness(float thickness) => drawingConfig.lineThickness = thickness;
    
    public Texture2D GetDrawingTexture() => textureHandler.DrawingTexture;
    public DrawingConfig GetDrawingConfig() => drawingConfig;
    
    public bool IsBucketModeActive() => bucketModeActive;
    public Color GetSelectedBucketColor() => selectedBucketColor;
    
    private void Update()
    {
        if (bucketModeActive && Input.GetKeyDown(KeyCode.Escape))
        {
            SetShape(ShapeType.Rectangle);
        }
    }
}
