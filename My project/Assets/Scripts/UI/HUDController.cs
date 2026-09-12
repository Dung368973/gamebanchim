using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the in-game Heads-Up Display (HUD): Score, High Score, Wave, Combo indicator,
/// Player Lives (hearts/bar), Shield energy gauge, and Boss health.
/// Subscribes strictly to GameEvents for zero-coupling architecture.
/// Supports both legacy UnityEngine.UI.Text and modern TMPro.TMP_Text seamlessly.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private Text scoreText;
    [SerializeField] private TMP_Text scoreTMP;
    [SerializeField] private Text highScoreText;
    [SerializeField] private TMP_Text highScoreTMP;

    [Header("Wave Display")]
    [SerializeField] private Text waveText;
    [SerializeField] private TMP_Text waveTMP;

    [Header("Combo Display")]
    [SerializeField] private GameObject comboRoot;
    [SerializeField] private Text comboText;
    [SerializeField] private TMP_Text comboTMP;
    [SerializeField] private Image comboFillBar;
    [SerializeField] private Transform comboScaleTarget;

    [Header("Health Display")]
    [SerializeField] private Image[] heartImages;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Image healthFillBar;

    [Header("Shield Display")]
    [SerializeField] private GameObject shieldRoot;
    [SerializeField] private Image shieldFillBar;

    [Header("Boss Display")]
    [SerializeField] private GameObject bossRoot;
    [SerializeField] private Text bossNameText;
    [SerializeField] private TMP_Text bossNameTMP;
    [SerializeField] private Image bossFillBar;

    private Coroutine _comboPopCoroutine;
    private int _currentScore = 0;
    private int _currentCombo = 0;

    private void OnEnable()
    {
        GameEvents.OnScoreChanged += HandleScoreChanged;
        GameEvents.OnComboChanged += HandleComboChanged;
        GameEvents.OnWaveStarted += HandleWaveStarted;
        GameEvents.OnPlayerHealthChanged += HandlePlayerHealthChanged;
        GameEvents.OnShieldChanged += HandleShieldChanged;
        GameEvents.OnBossHealthChanged += HandleBossHealthChanged;
        GameEvents.OnBossDefeated += HandleBossDefeated;
        GameEvents.OnGameRestart += HandleGameRestart;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreChanged -= HandleScoreChanged;
        GameEvents.OnComboChanged -= HandleComboChanged;
        GameEvents.OnWaveStarted -= HandleWaveStarted;
        GameEvents.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
        GameEvents.OnShieldChanged -= HandleShieldChanged;
        GameEvents.OnBossHealthChanged -= HandleBossHealthChanged;
        GameEvents.OnBossDefeated -= HandleBossDefeated;
        GameEvents.OnGameRestart -= HandleGameRestart;
    }

    private void Start()
    {
        InitializeHUD();
    }

    private void Update()
    {
        if (ScoreManager.Instance != null && ScoreManager.Instance.ComboTimer > 0f)
        {
            UpdateComboDecay(ScoreManager.Instance.ComboTimer, ScoreManager.DefaultComboDuration);
        }
        else if (comboFillBar != null && comboFillBar.fillAmount > 0f)
        {
            comboFillBar.fillAmount = 0f;
        }
    }

    public void InitializeHUD()
    {
        HandleScoreChanged(0);
        HandleWaveStarted(1);
        HandleComboChanged(0, 1.0f);
        HandlePlayerHealthChanged(3, 3);
        HandleShieldChanged(0f);
        HandleBossDefeated();

        int storedHigh = PlayerPrefs.GetInt("HIGH_SCORE_KEY", 0);
        UpdateHighScoreText(storedHigh);
    }

    public void HandleScoreChanged(int score)
    {
        _currentScore = Mathf.Max(0, score);
        string formatted = _currentScore.ToString("N0", CultureInfo.InvariantCulture);
        SetText(scoreText, scoreTMP, formatted);

        int currentHigh = PlayerPrefs.GetInt("HIGH_SCORE_KEY", 0);
        if (_currentScore > currentHigh)
        {
            currentHigh = _currentScore;
        }
        UpdateHighScoreText(currentHigh);
    }

    public void UpdateHighScoreText(int high)
    {
        string formatted = high.ToString("N0", CultureInfo.InvariantCulture);
        SetText(highScoreText, highScoreTMP, formatted);
    }

    public void HandleWaveStarted(int wave)
    {
        string formatted = "WAVE " + Mathf.Max(1, wave);
        SetText(waveText, waveTMP, formatted);
    }

    public void HandleComboChanged(int count, float multiplier)
    {
        _currentCombo = count;

        if (count > 1)
        {
            if (comboRoot != null && !comboRoot.activeSelf)
            {
                comboRoot.SetActive(true);
            }

            string text = $"{count}x COMBO! ({multiplier:F1}x)";
            SetText(comboText, comboTMP, text);

            TriggerComboPop();
        }
        else
        {
            if (comboRoot != null && comboRoot.activeSelf)
            {
                comboRoot.SetActive(false);
            }
        }
    }

    public void UpdateComboDecay(float remaining, float maxDuration = 3.0f)
    {
        if (comboFillBar != null && maxDuration > 0f)
        {
            comboFillBar.fillAmount = Mathf.Clamp01(remaining / maxDuration);
        }
    }

    public void HandlePlayerHealthChanged(int current, int max)
    {
        current = Mathf.Max(0, current);
        max = Mathf.Max(1, max);

        if (heartImages != null)
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] == null) continue;
                bool isFull = i < current;

                if (fullHeartSprite != null && emptyHeartSprite != null)
                {
                    heartImages[i].sprite = isFull ? fullHeartSprite : emptyHeartSprite;
                }
                else
                {
                    heartImages[i].color = isFull ? Color.white : new Color(1f, 1f, 1f, 0.25f);
                }
            }
        }

        if (healthFillBar != null)
        {
            healthFillBar.fillAmount = Mathf.Clamp01((float)current / max);
        }
    }

    public void HandleShieldChanged(float normalized)
    {
        bool active = normalized > 0f;
        if (shieldRoot != null)
        {
            shieldRoot.SetActive(active);
        }

        if (shieldFillBar != null)
        {
            shieldFillBar.fillAmount = Mathf.Clamp01(normalized);
        }
    }

    public void HandleBossHealthChanged(string bossName, float normalized)
    {
        if (bossRoot != null)
        {
            bossRoot.SetActive(normalized > 0f);
        }

        SetText(bossNameText, bossNameTMP, bossName);

        if (bossFillBar != null)
        {
            bossFillBar.fillAmount = Mathf.Clamp01(normalized);
        }
    }

    public void HandleBossDefeated()
    {
        if (bossRoot != null)
        {
            bossRoot.SetActive(false);
        }
    }

    public void HandleGameRestart()
    {
        InitializeHUD();
    }

    private void TriggerComboPop()
    {
        if (comboScaleTarget == null) return;

        if (_comboPopCoroutine != null)
        {
            StopCoroutine(_comboPopCoroutine);
        }
        _comboPopCoroutine = StartCoroutine(ComboPopRoutine());
    }

    private IEnumerator ComboPopRoutine()
    {
        float elapsed = 0f;
        float duration = 0.15f;
        Vector3 startScale = Vector3.one * 1.3f;
        Vector3 targetScale = Vector3.one;

        comboScaleTarget.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            comboScaleTarget.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        comboScaleTarget.localScale = targetScale;
        _comboPopCoroutine = null;
    }

    private static void SetText(Text uiText, TMP_Text tmpText, string content)
    {
        if (uiText != null) uiText.text = content;
        if (tmpText != null) tmpText.text = content;
    }
}
