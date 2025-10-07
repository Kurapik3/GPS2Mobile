[System.Serializable]
public class UnitData
{
    public string unitName { get; private set; }
    public int cost { get; private set; }
    public int range { get; private set; }
    public int movement { get; private set; }
    public int hp { get; private set; }
    public int attack { get; private set; }
    public bool isCombat { get; private set; }
    public string ability { get; private set; }


    public UnitData(string Name,int Cost, int Range, int Movement, int Hp, int Atk, bool IsCombat, string Ability)
    {
        unitName = Name;
        cost = Cost;
        range = Range;
        movement = Movement;
        hp = Hp;
        attack = Atk;
        isCombat = IsCombat;
        ability = Ability;
    }

}