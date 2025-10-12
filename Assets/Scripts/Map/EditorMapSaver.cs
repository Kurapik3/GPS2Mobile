#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


public class EditorMapSaver : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;

    [ContextMenu("Save Base Map")]
    void SaveBaseMap()
    {
        if (mapGenerator.MapData != null)
        {
            MapSaveLoad.Save(mapGenerator.MapData, "MySavedMap");
        }
        else
        {
            Debug.LogError("No MapData found in MapGenerator!");
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(EditorMapSaver))]
    public class EditorMapSaverInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorMapSaver editor = (EditorMapSaver)target;

            if (GUILayout.Button("Save Base Map"))
            {
                editor.SaveBaseMap();
            }
        }
    }
#endif
}