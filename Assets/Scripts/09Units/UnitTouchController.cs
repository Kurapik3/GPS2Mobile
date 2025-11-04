using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System.Collections.Generic;

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
            HexTile tile = hit.collider.GetComponentInParent<HexTile>();
            if (tile != null && selectedUnit != null && selectedUnit.GetAvailableTiles().Contains(tile))
            {
                selectedUnit.TryMove(tile);
                DeselectUnit();
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
    }

    private void DeselectUnit()
    {
        if (selectedUnit != null)
            selectedUnit.SetSelected(false);

        selectedUnit = null;
    }
}
