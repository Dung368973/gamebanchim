using System;

public enum PowerUpType
{
    SpreadShot,
    RapidFire,
    HealthRecovery,
    Shield
}

public enum GameState
{
    Boot,
    MainMenu,
    Playing,
    WaveTransition,
    Paused,
    GameOver
}

public enum EnemyType
{
    BasicSparrow,
    FastFalcon,
    TankEagle,
    BossPhoenix
}

public enum SFXType
{
    Shoot,
    Hit,
    FeatherBurst,
    Explosion,
    PowerupPickup,
    GameOver,
    BossAlert,
    ShieldBreak
}

/// <summary>
/// Central decoupled event messaging bus for Game Bắn Chim.
/// Subscribed systems communicate without direct class dependencies.
/// </summary>
public static class GameEvents
{
    // Player Health & Death
    public static Action<int, int> OnPlayerHealthChanged; // (currentHealth, maxHealth)
    public static Action OnPlayerDied;
    public static Action<float> OnShieldChanged; // (shieldNormalized 0..1)

    // Scoring & Combos
    public static Action<int> OnScoreChanged; // (currentScore)
    public static Action<int, float> OnComboChanged; // (comboCount, multiplier)

    // Waves & Bosses
    public static Action<int> OnWaveStarted; // (waveNumber)
    public static Action<int> OnWaveCompleted; // (waveNumber)
    public static Action<string, float> OnBossHealthChanged; // (bossName, healthNormalized)
    public static Action OnBossDefeated;

    // Power-ups
    public static Action<PowerUpType, float> OnPowerUpCollected; // (type, duration)

    // Game Lifecycle
    public static Action<int, int> OnGameOver; // (finalScore, highScore)
    public static Action OnGameRestart;

    /// <summary>
    /// Clears all static event delegates to prevent memory leaks and dangling handlers across scene transitions.
    /// </summary>
    public static void ResetAllEvents()
    {
        OnPlayerHealthChanged = null;
        OnPlayerDied = null;
        OnShieldChanged = null;

        OnScoreChanged = null;
        OnComboChanged = null;

        OnWaveStarted = null;
        OnWaveCompleted = null;
        OnBossHealthChanged = null;
        OnBossDefeated = null;

        OnPowerUpCollected = null;

        OnGameOver = null;
        OnGameRestart = null;
    }
}
