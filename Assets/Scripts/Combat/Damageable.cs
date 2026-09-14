using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Common interface for any entity that possesses health and can receive damage.
/// </summary>
public interface IDamageable
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    void TakeDamage(int amount);
    void Die();
}

/// <summary>
/// General-purpose health and damage receiving component.
/// Can be attached to enemies, player, shields, or obstacles.
/// </summary>
public class Damageable : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int currentHealth = 1;
    [SerializeField] private bool isInvulnerable = false;

    [Header("Death Behavior")]
    [SerializeField] private bool deactivateOnDeath = false;
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelay = 0f;

    // C# Events for decoupled script listening
    public event Action<int, int> OnHealthChanged; // (current, max)
    public event Action<int> OnDamaged;           // (damageAmount)
    public event Action OnDied;

    // UnityEvents for inspector wiring
    [Header("Unity Events")]
    public UnityEvent<int, int> onHealthChangedUnityEvent;
    public UnityEvent<int> onDamagedUnityEvent;
    public UnityEvent onDiedUnityEvent;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public bool IsInvulnerable
    {
        get => isInvulnerable;
        set => isInvulnerable = value;
    }

    private void Awake()
    {
        if (currentHealth <= 0 && maxHealth > 0)
        {
            currentHealth = maxHealth;
        }
    }

    /// <summary>
    /// Initializes or reconfigures maximum and current health.
    /// Useful when pooling enemies with scaling health.
    /// </summary>
    public void Initialize(int maxHp)
    {
        maxHealth = Mathf.Max(1, maxHp);
        currentHealth = maxHealth;
        isInvulnerable = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        onHealthChangedUnityEvent?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Restores health back to maximum.
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isInvulnerable = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        onHealthChangedUnityEvent?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Inflicts damage on this entity if not invulnerable.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (!IsAlive || isInvulnerable || amount <= 0) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        OnDamaged?.Invoke(amount);
        onDamagedUnityEvent?.Invoke(amount);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        onHealthChangedUnityEvent?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Restores health up to maxHealth.
    /// </summary>
    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        onHealthChangedUnityEvent?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Executes death routine and triggers listeners.
    /// </summary>
    public void Die()
    {
        currentHealth = 0;
        OnDied?.Invoke();
        onDiedUnityEvent?.Invoke();

        if (deactivateOnDeath)
        {
            gameObject.SetActive(false);
        }
        else if (destroyOnDeath)
        {
            if (destroyDelay > 0f)
            {
                Destroy(gameObject, destroyDelay);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
