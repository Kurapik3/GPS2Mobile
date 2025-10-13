using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaMobileUII : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private Vector2 lastScreenSize = new Vector2(0, 0 );
    private ScreenOrientation lastOrietation = ScreenOrientation.AutoRotation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        FitInScreen();
    }

    private void Update()
    {
        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y
            || Screen.safeArea != lastSafeArea || Screen.orientation != lastOrietation)
        {
            lastScreenSize = new Vector2 (Screen.width, Screen.height);
            lastOrietation = Screen.orientation;
            lastSafeArea = Screen.safeArea;

            FitInScreen();
        }


    }

    private void FitInScreen()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }    
}

