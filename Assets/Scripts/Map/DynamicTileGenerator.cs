using UnityEngine;
using System.Collections.Generic;
public class DynamicTileGenerator : MonoBehaviour
{
    [SerializeField] private GameObject fishPrefab;
    [SerializeField] private GameObject debrisPrefab;
    [Range(0f, 1f)] public float fishChance = 0.2f;
    [Range(0f, 1f)] public float debrisChance = 0.1f;
    
    public void GenerateDynamicElements()
    {
        foreach (var tile in MapManager.Instance.GetTiles())
        {
            //Checks if tiles already have a structure on it
            if(tile.HasStructure)
            {
                continue;
            }
            float rand = Random.value;
            GameObject toSpawn = null;
            if (rand < fishChance)
            {
                toSpawn = fishPrefab;
            }
            else if(rand <fishChance +debrisChance)
            {
                toSpawn = debrisPrefab;
            }

            if(toSpawn != null)
            {
                var obj = Instantiate(toSpawn, tile.transform);
                obj.transform.localPosition = Vector3.zero;
                obj.name = toSpawn.name;
            }
        }
    }
}
