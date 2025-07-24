using UnityEngine;

[System.Serializable]
public class DrawingConfig
{
    public Color shapeColor = Color.black;
    public float lineThickness = 5f;
    
    public DrawingConfig()
    {
    }
    
    public DrawingConfig(Color color, float thickness)
    {
        shapeColor = color;
        lineThickness = thickness;
    }
}