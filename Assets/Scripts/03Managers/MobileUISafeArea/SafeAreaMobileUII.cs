//using UnityEngine;
//using UnityEngine.UI;

//[RequireComponent(typeof(RectTransform))]
//public class SafeAreaMobileUII : MonoBehaviour
//{
//    private RectTransform canvasRectTransform;
//    private DrivenRectTransformTracker tracker;
//    private Rect lastSafeAreaZone;

//    private void OnEnable()
//    {
//        canvasRectTransform = GetComponent<RectTransform>();
//        tracker.Add(this, canvasRectTransform, DrivenTransformProperties.All);
//        Canvas.willRenderCanvases += UpdateSafeArea;
//    }

//    private void OnDisable()
//    {
//        tracker.Clear();
//        Canvas.willRenderCanvases -= UpdateSafeArea;
//    }

//    private void UpdateSafeArea()
//    {
//        Rect safeAreaZone = Screen.safeArea;

//        if (safeAreaZone.Equals(lastSafeAreaZone)) return;

//        lastSafeAreaZone = safeAreaZone;

//        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

//        Vector2 anchorMin = safeAreaZone.min / screenSize;
//        Vector2 acnhorMax = safeAreaZone.max / screenSize;
//        canvasRectTransform.anchorMin = anchorMin;
//        canvasRectTransform.anchorMax = acnhorMax;

//        canvasRectTransform.localScale = Vector2.one;
//    }
//}

using UnityEngine;
using UnityEngine.UI;
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))] 
public class SafeAreaMobileUII : MonoBehaviour 
{ 
    private RectTransform rectTransform;
    private DrivenRectTransformTracker tracker; 
    private Rect lastSafeArea = new Rect(0, 0, 0, 0); 
    private Vector2 lastScreenSize = new Vector2(0, 0);
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation; 
    private void Awake() 
    { 
        rectTransform = GetComponent<RectTransform>(); 
        ApplySafeArea(); 
    } 
    
    private void OnEnable() 
    { 
        tracker.Add(this, rectTransform, DrivenTransformProperties.All); 
        Canvas.willRenderCanvases += OnCanvasWillRender; 
        ApplySafeArea();
    } 
    
    private void OnDisable() 
    { 
        tracker.Clear(); Canvas.willRenderCanvases -= OnCanvasWillRender;
    } 

    private void Update()
    { 
        // Recalculate if anything changes
      if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y || !Screen.safeArea.Equals(lastSafeArea) || Screen.orientation != lastOrientation) { lastScreenSize = new Vector2(Screen.width, Screen.height); 
            lastOrientation = Screen.orientation; lastSafeArea = Screen.safeArea; 
            ApplySafeArea();
        } 
    }
    
    private void OnCanvasWillRender() 
    { 
        // Optional: ensure fit before each render (for dynamic layout)
        ApplySafeArea(); 
    } 
    
    private void ApplySafeArea() 
    { 
        Rect safeArea = Screen.safeArea;
        
        // Convert pixel values to normalized anchor positions
        Vector2 anchorMin = safeArea.position; 
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width; anchorMin.y /= Screen.height; 
        anchorMax.x /= Screen.width; anchorMax.y /= Screen.height; 
        rectTransform.anchorMin = anchorMin; rectTransform.anchorMax = anchorMax; 
        
        // Optional: reset scale to prevent distortion
        rectTransform.localScale = Vector3.one; 
        
        // Update cached values
        lastSafeArea = safeArea; lastScreenSize = new Vector2(Screen.width, Screen.height); lastOrientation = Screen.orientation; 
    } 
}