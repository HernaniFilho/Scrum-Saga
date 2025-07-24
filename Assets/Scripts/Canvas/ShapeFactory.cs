using UnityEngine;
using UnityEngine.UI;

public static class ShapeFactory
{
    public static GameObject CreatePermanentShape(RectTransform previewTransform, ShapeType shapeType, DrawingConfig config, Transform parent)
    {
        GameObject shape = new GameObject($"Shape_{shapeType}_{Time.time}");
        shape.transform.SetParent(parent, false);
        
        RectTransform rt = shape.AddComponent<RectTransform>();
        rt.anchoredPosition = previewTransform.anchoredPosition;
        rt.sizeDelta = previewTransform.sizeDelta;
        rt.localRotation = previewTransform.localRotation;
        rt.pivot = previewTransform.pivot;
        rt.anchorMin = previewTransform.anchorMin;
        rt.anchorMax = previewTransform.anchorMax;

        switch (shapeType)
        {
            case ShapeType.Rectangle:
                ShapeOutlineCreator.CreateRectangleOutline(shape, config);
                break;
            case ShapeType.Circle:
                ShapeOutlineCreator.CreateCircleOutline(shape, config);
                break;
            case ShapeType.Ellipse:
                ShapeOutlineCreator.CreateEllipseOutline(shape, config);
                break;
            case ShapeType.Line:
                Image newImage = shape.AddComponent<Image>();
                newImage.color = config.shapeColor;
                newImage.raycastTarget = false;
                break;
        }
        
        return shape;
    }
}
