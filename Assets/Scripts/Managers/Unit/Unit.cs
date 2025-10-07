using UnityEngine;

public class Unit : MonoBehaviour
{
    

    void Start()
    {
        UnitS.instance.allUnitList.Add(gameObject);
    }

    private void OnDestroy()
    {
        UnitS.instance.allUnitList.Remove(gameObject);
    }


}
