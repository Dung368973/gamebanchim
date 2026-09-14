using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central Audio Manager matching PROJECT.md interface contracts.
/// Features high-performance SFX voice pooling, pitch modulation for game-feel juice,
/// and automated procedural fallback audio synthesis.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.9f;
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.65f;

    [Header("Voice Pooling")]
    [SerializeField] private int sfxVoicePoolSize = 12;

    [Header("Pre-assigned Clips (Optional - auto-generated if null)")]
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip featherBurstClip;
    [SerializeField] private AudioClip explosionClip;
    [SerializeField] private AudioClip powerupClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip bossAlertClip;
    [SerializeField] private AudioClip shieldBreakClip;
    [SerializeField] private AudioClip bgmLoopClip;

    private AudioSource bgmSource;
    private List<AudioSource> sfxVoices;
    private int nextVoiceIndex = 0;
    private readonly Dictionary<SFXType, AudioClip> clipCache = new Dictionary<SFXType, AudioClip>();

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            UpdateBGMVolume();
        }
    }

    public float SFXVolume
    {
        get => sfxVolume;
        set => sfxVolume = Mathf.Clamp01(value);
    }

    public float BGMVolume
    {
        get => bgmVolume;
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            UpdateBGMVolume();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();
        InitializeAudioClips();
    }

    private void InitializeAudioSources()
    {
        // 1. Setup BGM Source
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.priority = 0; // Highest priority
        UpdateBGMVolume();

        // 2. Setup SFX Voices Pool
        sfxVoices = new List<AudioSource>(sfxVoicePoolSize);
        for (int i = 0; i < sfxVoicePoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.priority = 128;
            sfxVoices.Add(source);
        }
    }

    private void InitializeAudioClips()
    {
        // Generate or cache SFX clips
        clipCache[SFXType.Shoot] = shootClip != null ? shootClip : ProceduralAudio.CreateShootClip();
        clipCache[SFXType.Hit] = hitClip != null ? hitClip : ProceduralAudio.CreateHitClip();
        clipCache[SFXType.FeatherBurst] = featherBurstClip != null ? featherBurstClip : ProceduralAudio.CreateFeatherBurstClip();
        clipCache[SFXType.Explosion] = explosionClip != null ? explosionClip : ProceduralAudio.CreateExplosionClip();
        clipCache[SFXType.PowerupPickup] = powerupClip != null ? powerupClip : ProceduralAudio.CreatePowerupClip();
        clipCache[SFXType.GameOver] = gameOverClip != null ? gameOverClip : ProceduralAudio.CreateGameOverClip();
        clipCache[SFXType.BossAlert] = bossAlertClip != null ? bossAlertClip : ProceduralAudio.CreateBossAlertClip();
        clipCache[SFXType.ShieldBreak] = shieldBreakClip != null ? shieldBreakClip : ProceduralAudio.CreateShieldBreakClip();

        // Generate or cache BGM clip
        if (bgmLoopClip == null)
        {
            bgmLoopClip = ProceduralAudio.CreateSynthwaveBGMClip();
        }

        bgmSource.clip = bgmLoopClip;
    }

    /// <summary>
    /// Plays a sound effect by SFXType with optional volume and randomized pitch jitter to avoid repetition fatigue.
    /// </summary>
    public void PlaySFX(SFXType type, float volume = 1f, float pitchJitter = 0.05f)
    {
        if (!clipCache.TryGetValue(type, out AudioClip clip) || clip == null)
        {
            return;
        }

        PlaySFX(clip, volume, pitchJitter);
    }

    /// <summary>
    /// Plays an arbitrary AudioClip with voice pooling and pitch jitter.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchJitter = 0.05f)
    {
        if (clip == null || sfxVoices == null || sfxVoices.Count == 0) return;

        AudioSource voice = GetAvailableSFXVoice();
        float jitter = pitchJitter > 0f ? Random.Range(-pitchJitter, pitchJitter) : 0f;
        voice.pitch = Mathf.Clamp(1f + jitter, 0.5f, 2.0f);
        voice.volume = Mathf.Clamp01(volume * sfxVolume * masterVolume);
        voice.clip = clip;
        voice.Play();
    }

    /// <summary>
    /// Starts playing the looping background music.
    /// </summary>
    public void PlayBGM()
    {
        if (bgmSource == null) return;

        if (bgmSource.clip == null && bgmLoopClip != null)
        {
            bgmSource.clip = bgmLoopClip;
        }

        UpdateBGMVolume();

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    /// <summary>
    /// Plays a custom background music clip.
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        UpdateBGMVolume();
        bgmSource.Play();
    }

    /// <summary>
    /// Stops background music playback.
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }

    /// <summary>
    /// Pauses BGM playback.
    /// </summary>
    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
    }

    /// <summary>
    /// Resumes BGM playback if paused.
    /// </summary>
    public void ResumeBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.UnPause();
        }
    }

    private void UpdateBGMVolume()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = Mathf.Clamp01(bgmVolume * masterVolume);
        }
    }

    private AudioSource GetAvailableSFXVoice()
    {
        // 1. Check for any idle voice
        for (int i = 0; i < sfxVoices.Count; i++)
        {
            if (!sfxVoices[i].isPlaying)
            {
                return sfxVoices[i];
            }
        }

        // 2. Round-robin fallback if all voices are currently busy
        AudioSource voice = sfxVoices[nextVoiceIndex];
        nextVoiceIndex = (nextVoiceIndex + 1) % sfxVoices.Count;
        return voice;
    }
}
