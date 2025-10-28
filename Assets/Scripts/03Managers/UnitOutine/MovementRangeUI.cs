using UnityEngine;

/// <summary>
/// Listens to unit selection events and triggers the unit's own range indicator methods.
/// This is a lightweight coordinator that uses the UnitBase's ShowRangeIndicators/HideRangeIndicators.
/// </summary>
public class UnitMovementRangeUI : MonoBehaviour
{
    private UnitBase currentSelectedUnit = null;

    private void OnEnable()
    {
        // Subscribe to unit selection events
        EventBus.Subscribe<UnitSelectionEvents.UnitSelectedEvent>(OnUnitSelected);
        EventBus.Subscribe<UnitSelectionEvents.UnitDeselectedEvent>(OnUnitDeselected);
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        EventBus.Unsubscribe<UnitSelectionEvents.UnitSelectedEvent>(OnUnitSelected);
        EventBus.Unsubscribe<UnitSelectionEvents.UnitDeselectedEvent>(OnUnitDeselected);

        // Clean up any active indicators
        if (currentSelectedUnit != null)
        {
            currentSelectedUnit.HideRangeIndicators();
        }
    }

    private void OnUnitSelected(UnitSelectionEvents.UnitSelectedEvent evt)
    {
        Debug.Log($"=== Unit Selected: {evt.unit.unitName} ===");

        // Hide previous unit's indicators if any
        if (currentSelectedUnit != null)
        {
            currentSelectedUnit.HideRangeIndicators();
        }

        currentSelectedUnit = evt.unit;

        if (currentSelectedUnit == null)
        {
            Debug.LogError("Selected unit is NULL!");
            return;
        }

        // Show range indicators for the selected unit
        currentSelectedUnit.ShowRangeIndicators();
    }

    private void OnUnitDeselected(UnitSelectionEvents.UnitDeselectedEvent evt)
    {
        Debug.Log($"=== Unit Deselected: {evt.unit?.unitName} ===");

        // Hide the indicators
        if (evt.unit != null)
        {
            evt.unit.HideRangeIndicators();
        }

        currentSelectedUnit = null;
    }
}

public static class UnitSelectionEvents
{
    public class UnitSelectedEvent
    {
        public UnitBase unit;
        public UnitSelectedEvent(UnitBase unit) { this.unit = unit; }
    }

    public class UnitDeselectedEvent
    {
        public UnitBase unit;
        public UnitDeselectedEvent(UnitBase unit) { this.unit = unit; }
    }
}