using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShootEmUp.Tests.E2E
{
    #region F01: Mobile Touch / Mouse Drag
    public class TouchDragSimulation
    {
        public Vector2 PlayerPosition { get; set; }
        public Vector2 TouchOffset { get; set; }
        public bool IsDragging { get; private set; }
        public int ActiveFingerId { get; private set; } = -1;

        public const float DefaultErgonomicLift = 0.6f;
        public const float DefaultPlayerSpeed = 8.0f;
        public const float DefaultSmoothingLambda = 25.0f;

        public void OnTouchDown(int fingerId, Vector2 touchWorldPos, bool useErgonomicLift = false)
        {
            if (IsDragging) return; // Ignore multi-touch secondary fingers

            ActiveFingerId = fingerId;
            IsDragging = true;
            if (useErgonomicLift)
            {
                TouchOffset = new Vector2(0f, DefaultErgonomicLift);
            }
            else
            {
                TouchOffset = PlayerPosition - touchWorldPos;
            }
        }

        public void OnTouchMove(int fingerId, Vector2 touchWorldPos, float dt, bool useSmoothing = false)
        {
            if (!IsDragging || fingerId != ActiveFingerId) return;

            Vector2 target = touchWorldPos + TouchOffset;
            if (useSmoothing)
            {
                if (dt <= 0f) return;
                float factor = 1f - Mathf.Exp(-DefaultSmoothingLambda * dt);
                PlayerPosition = Vector2.Lerp(PlayerPosition, target, factor);
            }
            else
            {
                PlayerPosition = target;
            }
        }

        public void OnTouchUp(int fingerId)
        {
            if (fingerId == ActiveFingerId)
            {
                IsDragging = false;
                ActiveFingerId = -1;
            }
        }

        public void OnKeyboardInput(Vector2 inputDirection, float dt)
        {
            if (IsDragging) return; // Touch drag overrides keyboard
            Vector2 clampedDir = Vector2.ClampMagnitude(inputDirection, 1.0f);
            PlayerPosition += clampedDir * (DefaultPlayerSpeed * dt);
        }
    }
    #endregion

    #region F02: Viewport Clamping
    public class ViewportClampingModel
    {
        public float OrthoSize { get; set; } = 6.0f;
        public float AspectRatio { get; set; } = 9.0f / 16.0f; // 0.5625
        public Vector2 CameraCenter { get; set; } = Vector2.zero;
        public Vector2 PlayerExtents { get; set; } = new Vector2(0.4f, 0.4f);
        public Vector2 Padding { get; set; } = new Vector2(0.1f, 0.1f);
        public float BottomBarPadding { get; set; } = 0.3f;

        public float OrthoHalfWidth => OrthoSize * AspectRatio; // 3.375
        public float MinX => CameraCenter.x - OrthoHalfWidth + PlayerExtents.x + Padding.x;
        public float MaxX => CameraCenter.x + OrthoHalfWidth - PlayerExtents.x - Padding.x;
        public float MinY => CameraCenter.y - OrthoSize + PlayerExtents.y + Padding.y + BottomBarPadding;
        public float MaxY => CameraCenter.y + (OrthoSize * 0.25f); // Clamps to lower 62.5%

        public Vector2 Clamp(Vector2 pos)
        {
            float x = Mathf.Clamp(pos.x, MinX, MaxX);
            float y = Mathf.Clamp(pos.y, MinY, MaxY);
            return new Vector2(x, y);
        }
    }
    #endregion

    #region F03: Auto-Shooting System
    public class AutoShootingModel
    {
        public const float BaseFireInterval = 0.20f; // 5.0 shots/sec
        public const float BulletSpeed = 14.0f;

        public float FireTimer { get; set; } = 0f;
        public bool IsRapidFireActive { get; set; } = false;
        public bool IsEnabled { get; set; } = true;

        public float EffectiveInterval => IsRapidFireActive ? (BaseFireInterval * 0.5f) : BaseFireInterval;

        public int Update(float dt, Action spawnCallback = null)
        {
            if (!IsEnabled || dt <= 0f) return 0;

            FireTimer += dt;
            int shots = 0;
            float interval = EffectiveInterval;

            while (FireTimer >= interval - 0.0001f)
            {
                FireTimer = Mathf.Max(0f, FireTimer - interval);
                shots++;
                spawnCallback?.Invoke();
            }

            return shots;
        }

        public static Vector2[] GetSpreadVelocities(int weaponLevel, float speed = BulletSpeed)
        {
            switch (weaponLevel)
            {
                case 1: // 3-way spread
                    return new Vector2[]
                    {
                        CalculateSpreadVelocity(-15f, speed),
                        CalculateSpreadVelocity(0f, speed),
                        CalculateSpreadVelocity(15f, speed)
                    };
                case 2: // 5-way spread
                    return new Vector2[]
                    {
                        CalculateSpreadVelocity(-30f, speed),
                        CalculateSpreadVelocity(-15f, speed),
                        CalculateSpreadVelocity(0f, speed),
                        CalculateSpreadVelocity(15f, speed),
                        CalculateSpreadVelocity(30f, speed)
                    };
                default: // 1-way single shot
                    return new Vector2[] { new Vector2(0f, speed) };
            }
        }

        private static Vector2 CalculateSpreadVelocity(float angleDeg, float speed)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(-Mathf.Sin(rad) * speed, Mathf.Cos(rad) * speed);
        }
    }
    #endregion

    #region F05: Player Health & Invulnerability
    public class PlayerHealthModel
    {
        public const int MaxLives = 3;
        public const float DefaultIFramesDuration = 2.0f;
        public const float ShieldGracePeriod = 1.0f;

        public int CurrentLives { get; private set; } = MaxLives;
        public float InvulnerabilityTimer { get; private set; } = 0f;
        public bool HasShield { get; private set; } = false;
        public float ShieldDurationTimer { get; private set; } = 0f;

        public bool IsInvulnerable => InvulnerabilityTimer > 0f;
        public bool IsAlive => CurrentLives > 0;

        public void ActivateShield(float duration = 8.0f)
        {
            HasShield = true;
            ShieldDurationTimer = duration;
        }

        public void Update(float dt)
        {
            if (InvulnerabilityTimer > 0f)
            {
                InvulnerabilityTimer = Mathf.Max(0f, InvulnerabilityTimer - dt);
            }

            if (HasShield && ShieldDurationTimer > 0f)
            {
                ShieldDurationTimer -= dt;
                if (ShieldDurationTimer <= 0f)
                {
                    HasShield = false;
                }
            }
        }

        public bool TakeDamage(int amount = 1)
        {
            if (!IsAlive || amount <= 0) return false;

            if (HasShield)
            {
                HasShield = false;
                InvulnerabilityTimer = ShieldGracePeriod;
                return false; // Shield absorbed hit, no lives lost
            }

            if (IsInvulnerable) return false;

            CurrentLives = Mathf.Max(0, CurrentLives - amount);
            InvulnerabilityTimer = DefaultIFramesDuration;
            return true; // Damage applied
        }

        public bool Heal(int amount = 1)
        {
            if (!IsAlive || CurrentLives >= MaxLives || amount <= 0) return false;
            CurrentLives = Mathf.Min(MaxLives, CurrentLives + amount);
            return true;
        }

        public void Reset()
        {
            CurrentLives = MaxLives;
            InvulnerabilityTimer = 0f;
            HasShield = false;
            ShieldDurationTimer = 0f;
        }
    }
    #endregion

    #region F06-F08: Bird Trajectories
    public static class EnemyKinematics
    {
        public static Vector2 BasicBirdPosition(Vector2 origin, Vector2 flockOffset, float speedY, float speedX, float time)
        {
            return new Vector2(
                origin.x + flockOffset.x + (speedX * time),
                origin.y + flockOffset.y - (speedY * time)
            );
        }

        public static Vector2 FastBirdSinePosition(float x0, float y0, float amplitude, float omega, float phase, float speedY, float time)
        {
            float x = x0 + (amplitude * Mathf.Sin((omega * time) + phase));
            float y = y0 - (speedY * time);
            return new Vector2(x, y);
        }

        public static float FastBirdHeadingAngle(float amplitude, float omega, float phase, float speedY, float time)
        {
            float vx = amplitude * omega * Mathf.Cos((omega * time) + phase);
            float vy = -speedY;
            return Mathf.Atan2(vy, vx) - (Mathf.PI * 0.5f);
        }

        public static Vector2 TankBirdPosition(Vector2 origin, float targetY, float entrySpeed, float sweepAmp, float sweepOmega, float time)
        {
            float entryTime = Mathf.Max(0f, (origin.y - targetY) / entrySpeed);
            if (time < entryTime)
            {
                return new Vector2(origin.x, origin.y - (entrySpeed * time));
            }
            else
            {
                float sweepTime = time - entryTime;
                float x = origin.x + (sweepAmp * Mathf.Sin(sweepOmega * sweepTime));
                return new Vector2(x, targetY);
            }
        }
    }
    #endregion

    #region F11: Wave Spawner Difficulty Scaling
    public static class WaveScalingMath
    {
        public static float CalculateSpawnInterval(int wave)
        {
            return Mathf.Max(0.35f, 1.4f * Mathf.Pow(0.90f, Mathf.Max(0, wave - 1)));
        }

        public static float CalculateSpeedMultiplier(int wave)
        {
            return Mathf.Min(1.85f, 1.0f + (0.04f * Mathf.Max(0, wave - 1)));
        }

        public static int CalculateBossHealth(int wave, int baseHealth = 30)
        {
            return baseHealth + (Mathf.Max(0, wave - 1) * 10);
        }

        public static (float basic, float fast, float tank) CalculateMixProbabilities(int wave)
        {
            float basic = Mathf.Max(0.25f, 1.0f - (0.08f * Mathf.Max(0, wave - 1)));
            float fast = Mathf.Min(0.55f, 0.0f + (0.06f * Mathf.Max(0, wave - 1)));
            float tank = Mathf.Max(0f, 1.0f - basic - fast);
            return (basic, fast, tank);
        }
    }
    #endregion

    #region F12-F15: Power-ups & Magnetism
    public class PowerUpSimulation
    {
        public PowerUpType Type { get; set; }
        public Vector2 Position { get; set; }
        public bool IsCollected { get; set; } = false;

        public const float FloatSpeed = 1.8f;
        public const float SwayAmplitude = 0.35f;
        public const float SwayFrequency = 2.5f;
        public const float MagnetismRadius = 1.6f;
        public const float MagnetismAccel = 12.0f;

        private float _spawnX;
        private float _time;
        private Vector2 _velocity;

        public PowerUpSimulation(PowerUpType type, Vector2 spawnPos)
        {
            Type = type;
            Position = spawnPos;
            _spawnX = spawnPos.x;
            _time = 0f;
            _velocity = new Vector2(0f, -FloatSpeed);
        }

        public void Update(float dt, Vector2 playerPos)
        {
            if (IsCollected) return;

            _time += dt;
            float dist = Vector2.Distance(Position, playerPos);

            if (dist < MagnetismRadius)
            {
                Vector2 dir = (playerPos - Position).normalized;
                _velocity += dir * (MagnetismAccel * dt);
                Position += _velocity * dt;
            }
            else
            {
                float x = _spawnX + (SwayAmplitude * Mathf.Sin(SwayFrequency * _time));
                float y = Position.y - (FloatSpeed * dt);
                Position = new Vector2(x, y);
            }
        }
    }
    #endregion

    #region F16: Score & Combo Multiplier
    public class ScoreComboModel
    {
        public const float ComboDuration = 3.0f;
        public const int ScoreBasic = 100;
        public const int ScoreFast = 250;
        public const int ScoreTank = 1500;

        public int TotalScore { get; private set; } = 0;
        public int HighScore { get; private set; } = 0;
        public int ComboCount { get; private set; } = 0;
        public float ComboTimer { get; private set; } = 0f;

        public float Multiplier
        {
            get
            {
                if (ComboCount < 5) return 1.0f;
                if (ComboCount < 10) return 1.5f;
                if (ComboCount < 15) return 2.0f;
                if (ComboCount < 20) return 2.5f;
                return 3.0f;
            }
        }

        public void RegisterHit(EnemyType enemyType)
        {
            ComboCount++;
            ComboTimer = ComboDuration;

            int baseScore = enemyType switch
            {
                EnemyType.BasicSparrow => ScoreBasic,
                EnemyType.FastFalcon => ScoreFast,
                EnemyType.TankEagle => ScoreTank,
                EnemyType.BossPhoenix => ScoreTank * 2,
                _ => ScoreBasic
            };

            int delta = Mathf.RoundToInt(baseScore * Multiplier);
            TotalScore += delta;

            if (TotalScore > HighScore)
            {
                HighScore = TotalScore;
            }
        }

        public void OnPlayerDamaged()
        {
            ComboCount = 0;
            ComboTimer = 0f;
        }

        public void Update(float dt)
        {
            if (ComboTimer > 0f)
            {
                ComboTimer -= dt;
                if (ComboTimer <= 0f)
                {
                    ComboCount = 0;
                    ComboTimer = 0f;
                }
            }
        }

        public void ResetGame()
        {
            TotalScore = 0;
            ComboCount = 0;
            ComboTimer = 0f;
        }

        public void SetInitialHighScore(int initial)
        {
            HighScore = initial;
        }
    }
    #endregion

    #region F17: Game Loop State Machine
    public class GameLoopStateMachine
    {
        public GameState CurrentState { get; private set; } = GameState.Boot;
        public int CurrentWave { get; private set; } = 1;

        public event Action<GameState, GameState> OnStateChanged;

        public bool TransitionTo(GameState next)
        {
            bool isValid = (CurrentState, next) switch
            {
                (GameState.Boot, GameState.MainMenu) => true,
                (GameState.MainMenu, GameState.Playing) => true,
                (GameState.Playing, GameState.Paused) => true,
                (GameState.Paused, GameState.Playing) => true,
                (GameState.Playing, GameState.WaveTransition) => true,
                (GameState.WaveTransition, GameState.Playing) => true,
                (GameState.Playing, GameState.GameOver) => true,
                (GameState.WaveTransition, GameState.GameOver) => true,
                (GameState.GameOver, GameState.Playing) => true, // Instant restart
                (GameState.GameOver, GameState.MainMenu) => true,
                _ => false
            };

            if (isValid)
            {
                GameState prev = CurrentState;
                CurrentState = next;
                if (next == GameState.WaveTransition)
                {
                    CurrentWave++;
                }
                else if (next == GameState.Playing && prev == GameState.GameOver)
                {
                    CurrentWave = 1;
                }
                OnStateChanged?.Invoke(prev, next);
            }

            return isValid;
        }
    }
    #endregion

    #region F19: Parallax Leapfrog Model
    public class ParallaxLeapfrogModel
    {
        public float ScrollSpeed { get; set; }
        public float SpriteHeight { get; set; }
        public float PosA { get; set; }
        public float PosB { get; set; }

        public ParallaxLeapfrogModel(float speed, float height)
        {
            ScrollSpeed = speed;
            SpriteHeight = height;
            PosA = 0f;
            PosB = height;
        }

        public void Update(float dt)
        {
            float delta = ScrollSpeed * dt;
            PosA -= delta;
            PosB -= delta;

            if (PosA <= -SpriteHeight)
            {
                PosA = PosB + SpriteHeight;
            }
            if (PosB <= -SpriteHeight)
            {
                PosB = PosA + SpriteHeight;
            }
        }
    }
    #endregion
}
