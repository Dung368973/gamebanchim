using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wave Spawner & Difficulty Scaling System matching Milestone 3 (R2).
/// Features a robust 3-state machine (Spawning -> WaitingForClear -> WaveTransition),
/// dynamic difficulty scaling (spawn interval decay, speed multiplier, enemy count, HP scaling),
/// and zero-allocation object pooling for all three bird archetypes.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    public enum WaveState
    {
        Idle,
        Spawning,
        WaitingForClear,
        WaveTransition
    }

    [Header("Wave State")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private WaveState currentState = WaveState.Idle;
    [SerializeField] private bool autoStart = true;

    [Header("Spawn Boundaries")]
    [SerializeField] private float spawnY = 6.8f;
    [SerializeField] private float spawnMinX = -2.5f;
    [SerializeField] private float spawnMaxX = 2.5f;

    [Header("Scaling Configuration")]
    [SerializeField] private float baseSpawnInterval = 1.40f;
    [SerializeField] private float minSpawnInterval = 0.35f;
    [SerializeField] private float intervalDecayRate = 0.85f; // +15% spawn rate per wave
    [SerializeField] private int baseEnemyCount = 4;
    [SerializeField] private int enemyCountIncrementPerWave = 2;
    [SerializeField] private float transitionDuration = 2.5f;

    [Header("Bird Prefabs (Optional - created procedurally if unassigned)")]
    [SerializeField] private BasicBird basicBirdPrefab;
    [SerializeField] private FastBird fastBirdPrefab;
    [SerializeField] private TankBird tankBirdPrefab;

    [Header("Pool Capacities")]
    [SerializeField] private int basicPoolCapacity = 30;
    [SerializeField] private int fastPoolCapacity = 20;
    [SerializeField] private int tankPoolCapacity = 5;

    // Object Pools
    private ObjectPool<BasicBird> _basicPool;
    private ObjectPool<FastBird> _fastPool;
    private ObjectPool<TankBird> _tankPool;

    // Runtime state tracking
    private readonly List<EnemyBase> _activeEnemies = new List<EnemyBase>();
    private int _enemiesSpawnedThisWave = 0;
    private int _enemiesTargetThisWave = 0;
    private float _spawnTimer = 0f;
    private float _transitionTimer = 0f;
    private bool _isSpawningEnabled = false;

    public int CurrentWave => currentWave;
    public WaveState CurrentState => currentState;
    public int ActiveEnemyCount => _activeEnemies.Count;
    public int EnemiesSpawnedThisWave => _enemiesSpawnedThisWave;
    public int EnemiesTargetThisWave => _enemiesTargetThisWave;
    public float EffectiveSpawnInterval => CalculateSpawnInterval(currentWave);
    public float CurrentSpeedMultiplier => CalculateSpeedMultiplier(currentWave);
    public IReadOnlyList<EnemyBase> ActiveEnemies => _activeEnemies;

    // Event bus for external observers
    public event Action<int> OnWaveStateChanged;
    public event Action<EnemyBase> OnEnemySpawned;

    private void Awake()
    {
        InitializeObjectPools();
    }

    private void Start()
    {
        GameEvents.OnGameRestart += HandleGameRestart;

        if (autoStart)
        {
            StartSpawning();
        }
    }

    private void OnDestroy()
    {
        GameEvents.OnGameRestart -= HandleGameRestart;
    }

    private void Update()
    {
        UpdateSpawner(Time.deltaTime);
    }

    /// <summary>
    /// Deterministic update routine for both runtime Unity loop and offline unit testing.
    /// </summary>
    public void UpdateSpawner(float dt)
    {
        if (!_isSpawningEnabled || dt <= 0f) return;

        switch (currentState)
        {
            case WaveState.Spawning:
                UpdateSpawningState(dt);
                break;

            case WaveState.WaitingForClear:
                UpdateWaitingForClearState();
                break;

            case WaveState.WaveTransition:
                UpdateWaveTransitionState(dt);
                break;
        }
    }

    #region State Machine Logic
    private void UpdateSpawningState(float dt)
    {
        _spawnTimer += dt;
        float interval = EffectiveSpawnInterval;

        while (_spawnTimer >= interval && _enemiesSpawnedThisWave < _enemiesTargetThisWave)
        {
            _spawnTimer -= interval;
            SpawnNextEnemy();
        }

        // All enemies for this wave have been deployed -> wait for player to clear them
        if (_enemiesSpawnedThisWave >= _enemiesTargetThisWave)
        {
            currentState = WaveState.WaitingForClear;
        }
    }

    private void UpdateWaitingForClearState()
    {
        // Check if all active enemies have died or despawned
        if (_activeEnemies.Count == 0)
        {
            GameEvents.OnWaveCompleted?.Invoke(currentWave);
            currentState = WaveState.WaveTransition;
            _transitionTimer = 0f;
        }
    }

    private void UpdateWaveTransitionState(float dt)
    {
        _transitionTimer += dt;
        if (_transitionTimer >= transitionDuration)
        {
            AdvanceToNextWave();
        }
    }
    #endregion

    #region Wave Lifecycle Controls
    /// <summary>
    /// Starts wave progression from currentWave.
    /// </summary>
    public void StartSpawning()
    {
        _isSpawningEnabled = true;
        SetupWave(currentWave);
    }

    /// <summary>
    /// Pauses or halts spawning operations.
    /// </summary>
    public void StopSpawning()
    {
        _isSpawningEnabled = false;
        currentState = WaveState.Idle;
    }

    /// <summary>
    /// Prepares wave parameters and dispatches OnWaveStarted.
    /// </summary>
    public void SetupWave(int waveNumber)
    {
        currentWave = Mathf.Max(1, waveNumber);
        _enemiesTargetThisWave = CalculateEnemyCount(currentWave);
        _enemiesSpawnedThisWave = 0;
        _spawnTimer = 0f;
        currentState = WaveState.Spawning;

        GameEvents.OnWaveStarted?.Invoke(currentWave);
        OnWaveStateChanged?.Invoke(currentWave);
    }

    /// <summary>
    /// Advances wave counter and enters Spawning state.
    /// </summary>
    public void AdvanceToNextWave()
    {
        SetupWave(currentWave + 1);
    }

    /// <summary>
    /// Resets wave spawner back to wave 1 and clears all active enemies.
    /// </summary>
    public void RestartSpawner()
    {
        ClearActiveEnemies();
        currentWave = 1;
        SetupWave(1);
    }

    private void HandleGameRestart()
    {
        RestartSpawner();
    }

    /// <summary>
    /// Recycles all currently active enemies immediately.
    /// </summary>
    public void ClearActiveEnemies()
    {
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            if (_activeEnemies[i] != null)
            {
                _activeEnemies[i].ReturnToPool();
            }
        }
        _activeEnemies.Clear();
    }
    #endregion

    #region Spawning & Archetype Selection
    private void SpawnNextEnemy()
    {
        EnemyType typeToSpawn = SelectEnemyTypeForWave(currentWave, _enemiesSpawnedThisWave, _enemiesTargetThisWave);
        float spawnX = UnityEngine.Random.Range(spawnMinX, spawnMaxX);
        Vector2 spawnPos = new Vector2(spawnX, spawnY);
        float speedMultiplier = CurrentSpeedMultiplier;

        EnemyBase enemyInstance = null;

        switch (typeToSpawn)
        {
            case EnemyType.FastFalcon:
                enemyInstance = SpawnFastBird(spawnPos, speedMultiplier);
                break;

            case EnemyType.TankEagle:
                enemyInstance = SpawnTankBird(spawnPos, speedMultiplier);
                break;

            case EnemyType.BasicSparrow:
            default:
                enemyInstance = SpawnBasicBird(spawnPos, speedMultiplier);
                break;
        }

        if (enemyInstance != null)
        {
            _activeEnemies.Add(enemyInstance);
            _enemiesSpawnedThisWave++;

            enemyInstance.OnEnemyDied += HandleEnemyRemoved;
            enemyInstance.OnEnemyDespawned += HandleEnemyRemoved;

            OnEnemySpawned?.Invoke(enemyInstance);
        }
    }

    private BasicBird SpawnBasicBird(Vector2 spawnPos, float speedMult)
    {
        EnsurePools();
        BasicBird bird = _basicPool.Get();
        bird.transform.position = spawnPos;
        bird.Initialize(BasicBird.DefaultHealth, BasicBird.DefaultSpeed * speedMult);
        bird.SetReturnAction(e => _basicPool.Return((BasicBird)e));
        return bird;
    }

    private FastBird SpawnFastBird(Vector2 spawnPos, float speedMult)
    {
        EnsurePools();
        FastBird bird = _fastPool.Get();
        bird.InitializeTrajectory(spawnPos, FastBird.DefaultAmplitude, FastBird.DefaultOmega, 0f, FastBird.DefaultSpeed * speedMult);
        bird.SetReturnAction(e => _fastPool.Return((FastBird)e));
        return bird;
    }

    private TankBird SpawnTankBird(Vector2 spawnPos, float speedMult)
    {
        EnsurePools();
        TankBird bird = _tankPool.Get();
        int tankHP = CalculateTankHealth(currentWave);
        bird.InitializeTank(spawnPos, tankHP, TankBird.DefaultSpeed * speedMult);
        bird.SetReturnAction(e => _tankPool.Return((TankBird)e));
        return bird;
    }

    private void HandleEnemyRemoved(EnemyBase enemy)
    {
        if (enemy == null) return;

        enemy.OnEnemyDied -= HandleEnemyRemoved;
        enemy.OnEnemyDespawned -= HandleEnemyRemoved;

        _activeEnemies.Remove(enemy);
    }
    #endregion

    #region Difficulty Scaling Calculations
    /// <summary>
    /// Calculates spawn interval with dynamic 15% rate acceleration per wave, clamped at minSpawnInterval (0.35s).
    /// </summary>
    public float CalculateSpawnInterval(int wave)
    {
        return Mathf.Max(minSpawnInterval, baseSpawnInterval * Mathf.Pow(intervalDecayRate, Mathf.Max(0, wave - 1)));
    }

    /// <summary>
    /// Calculates enemy speed scaling factor (+4% per wave, capped at 1.85x).
    /// </summary>
    public float CalculateSpeedMultiplier(int wave)
    {
        return Mathf.Min(1.85f, 1.0f + (0.04f * Mathf.Max(0, wave - 1)));
    }

    /// <summary>
    /// Calculates total enemies to spawn for this wave (+2 enemies per wave).
    /// </summary>
    public int CalculateEnemyCount(int wave)
    {
        return baseEnemyCount + (Mathf.Max(0, wave - 1) * enemyCountIncrementPerWave);
    }

    /// <summary>
    /// Calculates Tank Bird HP scaling for higher waves (+5 HP per wave past wave 3).
    /// </summary>
    public int CalculateTankHealth(int wave)
    {
        return TankBird.DefaultHealth + (Mathf.Max(0, wave - 3) * 5);
    }

    /// <summary>
    /// Selects enemy archetype according to wave milestone:
    /// - Wave 1: 100% Basic Birds
    /// - Wave 2: 60% Basic, 40% Fast Birds
    /// - Wave 3+: Basic + Fast + guaranteed Tank Bird boss at the end of the wave
    /// </summary>
    public EnemyType SelectEnemyTypeForWave(int wave, int spawnedIndex, int totalTarget)
    {
        if (wave == 1)
        {
            return EnemyType.BasicSparrow;
        }

        if (wave == 2)
        {
            return (spawnedIndex % 2 == 1) ? EnemyType.FastFalcon : EnemyType.BasicSparrow;
        }

        // Wave 3+: Last enemy in wave is always a Tank Bird mini-boss
        if (spawnedIndex == totalTarget - 1)
        {
            return EnemyType.TankEagle;
        }

        // Intermediate enemies roll between Basic (60%) and Fast (40%)
        float roll = UnityEngine.Random.value;
        return roll < 0.40f ? EnemyType.FastFalcon : EnemyType.BasicSparrow;
    }
    #endregion

    #region Object Pooling Setup
    private void InitializeObjectPools()
    {
        EnsurePools();
    }

    private void EnsurePools()
    {
        if (_basicPool != null && _fastPool != null && _tankPool != null) return;

        // 1. Basic Bird Pool
        if (basicBirdPrefab == null)
        {
            basicBirdPrefab = CreateProceduralPrefab<BasicBird>("BasicBird_Prefab");
        }
        _basicPool = new ObjectPool<BasicBird>(basicBirdPrefab, basicPoolCapacity, transform);

        // 2. Fast Bird Pool
        if (fastBirdPrefab == null)
        {
            fastBirdPrefab = CreateProceduralPrefab<FastBird>("FastBird_Prefab");
        }
        _fastPool = new ObjectPool<FastBird>(fastBirdPrefab, fastPoolCapacity, transform);

        // 3. Tank Bird Pool
        if (tankBirdPrefab == null)
        {
            tankBirdPrefab = CreateProceduralPrefab<TankBird>("TankBird_Prefab");
        }
        _tankPool = new ObjectPool<TankBird>(tankBirdPrefab, tankPoolCapacity, transform);
    }

    private T CreateProceduralPrefab<T>(string name) where T : EnemyBase
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform, false);

        // Add 2D BoxCollider
        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.8f, 0.8f);

        // Add SpriteRenderer with white 4x4 procedural texture
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);

        // Add BoundaryCleaner
        obj.AddComponent<BoundaryCleaner>();

        // Add Enemy Component
        T component = obj.AddComponent<T>();
        obj.SetActive(false);
        return component;
    }
    #endregion
}
