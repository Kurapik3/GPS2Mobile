using UnityEngine;

public class SafeAreaUIScaler : MonoBehaviour
{
    [Header("Design Settings")]
    public Vector2 referenceResolution = new Vector2(800, 600); 
    public bool matchWidthOrHeight = true; 

    private Canvas canvas;
    private RectTransform rectTransform;
    private Rect lastSafeArea;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        rectTransform = canvas.GetComponent<RectTransform>();
        ApplyScaling();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea)
            ApplyScaling();
    }

    void ApplyScaling()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        float scaleX = safeArea.width / referenceResolution.x;
        float scaleY = safeArea.height / referenceResolution.y;

        float finalScale = Mathf.Min(scaleX, scaleY);

        if (matchWidthOrHeight)
            finalScale = scaleX;
        else
            finalScale = scaleY;

        rectTransform.localScale = new Vector3(finalScale, finalScale, 1f);

        Vector2 offset = new Vector2(
            (safeArea.x + safeArea.width / 2f) - (Screen.width / 2f),
            (safeArea.y + safeArea.height / 2f) - (Screen.height / 2f)
        );

        rectTransform.anchoredPosition = offset / finalScale;
    }
}
