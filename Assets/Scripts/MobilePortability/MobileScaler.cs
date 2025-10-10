using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using TMPro;
using System.Runtime.InteropServices;

public class MobileScaler : MonoBehaviour
{
    public CanvasScaler canvasScaler;
    public GameObject MobileSidebar;
    public List<GameObject> DesktopObjects = new List<GameObject>();
    public bool isMobile = false;
    public TMP_Text debugText;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int GetViewportWidth();

    [DllImport("__Internal")]
    private static extern int GetViewportHeight();
#endif

    private Vector2 GetViewportSize()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return new Vector2(GetViewportWidth(), GetViewportHeight());
#else
        return new Vector2(Screen.width, Screen.height);
#endif
    }

    void Awake()
    {
        Vector2 viewport = GetViewportSize();
        isMobile = viewport.x < 1000;
        // debugText.text = $"{viewport.x}x{viewport.y}";

        if (isMobile)
        {
            ManageMobile();
            Debug.Log("Mobile!");
        }
        else
        {
            ManageDesktop();
            Debug.Log("Desktop!");
        }
    }

    void Update()
    {
        Vector2 viewport = GetViewportSize();
        bool currentlyMobile = viewport.x < 1000;

        if (currentlyMobile != isMobile)
        {
            isMobile = currentlyMobile;
            // debugText.text = $"{viewport.x}x{viewport.y}";

            if (isMobile)
            {
                ManageMobile();
            }
            else
            {
                ManageDesktop();
            }
        }
    }

    public void ManageMobile()
    {
        canvasScaler.referenceResolution = new Vector2(1280, 720);

        if (MobileSidebar != null)
        {
            MobileSidebar.SetActive(true);
        }

        foreach (GameObject obj in DesktopObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    public void ManageDesktop()
    {
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        if (MobileSidebar != null)
        {
            MobileSidebar.SetActive(false);
        }
        
        foreach (GameObject obj in DesktopObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }
}
