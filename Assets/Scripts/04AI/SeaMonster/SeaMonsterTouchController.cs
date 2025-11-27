using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class SeaMonsterTouchController : MonoBehaviour
{
    [SerializeField] private Camera cam;
    private SeaMonsterBase selectedMonster;

    // Kenneth's
    [SerializeField] private PopUpManager popupManager;
    //----------

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
                    //ShowCreaturePopup(monster);
                    SelectMonster(monster);
                    return;

                }
                else
                {
                    Debug.Log("[Touch] Monster is UNTAMED, cannot select.");
                }
            }

            // Detect tile
            HexTile tile = hit.collider.GetComponentInParent<HexTile>();
            if (tile != null /* Kenneth's ->*/ && selectedMonster != null && selectedMonster.State == SeaMonsterState.Tamed)
            {

                if (selectedMonster != null && selectedMonster.State == SeaMonsterState.Tamed)
                {
                    selectedMonster.OnPlayerClickTile(tile);
                    DeselectMonster();
                    //popupManager?.HidePopup();
                    return;
                }
                else
                {
                    Debug.Log("[Touch] Tile clicked but no tamed monster is selected.");
                }
            }

            Debug.Log("[Touch] Clicked something else, DeselectMonster()");
            DeselectMonster();
            //popupManager?.HidePopup();
        }
        else
        {
            Debug.Log("[Touch] Raycast hit NOTHING.");

            //popupManager?.HidePopup();
        }
    }

    private void SelectMonster(SeaMonsterBase monster)
    {
        DeselectMonster();
        selectedMonster = monster;
        selectedMonster.SetSelected(true);
    }

    private void DeselectMonster()
    {
        if (selectedMonster != null)
            selectedMonster.SetSelected(false);
        selectedMonster = null;
    }

    // Kenneth's
    //private void ShowCreaturePopup(SeaMonsterBase monster)
    //{
    //    InteractableObject interactable = monster.GetComponent<InteractableObject>();
    //    if (interactable?.objectData != null)
    //    {
    //        popupManager?.ShowPopup(interactable.objectData);
    //    }
    //    else
    //    {
    //        Debug.LogError($"No InteractableObject on {monster.name}");
    //    }
    //}
}
    //----------

