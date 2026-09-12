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

    private void Update()
    {
        if (!cleanOnUpdate) return;

        Vector3 pos = transform.position;
        if (pos.y < minY || pos.y > maxY || pos.x < minX || pos.x > maxX)
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
    /// </summary>
    public static bool IsOutOfBounds(Vector3 position, float padX = 0f, float padY = 0f)
    {
        return position.y < (DefaultMinY - padY) ||
               position.y > (DefaultMaxY + padY) ||
               position.x < (DefaultMinX - padX) ||
               position.x > (DefaultMaxX + padX);
    }
}
