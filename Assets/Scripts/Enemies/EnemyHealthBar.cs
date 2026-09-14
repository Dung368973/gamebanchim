using UnityEngine;

/// <summary>
/// World-space floating 2D health bar displayed above heavy/boss enemies.
/// Uses self-contained procedural sprites and left-pivoted scaling for zero-dependency rendering.
/// Reflects CurrentHealth / MaxHealth ratio smoothly.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(1.2f, 0.15f);
    [SerializeField] private Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
    [SerializeField] private Color fillColor = new Color(0.2f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color lowHealthColor = new Color(0.95f, 0.25f, 0.25f, 1f);

    [Header("Components")]
    [SerializeField] private Transform backgroundTransform;
    [SerializeField] private Transform fillTransform;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer fillRenderer;

    private float _healthRatio = 1f;
    private Transform _targetTransform;

    public float HealthRatio => _healthRatio;
    public Vector3 Offset { get => offset; set => offset = value; }

    private void Awake()
    {
        InitializeBarVisuals();
    }

    private void LateUpdate()
    {
        if (_targetTransform != null)
        {
            transform.position = _targetTransform.position + offset;
        }
    }

    /// <summary>
    /// Attaches this health bar to follow a specific target transform in world space.
    /// </summary>
    public void SetTarget(Transform target)
    {
        _targetTransform = target;
        if (_targetTransform != null)
        {
            transform.position = _targetTransform.position + offset;
        }
    }

    /// <summary>
    /// Updates the health ratio and adjusts visual scale and color.
    /// </summary>
    public void UpdateHealth(int current, int max)
    {
        if (max <= 0)
        {
            SetNormalized(0f);
            return;
        }

        float ratio = Mathf.Clamp01((float)current / max);
        SetNormalized(ratio);
    }

    /// <summary>
    /// Directly sets the normalized health ratio [0, 1].
    /// </summary>
    public void SetNormalized(float normalized)
    {
        _healthRatio = Mathf.Clamp01(normalized);

        if (fillTransform != null)
        {
            fillTransform.localScale = new Vector3(_healthRatio, 1f, 1f);
        }

        if (fillRenderer != null)
        {
            // Tint from green to yellow to red as health drops
            fillRenderer.color = Color.Lerp(lowHealthColor, fillColor, _healthRatio);
        }

        // Auto-hide when health reaches zero
        if (_healthRatio <= 0f)
        {
            SetVisible(false);
        }
        else
        {
            SetVisible(true);
        }
    }

    /// <summary>
    /// Toggles visibility of the health bar.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (backgroundRenderer != null) backgroundRenderer.enabled = visible;
        if (fillRenderer != null) fillRenderer.enabled = visible;
    }

    /// <summary>
    /// Generates self-contained background and fill sprite renderers if not pre-configured.
    /// </summary>
    private void InitializeBarVisuals()
    {
        if (backgroundRenderer != null && fillRenderer != null) return;

        // Create 1x1 white texture for 9-slice / scaled rendering
        Texture2D pixelTex = Texture2D.whiteTexture;

        // 1. Background Bar (Centered pivot)
        if (backgroundTransform == null)
        {
            GameObject bgObj = new GameObject("HealthBar_BG");
            bgObj.transform.SetParent(transform, false);
            backgroundTransform = bgObj.transform;

            backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = Sprite.Create(pixelTex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            backgroundRenderer.color = backgroundColor;
            backgroundRenderer.sortingOrder = 10;

            backgroundTransform.localScale = new Vector3(barSize.x * 25f, barSize.y * 25f, 1f);
        }

        // 2. Foreground Fill Bar (Left-aligned pivot for horizontal shrinking)
        if (fillTransform == null)
        {
            GameObject fillObj = new GameObject("HealthBar_Fill");
            fillObj.transform.SetParent(backgroundTransform, false);
            fillTransform = fillObj.transform;

            // Offset fill bar to align left edge with background left edge
            fillTransform.localPosition = new Vector3(-0.5f, 0f, 0f);

            fillRenderer = fillObj.AddComponent<SpriteRenderer>();
            // Pivot at (0.0, 0.5) so scale.x scales strictly rightward from left
            fillRenderer.sprite = Sprite.Create(pixelTex, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 100f);
            fillRenderer.color = fillColor;
            fillRenderer.sortingOrder = 11;

            fillTransform.localScale = new Vector3(1f, 1f, 1f);
        }
    }
}
