using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class UnitTouchController : MonoBehaviour
{
    [SerializeField] private Camera cam;
    private UnitBase selectedUnit;
    private List<HexTile> highlightedTiles = new List<HexTile>();

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        EnhancedTouchSupport.Enable();
    }

    private void Update()
    {
        if (Touch.activeTouches.Count == 0) return;

        var touch = Touch.activeTouches[0];
        if (touch.phase != UnityEngine.InputSystem.TouchPhase.Ended) return;

        Ray ray = cam.ScreenPointToRay(touch.screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Detect unit
            UnitBase unit = hit.collider.GetComponentInParent<UnitBase>();
            if (unit != null)
            {
                SelectUnit(unit);
                return;
            }

            // Detect tile
            //HexTile tile = hit.collider.GetComponentInParent<HexTile>();
            if (selectedUnit != null && hit.collider.GetComponentInParent<HexTile>() is HexTile tile)
            {
                if (tile.currentEnemyUnit != null || tile.currentEnemyBase != null || tile.currentSeaMonster != null)
                {
                    Debug.Log($"Attacking target on tile ({tile.q},{tile.r})");
                    selectedUnit.Attack(tile); //For attack unit
                    DeselectUnit();
                    return;
                }

                if (selectedUnit.GetAvailableTiles().Contains(tile))
                {
                    selectedUnit.TryMove(tile);
                    DeselectUnit();
                    return;
                }

                Debug.Log("Tile not reachable or blocked!");
                return;
            }

            // Deselect if tapped elsewhere
            DeselectUnit();
        }
    }

    private void SelectUnit(UnitBase unit)
    {
        DeselectUnit();
        selectedUnit = unit;
        selectedUnit.SetSelected(true);

        selectedUnit.ShowAttackIndicators();
    }

    private void DeselectUnit()
    {
        if (selectedUnit != null)
        {
            selectedUnit.SetSelected(false);
            selectedUnit.HideAttackIndicators();
        }

        selectedUnit = null;
    }
}
