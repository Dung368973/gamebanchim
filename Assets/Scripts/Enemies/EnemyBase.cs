using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Abstract base class for all bird enemy types in Game Bắn Chim.
/// Implements IDamageable for combat damage receiving and IPooledObject for zero-allocation recycling.
/// Controls downward kinematics, boundary cleaner integration, hit flashing, audio feedback, and score dispatching.
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IDamageable, IPooledObject
{
    [Header("Enemy Stats")]
    [SerializeField] protected float speed = 3.0f;
    [SerializeField] protected int currentHealth = 1;
    [SerializeField] protected int maxHealth = 1;
    [SerializeField] protected int scoreValue = 100;
    [SerializeField] protected EnemyType enemyType = EnemyType.BasicSparrow;
    [SerializeField] protected bool isBoss = false;
    [SerializeField] protected Color birdColor = Color.white;

    [Header("Components")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Collider2D enemyCollider;
    [SerializeField] protected BoundaryCleaner boundaryCleaner;

    [Header("Hit Flash Settings")]
    [SerializeField] protected float flashDuration = 0.08f;
    [SerializeField] protected Color flashColor = Color.white;

    protected bool _isReturnedToPool = false;
    protected Coroutine _flashCoroutine;
    protected Action<EnemyBase> _returnToPoolAction;

    // Events for wave spawner and UI listeners
    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamaged;
    public event Action<EnemyBase> OnEnemyDied;
    public event Action<EnemyBase> OnEnemyDespawned;

    public float Speed
    {
        get => speed;
        set => speed = value;
    }

    public int CurrentHealth => currentHealth;

    public int MaxHealth
    {
        get => maxHealth;
        set => maxHealth = Mathf.Max(1, value);
    }

    public int ScoreValue
    {
        get => scoreValue;
        set => scoreValue = Mathf.Max(0, value);
    }

    public bool IsAlive => currentHealth > 0 && !_isReturnedToPool;

    public bool IsBoss
    {
        get => isBoss;
        set => isBoss = value;
    }

    public EnemyType BirdType => enemyType;
    public Color BirdColor => birdColor;
    public SpriteRenderer SpriteRenderer => spriteRenderer;

    protected virtual void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (enemyCollider == null)
        {
            enemyCollider = GetComponent<Collider2D>();
        }

        if (boundaryCleaner == null)
        {
            boundaryCleaner = GetComponent<BoundaryCleaner>();
        }

        if (boundaryCleaner != null)
        {
            boundaryCleaner.OnCleaned += HandleBoundaryCleaned;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = birdColor;
        }

        if (currentHealth <= 0 && maxHealth > 0)
        {
            currentHealth = maxHealth;
        }
    }

    protected virtual void OnDestroy()
    {
        if (boundaryCleaner != null)
        {
            boundaryCleaner.OnCleaned -= HandleBoundaryCleaned;
        }
    }

    protected virtual void Update()
    {
        if (!IsAlive) return;

        Move(Time.deltaTime);

        // Check off-screen boundary failsafe
        if (BoundaryCleaner.IsOutOfBounds(transform.position, 1.0f, 1.0f))
        {
            DespawnOffScreen();
        }
    }

    /// <summary>
    /// Kinematic movement routine executed every frame. Subclasses define specific trajectories.
    /// </summary>
    protected abstract void Move(float dt);

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsAlive || other == null) return;

        // Collision with player ship
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null && playerHealth.IsAlive)
        {
            playerHealth.TakeDamage(1);
            DieWithoutScore();
        }
    }

    /// <summary>
    /// Binds a return-to-pool action callback for recycling.
    /// </summary>
    public void SetReturnAction(Action<EnemyBase> returnAction)
    {
        _returnToPoolAction = returnAction;
    }

    /// <summary>
    /// Initializes health and stats when spawned from pool.
    /// </summary>
    public virtual void Initialize(int maxHp, float spd = -1f)
    {
        maxHealth = Mathf.Max(1, maxHp);
        currentHealth = maxHealth;
        if (spd > 0f) speed = spd;
        _isReturnedToPool = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = birdColor;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Resets health back to full.
    /// </summary>
    public virtual void ResetHealth()
    {
        currentHealth = maxHealth;
        _isReturnedToPool = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = birdColor;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Receives projectile damage, plays hit feedback, flashes sprite, and triggers death if health reaches 0.
    /// </summary>
    public virtual void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        OnDamaged?.Invoke(amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Visual hit flash
        StartFlash();

        // Audio SFX
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.Hit);
        }

        // Particle sparks
        if (FXManager.Instance != null)
        {
            FXManager.Instance.PlayHitSparks(transform.position);
        }

        if (currentHealth == 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Standard death routine triggered when health reaches zero by player gunfire.
    /// Awards score, triggers death SFX/FX, drops power-ups, and recycles.
    /// </summary>
    public virtual void Die()
    {
        if (_isReturnedToPool) return;

        currentHealth = 0;

        // Audio SFX
        SFXType deathSFX = isBoss ? SFXType.Explosion : SFXType.FeatherBurst;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(deathSFX);
        }

        // Feather burst particle FX
        if (FXManager.Instance != null)
        {
            int featherCount = isBoss ? 70 : 25;
            FXManager.Instance.PlayFeatherBurst(transform.position, birdColor, featherCount);
        }

        // Dispatch enemy killed event for ScoreManager (fallback to OnScoreChanged if ScoreManager is absent)
        GameEvents.OnEnemyKilled?.Invoke(enemyType, scoreValue);
        if (ScoreManager.Instance == null)
        {
            GameEvents.OnScoreChanged?.Invoke(scoreValue);
        }

        // Boss alert/event
        if (isBoss)
        {
            GameEvents.OnBossDefeated?.Invoke();
        }

        // Handle item drop (Tank bird guaranteed drops)
        HandleDrops();

        OnEnemyDied?.Invoke(this);

        ReturnToPool();
    }

    /// <summary>
    /// Suicide death routine when ramming into player ship: dies with effects, but awards no score.
    /// </summary>
    public virtual void DieWithoutScore()
    {
        if (_isReturnedToPool) return;

        currentHealth = 0;

        SFXType deathSFX = isBoss ? SFXType.Explosion : SFXType.FeatherBurst;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(deathSFX);
        }

        if (FXManager.Instance != null)
        {
            int featherCount = isBoss ? 70 : 25;
            FXManager.Instance.PlayFeatherBurst(transform.position, birdColor, featherCount);
        }

        OnEnemyDied?.Invoke(this);

        ReturnToPool();
    }

    /// <summary>
    /// Despawns off-screen without awarding score or combo points.
    /// </summary>
    public virtual void DespawnOffScreen()
    {
        if (_isReturnedToPool) return;

        OnEnemyDespawned?.Invoke(this);
        ReturnToPool();
    }

    protected virtual void HandleBoundaryCleaned()
    {
        DespawnOffScreen();
    }

    /// <summary>
    /// Virtual hook for item dropping logic (e.g. TankBird power-up drop).
    /// </summary>
    protected virtual void HandleDrops()
    {
        // Default enemies drop nothing or roll random chance
    }

    /// <summary>
    /// Recycles this enemy back to its pool or deactivates it.
    /// </summary>
    public virtual void ReturnToPool()
    {
        if (_isReturnedToPool) return;
        _isReturnedToPool = true;

        StopFlash();

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        if (_returnToPoolAction != null)
        {
            _returnToPoolAction.Invoke(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    #region Hit Flash
    protected virtual void StartFlash()
    {
        StopFlash();
        if (gameObject.activeInHierarchy && spriteRenderer != null)
        {
            _flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    protected virtual void StopFlash()
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = birdColor;
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = birdColor;
        }

        _flashCoroutine = null;
    }
    #endregion

    #region IPooledObject Implementation
    public virtual void OnSpawnFromPool()
    {
        _isReturnedToPool = false;
        currentHealth = maxHealth;

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = birdColor;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public virtual void OnReturnToPool()
    {
        _isReturnedToPool = true;
        StopFlash();

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
    }
    #endregion
}
