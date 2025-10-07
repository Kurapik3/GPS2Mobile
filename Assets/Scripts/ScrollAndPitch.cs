using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch; 

public class ScrollAndPitch : MonoBehaviour
{
    [SerializeField] private Camera cam;
    private Plane plane;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        EnhancedTouchSupport.Enable(); // enable enhanced touch
    }

    private void Update()
    {
        var touches = Touch.activeTouches;
        if (touches.Count == 0) return;

        // Always update the plane
        plane.SetNormalAndPosition(transform.up, transform.position);

        // One finger drag
        if (touches.Count == 1)
        {
            var touch = touches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                var move = PlanePositionDelta(touch);
                if (move != Vector3.zero)
                    cam.transform.Translate(move, Space.World);
            }
        }

        // Two finger pinch zoom
        if (touches.Count >= 2)
        {
            var t1 = touches[0];
            var t2 = touches[1];

            var pos1 = PlanePosition(t1.screenPosition);
            var pos2 = PlanePosition(t2.screenPosition);

            var pos1b = PlanePosition(t1.screenPosition - t1.delta);
            var pos2b = PlanePosition(t2.screenPosition - t2.delta);

            var zoom = Vector3.Distance(pos1, pos2) / Vector3.Distance(pos1b, pos2b);

            if (zoom > 0 && zoom < 10f)
            {
                // midpoint between two touches
                Vector3 midpoint = (pos1 + pos2) * 0.5f;

                // direction from midpoint to camera
                Vector3 direction = cam.transform.position - midpoint;

                // clamp distance
                float minDistance = 5f;
                float maxDistance = 50f;

                float currentDistance = direction.magnitude * (1f / zoom);
                currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

                // move camera
                cam.transform.position = midpoint + direction.normalized * currentDistance;
            }
        }
    }

    private Vector3 PlanePositionDelta(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
    {
        if (touch.phase != UnityEngine.InputSystem.TouchPhase.Moved)
            return Vector3.zero;

        var rayBefore = cam.ScreenPointToRay(touch.screenPosition - touch.delta);
        var rayNow = cam.ScreenPointToRay(touch.screenPosition);

        if (plane.Raycast(rayBefore, out var enterBefore) && plane.Raycast(rayNow, out var enterNow))
            return rayBefore.GetPoint(enterBefore) - rayNow.GetPoint(enterNow);

        return Vector3.zero;
    }

    private Vector3 PlanePosition(Vector2 screenPos)
    {
        var rayNow = cam.ScreenPointToRay(screenPos);
        if (plane.Raycast(rayNow, out var enterNow))
            return rayNow.GetPoint(enterNow);

        return Vector3.zero;
    }
}
