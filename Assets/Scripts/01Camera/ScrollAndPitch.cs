using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ScrollAndPitch : MonoBehaviour
{
    [SerializeField] private Camera cam;

    [Header("Camera Movement Limits")]
    [SerializeField] private float minX = -22f;
    [SerializeField] private float maxX = 4f;
    [SerializeField] private float minZ = -9f;
    [SerializeField] private float maxZ = 8f;

    [Header("Camera Zoom Limits")]
    [SerializeField] private float minZoomDistance = 5f;   
    [SerializeField] private float maxZoomDistance = 50f;
    [SerializeField] private float zoomSpeed = 0.5f;

    private Plane plane;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        EnhancedTouchSupport.Enable();
    }

    private void Update()
    {
        var touches = Touch.activeTouches;
        if (touches.Count == 0) return;

        plane.SetNormalAndPosition(transform.up, transform.position);

        if (touches.Count == 1)
        {
            var touch = touches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && touch.delta.magnitude > 10f)
            {
                var move = PlanePositionDelta(touch);
                if (move != Vector3.zero)
                {
                    cam.transform.Translate(move, Space.World);
                    ClampCameraPosition(); 
                }
            }
        }

        if (touches.Count >= 2)
        {
            var t1 = touches[0];
            var t2 = touches[1];

            var pos1 = PlanePosition(t1.screenPosition);
            var pos2 = PlanePosition(t2.screenPosition);
            var pos1b = PlanePosition(t1.screenPosition - t1.delta);
            var pos2b = PlanePosition(t2.screenPosition - t2.delta);

            float prevDistance = Vector3.Distance(pos1b, pos2b);
            float currentDistance = Vector3.Distance(pos1, pos2);
            float zoomFactor = (currentDistance - prevDistance) * zoomSpeed;

            Vector3 forward = cam.transform.forward;
            Vector3 newPos = cam.transform.position + forward * zoomFactor;

            float currentDistanceFromGround = Vector3.Distance(newPos, transform.position);
            if (currentDistanceFromGround >= minZoomDistance && currentDistanceFromGround <= maxZoomDistance)
            {
                cam.transform.position = newPos;
            }

            ClampCameraPosition();
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

    private void ClampCameraPosition()
    {
        Vector3 pos = cam.transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        cam.transform.position = pos;
    }
}
