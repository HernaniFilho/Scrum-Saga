using UnityEngine;
using UnityEngine.UI;

public class ShapePreviewHandler
{
    private RectTransform preview;
    private Vector2 startPos;
    private RectTransform drawingArea;
    
    public RectTransform Preview => preview;
    public Vector2 StartPosition => startPos;
    
    public void Initialize(RectTransform area)
    {
        drawingArea = area;
    }
    
    public void CreatePreview(Vector2 startPosition, ShapeType shapeType, DrawingConfig config)
    {
        startPos = startPosition;
        
        GameObject previewGo = new GameObject("Preview");
        preview = previewGo.AddComponent<RectTransform>();
        previewGo.transform.SetParent(drawingArea, false);
        
        preview.anchorMin = new Vector2(0.5f, 0.5f);
        preview.anchorMax = new Vector2(0.5f, 0.5f);
        preview.pivot = shapeType == ShapeType.Line ? new Vector2(0, 0.5f) : new Vector2(0.5f, 0.5f);
        preview.anchoredPosition = startPos;
        preview.sizeDelta = Vector2.zero;

        switch (shapeType)
        {
            case ShapeType.Rectangle:
                CreateRectangleOutline(previewGo, config);
                break;
            case ShapeType.Circle:
                CreateCircleOutline(previewGo, config);
                break;
            case ShapeType.Ellipse:
                CreateEllipseOutline(previewGo, config);
                break;
            case ShapeType.Line:
                Image imageComponent = previewGo.AddComponent<Image>();
                imageComponent.raycastTarget = false;
                imageComponent.sprite = SpriteCreator.CreateLineSprite(config.lineThickness, config.shapeColor);
                imageComponent.type = Image.Type.Sliced;
                break;
        }
    }
    
    public void UpdatePreview(Vector2 currentPos, ShapeType shapeType, DrawingConfig config)
    {
        if (preview == null) return;

        switch (shapeType)
        {
            case ShapeType.Rectangle:
            case ShapeType.Circle:
            case ShapeType.Ellipse:
                UpdateRectangularShape(currentPos, shapeType);
                break;
            case ShapeType.Line:
                UpdateLineShape(currentPos, config);
                break;
        }
    }
    
    private void UpdateRectangularShape(Vector2 currentPos, ShapeType shapeType)
    {
        Vector2 size = new Vector2(
            Mathf.Abs(currentPos.x - startPos.x),
            Mathf.Abs(currentPos.y - startPos.y)
        );
        
        if (shapeType == ShapeType.Circle)
        {
            float diameter = Mathf.Max(size.x, size.y);
            size = new Vector2(diameter, diameter);
        }
        
        Vector2 center = (startPos + currentPos) / 2f;
        
        preview.anchoredPosition = center;
        preview.sizeDelta = size;
        preview.localRotation = Quaternion.identity;
        
        if (shapeType == ShapeType.Circle)
        {
            UpdateCircleShape();
        }
        else if (shapeType == ShapeType.Ellipse)
        {
            UpdateEllipseShape();
        }
    }
    
    private void UpdateCircleShape()
    {
        if (preview == null) return;
        
        Image imageComponent = preview.GetComponent<Image>();
        if (imageComponent != null)
        {
            float circleSize = Mathf.Max(preview.sizeDelta.x, preview.sizeDelta.y);
            if (circleSize > 0)
            {
                if (imageComponent.sprite != null)
                {
                    Object.DestroyImmediate(imageComponent.sprite.texture);
                    Object.DestroyImmediate(imageComponent.sprite);
                }
                
                imageComponent.sprite = SpriteCreator.CreateCircleOutlineSprite(circleSize, preview.GetComponent<Image>().color, 5f);
            }
        }
    }
    
    private void UpdateEllipseShape()
    {
        if (preview == null) return;
        
        Image imageComponent = preview.GetComponent<Image>();
        if (imageComponent != null)
        {
            Vector2 ellipseSize = preview.sizeDelta;
            if (ellipseSize.x > 0 && ellipseSize.y > 0)
            {
                if (imageComponent.sprite != null)
                {
                    Object.DestroyImmediate(imageComponent.sprite.texture);
                    Object.DestroyImmediate(imageComponent.sprite);
                }
                
                imageComponent.sprite = SpriteCreator.CreateEllipseOutlineSprite(ellipseSize, imageComponent.color, 5f);
            }
        }
    }
    
    private void UpdateLineShape(Vector2 currentPos, DrawingConfig config)
    {
        Vector2 direction = currentPos - startPos;
        float distance = direction.magnitude;
        
        if (distance > 0.1f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            preview.anchoredPosition = startPos;
            preview.sizeDelta = new Vector2(distance, config.lineThickness); 
            preview.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    public void DestroyPreview()
    {
        if (preview != null)
        {
            Object.Destroy(preview.gameObject);
            preview = null;
        }
    }
    
    public bool IsPreviewTooSmall(ShapeType shapeType)
    {
        if (preview == null) return true;
        
        if (shapeType == ShapeType.Line)
        {
            return preview.sizeDelta.x < 10f;
        }
        else
        {
            return preview.sizeDelta.x < 10f && preview.sizeDelta.y < 10f;
        }
    }
    
    private void CreateRectangleOutline(GameObject parent, DrawingConfig config)
    {
        ShapeOutlineCreator.CreateRectangleOutline(parent, config);
    }
    
    private void CreateCircleOutline(GameObject parent, DrawingConfig config)
    {
        ShapeOutlineCreator.CreateCircleOutline(parent, config);
    }
    
    private void CreateEllipseOutline(GameObject parent, DrawingConfig config)
    {
        ShapeOutlineCreator.CreateEllipseOutline(parent, config);
    }
}
