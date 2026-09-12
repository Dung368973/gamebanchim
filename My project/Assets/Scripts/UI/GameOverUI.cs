using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Modal dialog displayed upon player defeat.
/// Shows final score, high score, new record badge, and provides an instant restart button.
/// Supports both UnityEngine.UI.Text and TMPro.TMP_Text seamlessly.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    public const string HighScoreKey = "HIGH_SCORE_KEY";

    [Header("Modal Panel")]
    [SerializeField] private GameObject modalPanel;

    [Header("Score Display")]
    [SerializeField] private Text finalScoreText;
    [SerializeField] private TMP_Text finalScoreTMP;
    [SerializeField] private Text highScoreText;
    [SerializeField] private TMP_Text highScoreTMP;
    [SerializeField] private GameObject newBestBadge;

    [Header("Controls")]
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        if (modalPanel != null)
        {
            modalPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartButtonClicked);
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

    /// <summary>
    /// Activates game over modal, calculates new record status, formats score text, and plays SFX.
    /// </summary>
    public void HandleGameOver(int finalScore, int highScore)
    {
        finalScore = Mathf.Max(0, finalScore);
        highScore = Mathf.Max(0, highScore);

        // Verify and update stored high score
        int savedHigh = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (finalScore > savedHigh)
        {
            savedHigh = finalScore;
            PlayerPrefs.SetInt(HighScoreKey, savedHigh);
            PlayerPrefs.Save();
        }

        int displayHigh = Mathf.Max(highScore, savedHigh);
        bool isNewRecord = finalScore >= displayHigh && finalScore > 0;

        string finalStr = "SCORE: " + finalScore.ToString("N0", CultureInfo.InvariantCulture);
        string highStr = "BEST: " + displayHigh.ToString("N0", CultureInfo.InvariantCulture);

        SetText(finalScoreText, finalScoreTMP, finalStr);
        SetText(highScoreText, highScoreTMP, highStr);

        if (newBestBadge != null)
        {
            newBestBadge.SetActive(isNewRecord);
        }

        if (modalPanel != null)
        {
            modalPanel.SetActive(true);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.GameOver);
        }
    }

    public void OnRestartButtonClicked()
    {
        if (modalPanel != null)
        {
            modalPanel.SetActive(false);
        }

        GameEvents.OnGameRestart?.Invoke();
    }

    private void HandleGameRestart()
    {
        if (modalPanel != null)
        {
            modalPanel.SetActive(false);
        }
    }

    private static void SetText(Text uiText, TMP_Text tmpText, string content)
    {
        if (uiText != null) uiText.text = content;
        if (tmpText != null) tmpText.text = content;
    }
}
