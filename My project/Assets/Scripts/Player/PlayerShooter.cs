using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles player automatic weapon discharge, fire rate accumulation,
/// multi-directional spread weapon patterns, rapid-fire buffs, and zero-allocation projectile pooling.
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    public const float DefaultBaseInterval = 0.20f; // 5.0 shots/sec
    public const float DefaultBulletSpeed = 14.0f;
    public const int DefaultInitialPoolCapacity = 60;
    public const float DefaultSpreadDuration = 10.0f;
    public const float DefaultRapidFireDuration = 8.0f;

    [Header("Firing Configuration")]
    [SerializeField] private float baseFireInterval = DefaultBaseInterval;
    [SerializeField] private float bulletSpeed = DefaultBulletSpeed;
    [SerializeField] private int bulletDamage = 1;
    [SerializeField] private bool autoFireEnabled = true;

    [Header("Weapon Upgrades")]
    [SerializeField] private int weaponLevel = 0; // 0 = Single, 1 = 3-way spread, 2 = 5-way spread
    [SerializeField] private bool isRapidFire = false;

    [Header("Projectile Pooling")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private int initialPoolCapacity = DefaultInitialPoolCapacity;

    [Header("Muzzle Offsets")]
    [SerializeField] private Vector2 defaultMuzzleOffset = new Vector2(0f, 0.45f);

    private ObjectPool<Bullet> _bulletPool;
    private PlayerHealth _playerHealth;
    private float _fireTimer = 0f;
    private float _spreadDurationTimer = 0f;
    private float _rapidFireDurationTimer = 0f;

    public float BaseFireInterval
    {
        get => baseFireInterval;
        set => baseFireInterval = Mathf.Max(0.01f, value);
    }

    public float EffectiveInterval => isRapidFire ? (baseFireInterval * 0.5f) : baseFireInterval;
    public bool IsRapidFire => isRapidFire;
    public int WeaponLevel => weaponLevel;
    public bool AutoFireEnabled
    {
        get => autoFireEnabled;
        set => autoFireEnabled = value;
    }
    public float FireTimer => _fireTimer;
    public ObjectPool<Bullet> BulletPool => _bulletPool;

    private void Awake()
    {
        _playerHealth = GetComponent<PlayerHealth>();

        if (bulletPrefab != null && _bulletPool == null)
        {
            _bulletPool = new ObjectPool<Bullet>(bulletPrefab, initialPoolCapacity, transform.parent);
        }
    }

    private void Start()
    {
        GameEvents.OnPowerUpCollected += HandlePowerUpCollected;
        GameEvents.OnGameRestart += HandleGameRestart;
    }

    private void OnDestroy()
    {
        GameEvents.OnPowerUpCollected -= HandlePowerUpCollected;
        GameEvents.OnGameRestart -= HandleGameRestart;
    }

    private void Update()
    {
        // Update power-up timers
        UpdateBuffTimers(Time.deltaTime);

        // Do not shoot if dead or auto-fire is turned off
        if (_playerHealth != null && !_playerHealth.IsAlive) return;
        if (!autoFireEnabled) return;

        // Fire rate accumulator loop
        _fireTimer += Time.deltaTime;
        float interval = EffectiveInterval;

        while (_fireTimer >= interval - 0.0001f)
        {
            _fireTimer = Mathf.Max(0f, _fireTimer - interval);
            Fire();
        }
    }

    /// <summary>
    /// Updates remaining duration on active weapon buffs.
    /// </summary>
    public void UpdateBuffTimers(float dt)
    {
        if (dt <= 0f) return;

        if (_spreadDurationTimer > 0f)
        {
            _spreadDurationTimer -= dt;
            if (_spreadDurationTimer <= 0f)
            {
                weaponLevel = 0;
            }
        }

        if (_rapidFireDurationTimer > 0f)
        {
            _rapidFireDurationTimer -= dt;
            if (_rapidFireDurationTimer <= 0f)
            {
                isRapidFire = false;
            }
        }
    }

    /// <summary>
    /// Executes projectile spawning according to active weapon level pattern.
    /// Returns the number of bullets discharged.
    /// </summary>
    public int Fire()
    {
        (Vector2[] muzzles, Vector2[] velocities) = GetPatternMuzzlesAndVelocities(weaponLevel, bulletSpeed);
        int count = velocities.Length;

        Vector2 shipPos = transform.position;

        for (int i = 0; i < count; i++)
        {
            Vector2 spawnPos = shipPos + muzzles[i];
            Vector2 vel = velocities[i];
            Vector2 dir = vel.normalized;

            if (_bulletPool != null)
            {
                float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg);

                Bullet bullet = _bulletPool.Get(spawnPos, rot);
                bullet.SetPool(_bulletPool);
                bullet.Initialize(dir, bulletSpeed, bulletDamage, playerBullet: true);
            }
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.Shoot, 0.85f, 0.05f);
        }

        return count;
    }

    /// <summary>
    /// Injects an external object pool for bullet instancing.
    /// </summary>
    public void SetPool(ObjectPool<Bullet> pool)
    {
        _bulletPool = pool;
    }

    /// <summary>
    /// Activates rapid fire for the specified duration. Resets duration if already active.
    /// </summary>
    public void SetRapidFire(bool active, float duration = DefaultRapidFireDuration)
    {
        isRapidFire = active;
        if (active)
        {
            _rapidFireDurationTimer = duration;
        }
        else
        {
            _rapidFireDurationTimer = 0f;
        }
    }

    /// <summary>
    /// Sets weapon spread level (0: Single, 1: 3-way, 2: 5-way) with buff duration.
    /// </summary>
    public void SetWeaponLevel(int level, float duration = DefaultSpreadDuration)
    {
        weaponLevel = Mathf.Clamp(level, 0, 2);
        if (weaponLevel > 0)
        {
            _spreadDurationTimer = duration;
        }
        else
        {
            _spreadDurationTimer = 0f;
        }
    }

    private void HandlePowerUpCollected(PowerUpType type, float duration)
    {
        switch (type)
        {
            case PowerUpType.SpreadShot:
                int nextLevel = Mathf.Min(2, weaponLevel + 1);
                SetWeaponLevel(nextLevel, duration > 0f ? duration : DefaultSpreadDuration);
                break;
            case PowerUpType.RapidFire:
                SetRapidFire(true, duration > 0f ? duration : DefaultRapidFireDuration);
                break;
        }
    }

    private void HandleGameRestart()
    {
        weaponLevel = 0;
        isRapidFire = false;
        _spreadDurationTimer = 0f;
        _rapidFireDurationTimer = 0f;
        _fireTimer = 0f;
    }

    /// <summary>
    /// Calculates velocity vectors for spread patterns.
    /// </summary>
    public static Vector2[] GetSpreadVelocities(int level, float speed = DefaultBulletSpeed)
    {
        switch (level)
        {
            case 1: // 3-way spread
                return new Vector2[]
                {
                    CalculateSpreadVelocity(-15f, speed),
                    CalculateSpreadVelocity(0f, speed),
                    CalculateSpreadVelocity(15f, speed)
                };
            case 2: // 5-way spread
                return new Vector2[]
                {
                    CalculateSpreadVelocity(-30f, speed),
                    CalculateSpreadVelocity(-15f, speed),
                    CalculateSpreadVelocity(0f, speed),
                    CalculateSpreadVelocity(15f, speed),
                    CalculateSpreadVelocity(30f, speed)
                };
            default: // Single shot
                return new Vector2[] { new Vector2(0f, speed) };
        }
    }

    private static (Vector2[] muzzles, Vector2[] velocities) GetPatternMuzzlesAndVelocities(int level, float speed)
    {
        Vector2[] vels = GetSpreadVelocities(level, speed);
        Vector2[] muzzles;

        switch (level)
        {
            case 1:
                muzzles = new Vector2[]
                {
                    new Vector2(-0.15f, 0.40f),
                    new Vector2(0.00f, 0.45f),
                    new Vector2(0.15f, 0.40f)
                };
                break;
            case 2:
                muzzles = new Vector2[]
                {
                    new Vector2(-0.30f, 0.35f),
                    new Vector2(-0.15f, 0.40f),
                    new Vector2(0.00f, 0.45f),
                    new Vector2(0.15f, 0.40f),
                    new Vector2(0.30f, 0.35f)
                };
                break;
            default:
                muzzles = new Vector2[]
                {
                    new Vector2(0f, 0.45f)
                };
                break;
        }

        return (muzzles, vels);
    }

    private static Vector2 CalculateSpreadVelocity(float angleDeg, float speed)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(-Mathf.Sin(rad) * speed, Mathf.Cos(rad) * speed);
    }
}
