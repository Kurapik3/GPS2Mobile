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
                }
            }

            // Detect tile
            HexTile tile = hit.collider.GetComponentInParent<HexTile>();
            if (tile != null)
            {

                if (selectedMonster != null && selectedMonster.State == SeaMonsterState.Tamed)
                {
                    selectedMonster.OnPlayerClickTile(tile);
                    DeselectMonster();
                    return;
                }
            }
            DeselectMonster();
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
}
