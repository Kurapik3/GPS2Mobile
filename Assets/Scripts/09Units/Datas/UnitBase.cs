using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public abstract class UnitBase : MonoBehaviour
{
    [Header("Indicators")]
    [SerializeField] private GameObject rangeIndicatorPrefab;
    [SerializeField] private GameObject attackIndicatorPrefab;
    [HideInInspector] public bool IsInTurf = false;

    private List<GameObject> activeIndicators = new List<GameObject>();
    private List<GameObject> activeAttackIndicators = new List<GameObject>();
    private List<HexTile> tilesInAttackRange = new List<HexTile>();

    private List<HexTile> tilesInRange = new List<HexTile>();

    [Header("Unit Identity")]
    public int unitId = -1;

    [Header("Base Stats (Loaded from CSV)")]
    public string unitName;
    public int cost;
    public int range;
    public int movement;
    public int hp;
    public int attack;
    public bool isCombat;

    public bool isSelected = false;

    public HexTile currentTile;
    private Renderer rend;


    public bool hasMovedThisTurn = false;
    public bool HasAttackThisTurn  = false;

    [Header("Fog of War Settings")]
    [SerializeField] public int fogRevealRadius = 1;

    // ---- KENNETH'S ----
    private UnitHPDisplay hpDisplay;

    // --------------------

    [Header("Range Attack")]
    [SerializeField] private GameObject projectilePrefab;
    protected virtual void Start()
    {
        rend = GetComponent<Renderer>();
        UpdateSelectionVisual();

        if (UnitManager.Instance != null)
        {
            UnitManager.Instance.RegisterUnit(this);
        }

        // ---- KENNETH'S ----
        hpDisplay = GetComponent<UnitHPDisplay>();
        // --------------------
    }
    public virtual void Initialize(UnitData data, HexTile startingTile)
    {
        unitName = data.unitName;
        hp = data.hp;
        attack = data.attack;
        movement = data.movement;
        range = data.range;
        isCombat = data.isCombat;

        currentTile = startingTile;
        if (startingTile != null)
        {
            Vector3 pos = startingTile.transform.position;
            pos.y = 2f;
            transform.position = pos;
        }

    }

    public virtual void Attack(HexTile target)
    {
        if (currentTile == null || target == null)
        {
            Debug.LogWarning("Either attacker or target is not on a tile!");
            return;
        }

        int distance = HexDistance(currentTile.q, currentTile.r, target.q, target.r);

        // Check if target is within attack range
        if (distance > range)
        {
            Debug.Log($"{unitName} tried to attack {target.currentEnemyUnit.unitType}, but target is out of range! (distance: {distance}, range: {range})");
            return;
        }

        HideAttackIndicators();

        StartCoroutine(PerformAttack(target));

        EventBus.Publish(new ActionMadeEvent());
    }

    protected virtual IEnumerator PerformAttack(HexTile target)
    {
        //Default: Melee attack
        yield return PlayAttackAnimation(target, false);
    }

    protected IEnumerator PlayAttackAnimation(HexTile target, bool isRanged, int splashRadius = 0)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = target.transform.position + Vector3.up * 2f;

        if (!isRanged)
        {
            //Melee: dash and return
            yield return LerpPosition(transform, startPos, endPos, 0.2f);
            HitTarget(target, splashRadius);
            yield return LerpPosition(transform, endPos, startPos, 0.2f);
        }
        else
        {
            //Ranged: projectile arc
            Vector3 projStart = startPos + Vector3.up * 1.5f;
            Vector3 projEnd = endPos;
            GameObject projectile = Instantiate(projectilePrefab, projStart, Quaternion.identity);

            float time = 0f;
            float duration = 1f;

            while (time < 1f)
            {
                time += Time.deltaTime / duration;

                //Midpoint of the arc
                Vector3 mid = (projStart + projEnd) / 2 + Vector3.up * 2.5f;

                Vector3 m1 = Vector3.Lerp(projStart, mid, time);
                Vector3 m2 = Vector3.Lerp(mid, projEnd, time);
                Vector3 pos = Vector3.Lerp(m1, m2, time);

                projectile.transform.position = pos;
                projectile.transform.LookAt(projEnd);
                projectile.transform.Rotate(90f, 0f, 0f);

                yield return null;
            }

            Destroy(projectile);
            HitTarget(target, splashRadius);
        }
    }

    private IEnumerator LerpPosition(Transform t, Vector3 from, Vector3 to, float duration)
    {
        float time = 0f;
        Vector3 direction = to - from;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg; //Facing target direction
            t.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        while (time < 1f)
        {
            time += Time.deltaTime / duration;
            t.position = Vector3.Lerp(from, to, time);
            yield return null;
        }
    }

    protected GameObject GetTargetGameObject(HexTile tile)
    {
        if (tile == null) return null;

        if (tile.currentEnemyUnit != null) 
            return tile.currentEnemyUnit.gameObject;
        if (tile.currentEnemyBase != null) 
            return tile.currentEnemyBase.gameObject;
        if (tile.currentSeaMonster != null) 
            return tile.currentSeaMonster.gameObject;

        return null;
    }

    protected void HitTarget(HexTile target, int splashRadius = 0)
    {
        GameObject targetGO = GetTargetGameObject(target);
        if (targetGO != null)
        {
            Knockback(targetGO, this.transform.forward.normalized, () =>
            {
                ApplyDamage(target, attack, splashRadius); //Play animation first, then apply damage
            });
        }
        else
        {
            ApplyDamage(target, attack, splashRadius);
        }
    }

    private void Knockback(GameObject target, Vector3 direction, System.Action onComplete = null)
    {
        if (target == null)
            return;

        Vector3 hitPos = target.transform.position + direction.normalized * 0.3f;

        target.transform
        .DOMove(hitPos, 0.1f)
        .SetLoops(2, LoopType.Yoyo)
        .SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            onComplete?.Invoke(); //Wait until animation completed then apply damage
        });
    }

    private void ApplyDamage(HexTile target, int damage, int splashRadius = 0)
    {
        if (target.currentEnemyUnit == null && target.currentEnemyBase == null && target.currentSeaMonster == null)
        {
            return;
        }

        if (target.currentEnemyUnit != null)
        {
            target.currentEnemyUnit.TakeDamage(attack);
            //Debug.Log($"{unitName} attacked {target.currentEnemyUnit.unitType} for {attack} damage!");
            if (target.currentEnemyUnit.unitType == "Tanker")
            {
                this.TakeDamage(damage);
            }
            HasAttackThisTurn = false;
        }
        else if (target.currentEnemyBase != null)
        {
            target.currentEnemyBase.TakeDamage(attack);
            //Debug.Log($"{unitName} attacked {target.currentEnemyBase} for {attack} damage!");
            HasAttackThisTurn = false;
        }
        else if (target.currentSeaMonster != null)
        {
            target.currentSeaMonster.TakeDamage(attack);
            if(TechTree.Instance.IsHunterMask)
            {
                target.currentSeaMonster.TakeDamage(5);
            }
            //Debug.Log($"{unitName} attacked {target.currentSeaMonster.MonsterName} for {attack} damage!");
            HasAttackThisTurn = false;
        }
    }

    public virtual void TakeDamage(int amount)
    {

        hp -= amount;
        if (hp <= 0)
        {
            hp = 0;
            Die();
        }

        // ---- KENNETH'S ----
        if (hpDisplay != null)
        {
            hpDisplay.UpdateHPDisplay();
        }
        // --------------------

        Debug.Log($"{unitName} took {amount} damage. Remaining HP: {hp}");
        EventBus.Publish(new ActionMadeEvent());
    }

    protected virtual void Die()
    {
        Debug.Log($"{unitName} has died!");
        ManagerAudio.instance.PlaySFX("UnitDie");
        //Release current tile
        if (currentTile != null)
        {
            currentTile.SetOccupiedByUnit(false);
            currentTile.currentUnit = null;
        }
        HideRangeIndicators();

        if (UnitManager.Instance != null)
        {
            UnitManager.Instance.UnregisterUnit(unitId);
        }
        Destroy(gameObject);
        EventBus.Publish(new ActionMadeEvent());
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSelectionVisual();
        HideRangeIndicators();
        HideAttackIndicators();
        if (isSelected && !hasMovedThisTurn)
        {
            ShowRangeIndicators();
            EventBus.Publish(new SeaMonsterEvents.UnitSelectedEvent(this, selected));
        }
        else
        {
            HideRangeIndicators();
        }
    }
    private void UpdateSelectionVisual()
    {
        if (rend != null)
            rend.material.color = isSelected ? Color.yellow : Color.white;
    }

    public List<HexTile> GetAvailableTiles()
    {
        List<HexTile> tiles = new List<HexTile>();
        foreach (var tile in MapManager.Instance.GetTiles())
        {
            if (HexDistance(currentTile.q, currentTile.r, tile.q, tile.r) <= movement && tile.IsWalkableForAI() && !tile.IsFogged)
            {
                tiles.Add(tile);
            }
        }
        return tiles;
    }

    public virtual void TryMove(HexTile targetTile)
    {
        if (targetTile == null)
            return;

        int distance = HexDistance(currentTile.q, currentTile.r, targetTile.q, targetTile.r);
        bool ignoreTurtleWall = TechTree.Instance.IsMutualism;

        if (targetTile.currentSeaMonster != null && targetTile.currentSeaMonster.monsterName == "Turtle Wall")
        {
            MapManager.Instance.SetUnitOccupied(targetTile.HexCoords, false);
        }

        if (distance > movement)
        {
            Debug.Log($"{unitName} can't move that far! (Range: {movement}, Target: {distance})");
            return;
        }

        if (targetTile.IsOccupiedByUnit)
        {
            Debug.Log($"{unitName} can't move there. Target tile is blocked or occupied!");
            return;
        }
        if (!ignoreTurtleWall && targetTile.IsBlockedByTurtleWall)
        {
            Debug.LogWarning($"{unitName} cannot stop on TurtleWall tile.");
            return;
        }
        if (!hasMovedThisTurn)
        {
            Move(targetTile, ignoreTurtleWall);
        }
        else return;
    }

    public virtual void Move(HexTile targetTile, bool ignoreTurtleWall = false)
    {
        if (targetTile == null) return;

        //Release old tile
        if (currentTile != null)
        {
            currentTile.SetOccupiedByUnit(false);
            currentTile.currentUnit = null;
        }
        StartCoroutine(MoveUnitPath(targetTile, ignoreTurtleWall));
        //transform.position = targetTile.transform.position + Vector3.up * 2f; // optional y offset
        ManagerAudio.instance.PlaySFX("UnitMove");
        //Update new tile
        currentTile = targetTile;
        currentTile.SetOccupiedByUnit(true); //Occupied new tile
        targetTile.currentUnit = this;

        Debug.Log($"{unitName} moved to ({currentTile.q}, {currentTile.r})");
        hasMovedThisTurn = true;
        RevealNearbyFog(currentTile);
        EventBus.Publish(new ActionMadeEvent());
    }

    private IEnumerator MoveUnitPath(HexTile targetTile, bool ignoreTurtleWall)
    {
        List<Vector2Int> path = AIPathFinder.GetPath(currentTile.HexCoords, targetTile.HexCoords);
        if (path == null || path.Count == 0)
            yield break;

        if (path[0] != currentTile.HexCoords)
            path.Insert(0, currentTile.HexCoords);

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int prevHex = path[i - 1];
            Vector2Int currHex = path[i];

            HexTile tile = MapManager.Instance.GetTileAtHexPosition(currHex);
            if (tile == null)
                yield break;

            // If the last tile (target destination) is blocked by turtle wall
            // Skip movement even if have Mutualism tech unlocked
            if (i == path.Count - 1 && tile.IsBlockedByTurtleWall && !ignoreTurtleWall)
            {
                Debug.Log($"{unitName} cannot stop on TurtleWall tile at {currHex}");
                yield break;
            }

            yield return SmoothMove(prevHex, currHex);
        }
    }

    private IEnumerator SmoothMove(Vector2Int startHex, Vector2Int endHex)
    {

        Vector3 startPos = MapManager.Instance.HexToWorld(startHex);
        startPos.y += 2f;

        Vector3 endPos = MapManager.Instance.HexToWorld(endHex);
        endPos.y += 2f;

        Vector3 direction = endPos - startPos;
        direction.y = 0; //Ignore vertical move

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        //Face movement direction on Y axis
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        float t = 0f;
        float duration = 0.5f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
    }

    public virtual void ResetMove()
    {
        hasMovedThisTurn = false;
        HasAttackThisTurn = false;
    }

    protected void RevealNearbyFog(HexTile centerTile)
    {
        if (centerTile == null) return;

        List<HexTile> nearbyTiles = GetTilesInRange(centerTile.q, centerTile.r, fogRevealRadius);
        foreach (HexTile tile in nearbyTiles)
        {
            FogSystem fog = FindAnyObjectByType<FogSystem>();
            if (tile.fogInstance != null)
            {
                if (fog != null)
                {
                    fog.RevealTilesAround(centerTile.HexCoords, fogRevealRadius);
                }
            }
        }

        Debug.Log($"{unitName} revealed fog around tile ({centerTile.q}, {centerTile.r})");
    }

    protected List<HexTile> GetTilesInRange(int q, int r, int radius)
    {
        List<HexTile> tilesInRange = new List<HexTile>();
        foreach (HexTile tile in MapManager.Instance.GetTiles())
        {
            int dist = HexDistance(q, r, tile.q, tile.r);
            if (dist <= radius)
                tilesInRange.Add(tile);
        }
        return tilesInRange;
    }

    protected int HexDistance(int q1, int r1, int q2, int r2)
    {
        int dq = q2 - q1;
        int dr = r2 - r1;
        int ds = (-q2 - r2) - (-q1 - r1);
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(ds)) / 2;
    }

    // ----------------------------------------[Kenneth's Work]---------------------------------------- //
    public void ShowRangeIndicators()
    {
        HideRangeIndicators();

        if (rangeIndicatorPrefab == null)
        {
            Debug.LogWarning($"{unitName}: Range Indicator Prefab is not assigned!");
            return;
        }

        if (currentTile == null)
        {
            Debug.LogWarning($"{unitName}: Cannot show range - no current tile!");
            return;
        }

        CalculateTilesInRange();

        Debug.Log($"{unitName}: Showing {tilesInRange.Count} range indicators");

        // Spawn indicators
        foreach (var tile in tilesInRange)
        {
            if (tile == null) continue;

            Vector3 spawnPos = new Vector3(tile.transform.position.x, 2.0f, tile.transform.position.z);

            GameObject indicator = Instantiate(rangeIndicatorPrefab, spawnPos, Quaternion.Euler(90f, 0f, 0f));
            indicator.name = $"RangeIndicator{tile.q}{tile.r}";
            activeIndicators.Add(indicator);
        }
    }

    public void HideRangeIndicators()
    {
        foreach (var indicator in activeIndicators)
        {
            if (indicator != null)
                Destroy(indicator);
        }
        activeIndicators.Clear();
        tilesInRange.Clear();
    }

    private void CalculateTilesInRange()
    {
        tilesInRange.Clear();
        if (MapManager.Instance == null)
        {
            Debug.LogError("MapManager.Instance is null!");
            return;
        }

        // Use flood-fill algorithm to find reachable tiles
        Dictionary<Vector2Int, int> reachableTiles = new Dictionary<Vector2Int, int>();
        Queue<(HexTile tile, int distance)> queue = new Queue<(HexTile, int)>();

        // Start from current tile
        queue.Enqueue((currentTile, 0));
        reachableTiles[new Vector2Int(currentTile.q, currentTile.r)] = 0;

        Dictionary<Vector2Int, HexTile> allTiles = (Dictionary<Vector2Int, HexTile>)MapManager.Instance.GetAllTiles();

        while (queue.Count > 0)
        {
            var (currentCheckTile, currentDistance) = queue.Dequeue();

            // Don't expand beyond movement range
            if (currentDistance >= movement)
                continue;

            // Check all 6 neighbors of hex tile
            Vector2Int[] neighbors = GetHexNeighbors(currentCheckTile.q, currentCheckTile.r);

            foreach (var neighborCoord in neighbors)
            {
                bool ignoreTurtleWall = TechTree.Instance.IsMutualism;

                // Skip if already visited with shorter distance
                if (reachableTiles.ContainsKey(neighborCoord) && reachableTiles[neighborCoord] <= currentDistance + 1)
                    continue;

                // Check if tile exists in map
                if (!allTiles.TryGetValue(neighborCoord, out HexTile neighborTile))
                    continue;

                // Skip blocked or occupied tiles (they block path but we don't mark them as reachable)
                if (neighborTile.HasStructure || neighborTile.IsOccupiedByUnit)
                    continue;

                // Before unlock mutualism tech, skip tile that is blocked by turtle wall
                if (!ignoreTurtleWall && neighborTile.IsBlockedByTurtleWall)
                    continue;

                // Mark as reachable and add to queue for further exploration
                reachableTiles[neighborCoord] = currentDistance + 1;
                queue.Enqueue((neighborTile, currentDistance + 1));
            }
        }

        // Convert reachable tiles to list (excluding starting tile)
        foreach (var kvp in reachableTiles)
        {
            if (kvp.Value > 0) // Exclude the current tile (distance 0)
            {
                if (allTiles.TryGetValue(kvp.Key, out HexTile tile))
                {
                    tilesInRange.Add(tile);
                }
            }
        }
    }

    // Helper method to get the 6 neighboring hex coordinates
    private Vector2Int[] GetHexNeighbors(int q, int r)
    {
        return new Vector2Int[]
        {
        new Vector2Int(q + 1, r),     // East
        new Vector2Int(q - 1, r),     // West
        new Vector2Int(q, r + 1),     // Southeast
        new Vector2Int(q, r - 1),     // Northwest
        new Vector2Int(q + 1, r - 1), // Northeast
        new Vector2Int(q - 1, r + 1)  // Southwest
        };
    }

    private void OnDestroy()
    {
        HideRangeIndicators();
        HideAttackIndicators();
    }



    //Attack UI indictor 
    public void ShowAttackIndicators()
    {
        HideAttackIndicators();

        if (attackIndicatorPrefab == null)
        {
            Debug.LogWarning($"{unitName}: Attack Indicator Prefab not assigned!");
            return;
        }

        if (currentTile == null)
            return;

        CalculateTilesInAttackRange();

        foreach (var tile in tilesInAttackRange)
        {
            Vector3 pos = new Vector3(tile.transform.position.x, 2f, tile.transform.position.z);
            GameObject indicator = Instantiate(attackIndicatorPrefab, pos, Quaternion.Euler(90, 0, 0));
            indicator.name = $"AttackIndicator{tile.q}{tile.r}";
            activeAttackIndicators.Add(indicator);
        }
    }

    public void HideAttackIndicators()
    {
        foreach (var ind in activeAttackIndicators)
            if (ind != null)
                Destroy(ind);

        activeAttackIndicators.Clear();
        tilesInAttackRange.Clear();
    }

    private void CalculateTilesInAttackRange()
    {
        tilesInAttackRange.Clear();

        foreach (HexTile tile in MapManager.Instance.GetTiles())
        {
            int dist = HexDistance(currentTile.q, currentTile.r, tile.q, tile.r);

            if (dist <= range && dist > 0) // cannot attack itself
            {
                if (tile.currentEnemyUnit != null ||
                    tile.currentEnemyBase != null ||
                    tile.currentSeaMonster != null)
                {
                    tilesInAttackRange.Add(tile);
                }
            }
        }
    }

    public void SetPositionToTile(int q, int r)
    {
        if (MapManager.Instance.TryGetTile(new Vector2Int(q, r), out HexTile tile))
        {
            transform.position = tile.transform.position + Vector3.up * 2f;
            currentTile = tile;
        }
        else
        {
            Debug.LogWarning($"Tile ({q}, {r}) not found!");
        }
    }

}
