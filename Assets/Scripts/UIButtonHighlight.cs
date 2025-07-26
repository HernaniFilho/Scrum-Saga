using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHighlight : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Imagem para Destaque")]
    public GameObject hoverHighlight;
    [Header("Imagem para Clique")]
    public GameObject clickHighlight;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverHighlight != null)
            hoverHighlight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverHighlight != null)
            hoverHighlight.SetActive(false);

        if (clickHighlight != null)
            clickHighlight.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (clickHighlight != null)
            clickHighlight.SetActive(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (clickHighlight != null)
            clickHighlight.SetActive(false);
    }
}
