using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextContainerManager : MonoBehaviour
{
    public TMP_Text childText;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void Update()
    {
        if (childText == null) return;

        bool hasText = !string.IsNullOrEmpty(childText.text);
        bool childActive = childText.gameObject.activeSelf;

        image.enabled = childActive && hasText;
    }
}
