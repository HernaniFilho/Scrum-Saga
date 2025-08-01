using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class SavedShape
{
    public ShapeType shapeType;
    public Vector2 position;
    public Vector2 size;
    public float rotation;
    public Color color;
    public float lineThickness;
    
    public SavedShape(ShapeData shapeData, RectTransform rectTransform)
    {
        shapeType = shapeData.shapeType;
        position = rectTransform.anchoredPosition;
        size = rectTransform.sizeDelta;
        rotation = rectTransform.localRotation.eulerAngles.z;
        color = shapeData.config.shapeColor;
        lineThickness = shapeData.config.lineThickness;
    }
}

[System.Serializable]
public class SavedDrawing
{
    public string name;
    public string timestamp;
    public byte[] textureData;
    public int width;
    public int height;
    public SavedShape[] shapes;
    
    public SavedDrawing(string drawingName, Texture2D texture, SavedShape[] savedShapes)
    {
        name = drawingName;
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        textureData = texture.EncodeToPNG();
        width = texture.width;
        height = texture.height;
        shapes = savedShapes;
    }
    
    public Texture2D ToTexture()
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.LoadImage(textureData);
        texture.filterMode = FilterMode.Point;
        return texture;
    }
}

public class DrawingSaveSystem : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private int maxSavedDrawings = 4;
    
    private List<SavedDrawing> savedDrawings = new List<SavedDrawing>();
    private ShapeDrawer shapeDrawer;
    
    public List<SavedDrawing> SavedDrawings => savedDrawings;
    public int MaxSavedDrawings => maxSavedDrawings;
    
    void Start()
    {
        shapeDrawer = GetComponent<ShapeDrawer>();
        if (shapeDrawer == null)
        {
            Debug.LogError("DrawingSaveSystem precisa estar no mesmo GameObject que ShapeDrawer!");
        }
    }
    
    public void SaveCurrentDrawing(string drawingName = "")
    {
        if (shapeDrawer == null)
        {
            Debug.LogError("ShapeDrawer não encontrado!");
            return;
        }
        
        // Força a renderização de todas as formas na textura antes de salvar
        Debug.Log("Iniciando processo de salvamento...");
        shapeDrawer.RenderAllShapesToTexture();
        Debug.Log("Renderização concluída, coletando dados das formas...");
        
        // Coleta dados de todas as formas
        SavedShape[] shapesData = shapeDrawer.GetAllShapesData();
        
        Texture2D currentTexture = shapeDrawer.GetDrawingTexture();
        if (currentTexture == null)
        {
            Debug.LogError("Textura de desenho não encontrada!");
            return;
        }
        
        // Gera nome automático se não fornecido
        if (string.IsNullOrEmpty(drawingName))
        {
            drawingName = $"Desenho {savedDrawings.Count + 1}";
        }
        
        SavedDrawing newDrawing = new SavedDrawing(drawingName, currentTexture, shapesData);
        savedDrawings.Add(newDrawing);
        
        // Remove o desenho mais antigo se exceder o limite
        if (savedDrawings.Count > maxSavedDrawings)
        {
            savedDrawings.RemoveAt(0);
        }
        
        Debug.Log($"Desenho '{drawingName}' salvo! Total: {savedDrawings.Count}");
    }
    
    /// <summary>
    /// Carrega um desenho salvo no canvas
    /// </summary>
    public void LoadDrawing(int index)
    {
        if (index < 0 || index >= savedDrawings.Count)
        {
            Debug.LogError($"Índice inválido: {index}");
            return;
        }
        
        if (shapeDrawer == null) return;
        
        SavedDrawing drawing = savedDrawings[index];
        Texture2D loadedTexture = drawing.ToTexture();
        
        // Limpa o canvas atual (remove formas visuais)
        shapeDrawer.ClearAll();
        
        // Copia os pixels da textura carregada para a textura atual
        Texture2D currentTexture = shapeDrawer.GetDrawingTexture();
        Color[] loadedPixels = loadedTexture.GetPixels();
        currentTexture.SetPixels(loadedPixels);
        currentTexture.Apply();
        
        // Limpa a textura temporária
        DestroyImmediate(loadedTexture);
        
        // Restaura as formas visuais
        if (drawing.shapes != null && drawing.shapes.Length > 0)
        {
            shapeDrawer.RestoreShapesFromData(drawing.shapes);
        }
        
        Debug.Log($"Desenho '{drawing.name}' carregado com {drawing.shapes?.Length ?? 0} formas!");
    }
    
    /// <summary>
    /// Remove um desenho salvo
    /// </summary>
    public void DeleteDrawing(int index)
    {
        if (index < 0 || index >= savedDrawings.Count)
        {
            Debug.LogError($"Índice inválido: {index}");
            return;
        }
        
        string drawingName = savedDrawings[index].name;
        savedDrawings.RemoveAt(index);
        Debug.Log($"Desenho '{drawingName}' removido!");
    }
    
    /// <summary>
    /// Limpa todos os desenhos salvos
    /// </summary>
    public void ClearAllSavedDrawings()
    {
        savedDrawings.Clear();
        Debug.Log("Todos os desenhos salvos foram removidos!");
    }
    
    /// <summary>
    /// Obtém informações sobre um desenho salvo
    /// </summary>
    public string GetDrawingInfo(int index)
    {
        if (index < 0 || index >= savedDrawings.Count)
        {
            return "Índice inválido";
        }
        
        SavedDrawing drawing = savedDrawings[index];
        return $"{drawing.name} - {drawing.timestamp}";
    }
    
    /// <summary>
    /// Verifica se há desenhos salvos
    /// </summary>
    public bool HasSavedDrawings()
    {
        return savedDrawings.Count > 0;
    }
    
    /// <summary>
    /// Obtém o número de desenhos salvos
    /// </summary>
    public int GetSavedDrawingsCount()
    {
        return savedDrawings.Count;
    }
    
    // Métodos públicos para UI
    public void SaveCurrentDrawingWithAutoName() => SaveCurrentDrawing();
    public void SaveCurrentDrawingWithName(string name) => SaveCurrentDrawing(name);
}
