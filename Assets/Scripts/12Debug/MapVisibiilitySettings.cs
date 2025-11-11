using UnityEngine;
[ExecuteInEditMode]
public class MapVisibiilitySettings : MonoBehaviour
{
    private static MapVisibiilitySettings instance;
    public static MapVisibiilitySettings Instance => instance;

    [Header("Developer / Debug Visibility For Map")]
    [Tooltip("Toggle all structures, enemies, ruins, etc. visible/invisible in editor.")]
    public bool showAllContents = true;

    private void Awake()
    {
        instance = this;
    }
}
