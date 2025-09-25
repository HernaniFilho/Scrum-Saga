using UnityEngine;

public class CenterBoard : MonoBehaviour
{
  private Camera playerCamera;

  [SerializeField] private float spawnDistance = 297f;
  [SerializeField] private float xOffset = 160f;
  [SerializeField] private float yOffset = -20f;
    
  private void Start()
  {
    playerCamera = Camera.main;
  }

  private void Update()
  {
    xOffset = OffsetManager.Instance != null ? OffsetManager.Instance.xOffset : 160f;
    yOffset = OffsetManager.Instance != null ? OffsetManager.Instance.yOffset : -20f;

    Vector3 screenCenter = new Vector3(
      Screen.width / 2f + xOffset,
      Screen.height / 2f + yOffset,
      spawnDistance
    );
    
    transform.position = playerCamera.ScreenToWorldPoint(screenCenter);
  }
}
