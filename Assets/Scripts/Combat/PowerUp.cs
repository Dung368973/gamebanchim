using System;
using UnityEngine;

/// <summary>
/// Represents a collectible power-up item dropped from birds or spawned during gameplay.
/// Implements IPooledObject for zero-allocation pooling.
/// Features dual-state motion: sinusoidal downward float and proximity player magnetism.
/// </summary>
public class PowerUp : MonoBehaviour, IPooledObject
{
    public const float DefaultFloatSpeed = 1.8f;
    public const float DefaultSwayAmplitude = 0.35f;
    public const float DefaultSwayFrequency = 2.5f;
    public const float DefaultMagnetismRadius = 1.6f;
    public const float DefaultMagnetismAccel = 12.0f;
    public const float DefaultDespawnY = -7.5f;

    public const float DefaultSpreadDuration = 10.0f;
    public const float DefaultRapidFireDuration = 8.0f;
    public const float DefaultShieldDuration = 8.0f;

    [Header("Power-up Settings")]
    [SerializeField] private PowerUpType type = PowerUpType.SpreadShot;
    [SerializeField] private float floatSpeed = DefaultFloatSpeed;
    [SerializeField] private float swayAmplitude = DefaultSwayAmplitude;
    [SerializeField] private float swayFrequency = DefaultSwayFrequency;
    [SerializeField] private float magnetismRadius = DefaultMagnetismRadius;
    [SerializeField] private float magnetismAccel = DefaultMagnetismAccel;
    [SerializeField] private float despawnY = DefaultDespawnY;

    [Header("References")]
    [SerializeField] private Collider2D powerUpCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private ObjectPool<PowerUp> _pool;
    private Transform _playerTransform;
    private Vector2 _velocity;
    private float _spawnX;
    private float _time;
    private bool _wasMagnetized;
    private bool _isCollected;
    private bool _isReturnedToPool;

    public PowerUpType Type
    {
        get => type;
        set
        {
            type = value;
            UpdateVisuals();
        }
    }

    public float FloatSpeed
    {
        get => floatSpeed;
        set => floatSpeed = value;
    }

    public float SwayAmplitude
    {
        get => swayAmplitude;
        set => swayAmplitude = value;
    }

    public float SwayFrequency
    {
        get => swayFrequency;
        set => swayFrequency = value;
    }

    public float MagnetismRadius
    {
        get => magnetismRadius;
        set => magnetismRadius = value;
    }

    public float MagnetismAccel
    {
        get => magnetismAccel;
        set => magnetismAccel = value;
    }

    public float DespawnY
    {
        get => despawnY;
        set => despawnY = value;
    }

    public bool IsCollected => _isCollected;
    public Vector2 Velocity => _velocity;
    public Transform PlayerTransform
    {
        get => _playerTransform;
        set => _playerTransform = value;
    }

    private void Awake()
    {
        if (powerUpCollider == null)
        {
            powerUpCollider = GetComponent<Collider2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        _spawnX = transform.position.x;
        _velocity = new Vector2(0f, -floatSpeed);
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        if (_isCollected || _isReturnedToPool) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Locate player transform if not already assigned
        if (_playerTransform == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
            }
            else
            {
                var playerComp = FindAnyObjectByType<PlayerController>();
                if (playerComp != null)
                {
                    _playerTransform = playerComp.transform;
                }
            }
        }

        Vector2 playerPos = _playerTransform != null
            ? (Vector2)_playerTransform.position
            : new Vector2(9999f, 9999f);

        UpdateKinematics(dt, playerPos);

        // Boundary despawn check
        if (transform.position.y < despawnY)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// Advances kinematic position and velocity by delta time according to dual motion model.
    /// </summary>
    public void UpdateKinematics(float dt, Vector2 playerPos)
    {
        if (_isCollected || _isReturnedToPool || dt <= 0f) return;

        _time += dt;
        Vector2 currentPos = transform.position;
        float dist = Vector2.Distance(currentPos, playerPos);

        if (dist < magnetismRadius)
        {
            Vector2 toPlayer = playerPos - currentPos;
            Vector2 dir = toPlayer.sqrMagnitude > 1e-8f ? toPlayer.normalized : Vector2.zero;
            _velocity += dir * (magnetismAccel * dt);
            currentPos += _velocity * dt;
            _wasMagnetized = true;
        }
        else
        {
            if (_wasMagnetized)
            {
                _spawnX = currentPos.x - (swayAmplitude * Mathf.Sin(swayFrequency * _time));
                _wasMagnetized = false;
            }
            _velocity = new Vector2(0f, -floatSpeed);
            float x = _spawnX + (swayAmplitude * Mathf.Sin(swayFrequency * _time));
            float y = currentPos.y - (floatSpeed * dt);
            currentPos = new Vector2(x, y);
        }

        transform.position = new Vector3(currentPos.x, currentPos.y, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isCollected || _isReturnedToPool) return;

        if (IsPlayerCollider(other))
        {
            Collect();
        }
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        if (other.GetComponent<PlayerHealth>() != null) return true;
        if (other.GetComponent<PlayerController>() != null) return true;
        if (other.name.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    /// <summary>
    /// Executes collection: freezes motion, triggers audio and events, and recycles.
    /// </summary>
    public void Collect()
    {
        if (_isCollected || _isReturnedToPool) return;

        _isCollected = true;
        float duration = GetDurationForType(type);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.PowerupPickup);
        }

        GameEvents.OnPowerUpCollected?.Invoke(type, duration);
        ReturnToPool();
    }

    public static float GetDurationForType(PowerUpType pType)
    {
        return pType switch
        {
            PowerUpType.SpreadShot => DefaultSpreadDuration,
            PowerUpType.RapidFire => DefaultRapidFireDuration,
            PowerUpType.HealthRecovery => 0f,
            PowerUpType.Shield => DefaultShieldDuration,
            _ => 8.0f
        };
    }

    public void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        // Visual color coding per power-up archetype
        spriteRenderer.color = type switch
        {
            PowerUpType.SpreadShot => new Color(0f, 0.9f, 1f, 1f),       // Vibrant Cyan
            PowerUpType.RapidFire => new Color(1f, 0.85f, 0.1f, 1f),      // Warm Gold / Amber
            PowerUpType.HealthRecovery => new Color(0.1f, 0.95f, 0.35f, 1f), // Vibrant Emerald Green
            PowerUpType.Shield => new Color(0.85f, 0.2f, 1f, 1f),        // Electric Magenta / Violet
            _ => Color.white
        };
    }

    public void Initialize(PowerUpType newType, ObjectPool<PowerUp> pool = null)
    {
        type = newType;
        if (pool != null)
        {
            _pool = pool;
        }

        OnSpawnFromPool();
    }

    public void SetPool(ObjectPool<PowerUp> pool)
    {
        _pool = pool;
    }

    #region IPooledObject Implementation
    public void OnSpawnFromPool()
    {
        _isCollected = false;
        _isReturnedToPool = false;
        _wasMagnetized = false;
        _time = 0f;
        _spawnX = transform.position.x;
        _velocity = new Vector2(0f, -floatSpeed);

        if (powerUpCollider != null)
        {
            powerUpCollider.enabled = true;
        }

        UpdateVisuals();
    }

    public void OnReturnToPool()
    {
        _isReturnedToPool = true;
        if (powerUpCollider != null)
        {
            powerUpCollider.enabled = false;
        }
    }

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
    #endregion
}
