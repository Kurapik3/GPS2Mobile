using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Global Builder AI module, attached in hierarchy.
/// Manages all Builder units on the map during Builder phase.
/// </summary>
public class BuilderAI : MonoBehaviour
{
    private EnemyUnitManager unitManager => EnemyUnitManager.Instance;

    private void OnEnable()
    {
        EventBus.Subscribe<ExecuteBuilderPhaseEvent>(OnBuilderPhase);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ExecuteBuilderPhaseEvent>(OnBuilderPhase);
    }

    private void OnBuilderPhase(ExecuteBuilderPhaseEvent evt)
    {
        StartCoroutine(RunBuildersPhase(evt.Turn));
    }

    public IEnumerator RunBuildersPhase(int turn)
    {
        if (unitManager == null) 
            yield break;

        //Get all Builder unit IDs
        List<int> builderIds = new List<int>();
        foreach (var id in unitManager.GetOwnedUnitIds())
        {
            if (unitManager.GetUnitType(id) == "Builder")
                builderIds.Add(id);
        }

        //End phase if no builder units
        if (builderIds.Count == 0)
        {
            EventBus.Publish(new BuilderPhaseEndEvent(turn));
            yield break;
        }

        foreach (int unitId in builderIds)
        {
            if (!unitManager.UnitObjects.ContainsKey(unitId)) 
                continue;

            Vector2Int currentPos = unitManager.GetUnitPosition(unitId);

            //Find closest Grove
            Vector2Int closestGrove = FindClosestGrove(currentPos);

            //Move up to 2 tiles towards Grove
            Vector2Int moveTarget = GetMoveTowards(currentPos, closestGrove, 2);

            if (moveTarget != currentPos)
            {
                EventBus.Publish(new EnemyMoveRequestEvent(unitId, moveTarget));
                //Wait for movement to finish visually
                yield return new WaitForSeconds(0.5f);
            }

            //Develop Grove if reached
            if (moveTarget == closestGrove)
            {
                EventBus.Publish(new BuilderDevelopGroveEvent(unitId, closestGrove));
            }
        }

        //Phase ends after all builders acted
        EventBus.Publish(new BuilderPhaseEndEvent(turn));
    }

    private Vector2Int FindClosestGrove(Vector2Int from)
    {
        GameObject[] groveObjects = GameObject.FindGameObjectsWithTag("Grove");
        if (groveObjects.Length == 0) return from;

        Vector2Int closest = groveObjects[0].GetComponent<GroveBase>().currentTile.HexCoords;
        int minDist = Mathf.Abs(from.x - closest.x) + Mathf.Abs(from.y - closest.y);

        foreach (var groveObj in groveObjects)
        {
            GroveBase grove = groveObj.GetComponent<GroveBase>();
            if (grove == null) continue;

            Vector2Int pos = grove.currentTile.HexCoords;
            int dist = Mathf.Abs(from.x - pos.x) + Mathf.Abs(from.y - pos.y);
            if (dist < minDist)
            {
                minDist = dist;
                closest = pos;
            }
        }

        return closest;
    }

    private Vector2Int GetMoveTowards(Vector2Int from, Vector2Int to, int maxSteps)
    {
        Vector2Int delta = to - from;
        int moveX = Mathf.Clamp(delta.x, -maxSteps, maxSteps);
        int remainingSteps = maxSteps - Mathf.Abs(moveX);
        int moveY = Mathf.Clamp(delta.y, -remainingSteps, remainingSteps);
        return from + new Vector2Int(moveX, moveY);
    }
}
