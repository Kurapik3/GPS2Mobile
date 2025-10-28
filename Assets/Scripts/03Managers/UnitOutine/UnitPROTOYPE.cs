using UnityEngine;

public class UnitPROTOYPE : MonoBehaviour
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


