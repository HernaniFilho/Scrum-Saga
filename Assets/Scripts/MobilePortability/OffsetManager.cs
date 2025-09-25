using UnityEngine;

public class OffsetManager : MonoBehaviour
{
  public static OffsetManager Instance { get; private set; }
      
  public float xOffset
  {
    get
    {
      return CalculateXOffset();
    }
  }
      
  public float yOffset
  {
    get
    {
      return CalculateYOffset();
    }
  }

  private bool debug = false;
      
  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);
  }
      
  private float CalculateXOffset()
  {
    float baseXOffset = 160f;
    float baseScreenWidth = 1686f;
    float screenWidth = Screen.width;

    float xOffset = (screenWidth / baseScreenWidth) * baseXOffset;

    if (debug)
    {
      Debug.LogError("[DEBUG Offset] - baseXOffset: " + baseXOffset);
      Debug.LogError("[DEBUG Offset] - baseScreenWidth: " + baseScreenWidth);
      Debug.LogError("[DEBUG Offset] - screenWidth: " + screenWidth);
      Debug.LogError("[DEBUG Offset] - xOffset: " + xOffset);
      Debug.LogError("---------------");
    }

    return xOffset;
  }
      
  private float CalculateYOffset()
  {
    float baseYOffset = -20f;
    float baseScreenHeight = 1000f;
    float screenHeight = Screen.height;

    float yOffset = (screenHeight / baseScreenHeight) * baseYOffset;

    if (debug)
    {
      Debug.LogError("[DEBUG Offset] - baseYOffset: " + baseYOffset);
      Debug.LogError("[DEBUG Offset] - baseScreenHeight: " + baseScreenHeight);
      Debug.LogError("[DEBUG Offset] - screenHeight: " + screenHeight);
      Debug.LogError("[DEBUG Offset] - yOffset: " + yOffset);
      Debug.LogError("---------------");
    }

    return yOffset;
  }
}