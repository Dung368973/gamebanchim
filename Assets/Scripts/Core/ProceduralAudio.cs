using System;
using UnityEngine;

/// <summary>
/// Mathematical audio synthesis engine that generates high-quality retro arcade sound effects
/// and synthwave background music entirely in code without external audio files.
/// </summary>
public static class ProceduralAudio
{
    public const int SampleRate = 44100;

    #region Waveform Primitives

    public static float Sine(float phase)
    {
        return Mathf.Sin(phase * Mathf.PI * 2f);
    }

    public static float Square(float phase, float duty = 0.5f)
    {
        float p = phase - Mathf.Floor(phase);
        return p < duty ? 1f : -1f;
    }

    public static float Sawtooth(float phase)
    {
        float p = phase - Mathf.Floor(phase + 0.5f);
        return 2f * p;
    }

    public static float Triangle(float phase)
    {
        float p = phase - Mathf.Floor(phase + 0.75f) + 0.25f;
        return Mathf.Abs(4f * p) - 1f;
    }

    public static float Noise(System.Random rng)
    {
        return (float)(rng.NextDouble() * 2.0 - 1.0);
    }

    public static float ADSR(float t, float attack, float decay, float sustainLevel, float release, float totalDuration)
    {
        if (t < 0f || t > totalDuration) return 0f;

        if (t < attack)
        {
            return attack > 0f ? t / attack : 1f;
        }

        float decayEnd = attack + decay;
        if (t < decayEnd)
        {
            float progress = (t - attack) / decay;
            return Mathf.Lerp(1f, sustainLevel, progress);
        }

        float releaseStart = totalDuration - release;
        if (t < releaseStart)
        {
            return sustainLevel;
        }

        if (release > 0f)
        {
            float progress = (t - releaseStart) / release;
            return Mathf.Lerp(sustainLevel, 0f, progress);
        }

        return 0f;
    }

    #endregion

    #region SFX Generators

    /// <summary>
    /// Creates a crisp retro arcade laser shot sound.
    /// Fast exponential pitch drop 950Hz -> 220Hz, square/saw hybrid.
    /// </summary>
    public static AudioClip CreateShootClip()
    {
        float duration = 0.09f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float progress = t / duration;

            // Exponential frequency drop
            float freq = Mathf.Lerp(950f, 220f, progress * progress);
            phase += freq / SampleRate;

            // Mix square and saw for punch
            float wave = Square(phase, 0.45f) * 0.4f + Sawtooth(phase) * 0.6f;
            float envelope = Mathf.Exp(-progress * 6f);

            samples[i] = wave * envelope * 0.7f;
        }

        AudioClip clip = AudioClip.Create("sfx_shoot", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Creates an organic impact thud for bullet striking an enemy bird.
    /// Sine body with noise crack.
    /// </summary>
    public static AudioClip CreateHitClip()
    {
        float duration = 0.06f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        var rng = new System.Random(42);

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float progress = t / duration;

            float freq = Mathf.Lerp(420f, 160f, progress);
            phase += freq / SampleRate;

            float sine = Sine(phase);
            float noise = Noise(rng) * 0.35f;
            float env = Mathf.Exp(-progress * 8f);

            samples[i] = (sine * 0.65f + noise) * env * 0.8f;
        }

        AudioClip clip = AudioClip.Create("sfx_hit", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Creates a soft feather burst poof sound when a bird is destroyed.
    /// Bandpass swept noise simulating fluttering feathers.
    /// </summary>
    public static AudioClip CreateFeatherBurstClip()
    {
        float duration = 0.22f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        var rng = new System.Random(101);

        float filterState = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float progress = t / duration;

            // Low-pass filter smoothing coefficient that decreases over time
            float cutoff = Mathf.Lerp(0.25f, 0.03f, progress);
            float rawNoise = Noise(rng);
            filterState += cutoff * (rawNoise - filterState);

            float env = ADSR(t, 0.02f, 0.08f, 0.4f, 0.12f, duration);
            samples[i] = filterState * env * 0.85f;
        }

        AudioClip clip = AudioClip.Create("sfx_feather_burst", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Creates a heavy multi-stage explosion for boss destruction or player death.
    /// Deep sub-bass frequency drop with rumbling distorted noise.
    /// </summary>
    public static AudioClip CreateExplosionClip()
    {
        float duration = 0.85f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        var rng = new System.Random(777);

        float phase = 0f;
        float noiseFilter = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float progress = t / duration;

            // Sub bass drop 160 -> 30 Hz
            float freq = Mathf.Lerp(160f, 30f, Mathf.Pow(progress, 0.4f));
            phase += freq / SampleRate;

            float sub = Sine(phase);
            float rawNoise = Noise(rng);
            noiseFilter += 0.15f * (rawNoise - noiseFilter);

            // Distorted grit
            float distorted = Mathf.Clamp(noiseFilter * 2.5f, -1f, 1f);

            float env = Mathf.Exp(-progress * 4.5f);
            samples[i] = (sub * 0.45f + distorted * 0.55f) * env;
        }

        Normalize(samples, 0.9f);
        AudioClip clip = AudioClip.Create("sfx_explosion", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Creates an uplifting crystal chime for collecting power-ups.
    /// 4-note ascending major arpeggio: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz).
    /// </summary>
    public static AudioClip CreatePowerupClip()
    {
        float duration = 0.28f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.50f };
        float noteDuration = duration / freqs.Length;

        for (int note = 0; note < freqs.Length; note++)
        {
            float freq = freqs[note];
            int startSample = (int)(note * noteDuration * SampleRate);
            int endSample = (int)((note + 1) * noteDuration * SampleRate);
            float phase = 0f;

            for (int i = startSample; i < endSample && i < sampleCount; i++)
            {
                float noteT = (float)(i - startSample) / SampleRate;
                float noteProgress = noteT / noteDuration;

                phase += freq / SampleRate;
                float wave = Sine(phase) * 0.75f + Triangle(phase * 2f) * 0.25f;
                float env = ADSR(noteT, 0.005f, 0.04f, 0.5f, 0.025f, noteDuration);

                samples[i] = wave * env * 0.75f;
            }
        }

        AudioClip clip = AudioClip.Create("sfx_powerup", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Creates a descending minor chord motif for Game Over defeat.
    /// Notes: A4 (440Hz), F4 (349Hz), D4 (293Hz), Bb3 (233Hz).
    /// </summary>
    public static AudioClip CreateGameOverClip()
    {
        float duration = 0.85f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        float[] freqs = { 440.00f, 349.23f, 293.66f, 233.08f };
        float noteDuration = duration / freqs.Length;

        for (int note = 0; note < freqs.Length; note++)
        {
            float freq = freqs[note];
            int startSample = (int)(note * noteDuration * SampleRate);
            int endSample = (int)((note + 1) * noteDuration * SampleRate);
            float phase = 0f;

            for (int i = startSample; i < endSample && i < sampleCount; i++)
            {
                float noteT = (float)(i - startSample) / SampleRate;
                phase += freq / SampleRate;

                float wave = Triangle(phase) * 0.7f + Square(phase, 0.5f) * 0.3f;
                float env = ADSR(noteT, 0.01f, 0.12f, 0.4f, 0.08f, noteDuration);

                samples[i] = wave * env * 0.7f;
            }
        }

        AudioClip clip = AudioClip.Create("sfx_game_over", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Creates a two-tone warning siren for boss alerts.
    /// </summary>
    public static AudioClip CreateBossAlertClip()
    {
        float duration = 0.75f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float sirenCycle = (t * 4f) % 1f;
            float freq = sirenCycle < 0.5f ? 440f : 660f;
            phase += freq / SampleRate;

            float wave = Sawtooth(phase) * 0.6f + Sine(phase) * 0.4f;
            float env = ADSR(t, 0.05f, 0.1f, 0.8f, 0.15f, duration);

            samples[i] = wave * env * 0.65f;
        }

        AudioClip clip = AudioClip.Create("sfx_boss_alert", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Creates an energy shield shatter sound.
    /// </summary>
    public static AudioClip CreateShieldBreakClip()
    {
        float duration = 0.35f;
        int sampleCount = (int)(SampleRate * duration);
        float[] samples = new float[sampleCount];
        var rng = new System.Random(333);

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float progress = t / duration;

            float freq = Mathf.Lerp(720f, 240f, progress);
            phase += freq / SampleRate;

            float tone = Sine(phase);
            float crackle = Noise(rng) * 0.4f;
            float env = Mathf.Exp(-progress * 5f);

            samples[i] = (tone * 0.6f + crackle) * env * 0.8f;
        }

        AudioClip clip = AudioClip.Create("sfx_shield_break", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    #endregion

    #region Synthwave BGM Loop Generator

    /// <summary>
    /// Generates a seamless 16-beat (4-bar) synthwave background music loop at 128 BPM.
    /// Chord progression: Am -> F -> C -> G.
    /// Features kick, snare, hi-hat, pulsing synth bass, and a 16th-note arpeggiated lead melody.
    /// </summary>
    public static AudioClip CreateSynthwaveBGMClip()
    {
        float bpm = 128f;
        float beatsPerBar = 4f;
        int barCount = 4;
        float totalBeats = beatsPerBar * barCount;
        float beatDuration = 60f / bpm; // ~0.46875s
        float totalDuration = totalBeats * beatDuration; // ~7.5s

        int sampleCount = (int)(SampleRate * totalDuration);
        float[] masterBuffer = new float[sampleCount];

        var rng = new System.Random(2026);

        // Chords frequencies: Am, F, C, G
        // Bass roots: A1 (55Hz), F1 (43.65Hz), C2 (65.41Hz), G1 (49.00Hz)
        float[] bassRoots = { 55.00f, 43.65f, 65.41f, 49.00f };

        // Chord triads for arpeggiator:
        // Am: A3, C4, E4, A4
        // F:  F3, A3, C4, F4
        // C:  C3, E3, G3, C4
        // G:  G3, B3, D4, G4
        float[][] chordTriads = new float[][]
        {
            new float[] { 220.00f, 261.63f, 329.63f, 440.00f }, // Am
            new float[] { 174.61f, 220.00f, 261.63f, 349.23f }, // F
            new float[] { 130.81f, 164.81f, 196.00f, 261.63f }, // C
            new float[] { 196.00f, 246.94f, 293.66f, 392.00f }  // G
        };

        float bassPhase = 0f;
        float leadPhase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float currentBeat = t / beatDuration;
            int barIndex = Mathf.Clamp((int)(currentBeat / beatsPerBar), 0, barCount - 1);
            float beatInBar = currentBeat % beatsPerBar;

            float sample = 0f;

            // 1. Kick Drum (Beats 0, 1, 2, 3)
            float beatProgress = currentBeat % 1.0f;
            float kickT = beatProgress * beatDuration;
            if (kickT < 0.18f)
            {
                float kickFreq = Mathf.Lerp(130f, 45f, kickT / 0.18f);
                float kickEnv = Mathf.Exp(-kickT * 22f);
                sample += Sine(kickT * kickFreq) * kickEnv * 0.55f;
            }

            // 2. Snare / Clap (Beats 1 and 3)
            float snarePhaseTime = (currentBeat % 2.0f);
            if (snarePhaseTime >= 1.0f && snarePhaseTime < 1.35f)
            {
                float snareT = (snarePhaseTime - 1.0f) * beatDuration;
                if (snareT < 0.18f)
                {
                    float snareNoise = Noise(rng);
                    float snareTone = Sine(snareT * 200f) * 0.4f;
                    float snareEnv = Mathf.Exp(-snareT * 20f);
                    sample += (snareNoise * 0.6f + snareTone) * snareEnv * 0.35f;
                }
            }

            // 3. Hi-Hat (8th note offbeats)
            float offbeat = (currentBeat * 2f) % 1.0f;
            float hatT = offbeat * (beatDuration * 0.5f);
            if (hatT < 0.05f)
            {
                float hatNoise = Noise(rng);
                float hatEnv = Mathf.Exp(-hatT * 60f);
                sample += hatNoise * hatEnv * 0.18f;
            }

            // 4. Bass Synth (Driving 8th notes)
            float bassRoot = bassRoots[barIndex];
            float eighthProgress = (currentBeat * 2f) % 1.0f;
            float eighthT = eighthProgress * (beatDuration * 0.5f);

            bassPhase += bassRoot / SampleRate;
            float bassWave = Square(bassPhase, 0.5f) * 0.7f + Triangle(bassPhase) * 0.3f;
            float bassEnv = Mathf.Exp(-eighthT * 6f);
            sample += bassWave * bassEnv * 0.32f;

            // 5. Lead Synth Arpeggio (16th notes floating across chord triad)
            float sixteenthProgress = (currentBeat * 4f) % 1.0f;
            float sixteenthT = sixteenthProgress * (beatDuration * 0.25f);
            int arpIndex = ((int)(currentBeat * 4f)) % 4;

            float leadFreq = chordTriads[barIndex][arpIndex];
            leadPhase += leadFreq / SampleRate;

            float leadWave = Sawtooth(leadPhase) * 0.5f + Sine(leadPhase) * 0.5f;
            float leadEnv = Mathf.Exp(-sixteenthT * 12f);
            sample += leadWave * leadEnv * 0.25f;

            masterBuffer[i] = sample;
        }

        Normalize(masterBuffer, 0.85f);

        AudioClip clip = AudioClip.Create("bgm_synthwave_loop", sampleCount, 1, SampleRate, false);
        clip.SetData(masterBuffer, 0);
        return clip;
    }

    #endregion

    #region Utilities

    private static void Normalize(float[] buffer, float targetPeak)
    {
        float max = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            float abs = Mathf.Abs(buffer[i]);
            if (abs > max) max = abs;
        }

        if (max > 0.0001f)
        {
            float scale = targetPeak / max;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] *= scale;
            }
        }
    }

    #endregion
}
