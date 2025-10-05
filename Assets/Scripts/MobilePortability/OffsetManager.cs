using UnityEngine;
using UnityEngine.UI;

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
    float xScale = GetComponent<RectTransform>().localScale.x;

    MobileScaler mobileScaler = GetComponent<MobileScaler>();
    bool isMobile = mobileScaler != null && mobileScaler.isMobile;
    
    float xOffset = isMobile ? 0 : baseXOffset * xScale;

    if (debug)
    {
      Debug.LogError("[DEBUG Offset] - baseXOffset: " + baseXOffset);
      Debug.LogError("[DEBUG Offset] - xScale: " + xScale);
      Debug.LogError("[DEBUG Offset] - xOffset: " + xOffset);
      Debug.LogError("---------------");
    }

    return xOffset;
  }
      
  private float CalculateYOffset()
  {
    float baseYOffset = -20f;
    float yScale = GetComponent<RectTransform>().localScale.y;

    MobileScaler mobileScaler = GetComponent<MobileScaler>();
    bool isMobile = mobileScaler != null && mobileScaler.isMobile;

    float yOffset = isMobile ? 0 : baseYOffset * yScale;

    if (debug)
    {
      Debug.LogError("[DEBUG Offset] - baseYOffset: " + baseYOffset);
      Debug.LogError("[DEBUG Offset] - yScale: " + yScale);
      Debug.LogError("[DEBUG Offset] - yOffset: " + yOffset);
      Debug.LogError("---------------");
    }

    return yOffset;
  }
}