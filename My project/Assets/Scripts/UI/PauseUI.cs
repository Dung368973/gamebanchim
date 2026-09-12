using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controls in-game pause functionality, HUD pause button, and pause modal dialog.
/// Supports both touch/mouse button interaction and keyboard shortcuts (Escape).
/// Coordinates with GameManager and Time.timeScale.
/// </summary>
public class PauseUI : MonoBehaviour
{
    [Header("HUD Controls")]
    [SerializeField] private Button pauseButton;

    [Header("Pause Modal Panel")]
    [SerializeField] private GameObject pauseModal;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;

    public bool IsPaused => pauseModal != null && pauseModal.activeSelf;

    private void Awake()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(PauseGame);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (pauseModal != null)
        {
            pauseModal.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(PauseGame);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnGameRestart += HandleGameRestart;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnGameRestart -= HandleGameRestart;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
#endif
    }

    public void TogglePause()
    {
        if (IsPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
        else
        {
            Time.timeScale = 0.0f;
        }

        if (pauseModal != null)
        {
            pauseModal.transform.SetAsLastSibling();
            pauseModal.SetActive(true);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
        else
        {
            Time.timeScale = 1.0f;
        }

        if (pauseModal != null)
        {
            pauseModal.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
        }
    }

    public void RestartGame()
    {
        if (pauseModal != null)
        {
            pauseModal.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Time.timeScale = 1.0f;
        }

        GameEvents.OnGameRestart?.Invoke();
    }

    private void HandleGameOver(int finalScore, int highScore)
    {
        if (pauseModal != null)
        {
            pauseModal.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }
    }

    private void HandleGameRestart()
    {
        if (pauseModal != null)
        {
            pauseModal.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
        }
    }
}
