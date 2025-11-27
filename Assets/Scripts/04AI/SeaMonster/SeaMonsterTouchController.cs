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

    private void Update()
    {
        if (Touch.activeTouches.Count == 0)
            return;

        var touch = Touch.activeTouches[0];
        Debug.Log($"[Touch] Phase = {touch.phase}, Position = {touch.screenPosition}");

        if (touch.phase != UnityEngine.InputSystem.TouchPhase.Ended)
            return;

        Ray ray = cam.ScreenPointToRay(touch.screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Detect sea monster
            SeaMonsterBase monster = hit.collider.GetComponentInParent<SeaMonsterBase>();
            if (monster != null)
            {
                if (monster.State == SeaMonsterState.Tamed)
                {
                    SelectMonster(monster);
                    return;
                }
                else
                {
                    Debug.Log("[Touch] Monster is UNTAMED, cannot select.");
                    return;
                }
            }

            // Detect tile
            HexTile tile = hit.collider.GetComponentInParent<HexTile>();
            if (tile != null)
            {
                EventBus.Publish(new TileSelectedEvent(tile));
                if (selectedMonster != null && selectedMonster.State == SeaMonsterState.Tamed)
                {
                    selectedMonster.OnPlayerClickTile(tile);
                    DeselectMonster();
                    return;
                }
                //else
                //{
                //    Debug.Log("[Touch] Tile clicked but no tamed monster is selected.");
                //}
                return;
            }
            return;
            //Debug.Log("[Touch] Clicked something else, DeselectMonster()");
            //if (TileSelector.CurrentTile != null)
            //{
            //    EventBus.Publish(new TileDeselectedEvent(TileSelector.CurrentTile));
            //}
            DeselectMonster();
        }
        //else
        //{
        //    Debug.Log("[Touch] Raycast hit NOTHING.");
        //    if (TileSelector.CurrentTile != null)
        //    {
        //        EventBus.Publish(new TileDeselectedEvent(TileSelector.CurrentTile));
        //    }
        //}

        if (TileSelector.CurrentTile != null)
        {
            EventBus.Publish(new TileDeselectedEvent(TileSelector.CurrentTile));
        }

        DeselectMonster();
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
}
