using UnityEngine;

[System.Serializable]
public class ObjectData
{
    public string objectName;
    public string description;
    public Sprite icon;
    public ObjectType objectType;
    public int apCost = 2;
    public bool showProximityBubble = false;
}
