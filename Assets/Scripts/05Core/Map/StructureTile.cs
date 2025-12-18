using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
public class StructureTile : MonoBehaviour
{
    [Header("Structure Settings")]
    [SerializeField] public StructureDatabase structureDatabase;
    [HideInInspector] public int selectedIndex = -1;
    [HideInInspector] public GameObject structureInstance;


    public void ApplyStructure()
    {
#if UNITY_EDITOR
        if (structureDatabase == null || structureDatabase.structures == null || structureDatabase.structures.Count == 0)
        {
            Debug.LogWarning($"[{name}] No StructureDatabase or entries found!");
            return;
        }
        selectedIndex = Mathf.Clamp(selectedIndex, 0, structureDatabase.structures.Count - 1);
        var data = structureDatabase.structures[selectedIndex];
        if (data == null || data.prefab == null)
        {
            Debug.LogWarning($"[{name}] Structure prefab missing!");
            return;
        }

        Transform meshChild = transform.Find("Mesh");
        Transform parentTarget = meshChild != null ? meshChild : transform;

        Transform old = parentTarget.Find("Structure");
        if (old != null)
        {
            DestroyImmediate(old.gameObject);
        }

        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        bool inPrefabStage = stage != null && stage.IsPartOfPrefabContents(gameObject);
        bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(gameObject);

        GameObject inst = null;
        if (inPrefabStage)
        {
            inst = (GameObject)PrefabUtility.InstantiatePrefab(data.prefab);
            SceneManager.MoveGameObjectToScene(inst, stage.scene);
            inst.transform.SetParent(parentTarget, false);
        }
        else if (!isPrefabAsset)
        {
            inst = (GameObject)PrefabUtility.InstantiatePrefab(data.prefab, parentTarget);
        }
        else
        {
            Debug.LogWarning($"Cannot instantiate structure into prefab asset directly. Open the prefab in Prefab Mode or place the map in a scene and apply there: {name}");
        }

        if (inst != null)
        {
            inst.name = "Structure";
            inst.transform.localPosition = data.yOffset;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            structureInstance = inst;
            EditorUtility.SetDirty(inst);
        }

        // update HexTile metadata so SaveMapData captures it
        HexTile hex = GetComponent<HexTile>();
        if (hex != null)
        {
            hex.structureIndex = selectedIndex;
            hex.StructureName = data.structureName;
            hex.SetStructure(data);
            EditorUtility.SetDirty(hex);
        }

        EditorUtility.SetDirty(this);
        // mark scene/prefab-dirty so user can save
        if (inPrefabStage)
        {
            EditorSceneManager.MarkSceneDirty(stage.scene);
        }
        else
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        Debug.Log($"[{name}] Structure '{data.structureName}' applied.");

        SceneView.RepaintAll();
#else
        Debug.LogWarning($"[{name}] ApplyStructure() skipped at runtime (Editor-only). Use ApplyStructureRuntime() instead.");
#endif
    }
    public void ApplyStructureRuntime()
    {
        if (structureDatabase == null || structureDatabase.structures == null || structureDatabase.structures.Count == 0)
        {
            Debug.LogWarning($"[{name}] No StructureDatabase or entries found!");
            return;
        }

        selectedIndex = Mathf.Clamp(selectedIndex, 0, structureDatabase.structures.Count - 1);
        var data = structureDatabase.structures[selectedIndex];
        if (data == null || data.prefab == null)
        {
            Debug.LogWarning($"[{name}] Structure prefab missing!");
            return;
        }

        Transform meshChild = transform.Find("Mesh");
        Transform parentTarget = meshChild != null ? meshChild : transform;

        // Remove old
        Transform old = parentTarget.Find("Structure");
        if (old != null)
            Destroy(old.gameObject);

        GameObject inst = Instantiate(data.prefab, parentTarget);
        inst.name = "Structure";
        inst.transform.localPosition = data.yOffset;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;
        structureInstance = inst;

        // Update metadata
        HexTile hex = GetComponent<HexTile>();
        if (hex != null)
        {
            hex.structureIndex = selectedIndex;
            hex.StructureName = data.structureName;
            hex.SetStructure(data);
        }

        Debug.Log($"[{name}] Structure '{data.structureName}' applied at runtime.");
    }

    // Apply by name (used when loading MapData)
    public void ApplyStructureByName(string name)
    {
        if (structureDatabase == null)
        { 
            structureDatabase = Resources.Load<StructureDatabase>("StructureDatabase"); 
        }

        if (structureDatabase == null)
        {
            Debug.LogWarning("StructureDatabase missing (Resources/StructureDatabase not found).");
            return;
        }

        int idx = structureDatabase.structures.FindIndex(s => s.structureName == name);
        if (idx >= 0)
        {
            selectedIndex = idx;
            ApplyStructure();
        }
        else
        {
            Debug.LogWarning($"Structure '{name}' not found in StructureDatabase.");
        }
    }
#if UNITY_EDITOR
    // remove structure object and clear hex metadata
    public void ClearStructure()
    {
        Transform old = transform.Find("Structure");
        if (old != null)
        {
            DestroyImmediate(old.gameObject);
        }

        HexTile hex = GetComponent<HexTile>();
        if (hex != null)
        {
            hex.structureIndex = -1;
            hex.StructureName = null;
            EditorUtility.SetDirty(hex);
        }

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
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
        EditorGUILayout.PropertyField(serializedObject.FindProperty("structureDatabase"));


        var dbProp = serializedObject.FindProperty("structureDatabase");
        var db = dbProp.objectReferenceValue as StructureDatabase;
        if (db != null && db.structures != null && db.structures.Count > 0)
        {
            SerializedProperty idxProp = serializedObject.FindProperty("selectedIndex");
            string[] names = db.GetAllNames();

            EditorGUI.BeginChangeCheck();
            idxProp.intValue = EditorGUILayout.Popup("Structure Type", Mathf.Clamp(idxProp.intValue, 0, names.Length - 1), names);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                tile.ApplyStructure(); // Auto-update when changed
            }

            GUILayout.Space(6);
            if (GUILayout.Button("Apply Structure (force)"))
            {
                tile.ApplyStructure();
            }
            if (GUILayout.Button("Clear Structure"))
            {
                tile.ClearStructure();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No StructureDatabase assigned or empty! Create/assign one to choose structures.", MessageType.Warning);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
