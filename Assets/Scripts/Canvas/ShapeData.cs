using UnityEngine;

[System.Serializable]
public class ShapeData : MonoBehaviour
{
    public ShapeType shapeType;
    public DrawingConfig config;
    
    public void Initialize(ShapeType type, DrawingConfig drawingConfig)
    {
        shapeType = type;
        config = new DrawingConfig
        {
            shapeColor = drawingConfig.shapeColor,
            lineThickness = drawingConfig.lineThickness
        };
    }
}
