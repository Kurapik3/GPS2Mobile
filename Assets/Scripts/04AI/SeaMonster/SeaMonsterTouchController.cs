using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class SeaMonsterTouchController : MonoBehaviour
{
    [SerializeField] private Camera cam;
    private SeaMonsterBase selectedMonster;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        EnhancedTouchSupport.Enable();
    }

    public bool HasSelectedMonster()
    {
        return selectedMonster != null;
    }

    private void SelectMonster(SeaMonsterBase monster)
    {
        DeselectMonster();
        selectedMonster = monster;
        selectedMonster.SetSelected(true);
        if (TileSelector.CurrentTile != null)
        {
            EventBus.Publish(new TileDeselectedEvent(TileSelector.CurrentTile));
        }
    }

    private void DeselectMonster()
    {
        if (selectedMonster != null)
            selectedMonster.SetSelected(false);
        selectedMonster = null;
    }

    public void TrySelectMonster(SeaMonsterBase monster)
    {
        if (monster.State != SeaMonsterState.Tamed) return;

        SelectMonster(monster);
    }

    public void MoveSelectedMonster(HexTile tile)
    {
        if (selectedMonster != null && selectedMonster.State == SeaMonsterState.Tamed)
        {
            selectedMonster.OnPlayerClickTile(tile);
            DeselectMonster();
        }
    }

}
