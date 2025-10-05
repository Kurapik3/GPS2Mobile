using UnityEngine;

public class BasicAI : ISubAI
{
    private IAIContext context;
    private IAIActor actor;

    public int Priority => 0; //Lowest priority (PLACEHOLDER_LOGIC) 

    public void Initialize(IAIContext context, IAIActor actor)
    {
        this.context = context;
        this.actor = actor;
    }

    public void Execute()
    {
        var units = context.GetOwnedUnitIds();
        if (units == null || units.Count == 0)
            return;

        foreach (var unitId in units)
        {
            Vector3 currentPos = context.GetUnitPosition(unitId);

            //==================== Movement ====================  
            Vector3 enemyPos = context.GetNearestEnemy(currentPos);
            //Vector3 targetPos = new Vector3(enemyPos.x - 1, currentPos.y, enemyPos.z);

            //if (targetPos != currentPos)
            //{
            //    actor.MoveTo(unitId, targetPos);
            //    Debug.Log($"BasicAI: unit {unitId} currentPos={currentPos}, destination={targetPos}");
            //}
            //==================== Attack ====================
            //If any enemy is within attack range, attack the closest one
            var nearbyEnemies = context.GetEnemiesInRange(currentPos, 2f); //Short attack range
            if (nearbyEnemies.Count > 0)
            {
                int target = nearbyEnemies[0]; //Pick the first or closest
                actor.AttackTarget(unitId, target);
                //continue; //Skip movement if attack is performed
            }


            //Vector3 destination = currentPos;
            //switch (difficulty)
            //{
            //    case AIController.Difficulty.Easy:
            //        //Move towards a random unexplored tile
            //        var unexplored = context.GetUnexploredTiles();
            //        if (unexplored.Count > 0)
            //            destination = unexplored[rng.Next(unexplored.Count)];
            //        break;

            //    case AIController.Difficulty.Normal:
            //        //Wander randomly by 1 tile in any direction
            //        destination = currentPos + new Vector3(rng.Next(-1, 2), 0, rng.Next(-1, 2));
            //        break;

            //    case AIController.Difficulty.Hard:
            //        //Move towards the first owned base -> as a simple "strategic" choice
            //        var bases = context.GetOwnedBaseIds();
            //        if (bases.Count > 0)
            //            destination = context.GetBasePosition(bases[0]);
            //        break;
            //}

            ////Random step (1 tile in any direction)
            //Vector3 destination = currentPos + new Vector3(rng.Next(-1, 2), 0, rng.Next(-1, 2));

            ////Move only if destination is different from current position
            //if (destination != currentPos)
            //{
            //    actor.MoveTo(unitId, destination);

            //}

        }
    }
}
