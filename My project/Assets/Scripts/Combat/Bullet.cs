using System;
using UnityEngine;

/// <summary>
/// High-speed pooled projectile for player and enemy weaponry.
/// Implements IPooledObject for zero-allocation recycling with ObjectPool.
/// Integrates with BoundaryCleaner for clean off-screen despawn.
/// </summary>
public class Bullet : MonoBehaviour, IPooledObject
{
    public const float DefaultSpeed = 14.0f;
    public const int DefaultDamage = 1;

    [Header("Projectile Properties")]
    [SerializeField] private float speed = DefaultSpeed;
    [SerializeField] private Vector2 direction = Vector2.up;
    [SerializeField] private int damage = DefaultDamage;
    [SerializeField] private bool isPlayerBullet = true;

    [Header("Components")]
    [SerializeField] private BoundaryCleaner boundaryCleaner;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D bulletCollider;

    private ObjectPool<Bullet> _pool;
    private bool _isReturnedToPool = false;

    public float Speed
    {
        get => speed;
        set => speed = value;
    }

    public Vector2 Direction
    {
        get => direction;
        set
        {
            direction = value;
            UpdateRotation();
        }
    }

    public int Damage
    {
        get => damage;
        set => damage = Mathf.Max(0, value);
    }

    public bool IsPlayerBullet
    {
        get => isPlayerBullet;
        set => isPlayerBullet = value;
    }

    public ObjectPool<Bullet> Pool => _pool;

    private void Awake()
    {
        if (boundaryCleaner == null)
        {
            boundaryCleaner = GetComponent<BoundaryCleaner>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (bulletCollider == null)
        {
            bulletCollider = GetComponent<Collider2D>();
        }

        if (boundaryCleaner != null)
        {
            boundaryCleaner.OnCleaned += HandleBoundaryCleaned;
        }
    }

    private void OnDestroy()
    {
        if (boundaryCleaner != null)
        {
            boundaryCleaner.OnCleaned -= HandleBoundaryCleaned;
        }
    }

    private void Update()
    {
        // Linear movement along direction vector
        Vector3 displacement = (Vector3)(direction.normalized * (speed * Time.deltaTime));
        transform.position += displacement;

        // Failsafe boundary check if BoundaryCleaner is not actively checking
        if (BoundaryCleaner.IsOutOfBounds(transform.position, 1.0f, 1.0f))
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isReturnedToPool || other == null) return;

        // Player bullet hitting enemy
        if (isPlayerBullet)
        {
            IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.CurrentHealth > 0)
            {
                damageable.TakeDamage(damage);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(SFXType.Hit);
                }

                ReturnToPool();
            }
        }
        else // Enemy bullet hitting player
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsAlive)
            {
                playerHealth.TakeDamage(damage);
                ReturnToPool();
            }
        }
    }

    /// <summary>
    /// Initializes bullet trajectory, velocity, and affiliation.
    /// </summary>
    public void Initialize(Vector2 dir, float spd = DefaultSpeed, int dmg = DefaultDamage, bool playerBullet = true)
    {
        direction = dir;
        speed = spd;
        damage = dmg;
        isPlayerBullet = playerBullet;
        _isReturnedToPool = false;

        UpdateRotation();
    }

    /// <summary>
    /// Binds the pool reference to allow automatic recycling.
    /// </summary>
    public void SetPool(ObjectPool<Bullet> pool)
    {
        _pool = pool;
    }

    /// <summary>
    /// Aligns transform rotation to match the trajectory heading angle.
    /// </summary>
    private void UpdateRotation()
    {
        if (direction.sqrMagnitude > 0.0001f)
        {
            float angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
        }
    }

    private void HandleBoundaryCleaned()
    {
        ReturnToPool();
    }

    /// <summary>
    /// Recycles bullet back into the object pool, or deactivates GameObject if unpooled.
    /// </summary>
    public void ReturnToPool()
    {
        if (_isReturnedToPool) return;
        _isReturnedToPool = true;

        if (_pool != null)
        {
            _pool.Return(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    #region IPooledObject Implementation
    public void OnSpawnFromPool()
    {
        _isReturnedToPool = false;
        if (bulletCollider != null)
        {
            bulletCollider.enabled = true;
        }
        UpdateRotation();
    }

    public void OnReturnToPool()
    {
        _isReturnedToPool = true;
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
        }
    }
    #endregion
}
