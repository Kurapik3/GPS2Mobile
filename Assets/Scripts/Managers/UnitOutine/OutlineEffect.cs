using UnityEngine;

public class OutlineEffect : MonoBehaviour
{
    public Color outlineColor = Color.yellow;
    public float outlineWidth = 3f;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Material outlineMaterial;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        outlineMaterial = new Material(Shader.Find("Outlined/Silhouetted Diffuse"));
        outlineMaterial.SetColor("_OutlineColor", outlineColor);
        outlineMaterial.SetFloat("_Outline", outlineWidth);
    }

    public void EnableOutline()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
            Material[] mats = renderers[i].materials;
            System.Array.Resize(ref mats, mats.Length + 1);
            mats[mats.Length - 1] = outlineMaterial;
            renderers[i].materials = mats;
        }
    }

    public void DisableOutline()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].materials = new Material[] { originalMaterials[i] };
        }
    }
}
