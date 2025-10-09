using UnityEngine;

public class BuilderUnit : UnitBase
{
    protected override void Start()
    {
        base.Start(); 
        Debug.Log($"{unitName} is a Builder unit ready to build! \nHP:{hp}, Attack:{attack}, Movement:{movement}");
    }

    public void BuildStructure(Vector3 position)
    {
        Debug.Log($"{unitName} is constructing a building at {position}");
      
    }
}
