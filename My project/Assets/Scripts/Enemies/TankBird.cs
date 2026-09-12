using System;
using UnityEngine;

/// <summary>
/// Tank / Mini-Boss Heavy Bird Archetype (Eagle).
/// Features high HP durability, floating world-space health bar, horizontal sweeping,
/// visual hit flash feedback, and guaranteed power-up drop upon destruction.
/// Default stats: Speed 1.5 u/s, 5 HP, 500 points.
/// </summary>
public class TankBird : EnemyBase
{
    public const float DefaultSpeed = 1.5f;
    public const int DefaultHealth = 5;
    public const int DefaultScore = 500;
    public const float DefaultSweepAmplitude = 1.2f;
    public const float DefaultSweepFrequency = 1.5f;

    [Header("Tank Bird Mechanics")]
    [SerializeField] private EnemyHealthBar healthBar;
    [SerializeField] private GameObject powerUpDropPrefab;
    [SerializeField] private float sweepAmplitude = DefaultSweepAmplitude;
    [SerializeField] private float sweepFrequency = DefaultSweepFrequency;
    [SerializeField] private bool useHoverSweep = false;
    [SerializeField] private float targetHoverY = 3.6f;

    private float _initialX = 0f;
    private float _sweepTimer = 0f;
    private bool _hasDropped = false;

    // Drop events for external power-up managers
    public static event Action<Vector3> OnTankBirdDeathDrop;
    public event Action<Vector3> OnPowerUpDropped;

    public EnemyHealthBar HealthBar => healthBar;
    public float SweepAmplitude { get => sweepAmplitude; set => sweepAmplitude = value; }
    public float SweepFrequency { get => sweepFrequency; set => sweepFrequency = value; }
    public bool UseHoverSweep { get => useHoverSweep; set => useHoverSweep = value; }
    public float TargetHoverY { get => targetHoverY; set => targetHoverY = value; }
    public GameObject PowerUpDropPrefab { get => powerUpDropPrefab; set => powerUpDropPrefab = value; }

    protected override void Awake()
    {
        speed = DefaultSpeed;
        maxHealth = DefaultHealth;
        currentHealth = DefaultHealth;
        scoreValue = DefaultScore;
        enemyType = EnemyType.TankEagle;
        isBoss = true;
        birdColor = new Color(0.9f, 0.35f, 0.25f, 1f); // Rust / crimson eagle

        base.Awake();

        _initialX = transform.position.x;
        _sweepTimer = 0f;
        _hasDropped = false;

        InitializeHealthBar();
    }

    private void InitializeHealthBar()
    {
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<EnemyHealthBar>();
        }

        if (healthBar == null)
        {
            GameObject barObj = new GameObject("EnemyHealthBar");
            barObj.transform.SetParent(transform, false);
            healthBar = barObj.AddComponent<EnemyHealthBar>();
        }

        if (healthBar != null)
        {
            healthBar.SetTarget(transform);
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Configures spawn position, scaling health for higher waves, and speed.
    /// </summary>
    public void InitializeTank(Vector2 spawnPosition, int hp = DefaultHealth, float spd = DefaultSpeed)
    {
        transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        _initialX = spawnPosition.x;
        _sweepTimer = 0f;
        _hasDropped = false;

        Initialize(hp, spd);

        if (healthBar != null)
        {
            healthBar.SetVisible(true);
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Movement kinematics: sweeps horizontally while slowly descending (or hovers at targetHoverY if enabled).
    /// </summary>
    protected override void Move(float dt)
    {
        _sweepTimer += dt;

        float targetX = _initialX + (sweepAmplitude * Mathf.Sin(sweepFrequency * _sweepTimer));
        float currentY = transform.position.y;
        float targetY;

        if (useHoverSweep)
        {
            if (currentY > targetHoverY)
            {
                targetY = Mathf.Max(targetHoverY, currentY - (speed * dt));
            }
            else
            {
                targetY = targetHoverY;
            }
        }
        else
        {
            targetY = currentY - (speed * dt);
        }

        transform.position = new Vector3(targetX, targetY, transform.position.z);
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);

        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }

        // Notify boss health changed if registered
        GameEvents.OnBossHealthChanged?.Invoke("Tank Eagle", (float)currentHealth / maxHealth);
    }

    /// <summary>
    /// Guaranteed drop handling upon death.
    /// </summary>
    protected override void HandleDrops()
    {
        if (_hasDropped) return;
        _hasDropped = true;

        Vector3 dropPosition = transform.position;

        // 1. Fire static and instance drop delegates
        OnTankBirdDeathDrop?.Invoke(dropPosition);
        OnPowerUpDropped?.Invoke(dropPosition);

        // 2. Instantiate drop prefab if assigned
        if (powerUpDropPrefab != null)
        {
            Instantiate(powerUpDropPrefab, dropPosition, Quaternion.identity);
        }
        else
        {
            // Pick a random power-up type for listeners
            PowerUpType randomType = (PowerUpType)UnityEngine.Random.Range(0, 4);
            GameEvents.OnPowerUpCollected?.Invoke(randomType, 8.0f);
        }
    }

    public override void OnSpawnFromPool()
    {
        base.OnSpawnFromPool();
        _initialX = transform.position.x;
        _sweepTimer = 0f;
        _hasDropped = false;

        if (healthBar != null)
        {
            healthBar.SetVisible(true);
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
    }

    public override void OnReturnToPool()
    {
        base.OnReturnToPool();

        if (healthBar != null)
        {
            healthBar.SetVisible(false);
        }
    }
}
