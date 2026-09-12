using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Central game lifecycle coordinator and finite state machine (FSM).
/// Regulates progression through Boot, MainMenu, Playing, WaveTransition, Paused, and GameOver.
/// Handles instant restart, wave advancing, and system-wide state broadcasting.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("State Machine")]
    [SerializeField] private GameState currentState = GameState.Boot;
    [SerializeField] private int currentWave = 1;
    [SerializeField] private float waveTransitionDuration = 2.5f;

    public GameState CurrentState => currentState;
    public int CurrentWave => currentWave;

    public event Action<GameState, GameState> OnStateChanged;

    private Coroutine _waveTransitionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDied += HandlePlayerDied;
        GameEvents.OnWaveCompleted += HandleWaveCompleted;
        GameEvents.OnGameRestart += HandleGameRestart;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        GameEvents.OnWaveCompleted -= HandleWaveCompleted;
        GameEvents.OnGameRestart -= HandleGameRestart;
    }

    private void Start()
    {
        Time.timeScale = 1.0f;
        if (currentState == GameState.Boot || currentState == GameState.MainMenu)
        {
            currentState = GameState.Playing;
            GameEvents.OnWaveStarted?.Invoke(currentWave);
        }
    }

    /// <summary>
    /// Attempts transition to requested state according to the authorized state transition matrix.
    /// Returns true if transition succeeded; false if rejected.
    /// </summary>
    public bool TransitionTo(GameState next)
    {
        if (!IsValidTransition(currentState, next))
        {
            return false;
        }

        GameState prev = currentState;
        currentState = next;

        HandleStateTransition(prev, next);
        OnStateChanged?.Invoke(prev, next);
        return true;
    }

    private void HandleStateTransition(GameState prev, GameState next)
    {
        switch (next)
        {
            case GameState.Playing:
                Time.timeScale = 1.0f;
                if (prev == GameState.GameOver || prev == GameState.Paused)
                {
                    currentWave = 1;
                }
                break;

            case GameState.Paused:
                Time.timeScale = 0.0f;
                break;

            case GameState.WaveTransition:
                currentWave++;
                if (_waveTransitionCoroutine != null)
                {
                    StopCoroutine(_waveTransitionCoroutine);
                }
                _waveTransitionCoroutine = StartCoroutine(WaveTransitionRoutine());
                break;

            case GameState.GameOver:
                int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.TotalScore : 0;
                int highScore = PlayerPrefs.GetInt("HIGH_SCORE_KEY", finalScore);
                if (finalScore > highScore)
                {
                    highScore = finalScore;
                }
                GameEvents.OnGameOver?.Invoke(finalScore, highScore);
                break;
        }
    }

    private IEnumerator WaveTransitionRoutine()
    {
        yield return new WaitForSeconds(waveTransitionDuration);

        if (currentState == GameState.WaveTransition)
        {
            TransitionTo(GameState.Playing);
            GameEvents.OnWaveStarted?.Invoke(currentWave);
        }
        _waveTransitionCoroutine = null;
    }

    public void PauseGame()
    {
        TransitionTo(GameState.Paused);
    }

    public void ResumeGame()
    {
        TransitionTo(GameState.Playing);
    }

    public void StartGame()
    {
        if (currentState == GameState.MainMenu)
        {
            TransitionTo(GameState.Playing);
            GameEvents.OnWaveStarted?.Invoke(currentWave);
        }
    }

    /// <summary>
    /// Reinitializes game loop: restores wave to 1, resets timeScale, and dispatches restart.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1.0f;
        currentWave = 1;

        TransitionTo(GameState.Playing);

        // Reset player health and position if available in scene
        var playerHealth = FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        if (playerHealth != null)
        {
            playerHealth.gameObject.SetActive(true);
            playerHealth.ResetHealth();
            playerHealth.transform.position = new Vector3(0f, -3.5f, 0f);
        }

        GameEvents.OnWaveStarted?.Invoke(currentWave);
    }

    private void HandlePlayerDied()
    {
        TransitionTo(GameState.GameOver);
    }

    private void HandleWaveCompleted(int waveNumber)
    {
        if (currentState == GameState.Playing)
        {
            TransitionTo(GameState.WaveTransition);
        }
    }

    private void HandleGameRestart()
    {
        RestartGame();
    }

    /// <summary>
    /// Validates whether the requested transition between states is permissible.
    /// </summary>
    public static bool IsValidTransition(GameState from, GameState to)
    {
        return (from, to) switch
        {
            (GameState.Boot, GameState.MainMenu) => true,
            (GameState.Boot, GameState.Playing) => true,
            (GameState.Boot, GameState.GameOver) => true,
            (GameState.MainMenu, GameState.Playing) => true,
            (GameState.MainMenu, GameState.GameOver) => true,
            (GameState.Playing, GameState.Paused) => true,
            (GameState.Paused, GameState.Playing) => true,
            (GameState.Playing, GameState.WaveTransition) => true,
            (GameState.WaveTransition, GameState.Playing) => true,
            (GameState.Playing, GameState.GameOver) => true,
            (GameState.WaveTransition, GameState.GameOver) => true,
            (GameState.Paused, GameState.GameOver) => true,
            (GameState.GameOver, GameState.Playing) => true,
            (GameState.GameOver, GameState.MainMenu) => true,
            _ => false
        };
    }
}
