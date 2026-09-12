using System;
using UnityEngine;

/// <summary>
/// Central manager for scoring, combo tracking, decay timers, and high-score persistence.
/// Broadcasts cumulative total score and stepped combo multipliers via GameEvents.
/// Decoupled from EnemyBase via OnEnemyKilled to prevent recursive delegate cycles.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public const string HighScoreKey = "HIGH_SCORE_KEY";
    public const float DefaultComboDuration = 3.0f;

    public const int DefaultScoreBasic = 100;
    public const int DefaultScoreFast = 200;
    public const int DefaultScoreTank = 500;
    public const int DefaultScoreBoss = 1000;

    public static ScoreManager Instance { get; private set; }

    [Header("Scoring State")]
    [SerializeField] private int totalScore = 0;
    [SerializeField] private int highScore = 0;
    [SerializeField] private int comboCount = 0;
    [SerializeField] private float comboTimer = 0f;

    private int _lastHealth = 3;

    public int TotalScore => totalScore;
    public int HighScore => highScore;
    public int ComboCount => comboCount;
    public float ComboTimer => comboTimer;
    public float Multiplier => CalculateMultiplier(comboCount);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void OnEnable()
    {
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
        GameEvents.OnPlayerHealthChanged += HandlePlayerHealthChanged;
        GameEvents.OnGameRestart += ResetGame;
    }

    private void OnDisable()
    {
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        GameEvents.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
        GameEvents.OnGameRestart -= ResetGame;
    }

    private void Start()
    {
        GameEvents.OnScoreChanged?.Invoke(totalScore);
        GameEvents.OnComboChanged?.Invoke(comboCount, Multiplier);
    }

    private void Update()
    {
        UpdateComboTimer(Time.deltaTime);
    }

    /// <summary>
    /// Advances combo decay timer by delta time. Resets combo when expired.
    /// </summary>
    public void UpdateComboTimer(float dt)
    {
        if (comboTimer > 0f)
        {
            comboTimer -= dt;
            if (comboTimer <= 0f)
            {
                comboCount = 0;
                comboTimer = 0f;
                GameEvents.OnComboChanged?.Invoke(0, 1.0f);
            }
        }
    }

    /// <summary>
    /// Registers an enemy elimination, updating combo count, multiplier, and total score.
    /// </summary>
    public void RegisterHit(EnemyType enemyType, int customBaseScore = 0)
    {
        comboCount++;
        comboTimer = DefaultComboDuration;
        float mult = CalculateMultiplier(comboCount);

        int baseScore = customBaseScore > 0 ? customBaseScore : GetDefaultBaseScore(enemyType);
        int delta = Mathf.RoundToInt(baseScore * mult);
        totalScore += delta;

        if (totalScore > highScore)
        {
            highScore = totalScore;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        GameEvents.OnScoreChanged?.Invoke(totalScore);
        GameEvents.OnComboChanged?.Invoke(comboCount, mult);
    }

    public void RegisterHit(EnemyType enemyType)
    {
        RegisterHit(enemyType, 0);
    }

    private void HandleEnemyKilled(EnemyType enemyType, int baseScore)
    {
        RegisterHit(enemyType, baseScore);
    }

    private void HandlePlayerHealthChanged(int current, int max)
    {
        // When player takes damage, reset combo immediately
        if (current < _lastHealth && current < max)
        {
            ResetCombo();
        }
        _lastHealth = current;
    }

    /// <summary>
    /// Resets combo count and timer to zero with 1.0x multiplier.
    /// </summary>
    public void ResetCombo()
    {
        comboCount = 0;
        comboTimer = 0f;
        GameEvents.OnComboChanged?.Invoke(0, 1.0f);
    }

    /// <summary>
    /// Resets current session score and combo while strictly preserving persistent high score.
    /// </summary>
    public void ResetGame()
    {
        totalScore = 0;
        comboCount = 0;
        comboTimer = 0f;
        _lastHealth = 3;

        GameEvents.OnScoreChanged?.Invoke(totalScore);
        GameEvents.OnComboChanged?.Invoke(comboCount, 1.0f);
    }

    public void SetInitialHighScore(int initial)
    {
        highScore = Mathf.Max(0, initial);
    }

    /// <summary>
    /// Calculates the stepped combo multiplier:
    /// 1-4: 1.0x, 5-9: 1.5x, 10-14: 2.0x, 15-19: 2.5x, 20+: 3.0x.
    /// </summary>
    public static float CalculateMultiplier(int count)
    {
        if (count < 5) return 1.0f;
        if (count < 10) return 1.5f;
        if (count < 15) return 2.0f;
        if (count < 20) return 2.5f;
        return 3.0f;
    }

    public static int GetDefaultBaseScore(EnemyType enemyType)
    {
        return enemyType switch
        {
            EnemyType.BasicSparrow => DefaultScoreBasic,
            EnemyType.FastFalcon => DefaultScoreFast,
            EnemyType.TankEagle => DefaultScoreTank,
            EnemyType.BossPhoenix => DefaultScoreBoss,
            _ => DefaultScoreBasic
        };
    }
}
