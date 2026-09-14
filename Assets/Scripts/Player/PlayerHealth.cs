using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages player lives, damage receiving, invulnerability frames, and shield protection.
/// Dispatches GameEvents for UI HUD updating and game over states.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    public const int DefaultMaxLives = 3;
    public const float DefaultIFramesDuration = 2.0f;
    public const float DefaultShieldGracePeriod = 1.0f;
    public const float DefaultShieldDuration = 8.0f;

    [Header("Lives & Health")]
    [SerializeField] private int maxHealth = DefaultMaxLives;
    [SerializeField] private int currentHealth = DefaultMaxLives;

    [Header("Invulnerability Settings")]
    [SerializeField] private float iFrameDuration = DefaultIFramesDuration;
    [SerializeField] private float shieldGracePeriod = DefaultShieldGracePeriod;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D playerCollider;

    private float _invulnerabilityTimer = 0f;
    private bool _hasShield = false;
    private float _shieldDurationTimer = 0f;
    private Coroutine _flashCoroutine;
    private Color _originalSpriteColor = Color.white;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentLives => currentHealth;
    public int MaxLives => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public bool IsInvulnerable => _invulnerabilityTimer > 0f;
    public bool HasShield => _hasShield;
    public float ShieldDurationTimer => _shieldDurationTimer;
    public float InvulnerabilityTimer => _invulnerabilityTimer;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider2D>();
        }

        if (spriteRenderer != null)
        {
            _originalSpriteColor = spriteRenderer.color;
        }

        currentHealth = maxHealth;
    }

    private void Start()
    {
        // Broadcast initial health state
        GameEvents.OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);

        // Listen for power-up and restart events
        GameEvents.OnPowerUpCollected += HandlePowerUpCollected;
        GameEvents.OnGameRestart += ResetHealth;
    }

    private void OnDestroy()
    {
        GameEvents.OnPowerUpCollected -= HandlePowerUpCollected;
        GameEvents.OnGameRestart -= ResetHealth;
    }

    private void OnDisable()
    {
        StopFlashRoutine();
    }

    private void Update()
    {
        // Countdown invulnerability frames
        if (_invulnerabilityTimer > 0f)
        {
            _invulnerabilityTimer = Mathf.Max(0f, _invulnerabilityTimer - Time.deltaTime);
        }

        // Countdown shield duration
        if (_hasShield && _shieldDurationTimer > 0f)
        {
            _shieldDurationTimer -= Time.deltaTime;
            float normalizedShield = Mathf.Clamp01(_shieldDurationTimer / DefaultShieldDuration);
            GameEvents.OnShieldChanged?.Invoke(normalizedShield);

            if (_shieldDurationTimer <= 0f)
            {
                _hasShield = false;
                _shieldDurationTimer = 0f;
                GameEvents.OnShieldChanged?.Invoke(0f);
            }
        }
    }

    /// <summary>
    /// Inflicts damage on the player, respecting active shields and invulnerability frames.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0) return;

        // Shield absorbs hit completely without losing a life
        if (_hasShield)
        {
            _hasShield = false;
            _shieldDurationTimer = 0f;
            _invulnerabilityTimer = shieldGracePeriod;

            GameEvents.OnShieldChanged?.Invoke(0f);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(SFXType.ShieldBreak);
            }

            StartFlashRoutine(shieldGracePeriod);
            return;
        }

        // Ignore damage during I-frames
        if (IsInvulnerable) return;

        // Apply damage to lives
        currentHealth = Mathf.Max(0, currentHealth - amount);
        _invulnerabilityTimer = iFrameDuration;

        // Reset combo on player hit
        GameEvents.OnComboChanged?.Invoke(0, 1.0f);

        // Notify HUD
        GameEvents.OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(SFXType.Hit);
            }

            StartFlashRoutine(iFrameDuration);
        }
    }

    /// <summary>
    /// Restores lives up to maxHealth. Rejected if player is already dead.
    /// </summary>
    public void Heal(int amount = 1)
    {
        if (!IsAlive || amount <= 0 || currentHealth >= maxHealth) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        GameEvents.OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Activates an invulnerable energy shield bubble for the specified duration.
    /// </summary>
    public void ActivateShield(float duration = DefaultShieldDuration)
    {
        if (!IsAlive) return;

        _hasShield = true;
        _shieldDurationTimer = Mathf.Max(_shieldDurationTimer, duration);
        GameEvents.OnShieldChanged?.Invoke(1.0f);
    }

    /// <summary>
    /// Resets health and invulnerability timers back to clean starting state.
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        _invulnerabilityTimer = 0f;
        _hasShield = false;
        _shieldDurationTimer = 0f;

        StopFlashRoutine();

        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        if (gameObject != null && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        GameEvents.OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
        GameEvents.OnShieldChanged?.Invoke(0f);
    }

    /// <summary>
    /// Executes player death routine, dispatches events, and plays explosion SFX.
    /// </summary>
    public void Die()
    {
        currentHealth = 0;
        _invulnerabilityTimer = 0f;
        _hasShield = false;
        _shieldDurationTimer = 0f;

        StopFlashRoutine();

        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.Explosion);
        }

        GameEvents.OnPlayerHealthChanged?.Invoke(0, maxHealth);
        GameEvents.OnPlayerDied?.Invoke();
    }

    private void HandlePowerUpCollected(PowerUpType type, float duration)
    {
        switch (type)
        {
            case PowerUpType.HealthRecovery:
                Heal(1);
                break;
            case PowerUpType.Shield:
                ActivateShield(duration > 0f ? duration : DefaultShieldDuration);
                break;
        }
    }

    #region Visual Flashing
    private void StartFlashRoutine(float duration)
    {
        StopFlashRoutine();
        if (gameObject.activeInHierarchy)
        {
            _flashCoroutine = StartCoroutine(FlashRoutine(duration));
        }
    }

    private void StopFlashRoutine()
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = _originalSpriteColor;
        }
    }

    private IEnumerator FlashRoutine(float duration)
    {
        if (spriteRenderer == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // alpha(t) = 0.6 + 0.4 * cos(20 * pi * t)
            float alpha = 0.6f + 0.4f * Mathf.Cos(20f * Mathf.PI * elapsed);
            Color c = _originalSpriteColor;
            c.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = c;

            yield return null;
            elapsed += Time.deltaTime;
        }

        spriteRenderer.color = _originalSpriteColor;
        _flashCoroutine = null;
    }
    #endregion
}
