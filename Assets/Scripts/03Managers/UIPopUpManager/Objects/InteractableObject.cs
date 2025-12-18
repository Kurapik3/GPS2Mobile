using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Object Info")]
    public ObjectData objectData;

    [Header("Selection Indicator")]
    public GameObject selectionIndicator; 

    // Called when this object is selected
    public void OnSelected()
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(true);
        }
    }

    // Called when this object is deselected
    public void OnDeselected()
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
    }
}
