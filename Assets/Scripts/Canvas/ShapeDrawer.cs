using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

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
    public FloodFillHandler floodFillHandler;
    private ShapePreviewHandler previewHandler;
    
    private ColorPickerPopup colorPickerPopup;
    private bool bucketModeActive = false;
    private Color selectedBucketColor = Color.red;
    
    private UndoSystem undoSystem;

    void Start()
    {
        InitializeCanvas();
        InitializeShapeContainer();
        InitializeHandlers();
        InitializeColorPicker();
        InitializeUndoSystem();
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
            // Busca pelo ColorPickerPopup existente na cena
            colorPickerPopup = FindObjectOfType<ColorPickerPopup>();
            
            if (colorPickerPopup == null)
            {
                Debug.LogError("ColorPickerPopup not found! Please add a ColorPickerPopup component to the scene.");
                return;
            }
            
            colorPickerPopup.OnColorSelected += OnBucketColorSelected;
            colorPickerPopup.OnPopupClosed += OnBucketColorPickerClosed;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing color picker: {e.Message}");
        }
    }
    
    private void InitializeUndoSystem()
    {
        undoSystem = GetComponent<UndoSystem>();
        if (undoSystem == null)
        {
            Debug.LogWarning("UndoSystem not found on this GameObject. Add UndoSystem component to enable undo functionality.");
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
            // Salva estado antes do flood fill
            if (undoSystem != null)
            {
                undoSystem.SaveStateBeforeAction();
            }
                
            PerformFloodFill(localPoint, selectedBucketColor);
            
            // Grava o comando de flood fill
            if (CommandRecorder.Instance != null)
            {
                CommandRecorder.Instance.RecordFloodFill(localPoint, selectedBucketColor);
            }
            
            // Notifica que foi realizado flood fill
            if (undoSystem != null)
            {
                undoSystem.OnFloodFillPerformed();
            }

            // Notifica RealizacaoTarefaManager que o balde foi usado
            if (RealizacaoTarefaManager.Instance != null)
            {
                RealizacaoTarefaManager.Instance.OnPlayerFinishedDrawing(() =>
                {
                    // Sincronizar flood fill em tempo real
                    RealizacaoTarefaManager.Instance.SyncFloodFillInRealTime(localPoint, selectedBucketColor);
                });
            }
            
            // Fecha o color picker após pintar
            if (colorPickerPopup != null && colorPickerPopup.IsVisible)
            {
                colorPickerPopup.ClosePopup();
            }
            return;
        }
        
        if (currentShape != ShapeType.Bucket)
        {
            // Salva estado antes de começar a desenhar
            if (undoSystem != null)
            {
                undoSystem.SaveStateBeforeAction();
            }
                
            previewHandler.CreatePreview(localPoint, currentShape, drawingConfig);
        }
    }
    
    private void OnBucketColorSelected(Color selectedColor)
    {
        selectedBucketColor = selectedColor;
    }
    
    private void OnBucketColorPickerClosed()
    {
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

        GameObject createdShape = CreatePermanentShape();
        previewHandler.DestroyPreview();
        
        // Notifica o UndoSystem sobre a nova forma criada
        if (undoSystem != null && createdShape != null)
        {
            undoSystem.OnShapeCreated(createdShape);
        }
        
        // Grava o comando no CommandRecorder
        if (CommandRecorder.Instance != null && createdShape != null)
        {
            CommandRecorder.Instance.RecordShapeFromGameObject(createdShape);
        }

        // Notifica RealizacaoTarefaManager que o desenho foi completado
        if (RealizacaoTarefaManager.Instance != null && createdShape != null)
        {
            RealizacaoTarefaManager.Instance.OnPlayerFinishedDrawing(() =>
            {
                // Sincronizar desenho em tempo real para todos os players
                RealizacaoTarefaManager.Instance.SyncDrawingInRealTime(createdShape);
            });
        }
    }

    private GameObject CreatePermanentShape()
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
        
        return shape;
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
        
        // Limpa histórico do undo ao fazer clear all
        if (undoSystem != null)
        {
            undoSystem.ClearUndoHistory();
        }
        
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
        if (colorPickerPopup != null)
        {
            selectedBucketColor = colorPickerPopup.GetCurrentSelectedColor();
        }
        
        if (colorPickerPopup != null)
        {
            if (colorPickerPopup.IsVisible)
            {
                colorPickerPopup.ClosePopup();
            }
            else
            {
                colorPickerPopup.ShowPopup();
            }
        }

        bucketModeActive = true;
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
    
    /// <summary>
    /// Força a renderização de todas as formas na textura (para salvar)
    /// </summary>
    public void RenderAllShapesToTexture()
    {
        if (finalShapeContainer == null || textureHandler == null) return;
        
        int shapesRendered = 0;
        Debug.Log($"Iniciando renderização de formas. Container tem {finalShapeContainer.transform.childCount} filhos");
        
        // Re-renderiza todas as formas na textura
        foreach (Transform child in finalShapeContainer.transform)
        {
            GameObject shape = child.gameObject;
            RectTransform rt = shape.GetComponent<RectTransform>();
            ShapeData shapeData = shape.GetComponent<ShapeData>();
            
            if (shapeData == null) 
            {
                Debug.LogWarning($"Forma {shape.name} não tem ShapeData! Tentando identificar pelo nome...");
                
                // Fallback: tenta identificar pelo nome para formas antigas
                if (shape.name.Contains("Rectangle"))
                {
                    textureHandler.DrawRectangleOnTexture(rt, drawingConfig);
                    shapesRendered++;
                }
                else if (shape.name.Contains("Circle"))
                {
                    textureHandler.DrawCircleOnTexture(rt, drawingConfig);
                    shapesRendered++;
                }
                else if (shape.name.Contains("Ellipse"))
                {
                    textureHandler.DrawEllipseOnTexture(rt, drawingConfig);
                    shapesRendered++;
                }
                else if (shape.name.Contains("Line"))
                {
                    textureHandler.DrawLineOnTexture(rt, drawingConfig);
                    shapesRendered++;
                }
                continue;
            }
            
            Debug.Log($"Renderizando forma {shapeData.shapeType} na textura com cor {shapeData.config.shapeColor}");
            
            // Usa as informações corretas da forma
            switch (shapeData.shapeType)
            {
                case ShapeType.Rectangle:
                    textureHandler.DrawRectangleOnTexture(rt, shapeData.config);
                    break;
                case ShapeType.Circle:
                    textureHandler.DrawCircleOnTexture(rt, shapeData.config);
                    break;
                case ShapeType.Ellipse:
                    textureHandler.DrawEllipseOnTexture(rt, shapeData.config);
                    break;
                case ShapeType.Line:
                    textureHandler.DrawLineOnTexture(rt, shapeData.config);
                    break;
            }
            shapesRendered++;
        }
        
        Debug.Log($"Renderizou {shapesRendered} formas na textura");
    }
    
    // Métodos GetAllShapesData() e RestoreShapesFromData() removidos
    // Agora usado CommandRecorder + CommandReplaySystem
    
    private void Update()
    {
        if (bucketModeActive && Input.GetKeyDown(KeyCode.Escape))
        {
            SetShape(ShapeType.Rectangle);
        }
    }
}
