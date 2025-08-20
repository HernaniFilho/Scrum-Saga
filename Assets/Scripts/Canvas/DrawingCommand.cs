using UnityEngine;

[System.Serializable]
public class DrawingCommand
{
    public ShapeType shapeType;
    public Vector2 position;
    public Vector2 size;
    public float rotation;
    public Color color;
    public float lineThickness;
    public long timestamp; // Para ordem de execução
    public string playerName; // Nome do player que criou o comando
    
    // Para comandos de flood fill
    public bool isFloodFill = false;
    public Vector2 floodFillPosition;
    public Color floodFillColor;
    
    public DrawingCommand()
    {
    }
    
    public DrawingCommand(ShapeType type, Vector2 pos, Vector2 sz, float rot, Color col, float thickness, string playerName = "")
    {
        shapeType = type;
        position = pos;
        size = sz;
        rotation = rot;
        color = col;
        lineThickness = thickness;
        timestamp = System.DateTime.Now.Ticks;
        this.playerName = playerName;
    }
    
    // Construtor a partir de dados existentes do shape
    public DrawingCommand(ShapeData shapeData, RectTransform rectTransform, string playerName = "")
    {
        shapeType = shapeData.shapeType;
        position = rectTransform.anchoredPosition;
        size = rectTransform.sizeDelta;
        rotation = rectTransform.localRotation.eulerAngles.z;
        color = shapeData.config.shapeColor;
        lineThickness = shapeData.config.lineThickness;
        timestamp = System.DateTime.Now.Ticks;
        isFloodFill = false;
        this.playerName = playerName;
    }
    
    // Construtor para comandos de flood fill
    public DrawingCommand(Vector2 fillPosition, Color fillColor, string playerName = "")
    {
        isFloodFill = true;
        floodFillPosition = fillPosition;
        floodFillColor = fillColor;
        timestamp = System.DateTime.Now.Ticks;
        this.playerName = playerName;
        
        // Valores padrão para campos não usados
        shapeType = ShapeType.Bucket;
        position = fillPosition;
        size = Vector2.one;
        rotation = 0;
        color = fillColor;
        lineThickness = 0;
    }
}
