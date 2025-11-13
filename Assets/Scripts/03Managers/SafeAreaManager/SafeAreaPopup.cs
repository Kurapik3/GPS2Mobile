using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaPopup : MonoBehaviour
{
    [Header("Popup Position")]
    [SerializeField] private PopupPosition popupPosition = PopupPosition.Bottom;
    [SerializeField] private bool centerHorizontally = true;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    [Header("Safe Area & Scaling")]
    [SerializeField] private bool respectSafeArea = true;
    [SerializeField] private bool autoScale = true;
    [SerializeField] private float maxScale = 1f;
    [SerializeField] private float minScale = 0.6f;
    [SerializeField] private float edgePadding = 50f;

    [Header("Offset Adjustments")]
    [SerializeField] private Vector2 customOffset = Vector2.zero;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = false;
    [SerializeField] private bool debugLog = false;

    private RectTransform rectTransform;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Vector2 showPosition;
    private Vector2 hidePosition;
    private Tween currentTween;
    private bool isInitialized = false;

    public enum PopupPosition
    {
        Top,
        Bottom,
        Left,
        Right,
        Center,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("SafeAreaPopup: No Canvas found in parent hierarchy!");
            return;
        }

        canvasRect = canvas.GetComponent<RectTransform>();
        Initialize();
    }

    private void Start()
    {
        rectTransform.anchoredPosition = hidePosition;

        if (debugLog)
        {
            Debug.Log($"Popup initialized - Show: {showPosition}, Hide: {hidePosition}, Scale: {rectTransform.localScale}");
        }
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        if (isInitialized || canvas == null) return;

        if (autoScale)
        {
            ApplyAutoScale();
        }

        CalculatePositions();
        isInitialized = true;
    }

    private void CalculatePositions()
    {
        // Get safe area
        Rect safeArea = respectSafeArea ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);

        // Get canvas size in its own space
        Vector2 canvasSize = canvasRect.rect.size;

        // Convert screen safe area to canvas local space
        Vector2 safeMin = ScreenToCanvasPosition(new Vector2(safeArea.xMin, safeArea.yMin));
        Vector2 safeMax = ScreenToCanvasPosition(new Vector2(safeArea.xMax, safeArea.yMax));

        // Get popup size (accounting for current scale)
        Vector2 popupSize = rectTransform.rect.size * rectTransform.localScale.x;

        if (debugLog)
        {
            Debug.Log($"Canvas Size: {canvasSize}");
            Debug.Log($"Safe Area (Screen): {safeArea}");
            Debug.Log($"Safe Area (Canvas): Min={safeMin}, Max={safeMax}");
            Debug.Log($"Popup Size (scaled): {popupSize}");
        }

        // Calculate positions based on popup position setting
        switch (popupPosition)
        {
            case PopupPosition.Bottom:
                showPosition = new Vector2(
                    centerHorizontally ? 0 : (safeMin.x + safeMax.x) / 2f,
                    safeMin.y + popupSize.y / 2f + edgePadding
                );
                hidePosition = new Vector2(showPosition.x, safeMin.y - popupSize.y - 100f);
                break;

            case PopupPosition.Top:
                showPosition = new Vector2(
                    centerHorizontally ? 0 : (safeMin.x + safeMax.x) / 2f,
                    safeMax.y - popupSize.y / 2f - edgePadding
                );
                hidePosition = new Vector2(showPosition.x, safeMax.y + popupSize.y + 100f);
                break;

            case PopupPosition.Left:
                showPosition = new Vector2(
                    safeMin.x + popupSize.x / 2f + edgePadding,
                    centerHorizontally ? 0 : (safeMin.y + safeMax.y) / 2f
                );
                hidePosition = new Vector2(safeMin.x - popupSize.x - 100f, showPosition.y);
                break;

            case PopupPosition.Right:
                showPosition = new Vector2(
                    safeMax.x - popupSize.x / 2f - edgePadding,
                    centerHorizontally ? 0 : (safeMin.y + safeMax.y) / 2f
                );
                hidePosition = new Vector2(safeMax.x + popupSize.x + 100f, showPosition.y);
                break;

            case PopupPosition.Center:
                showPosition = Vector2.zero;
                hidePosition = new Vector2(0, safeMin.y - popupSize.y - 100f);
                break;

            case PopupPosition.BottomLeft:
                showPosition = new Vector2(
                    safeMin.x + popupSize.x / 2f + edgePadding,
                    safeMin.y + popupSize.y / 2f + edgePadding
                );
                hidePosition = new Vector2(safeMin.x - popupSize.x - 100f, safeMin.y - popupSize.y - 100f);
                break;

            case PopupPosition.BottomRight:
                showPosition = new Vector2(
                    safeMax.x - popupSize.x / 2f - edgePadding,
                    safeMin.y + popupSize.y / 2f + edgePadding
                );
                hidePosition = new Vector2(safeMax.x + popupSize.x + 100f, safeMin.y - popupSize.y - 100f);
                break;

            case PopupPosition.TopLeft:
                showPosition = new Vector2(
                    safeMin.x + popupSize.x / 2f + edgePadding,
                    safeMax.y - popupSize.y / 2f - edgePadding
                );
                hidePosition = new Vector2(safeMin.x - popupSize.x - 100f, safeMax.y + popupSize.y + 100f);
                break;

            case PopupPosition.TopRight:
                showPosition = new Vector2(
                    safeMax.x - popupSize.x / 2f - edgePadding,
                    safeMax.y - popupSize.y / 2f - edgePadding
                );
                hidePosition = new Vector2(safeMax.x + popupSize.x + 100f, safeMax.y + popupSize.y + 100f);
                break;
        }

        // Apply custom offset
        showPosition += customOffset;

        if (debugLog)
        {
            Debug.Log($"Final Show Position: {showPosition}");
            Debug.Log($"Final Hide Position: {hidePosition}");
        }
    }

    private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
    {
        // Convert screen position to canvas local position
        Vector2 viewportPosition = new Vector2(
            screenPosition.x / Screen.width,
            screenPosition.y / Screen.height
        );

        Vector2 canvasSize = canvasRect.rect.size;

        return new Vector2(
            (viewportPosition.x - 0.5f) * canvasSize.x,
            (viewportPosition.y - 0.5f) * canvasSize.y
        );
    }

    private void ApplyAutoScale()
    {
        Rect safeArea = respectSafeArea ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);

        // Get canvas size
        Vector2 canvasSize = canvasRect.rect.size;

        // Calculate safe area size in canvas space
        float safeWidthRatio = safeArea.width / Screen.width;
        float safeHeightRatio = safeArea.height / Screen.height;

        float availableWidth = canvasSize.x * safeWidthRatio - (edgePadding * 2f);
        float availableHeight = canvasSize.y * safeHeightRatio - (edgePadding * 2f);

        // Get original size
        Vector2 originalSize = rectTransform.rect.size;

        // Calculate scale needed to fit
        float scaleX = availableWidth / originalSize.x;
        float scaleY = availableHeight / originalSize.y;

        // Use smaller scale to ensure fit
        float scale = Mathf.Min(scaleX, scaleY);

        // Clamp to min/max
        scale = Mathf.Clamp(scale, minScale, maxScale);

        // Apply scale
        rectTransform.localScale = Vector3.one * scale;

        if (debugLog)
        {
            Debug.Log($"Auto Scale Applied: {scale} (scaleX: {scaleX}, scaleY: {scaleY})");
        }
    }

    public void Show(bool instant = false)
    {
        currentTween?.Kill();

        // Recalculate in case screen changed
        if (autoScale)
        {
            ApplyAutoScale();
        }

        CalculatePositions();

        gameObject.SetActive(true);

        if (instant)
        {
            rectTransform.anchoredPosition = showPosition;
        }
        else
        {
            currentTween = rectTransform.DOAnchorPos(showPosition, animationDuration)
                .SetEase(showEase)
                .SetUpdate(true);
        }
    }

    public void Hide(bool instant = false, bool disableAfter = true)
    {
        currentTween?.Kill();

        if (instant)
        {
            rectTransform.anchoredPosition = hidePosition;
            if (disableAfter)
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            currentTween = rectTransform.DOAnchorPos(hidePosition, animationDuration)
                .SetEase(hideEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (disableAfter)
                    {
                        gameObject.SetActive(false);
                    }
                });
        }
    }

    public void Toggle()
    {
        if (IsVisible())
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public bool IsVisible()
    {
        return gameObject.activeSelf &&
               Vector2.Distance(rectTransform.anchoredPosition, showPosition) < 10f;
    }

    public void RecalculatePositions()
    {
        isInitialized = false;
        Initialize();
    }

    public void SetPosition(PopupPosition position)
    {
        popupPosition = position;
        RecalculatePositions();
    }

    public void SetEdgePadding(float padding)
    {
        edgePadding = padding;
        RecalculatePositions();
    }

    public void SetAnimationDuration(float duration)
    {
        animationDuration = duration;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;
        if (canvas == null || rectTransform == null) return;

        // Draw safe area bounds
        Rect safeArea = respectSafeArea ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);
        Vector2 safeMin = ScreenToCanvasPosition(new Vector2(safeArea.xMin, safeArea.yMin));
        Vector2 safeMax = ScreenToCanvasPosition(new Vector2(safeArea.xMax, safeArea.yMax));

        Gizmos.color = Color.yellow;
        Vector3 center = canvas.transform.TransformPoint((safeMin + safeMax) / 2f);
        Vector3 size = new Vector3(safeMax.x - safeMin.x, safeMax.y - safeMin.y, 0);
        Gizmos.DrawWireCube(center, size);

        // Draw show position
        Gizmos.color = Color.green;
        Vector3 worldShowPos = canvas.transform.TransformPoint(showPosition);
        Gizmos.DrawWireSphere(worldShowPos, 20f);

        // Draw hide position
        Gizmos.color = Color.red;
        Vector3 worldHidePos = canvas.transform.TransformPoint(hidePosition);
        Gizmos.DrawWireSphere(worldHidePos, 20f);
    }

    private void OnValidate()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }
#endif
}