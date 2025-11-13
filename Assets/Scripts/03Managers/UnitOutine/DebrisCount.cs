using UnityEngine;

public class DebrisCount : MonoBehaviour
{
    void Start()
    {
        DebirisSelect.instance.allDebrisList.Add(gameObject);
    }

    private void OnDestroy()
    {
        DebirisSelect.instance.allDebrisList.Remove(gameObject);
    }
}
