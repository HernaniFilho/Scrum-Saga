using UnityEngine;

public class CardSelector : MonoBehaviour
{
    private SprintPlanningManager sprintPlanningManager;
    private int cardIndex;

    public void Initialize(SprintPlanningManager manager, int index)
    {
        sprintPlanningManager = manager;
        cardIndex = index;
    }

    void OnMouseDown()
    {
        if (sprintPlanningManager != null)
        {
            Debug.Log($"Carta {cardIndex + 1} clicada!");
            sprintPlanningManager.OnCardSelected(cardIndex, gameObject);
        }
    }
}
