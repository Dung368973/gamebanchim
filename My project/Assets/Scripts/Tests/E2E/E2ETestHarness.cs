using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShootEmUp.Tests.E2E
{
    public interface ITestDamageable : IDamageable
    {
        bool IsAlive { get; }
        bool IsInvulnerable { get; set; }
        void Initialize(int maxHp);
        void ResetHealth();
        void Heal(int amount);
        event Action<int, int> OnHealthChanged;
        event Action<int> OnDamaged;
        event Action OnDied;
    }

    public class SimulatedDamageable : ITestDamageable
    {
        private int _maxHealth = 1;
        private int _currentHealth = 1;
        private bool _isInvulnerable = false;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsAlive => _currentHealth > 0;
        public bool IsInvulnerable { get => _isInvulnerable; set => _isInvulnerable = value; }

        public event Action<int, int> OnHealthChanged;
        public event Action<int> OnDamaged;
        public event Action OnDied;

        public void Initialize(int maxHp)
        {
            _maxHealth = Mathf.Max(1, maxHp);
            _currentHealth = _maxHealth;
            _isInvulnerable = false;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void ResetHealth()
        {
            _currentHealth = _maxHealth;
            _isInvulnerable = false;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive || _isInvulnerable || amount <= 0) return;
            _currentHealth -= amount;
            if (_currentHealth < 0) _currentHealth = 0;
            OnDamaged?.Invoke(amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            if (_currentHealth == 0) Die();
        }

        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void Die()
        {
            _currentHealth = 0;
            OnDied?.Invoke();
        }
    }

    public interface ITestObjectPool<T>
    {
        int AvailableCount { get; }
        int TotalCreated { get; }
        int ActiveCount { get; }
        T Get();
        void Return(T item);
        void Clear();
    }

    public class SimulatedObjectPool<T> : ITestObjectPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Stack<T> _pool;
        private readonly HashSet<T> _inPoolSet;
        private int _totalCreated;

        public int AvailableCount => _pool.Count;
        public int TotalCreated => _totalCreated;
        public int ActiveCount => _totalCreated - _pool.Count;

        public SimulatedObjectPool(Func<T> factory, int initialCapacity)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory), "Cannot create pool with null factory.");
            _factory = factory;
            _pool = new Stack<T>(Mathf.Max(initialCapacity, 4));
            _inPoolSet = new HashSet<T>();
            _totalCreated = 0;

            for (int i = 0; i < initialCapacity; i++)
            {
                T item = _factory();
                _totalCreated++;
                _pool.Push(item);
                _inPoolSet.Add(item);
            }
        }

        public T Get()
        {
            T item;
            if (_pool.Count > 0)
            {
                item = _pool.Pop();
                _inPoolSet.Remove(item);
            }
            else
            {
                item = _factory();
                _totalCreated++;
            }
            return item;
        }

        public void Return(T item)
        {
            if (item == null) return;
            if (_inPoolSet.Contains(item)) return;
            _pool.Push(item);
            _inPoolSet.Add(item);
        }

        public void Clear()
        {
            _pool.Clear();
            _inPoolSet.Clear();
            _totalCreated = 0;
        }
    }

    public class SimulatedBoundaryCleaner
    {
        public bool IsActive { get; private set; } = true;
        public event Action OnCleaned;

        public void Clean()
        {
            OnCleaned?.Invoke();
            IsActive = false;
        }
    }

    /// <summary>
    /// Test harness providing isolated execution context and capturing GameEvents dispatches.
    /// Compatible with both Unity runtime and standalone .NET execution.
    /// </summary>
    public class E2ETestHarness : IDisposable
    {
        public static bool IsUnityEngineAvailable { get; }

        static E2ETestHarness()
        {
            try
            {
                var go = new GameObject("Probe");
                UnityEngine.Object.DestroyImmediate(go);
                IsUnityEngineAvailable = true;
            }
            catch
            {
                IsUnityEngineAvailable = false;
            }
        }

        public List<int> ScoreHistory { get; } = new List<int>();
        public List<(int current, int max)> HealthHistory { get; } = new List<(int, int)>();
        public List<(int count, float mult)> ComboHistory { get; } = new List<(int, float)>();
        public List<int> WaveStartedHistory { get; } = new List<int>();
        public List<(PowerUpType type, float duration)> PowerUpHistory { get; } = new List<(PowerUpType, float)>();
        public List<(int finalScore, int highScore)> GameOverHistory { get; } = new List<(int, int)>();
        public int PlayerDiedCount { get; private set; } = 0;
        public int RestartCount { get; private set; } = 0;

        private readonly List<GameObject> _createdGameObjects = new List<GameObject>();

        public E2ETestHarness()
        {
            SetUp();
        }

        public void SetUp()
        {
            GameEvents.ResetAllEvents();
            ScoreHistory.Clear();
            HealthHistory.Clear();
            ComboHistory.Clear();
            WaveStartedHistory.Clear();
            PowerUpHistory.Clear();
            GameOverHistory.Clear();
            PlayerDiedCount = 0;
            RestartCount = 0;

            GameEvents.OnScoreChanged += score => ScoreHistory.Add(score);
            GameEvents.OnPlayerHealthChanged += (cur, max) => HealthHistory.Add((cur, max));
            GameEvents.OnComboChanged += (cnt, mult) => ComboHistory.Add((cnt, mult));
            GameEvents.OnWaveStarted += wave => WaveStartedHistory.Add(wave);
            GameEvents.OnPowerUpCollected += (type, dur) => PowerUpHistory.Add((type, dur));
            GameEvents.OnGameOver += (finalScore, highScore) => GameOverHistory.Add((finalScore, highScore));
            GameEvents.OnPlayerDied += () => PlayerDiedCount++;
            GameEvents.OnGameRestart += () => RestartCount++;
        }

        public ITestDamageable CreateDamageableEntity(int initialHp = 1)
        {
            var dmg = new SimulatedDamageable();
            dmg.Initialize(initialHp);
            return dmg;
        }

        public ITestObjectPool<ITestDamageable> CreateBulletPool(int initialCapacity)
        {
            return new SimulatedObjectPool<ITestDamageable>(() => new SimulatedDamageable(), initialCapacity);
        }

        public SimulatedBoundaryCleaner CreateBoundaryCleaner()
        {
            return new SimulatedBoundaryCleaner();
        }

        public void TearDown()
        {
            GameEvents.ResetAllEvents();

            if (IsUnityEngineAvailable)
            {
                foreach (var go in _createdGameObjects)
                {
                    if (go != null)
                    {
                        UnityEngine.Object.DestroyImmediate(go);
                    }
                }
            }
            _createdGameObjects.Clear();
        }

        public void Dispose()
        {
            TearDown();
        }
    }
}
