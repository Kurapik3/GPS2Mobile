using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif
public class StructureTile : MonoBehaviour
{
    [Header("Structure Settings")]
    [SerializeField] public StructureDatabase structureDatabase;
    //[HideInInspector] public string selectedStructureName;
    [HideInInspector] public int selectedIndex = 0;
    private GameObject structureInstance;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (structureDatabase == null)
        {
            structureDatabase = Resources.Load<StructureDatabase>("StructureDatabase");
        }
        if (!Application.isPlaying && structureDatabase != null)
        {
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    ApplyStructure();
                }
                    
            };
        }
    }
    private void OnEnable()
    {
        if (!Application.isPlaying && structureDatabase != null)
        {
            ApplyStructure();
        }
    }

    public void ApplyStructure()
    {
        //if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
        //{
        //    if (structureInstance != null)
        //    {
        //        DestroyImmediate(structureInstance);
        //        structureInstance = null;
        //    }
        //    return;
        //}
        if (structureDatabase == null || structureDatabase.structures == null || structureDatabase.structures.Count == 0)
        {
            Debug.LogWarning($"[{name}] No StructureDatabase or entries found!");
            return;
        }

        var data = structureDatabase.structures[Mathf.Clamp(selectedIndex, 0, structureDatabase.structures.Count - 1)];
        if (data.prefab == null)
        {
            Debug.LogWarning($"[{name}] Structure prefab missing!");
            return;
        }

        //Transform meshChild = transform.Find("Mesh");
        //Transform parentTarget = meshChild != null ? meshChild : transform;

        if (structureInstance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(structureInstance);
            }
            else
            {
                DestroyImmediate(structureInstance);
            }
        }

        structureInstance = (GameObject)PrefabUtility.InstantiatePrefab(data.prefab, transform);
        structureInstance.name = "Structure";
        structureInstance.transform.localPosition = data.yOffset;
        structureInstance.transform.localRotation = Quaternion.identity;
        structureInstance.transform.localScale = Vector3.one;
    }

#endif
}
#if UNITY_EDITOR
[CustomEditor(typeof(StructureTile))]
public class StructureTileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        StructureTile tile = (StructureTile)target;
        // Show database field
        EditorGUILayout.PropertyField(serializedObject.FindProperty("structureDatabase"));

        // If database exists, show dropdown    
        StructureDatabase db = (StructureDatabase)serializedObject.FindProperty("structureDatabase").objectReferenceValue;

        if (db != null && db.structures != null && db.structures.Count > 0)
        {
            SerializedProperty selectedIndexProp = serializedObject.FindProperty("selectedIndex");
            string[] options = db.structures.ConvertAll(s => s.name).ToArray();

            EditorGUI.BeginChangeCheck();
            selectedIndexProp.intValue = EditorGUILayout.Popup("Structure Type", selectedIndexProp.intValue, options);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                tile.ApplyStructure(); // Auto-update when changed
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No StructureDatabase assigned or empty!", MessageType.Warning);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
