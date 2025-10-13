using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Exploration behavior for enemy units in Dormant state.
/// Moves units randomly: 50% towards origin, 50% away.
/// Executed before CombatAI so movement is visible to Combat decisions.
/// </summary>
public class ExplorationAI : ISubAI
{
    private IAIContext context;
    private IAIActor actor;
    private System.Random rng = new System.Random();
    private float delay = 1f;

    public void Initialize(IAIContext context, IAIActor actor)
    {
        this.context = context;
        this.actor = actor;
    }

    public IEnumerator ExecuteStepByStep()
    {
        var units = context.GetOwnedUnitIds();
        if (units == null || units.Count == 0)
            yield break;

        Vector2Int origin = new Vector2Int(0, 0);

        foreach (var unitId in units)
        {
            //Skip units that are already aggressive (visible to player)
            if (context.IsUnitVisibleToPlayer(unitId))
                continue;

            Vector2Int currentHex = context.GetUnitPosition(unitId);
            int moveRange = 1;

            List<Vector2Int> reachableHexes = context.GetReachableHexes(currentHex, moveRange);
            reachableHexes.RemoveAll(hex => !MapManager.Instance.CanUnitStandHere(hex));

            if (reachableHexes.Count == 0)
                continue;

            //50% towards origin (0,0), 50% away
            bool moveTowards = rng.NextDouble() < 0.5;

            //Find the best hex direction
            Vector2Int targetHex = ChooseHexDirection(reachableHexes, currentHex, origin, moveTowards);

            //Convert target hex to world position
            Vector3 destination = context.HexToWorld(targetHex);

            actor.MoveTo(unitId, destination);
            Debug.Log($"[ExplorationAI] Unit {unitId} moving {(moveTowards ? "towards" : "away from")} origin to {destination}");

            yield return new WaitForSeconds(delay / AIController.AISpeedMultiplier);
        }
    }

    //Choose the best hex direction based on movement strategy (towards/away from origin)
    private Vector2Int ChooseHexDirection(List<Vector2Int> candidates, Vector2Int current, Vector2Int origin, bool moveTowards)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        int currentDist = context.GetHexDistance(current, origin);

        foreach (var hex in candidates)
        {
            if (context.IsTileOccupied(hex))
                continue;

            int dist = context.GetHexDistance(hex, origin);

            if (moveTowards && dist < currentDist) //Choose tiles that are closer to origin
                validMoves.Add(hex);
            if (moveTowards && dist > currentDist) //Choose tiles that are farther from origin
                validMoves.Add(hex);
        }

        //If no valid moves, pick a random adjacent hex (fallback)
        if (validMoves.Count == 0)
            validMoves = candidates;

        //Pick random from valid moves
        return validMoves[rng.Next(validMoves.Count)];
    }
}
