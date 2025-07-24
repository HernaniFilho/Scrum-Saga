using UnityEngine;
using UnityEngine.UI;

public static class ShapeOutlineCreator
{
    public static void CreateRectangleOutline(GameObject parent, DrawingConfig config)
    {
        // Linha superior
        GameObject topLine = new GameObject("TopLine");
        topLine.transform.SetParent(parent.transform, false);
        RectTransform topRT = topLine.AddComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0, 1);
        topRT.anchorMax = new Vector2(1, 1);
        topRT.offsetMin = new Vector2(0, -config.lineThickness);
        topRT.offsetMax = new Vector2(0, 0);
        Image topImage = topLine.AddComponent<Image>();
        topImage.color = config.shapeColor;
        topImage.raycastTarget = false;

        // Linha inferior
        GameObject bottomLine = new GameObject("BottomLine");
        bottomLine.transform.SetParent(parent.transform, false);
        RectTransform bottomRT = bottomLine.AddComponent<RectTransform>();
        bottomRT.anchorMin = new Vector2(0, 0);
        bottomRT.anchorMax = new Vector2(1, 0);
        bottomRT.offsetMin = new Vector2(0, 0);
        bottomRT.offsetMax = new Vector2(0, config.lineThickness);
        Image bottomImage = bottomLine.AddComponent<Image>();
        bottomImage.color = config.shapeColor;
        bottomImage.raycastTarget = false;

        // Linha esquerda
        GameObject leftLine = new GameObject("LeftLine");
        leftLine.transform.SetParent(parent.transform, false);
        RectTransform leftRT = leftLine.AddComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0, 0);
        leftRT.anchorMax = new Vector2(0, 1);
        leftRT.offsetMin = new Vector2(0, 0);
        leftRT.offsetMax = new Vector2(config.lineThickness, 0);
        Image leftImage = leftLine.AddComponent<Image>();
        leftImage.color = config.shapeColor;
        leftImage.raycastTarget = false;

        // Linha direita
        GameObject rightLine = new GameObject("RightLine");
        rightLine.transform.SetParent(parent.transform, false);
        RectTransform rightRT = rightLine.AddComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(1, 0);
        rightRT.anchorMax = new Vector2(1, 1);
        rightRT.offsetMin = new Vector2(-config.lineThickness, 0);
        rightRT.offsetMax = new Vector2(0, 0);
        Image rightImage = rightLine.AddComponent<Image>();
        rightImage.color = config.shapeColor;
        rightImage.raycastTarget = false;
    }
    
    public static void CreateCircleOutline(GameObject parent, DrawingConfig config)
    {
        RectTransform parentRT = parent.GetComponent<RectTransform>();
        float circleSize = Mathf.Max(parentRT.sizeDelta.x, parentRT.sizeDelta.y);
        
        Image imageComponent = parent.AddComponent<Image>();
        imageComponent.color = config.shapeColor;
        imageComponent.raycastTarget = false;
        imageComponent.sprite = SpriteCreator.CreateCircleOutlineSprite(circleSize, config.shapeColor, config.lineThickness);
        imageComponent.type = Image.Type.Simple;
    }
    
    public static void CreateEllipseOutline(GameObject parent, DrawingConfig config)
    {
        RectTransform parentRT = parent.GetComponent<RectTransform>();
        Vector2 ellipseSize = parentRT.sizeDelta;
        
        Image imageComponent = parent.AddComponent<Image>();
        imageComponent.color = config.shapeColor;
        imageComponent.raycastTarget = false;
        imageComponent.sprite = SpriteCreator.CreateEllipseOutlineSprite(ellipseSize, config.shapeColor, config.lineThickness);
        imageComponent.type = Image.Type.Simple;
    }
}
