using UnityEngine;

[CreateAssetMenu(menuName = "TileGen/StructureData")]
public class StructureData : ScriptableObject
{
    [Header("Structure Info")]
    [SerializeField] public string structureName;
    [SerializeField] public GameObject prefab;
    [SerializeField][Tooltip("Offset from tile center (for height adjustments)")] public Vector3 yOffset;

    [Tooltip("If true, this structure blocks dynamic tiles like Fish or Debris.")]
    [SerializeField] public bool blocksDynamic = true;
}
