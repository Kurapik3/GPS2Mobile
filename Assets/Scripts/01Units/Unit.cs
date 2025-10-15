using UnityEngine;

public class Unit : MonoBehaviour
{
    public bool isSelected = false;
    private Renderer rend;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        UpdateSelectionVisual();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSelectionVisual();
    }

    private void UpdateSelectionVisual()
    {
        if (rend != null)
            rend.material.color = isSelected ? Color.yellow : Color.white;
    }
}
