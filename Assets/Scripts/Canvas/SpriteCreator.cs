using UnityEngine;

public static class SpriteCreator
{
    public static Sprite CreateLineSprite(float thickness, Color color)
    {
        int width = 64;
        int height = Mathf.Max(12, Mathf.CeilToInt(thickness * 3f));
        
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        
        float solidHeight = height;
        float fadeHeight = (height - solidHeight) * 0.5f;
        
        for (int y = 0; y < height; y++)
        {
            float distToSolid;
            if (y < fadeHeight) {
                distToSolid = 1f - (y / fadeHeight);
            } else if (y > height - fadeHeight - 1) {
                distToSolid = 1f - ((height - 1 - y) / fadeHeight);
            } else {
                distToSolid = 0f;
            }
            
            float alpha = color.a * (1f - Mathf.Pow(distToSolid, 2));
            Color pixelColor = new Color(color.r, color.g, color.b, alpha);
            
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = pixelColor;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    
    public static Sprite CreateCircleOutlineSprite(float circleSize, Color shapeColor, float lineThickness)
    {
        int textureSize = Mathf.Max(64, Mathf.RoundToInt(circleSize + (lineThickness * 1.5f) * 4));
        textureSize = Mathf.NextPowerOfTwo(textureSize);
        
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[textureSize * textureSize];
        Vector2 center = new Vector2(textureSize * 0.5f, textureSize * 0.5f);
        
        float pixelsPerUnit = textureSize / circleSize;
        float outerRadius = (circleSize * 0.5f) * pixelsPerUnit;
        float innerRadius = outerRadius - ((lineThickness * 1.5f) * pixelsPerUnit);
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 pos = new Vector2(x + 0.5f, y + 0.5f);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= outerRadius && distance >= innerRadius)
                {
                    float alpha = 1f;
                    float edgeSoftness = 1f * pixelsPerUnit;
                    if (distance > outerRadius - edgeSoftness)
                        alpha = Mathf.SmoothStep(0f, 1f, (outerRadius - distance) / edgeSoftness);
                    else if (distance < innerRadius + edgeSoftness)
                        alpha = Mathf.SmoothStep(0f, 1f, (distance - innerRadius) / edgeSoftness);
                    
                    pixels[y * textureSize + x] = new Color(shapeColor.r, shapeColor.g, shapeColor.b, alpha);
                }
                else
                {
                    pixels[y * textureSize + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
    
    public static Sprite CreateEllipseOutlineSprite(Vector2 ellipseSize, Color shapeColor, float lineThickness)
    {
        float margin = lineThickness * 1.4f;
        int textureWidth  = Mathf.Max(64, Mathf.RoundToInt(ellipseSize.x + margin));
        int textureHeight = Mathf.Max(64, Mathf.RoundToInt(ellipseSize.y + margin));
        
        textureWidth  = Mathf.NextPowerOfTwo(textureWidth);
        textureHeight = Mathf.NextPowerOfTwo(textureHeight);
        
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[textureWidth * textureHeight];
        Vector2 center = new Vector2(textureWidth * 0.5f, textureHeight * 0.5f);
        
        float pixelsPerUnitX = textureWidth  / (ellipseSize.x + margin);
        float pixelsPerUnitY = textureHeight / (ellipseSize.y + margin);
        
        float outerRadiusX = (ellipseSize.x * 0.5f) * pixelsPerUnitX;
        float outerRadiusY = (ellipseSize.y * 0.5f) * pixelsPerUnitY;
        
        float innerRadiusX = outerRadiusX - (margin * pixelsPerUnitX);
        float innerRadiusY = outerRadiusY - (margin * pixelsPerUnitY);
        
        const float edgeSoftnessPixels = 1.0f;
        
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                Vector2 pos = new Vector2(x + 0.5f, y + 0.5f);
                Vector2 offset = pos - center;
                
                float outerValue = Mathf.Pow(offset.x / outerRadiusX,  2) +
                                   Mathf.Pow(offset.y / outerRadiusY,  2);
                float innerValue = Mathf.Pow(offset.x / innerRadiusX, 2) +
                                   Mathf.Pow(offset.y / innerRadiusY, 2);
                
                float alpha = 0;
                if (outerValue <= 1.0f && innerValue >= 1.0f)
                {
                    alpha = 1f;
                    if (outerValue > 1.0f - edgeSoftnessPixels / outerRadiusX)
                        alpha = Mathf.Clamp01((1.0f - outerValue) * outerRadiusX / edgeSoftnessPixels);
                    else if (innerValue < 1.0f + edgeSoftnessPixels / innerRadiusX)
                        alpha = Mathf.Clamp01((innerValue - 1.0f) * innerRadiusX / edgeSoftnessPixels);
                }
                
                pixels[y * textureWidth + x] = new Color(shapeColor.r, shapeColor.g, shapeColor.b, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        float pixelsPerUnit = Mathf.Min(pixelsPerUnitX, pixelsPerUnitY);
        return Sprite.Create(texture,
                             new Rect(0, 0, textureWidth, textureHeight),
                             new Vector2(0.5f, 0.5f),
                             pixelsPerUnit);
    }
}
