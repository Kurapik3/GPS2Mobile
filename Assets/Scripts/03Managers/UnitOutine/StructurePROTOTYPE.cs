using UnityEngine;

public class StructurePROTOTYPE : MonoBehaviour
{
    void Start()
    {
        SelectionOfStructureManager.instance.allStructureList.Add(gameObject);
    }

    private void OnDestroy()
    {
        SelectionOfStructureManager.instance.allStructureList.Remove(gameObject);
    }
}
