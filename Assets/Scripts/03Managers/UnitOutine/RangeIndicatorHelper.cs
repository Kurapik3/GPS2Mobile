using UnityEngine;

public class RangeIndicatorHelper : MonoBehaviour
{
    [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    [SerializeField] private Color hoverColor = new Color(0.3f, 1f, 0.3f, 0.7f);

    private Material mat;
    private Color currentColor;

    private void Start()
    {
        // Get the material
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            mat = meshRenderer.material;
            currentColor = mat.color;
            normalColor = currentColor;
            hoverColor = new Color(currentColor.r * 1.2f, currentColor.g * 1.2f, currentColor.b * 1.2f, currentColor.a * 1.4f);
        }
    }

    private void OnMouseEnter()
    {
        if (mat != null)
        {
            mat.color = hoverColor;
        }
    }

    private void OnMouseExit()
    {
        if (mat != null)
        {
            mat.color = normalColor;
        }
    }

    /// <summary>
    /// Set the base color of this indicator
    /// </summary>
    public void SetColor(Color color)
    {
        normalColor = color;
        currentColor = color;
        hoverColor = new Color(color.r * 1.2f, color.g * 1.2f, color.b * 1.2f, Mathf.Min(color.a * 1.4f, 1f));

        if (mat != null)
        {
            mat.color = color;
        }
    }
}
