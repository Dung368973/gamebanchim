using UnityEngine;

/// <summary>
/// Dynamically adapts RectTransform anchors to Screen.safeArea,
/// shielding UI elements from camera notches, display cutouts, and OS home indicators.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaHandler : MonoBehaviour
{
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private bool simulateInEditor = false;
    [SerializeField] private Rect simulatedSafeArea = new Rect(0, 80, 1080, 1760);

    private Rect _lastSafeArea = Rect.zero;
    private Vector2Int _lastScreenSize = Vector2Int.zero;
    private ScreenOrientation _lastOrientation = (ScreenOrientation)(-1);

    public RectTransform TargetRect => targetRect;

    private void Awake()
    {
        if (targetRect == null)
        {
            targetRect = GetComponent<RectTransform>();
        }
        ApplySafeArea();
    }

    private void Update()
    {
        Rect currentSafeArea = GetCurrentSafeArea();
        Vector2Int currentScreen = new Vector2Int(Screen.width, Screen.height);

        if (currentSafeArea != _lastSafeArea ||
            currentScreen != _lastScreenSize ||
            Screen.orientation != _lastOrientation)
        {
            ApplySafeArea();
        }
    }

    /// <summary>
    /// Recalculates and assigns normalized anchor boundaries to target RectTransform.
    /// </summary>
    public void ApplySafeArea()
    {
        if (targetRect == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safeArea = GetCurrentSafeArea();
        var (anchorMin, anchorMax) = CalculateAnchors(safeArea, Screen.width, Screen.height);

        targetRect.anchorMin = anchorMin;
        targetRect.anchorMax = anchorMax;
        targetRect.offsetMin = Vector2.zero;
        targetRect.offsetMax = Vector2.zero;

        _lastSafeArea = safeArea;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        _lastOrientation = Screen.orientation;
    }

    /// <summary>
    /// Pure mathematical helper converting pixel safeArea and screen dimensions to normalized anchors.
    /// </summary>
    public static (Vector2 anchorMin, Vector2 anchorMax) CalculateAnchors(Rect safeArea, float screenW, float screenH)
    {
        if (screenW <= 0f || screenH <= 0f)
        {
            return (Vector2.zero, Vector2.one);
        }

        Vector2 min = new Vector2(
            Mathf.Clamp01(safeArea.xMin / screenW),
            Mathf.Clamp01(safeArea.yMin / screenH)
        );

        Vector2 max = new Vector2(
            Mathf.Clamp01(safeArea.xMax / screenW),
            Mathf.Clamp01(safeArea.yMax / screenH)
        );

        return (min, max);
    }

    private Rect GetCurrentSafeArea()
    {
        if (Application.isEditor && simulateInEditor)
        {
            return simulatedSafeArea;
        }
        return Screen.safeArea;
    }
}
