using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Monitors and cleans up entities (bullets, enemies, drops) when they exit the playable screen boundaries.
/// Supports both Update-based coordinate checking and 2D Trigger boundary zones.
/// </summary>
public class BoundaryCleaner : MonoBehaviour
{
    public const float DefaultMinY = -7.5f;
    public const float DefaultMaxY = 7.5f;
    public const float DefaultMinX = -4.5f;
    public const float DefaultMaxX = 4.5f;

    [Header("Boundary Limits")]
    [SerializeField] private float minY = DefaultMinY;
    [SerializeField] private float maxY = DefaultMaxY;
    [SerializeField] private float minX = DefaultMinX;
    [SerializeField] private float maxX = DefaultMaxX;

    [Header("Behavior")]
    [Tooltip("If true, checks transform position every Update. Useful when attached directly to projectiles or enemies.")]
    [SerializeField] private bool cleanOnUpdate = true;

    [Tooltip("If true, deactivates on trigger exit. Useful when attached to a master boundary trigger volume.")]
    [SerializeField] private bool cleanOnTriggerExit = false;

    [Tooltip("If true, calls Destroy(gameObject). If false, deactivates gameObject for object pooling reuse.")]
    [SerializeField] private bool destroyInsteadOfDeactivate = false;

    public event Action OnCleaned;
    public UnityEvent onCleanedUnityEvent;

    public float MinY { get => minY; set => minY = value; }
    public float MaxY { get => maxY; set => maxY = value; }
    public float MinX { get => minX; set => minX = value; }
    public float MaxX { get => maxX; set => maxX = value; }

    private void Awake()
    {
        AdjustBoundsToCamera();
    }

    private void OnEnable()
    {
        AdjustBoundsToCamera();
    }

    private static Camera SafeGetMainCamera()
    {
        try
        {
            return Camera.main;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Expands boundary limits to match the active Camera's viewport with padding.
    /// </summary>
    public void AdjustBoundsToCamera()
    {
        Camera cam = SafeGetMainCamera();
        if (cam != null && cam.orthographic)
        {
            float halfWidth = cam.orthographicSize * cam.aspect;
            minX = Mathf.Min(minX, -halfWidth - 2.5f);
            maxX = Mathf.Max(maxX, halfWidth + 2.5f);
            float halfHeight = cam.orthographicSize;
            minY = Mathf.Min(minY, -halfHeight - 2.5f);
            maxY = Mathf.Max(maxY, halfHeight + 2.5f);
        }
    }

    private void Update()
    {
        if (!cleanOnUpdate) return;

        float effectiveMinX = minX;
        float effectiveMaxX = maxX;
        float effectiveMinY = minY;
        float effectiveMaxY = maxY;

        Camera cam = SafeGetMainCamera();
        if (cam != null && cam.orthographic)
        {
            float halfWidth = cam.orthographicSize * cam.aspect;
            effectiveMinX = Mathf.Min(effectiveMinX, -halfWidth - 2.5f);
            effectiveMaxX = Mathf.Max(effectiveMaxX, halfWidth + 2.5f);
            float halfHeight = cam.orthographicSize;
            effectiveMinY = Mathf.Min(effectiveMinY, -halfHeight - 2.5f);
            effectiveMaxY = Mathf.Max(effectiveMaxY, halfHeight + 2.5f);
        }

        Vector3 pos = transform.position;
        if (pos.y < effectiveMinY || pos.y > effectiveMaxY || pos.x < effectiveMinX || pos.x > effectiveMaxX)
        {
            Clean();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!cleanOnTriggerExit) return;

        // When used as a boundary box trigger, clean the passing entity
        if (collision != null && collision.gameObject != gameObject)
        {
            // Never deactivate or clean the player
            if (collision.CompareTag("Player") || collision.GetComponent<PlayerHealth>() != null || collision.GetComponentInParent<PlayerHealth>() != null)
            {
                return;
            }

            var otherCleaner = collision.GetComponent<BoundaryCleaner>();
            if (otherCleaner != null)
            {
                otherCleaner.Clean();
            }
            else
            {
                collision.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Executes the cleanup routine: invokes events and either deactivates or destroys the object.
    /// </summary>
    public void Clean()
    {
        OnCleaned?.Invoke();
        onCleanedUnityEvent?.Invoke();

        if (destroyInsteadOfDeactivate)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Static utility to check if a position is beyond standard game boundaries.
    /// Dynamically expands bounds when Camera.main is active to support widescreen / any aspect ratio.
    /// </summary>
    public static bool IsOutOfBounds(Vector3 position, float padX = 0f, float padY = 0f)
    {
        float minX = DefaultMinX;
        float maxX = DefaultMaxX;
        float minY = DefaultMinY;
        float maxY = DefaultMaxY;

        Camera cam = SafeGetMainCamera();
        if (cam != null && cam.orthographic)
        {
            float halfWidth = cam.orthographicSize * cam.aspect;
            minX = Mathf.Min(minX, -halfWidth - 2.5f);
            maxX = Mathf.Max(maxX, halfWidth + 2.5f);
            float halfHeight = cam.orthographicSize;
            minY = Mathf.Min(minY, -halfHeight - 2.5f);
            maxY = Mathf.Max(maxY, halfHeight + 2.5f);
        }

        return position.y < (minY - padY) ||
               position.y > (maxY + padY) ||
               position.x < (minX - padX) ||
               position.x > (maxX + padX);
    }
}
