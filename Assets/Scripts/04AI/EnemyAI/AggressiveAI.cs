//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

///// <summary>
///// Combat behavior for enemy units in Aggressive state.
///// Prioritizes player bases, then player units. 60% chance to attack if a target is in range.
///// If no targets, moves (70% towards origin, 30% away).
///// </summary>
//public class CombatAI : ISubAI
//{
//    private IAIContext context;
//    private IAIActor actor;
//    private System.Random rng = new System.Random();
//    private float delay = 1f;

//    public void Initialize(IAIContext context, IAIActor actor)
//    {
//        this.context = context;
//        this.actor = actor;
//    }

//    public IEnumerator ExecuteStepByStep()
//    {
//        var units = context.GetOwnedUnitIds();
//        if (units == null || units.Count == 0)
//            yield break;

//        foreach (var unitId in units)
//        {
//            //Skip dormant (invisible) units
//            if (!context.IsUnitVisibleToPlayer(unitId))
//                continue;

//            string unitType = context.GetUnitType(unitId);
//            Vector2Int currentHex = context.GetUnitPosition(unitId);
//            int attackRange = context.GetUnitAttackRange(unitId);

//            //Skip combat for Builder & Scout
//            if (unitType == "Builder" || unitType == "Scout")
//            {
//                MoveIdle(unitId, currentHex);
//                Debug.Log($"[CombatAI] {unitType} #{unitId} cannot attack, moving instead.");
//                yield return new WaitForSeconds(delay / AIController.AISpeedMultiplier);
//                continue;

//            }

//            //Check for player bases in range first
//            List<int> targets = context.GetEnemiesInRange(currentHex, attackRange); //"Enemies" means player entities from enemy AI POV
//            if (targets != null && targets.Count > 0)
//            {
//                //Choose priority: prefer bases (if possible)
//                int selected = ChoosePriorityTarget(targets, currentHex);

//                //60% chance to attack
//                if (rng.NextDouble() < 0.6)
//                {
//                    actor.AttackTarget(unitId, selected);
//                    Debug.Log($"[CombatAI] Unit {unitType} #{unitId} attacks entity {selected}");
//                    yield return new WaitForSeconds(delay / AIController.AISpeedMultiplier);
//                    continue;
//                }
//                else
//                {
//                    Debug.Log($"[CombatAI] Unit {unitType} #{unitId} rolled to not attack");
//                }
//            }

//            //If no valid target or skipped attack: decide movement (70% towards origin)
//            MoveIdle(unitId, currentHex);
//            yield return new WaitForSeconds(delay / AIController.AISpeedMultiplier);
//        }
//    }

//    private void MoveIdle(int unitId, Vector2Int currentHex)
//    {
//        int moveRange = context.GetUnitMoveRange(unitId);
//        List<Vector2Int> reachableHexes = context.GetReachableHexes(currentHex, moveRange);
//        reachableHexes.RemoveAll(hex => !MapManager.Instance.CanUnitStandHere(hex));

//        if (reachableHexes.Count == 0)
//            return;

//        bool moveToOrigin = rng.NextDouble() < 0.7;
//        Vector2Int originHex = new Vector2Int(0,0);
//        Vector2Int targetHex = ChooseTargetHex(reachableHexes, currentHex, originHex, moveToOrigin);
//        Vector3 destination = context.HexToWorld(targetHex);

//        actor.MoveTo(unitId, destination);
//        Debug.Log($"[CombatAI] Unit {unitId} moves {(moveToOrigin ? "towards" : "away from")} origin to {destination}");
//    }

//    //Chooses the most appropriate target —> prioritize bases, then units
//    private int ChoosePriorityTarget(List<int> candidateIds, Vector2Int fromPos)
//    {
//        int? baseTarget = null;
//        int? unitTarget = null;
//        int minBaseDistance = int.MaxValue;
//        int minUnitDistance = int.MaxValue;

//        foreach (var id in candidateIds)
//        {
//            Vector2Int enemyHex = context.GetEnemyPosition(id);
//            int distance = context.GetHexDistance(fromPos, enemyHex);

//            //Base priority
//            if (context.GetPlayerBaseIds().Contains(id))
//            {
//                if (distance < minBaseDistance)
//                {
//                    minBaseDistance = distance;
//                    baseTarget = id;
//                }
//            }
//            //Unit secondary priority
//            else if (context.GetPlayerUnitIds().Contains(id))
//            {
//                if (distance < minUnitDistance)
//                {
//                    minUnitDistance = distance;
//                    unitTarget = id;
//                }
//            }
//        }

//        //Prefer base > unit > fallback
//        if (baseTarget.HasValue)
//            return baseTarget.Value;
//        if (unitTarget.HasValue)
//            return unitTarget.Value;

//        return candidateIds[0]; //fallback
//    }

//    private Vector2Int ChooseTargetHex(List<Vector2Int> candidates, Vector2Int current, Vector2Int origin, bool moveTowards)
//    {
//        List<Vector2Int> valid = new List<Vector2Int>();
//        int currentDist = context.GetHexDistance(current, origin);

//        foreach (var hex in candidates)
//        {
//            int d = context.GetHexDistance(hex, origin);
//            if (moveTowards && d < currentDist) valid.Add(hex);
//            if (!moveTowards && d > currentDist) valid.Add(hex);
//        }

//        //Fallback
//        if (valid.Count == 0)
//            valid = candidates;

//        return valid[rng.Next(valid.Count)];
//    }
//}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aggressive logic for visible units.
/// Prioritizes bases > sea monsters > units when choosing targets.
/// If target in range: attack (60% chance). If not, move towards nearest player base.
/// </summary>
public class AggressiveAI : MonoBehaviour
{
    [SerializeField] private float stepDelay = 1f;
    private System.Random rng = new System.Random();

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyAIEvents.ExecuteAggressivePhaseEvent>(OnAggressivePhase);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyAIEvents.ExecuteAggressivePhaseEvent>(OnAggressivePhase);
    }

    private void OnAggressivePhase(EnemyAIEvents.ExecuteAggressivePhaseEvent evt)
    {
        StartCoroutine(RunAggressivePhase());
    }

    private IEnumerator RunAggressivePhase()
    {
        var eum = EnemyUnitManager.Instance;
        if (eum == null) 
            yield break;

        var unitIds = eum.GetOwnedUnitIds();
        if (unitIds == null || unitIds.Count == 0) 
            yield break;

        foreach (var id in unitIds)
        {
            eum.LockState(id);

            if (!IsUnitVisibleToPlayer(id))
            {
                eum.TrySetState(id, EnemyUnitManager.AIState.Dormant);
                continue;
            }

            string type = eum.GetUnitType(id);

            //Find targets in attack range
            int range = eum.GetUnitAttackRange(id);
            var targets = GetEnemiesInRange(eum.GetUnitPosition(id), range);

            if (targets.Count > 0)
            {
                int selected = ChoosePriorityTarget(targets, eum.GetUnitPosition(id));
                if (rng.NextDouble() < 0.6)
                {
                    EventBus.Publish(new EnemyAIEvents.EnemyAttackRequestEvent(id, selected));
                    yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
                    continue;
                }
            }

            //Move toward nearest player base if no target
            TryMoveIdle(id);
            yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
        }
    }

    private bool IsUnitVisibleToPlayer(int unitId)
    {
        FogSystem fog = FindFirstObjectByType<FogSystem>();
        if (fog == null) 
            return true; //Couldn't find fog, assume all units visible

        var pos = EnemyUnitManager.Instance.GetUnitPosition(unitId);
        return fog.revealedTiles.Contains(pos);
    }

    private List<int> GetEnemiesInRange(Vector2Int from, int range)
    {
        //Player units and player bases both considered "targets"
        var result = new List<int>();
        //var playerUnits = FindFirstObjectByType<UnitManager>();
        //if (playerUnits != null)
        //{
        //    foreach (var id in playerUnits.GetAllPlayerUnitIds())
        //    {
        //        var p = playerUnits.GetUnitPosition(id);
        //        if (AIPathFinder.GetHexDistance(from, p) <= range) 
        //            result.Add(id);
        //    }
        //}

        var players = GameObject.FindGameObjectsWithTag("PlayerUnit");
        foreach (var go in players)
        {
            Vector2Int hex = MapManager.Instance.WorldToHex(go.transform.position);
            if (AIPathFinder.GetHexDistance(from, hex) <= range)
            {
                //For fallback mapping, use instanceID as id
                result.Add(go.GetComponent<UnitBase>().UnitID);
            }
        }

        var playerBases = GameObject.FindGameObjectsWithTag("PlayerBase");
        foreach (var go in playerBases)
        {
            Vector2Int hex = MapManager.Instance.WorldToHex(go.transform.position);
            if (AIPathFinder.GetHexDistance(from, hex) <= range)
            {
                //For fallback mapping, use instanceID as id
                result.Add(go.GetInstanceID());
            }
        }
        return result;
    }

    private int ChoosePriorityTarget(List<int> candidates, Vector2Int fromPos)
    {
        int? bestBase = null;
        int? bestSeaMonster = null;
        int? bestUnit = null;

        int minBaseDist = int.MaxValue;
        int minSeaDist = int.MaxValue;
        int minUnitDist = int.MaxValue;

        foreach (int id in candidates)
        {
            var go = FindObjectFromInstanceID(id);
            if (go == null) continue;

            string tag = go.tag;
            Vector2Int pos = MapManager.Instance.WorldToHex(go.transform.position);
            int dist = AIPathFinder.GetHexDistance(fromPos, pos);

            if (tag == "PlayerBase")
            {
                if (dist < minBaseDist)
                {
                    minBaseDist = dist;
                    bestBase = id;
                }
            }
            else if (tag == "SeaMonster")
            {
                if (dist < minSeaDist)
                {
                    minSeaDist = dist;
                    bestSeaMonster = id;
                }
            }
            else if (tag == "PlayerUnit")
            {
                if (dist < minUnitDist)
                {
                    minUnitDist = dist;
                    bestUnit = id;
                }
            }
        }

        if (bestBase.HasValue) 
            return bestBase.Value;
        if (bestSeaMonster.HasValue) 
            return bestSeaMonster.Value;
        if (bestUnit.HasValue)
            return bestUnit.Value;

        //Fallback
        return candidates[0];
    }

    private void TryMoveIdle(int unitId)
    {
        EnemyUnitManager eum = EnemyUnitManager.Instance;
        if (!eum.CanUnitMove(unitId))
        {
            Debug.Log($"[DormantAI] Unit {unitId} just spawned, skip movement.");
            return;
        }
        var current = eum.GetUnitPosition(unitId);
        int moveRange = eum.GetUnitMoveRange(unitId);
        var candidates = AIPathFinder.GetReachableHexes(current, moveRange);
        candidates.RemoveAll(h => !MapManager.Instance.CanUnitStandHere(h));
        if (candidates.Count == 0) return;

        //Move toward origin as a simple heuristic
        Vector2Int origin = Vector2Int.zero;
        candidates.Sort((a, b) => AIPathFinder.GetHexDistance(a, origin).CompareTo(AIPathFinder.GetHexDistance(b, origin)));
        Vector2Int chosen = candidates[0];
        EventBus.Publish(new EnemyAIEvents.EnemyMoveRequestEvent(unitId, chosen));
    }

    //Temp - It's better to have player unit/base manager
    private GameObject FindObjectFromInstanceID(int id)
    {
        foreach (var obj in FindObjectsOfType<GameObject>())
        {
            if (obj.GetInstanceID() == id)
                return obj;
        }
        return null;
    }
}
