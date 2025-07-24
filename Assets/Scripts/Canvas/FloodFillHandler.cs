using UnityEngine;
using System.Collections.Generic;

public class FloodFillHandler
{
    private Texture2D drawingTexture;
    private RectTransform drawingArea;
    
    public void Initialize(Texture2D texture, RectTransform area)
    {
        drawingTexture = texture;
        drawingArea = area;
    }
    
    public void FloodFill(int startX, int startY, Color fillColor)
    {
        if (startX < 0 || startX >= drawingTexture.width || startY < 0 || startY >= drawingTexture.height)
            return;
            
        Color targetColor = drawingTexture.GetPixel(startX, startY);
        
        if (IsBlackPixel(targetColor)) return;
        
        if (ColorsAreEqual(fillColor, targetColor)) return;
        
        int width = drawingTexture.width;
        int height = drawingTexture.height;
        
        bool[,] visited = new bool[width, height];
        
        Queue<Vector2Int> pixels = new Queue<Vector2Int>();
        pixels.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;
        
        while (pixels.Count > 0)
        {
            Vector2Int point = pixels.Dequeue();
            int x = point.x;
            int y = point.y;
            
            drawingTexture.SetPixel(x, y, fillColor);
            
            CheckAndAddNeighbor(x + 1, y, width, height, targetColor, visited, pixels);
            CheckAndAddNeighbor(x - 1, y, width, height, targetColor, visited, pixels);
            CheckAndAddNeighbor(x, y + 1, width, height, targetColor, visited, pixels);
            CheckAndAddNeighbor(x, y - 1, width, height, targetColor, visited, pixels);
        }
        
        drawingTexture.Apply();
    }
    
    private void CheckAndAddNeighbor(int x, int y, int width, int height, Color targetColor, bool[,] visited, Queue<Vector2Int> pixels)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        if (visited[x, y]) return;
        
        Color pixelColor = drawingTexture.GetPixel(x, y);
        if (!ColorsAreEqual(pixelColor, targetColor)) return;
        
        visited[x, y] = true;
        pixels.Enqueue(new Vector2Int(x, y));
    }
    
    private bool ColorsAreEqual(Color a, Color b)
    {
        const float tolerance = 0.005f;
        return Mathf.Abs(a.r - b.r) < tolerance &&
            Mathf.Abs(a.g - b.g) < tolerance &&
            Mathf.Abs(a.b - b.b) < tolerance &&
            Mathf.Abs(a.a - b.a) < tolerance;
    }
    
    private bool IsBlackPixel(Color color)
    {
        const float blackThreshold = 0.1f;
        return color.r < blackThreshold && color.g < blackThreshold && color.b < blackThreshold;
    }
}
