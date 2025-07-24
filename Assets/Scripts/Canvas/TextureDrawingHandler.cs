using UnityEngine;
using System.Collections.Generic;

public class TextureDrawingHandler
{
    private Texture2D drawingTexture;
    private RectTransform drawingArea;
    
    public Texture2D DrawingTexture => drawingTexture;

    public void Initialize(RectTransform area)
    {
        drawingArea = area;
        InitializeDrawingTexture();
    }
    
    private void InitializeDrawingTexture()
    {
        int width = Mathf.RoundToInt(drawingArea.rect.width);
        int height = Mathf.RoundToInt(drawingArea.rect.height);

        if (width <= 0) width = 512;
        if (height <= 0) height = 512;

        drawingTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        drawingTexture.filterMode = FilterMode.Point;
        ClearTexture(Color.white);
    }
    
    public void ClearTexture(Color color)
    {
        Color[] pixels = new Color[drawingTexture.width * drawingTexture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        drawingTexture.SetPixels(pixels);
        drawingTexture.Apply();
    }
    
    public void DrawRectangleOnTexture(RectTransform rectTransform, DrawingConfig config)
    {
        Vector2 size = rectTransform.sizeDelta;
        Vector2 position = rectTransform.anchoredPosition;
        
        Vector2 drawingAreaSize = drawingArea.rect.size;
        Vector2 textureScale = new Vector2(drawingTexture.width / drawingAreaSize.x, drawingTexture.height / drawingAreaSize.y);
        
        Vector2 texturePos = new Vector2(
            (position.x + drawingAreaSize.x / 2) * textureScale.x - 1,
            (position.y + drawingAreaSize.y / 2) * textureScale.y
        );
        
        Vector2 textureSize = new Vector2(size.x * textureScale.x - 1, size.y * textureScale.y - 2);
        
        int startX = Mathf.RoundToInt(texturePos.x - textureSize.x / 2);
        int startY = Mathf.RoundToInt(texturePos.y - textureSize.y / 2);
        int endX = Mathf.RoundToInt(texturePos.x + textureSize.x / 2);
        int endY = Mathf.RoundToInt(texturePos.y + textureSize.y / 2);
        
        int thickness = Mathf.Max(1, Mathf.RoundToInt(config.lineThickness * 0.5f));
        
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if (x >= 0 && x < drawingTexture.width && y >= 0 && y < drawingTexture.height)
                {
                    bool isOnBorder = (y <= startY + thickness || y >= endY - thickness || 
                                    x <= startX + thickness || x >= endX - thickness);
                    
                    if (isOnBorder)
                    {
                        drawingTexture.SetPixel(x, y, config.shapeColor);
                    }
                }
            }
        }
        
        drawingTexture.Apply();
    }
    
    public void DrawCircleOnTexture(RectTransform rectTransform, DrawingConfig config)
    {
        Vector2 size = rectTransform.sizeDelta;
        Vector2 position = rectTransform.anchoredPosition;
        
        Vector2 drawingAreaSize = drawingArea.rect.size;
        Vector2 textureScale = new Vector2(drawingTexture.width / drawingAreaSize.x, drawingTexture.height / drawingAreaSize.y);
        
        Vector2 textureCenter = new Vector2(
            (position.x + drawingAreaSize.x / 2) * textureScale.x,
            (position.y + drawingAreaSize.y / 2) * textureScale.y
        );
        
        float radius = (size.x * textureScale.x) / 2f;
        float thickness = Mathf.Max(1f, config.lineThickness * 0.8f);
        
        int minX = Mathf.Max(0, Mathf.FloorToInt(textureCenter.x - radius - thickness));
        int maxX = Mathf.Min(drawingTexture.width - 1, Mathf.CeilToInt(textureCenter.x + radius + thickness));
        int minY = Mathf.Max(0, Mathf.FloorToInt(textureCenter.y - radius - thickness));
        int maxY = Mathf.Min(drawingTexture.height - 1, Mathf.CeilToInt(textureCenter.y + radius + thickness));
        
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), textureCenter);
                
                if (distance <= (radius - 2) && distance >= radius - thickness)
                {
                    drawingTexture.SetPixel(x, y, config.shapeColor);
                }
            }
        }
        
        drawingTexture.Apply();
    }
    
    public void DrawEllipseOnTexture(RectTransform rectTransform, DrawingConfig config)
    {
        Vector2 size = rectTransform.sizeDelta;
        Vector2 position = rectTransform.anchoredPosition;
        
        Vector2 drawingAreaSize = drawingArea.rect.size;
        Vector2 textureScale = new Vector2(drawingTexture.width / drawingAreaSize.x, drawingTexture.height / drawingAreaSize.y);
        
        Vector2 textureCenter = new Vector2(
            (position.x + drawingAreaSize.x / 2) * textureScale.x,
            (position.y + drawingAreaSize.y / 2) * textureScale.y
        );
        
        float radiusX = (size.x * textureScale.x) / 2f - 4.25f;
        float radiusY = (size.y * textureScale.y) / 2f - 4.25f;
        float thickness = Mathf.Max(1f, config.lineThickness * 0.8f);
        
        int minX = Mathf.Max(0, Mathf.FloorToInt(textureCenter.x - radiusX - thickness));
        int maxX = Mathf.Min(drawingTexture.width - 1, Mathf.CeilToInt(textureCenter.x + radiusX + thickness));
        int minY = Mathf.Max(0, Mathf.FloorToInt(textureCenter.y - radiusY - thickness));
        int maxY = Mathf.Min(drawingTexture.height - 1, Mathf.CeilToInt(textureCenter.y + radiusY + thickness));
        
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2 offset = new Vector2(x - textureCenter.x, y - textureCenter.y);
                float ellipseValue = (offset.x * offset.x) / (radiusX * radiusX) + 
                                (offset.y * offset.y) / (radiusY * radiusY);
                
                float innerEllipseValue = (offset.x * offset.x) / ((radiusX - thickness) * (radiusX - thickness)) + 
                                        (offset.y * offset.y) / ((radiusY - thickness) * (radiusY - thickness));
                
                if (ellipseValue <= 1.0f && innerEllipseValue >= 1.0f)
                {
                    drawingTexture.SetPixel(x, y, config.shapeColor);
                }
            }
        }
        
        drawingTexture.Apply();
    }
    
    public void DrawLineOnTexture(RectTransform rectTransform, DrawingConfig config)
    {
        Vector2 size = rectTransform.sizeDelta;
        Vector2 position = rectTransform.anchoredPosition;
        float rotation = rectTransform.localRotation.eulerAngles.z * Mathf.Deg2Rad;
        
        Vector2 drawingAreaSize = drawingArea.rect.size;
        Vector2 textureScale = new Vector2(drawingTexture.width / drawingAreaSize.x, drawingTexture.height / drawingAreaSize.y);
        
        Vector2 textureStart = new Vector2(
            (position.x + drawingAreaSize.x / 2) * textureScale.x,
            (position.y + drawingAreaSize.y / 2) * textureScale.y
        );
        
        Vector2 direction = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation));
        float lineLength = size.x * textureScale.x;
        Vector2 textureEnd = textureStart + direction * lineLength;
        
        float thickness = size.y * Mathf.Min(textureScale.x, textureScale.y);
        
        DrawThickLine(textureStart, textureEnd, thickness, config.shapeColor);
        drawingTexture.Apply();
    }
    
    private void DrawThickLine(Vector2 start, Vector2 end, float thickness, Color color)
    {
        if (Vector2.Distance(start, end) < 0.1f) return;

        Vector2 direction = (end - start).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x);
        float halfThickness = thickness / 2f;

        Vector2[] vertices = new Vector2[4];
        vertices[0] = start - normal * halfThickness;
        vertices[1] = end - normal * halfThickness;
        vertices[2] = end + normal * halfThickness;
        vertices[3] = start + normal * halfThickness;

        int minX = Mathf.FloorToInt(Mathf.Min(vertices[0].x, vertices[1].x, vertices[2].x, vertices[3].x));
        int maxX = Mathf.CeilToInt(Mathf.Max(vertices[0].x, vertices[1].x, vertices[2].x, vertices[3].x));
        int minY = Mathf.FloorToInt(Mathf.Min(vertices[0].y, vertices[1].y, vertices[2].y, vertices[3].y));
        int maxY = Mathf.CeilToInt(Mathf.Max(vertices[0].y, vertices[1].y, vertices[2].y, vertices[3].y));

        minX = Mathf.Max(0, minX);
        maxX = Mathf.Min(drawingTexture.width - 1, maxX);
        minY = Mathf.Max(0, minY);
        maxY = Mathf.Min(drawingTexture.height - 1, maxY);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (IsPointInRectangle(new Vector2(x, y), vertices))
                {
                    drawingTexture.SetPixel(x, y, color);
                }
            }
        }
    }

    private bool IsPointInRectangle(Vector2 p, Vector2[] v)
    {
        float d0 = (p.x - v[0].x) * (v[1].y - v[0].y) - (p.y - v[0].y) * (v[1].x - v[0].x);
        float d1 = (p.x - v[1].x) * (v[2].y - v[1].y) - (p.y - v[1].y) * (v[2].x - v[1].x);
        float d2 = (p.x - v[2].x) * (v[3].y - v[2].y) - (p.y - v[2].y) * (v[3].x - v[2].x);
        float d3 = (p.x - v[3].x) * (v[0].y - v[3].y) - (p.y - v[3].y) * (v[0].x - v[3].x);

        bool allNegative = d0 <= 0 && d1 <= 0 && d2 <= 0 && d3 <= 0;
        bool allPositive = d0 >= 0 && d1 >= 0 && d2 >= 0 && d3 >= 0;

        return allNegative || allPositive;
    }
}
