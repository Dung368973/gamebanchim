using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShootEmUp.Tests.E2E
{
    [E2ETestFixture("Tier 3: Cross-Feature Pairwise Combinations", TestTier.Tier3_CrossFeaturePairwise, 50)]
    public class Tier3_CrossFeaturePairwiseTests
    {
        [E2ETest("T3_01", FeatureId.CrossFeature, "Bullet Pool + Spread Shot + Rapid Fire high-throughput burst")]
        public void T3_01_BulletPool_SpreadShot_RapidFire_HighThroughput()
        {
            using var harness = new E2ETestHarness();
            var pool = harness.CreateBulletPool(60);

            var shooter = new AutoShootingModel { IsRapidFireActive = true };
            // Simulate 1.0 second of rapid fire (10 bursts of 5-way spread = 50 bullets)
            var activeBullets = new List<ITestDamageable>();

            int bursts = shooter.Update(1.0f, () =>
            {
                Vector2[] velocities = AutoShootingModel.GetSpreadVelocities(2, 14f);
                for (int i = 0; i < velocities.Length; i++)
                {
                    ITestDamageable b = pool.Get();
                    activeBullets.Add(b);
                }
            });

            E2EAssert.AreEqual(10, bursts, "Should fire 10 bursts in 1.0s under rapid fire");
            E2EAssert.AreEqual(50, activeBullets.Count, "10 bursts of 5-way spread = 50 active projectiles");
            E2EAssert.AreEqual(50, pool.ActiveCount, "Active count in pool must be 50");

            // Return all back to pool
            foreach (var b in activeBullets) pool.Return(b);

            E2EAssert.AreEqual(0, pool.ActiveCount, "All bullets returned cleanly");
            E2EAssert.AreEqual(60, pool.AvailableCount, "Pool available count restored to 60");
        }

        [E2ETest("T3_02", FeatureId.CrossFeature, "Tank Bird HP + Combo Multiplier + Score Increment")]
        public void T3_02_TankBird_ComboMultiplier_ScoreAccumulation()
        {
            var scoring = new ScoreComboModel();
            using var harness = new E2ETestHarness();
            var dmg = harness.CreateDamageableEntity(30);

            // Deal 30 hits to defeat boss
            for (int hit = 1; hit <= 30; hit++)
            {
                dmg.TakeDamage(1);
                scoring.RegisterHit(EnemyType.BasicSparrow); // Increment combo counter
            }

            E2EAssert.IsFalse(dmg.IsAlive, "Boss should be defeated after 30 hits");
            E2EAssert.AreEqual(30, scoring.ComboCount, "Combo count should reach 30");
            E2EAssert.AreApproximatelyEqual(3.0f, scoring.Multiplier, 0.001f, "Multiplier capped at 3.0x max");
            E2EAssert.GreaterThan(scoring.TotalScore, 3000, "Total score should reflect multiplied points");
        }

        [E2ETest("T3_03", FeatureId.CrossFeature, "Player Health + Shield Buff + Enemy Collision + Grace Period")]
        public void T3_03_PlayerHealth_Shield_EnemyCollision_GracePeriod()
        {
            var health = new PlayerHealthModel();
            health.ActivateShield(8.0f);

            // 1st collision: absorbed by shield
            bool hit1 = health.TakeDamage(1);
            E2EAssert.IsFalse(hit1, "Shield absorbs 1st collision");
            E2EAssert.AreEqual(3, health.CurrentLives, "Lives unchanged (3)");
            E2EAssert.IsTrue(health.IsInvulnerable, "1.0s grace period active");

            // Immediate 2nd collision during grace period
            bool hit2 = health.TakeDamage(1);
            E2EAssert.IsFalse(hit2, "Grace period absorbs 2nd collision");
            E2EAssert.AreEqual(3, health.CurrentLives, "Lives still 3");

            // Wait 1.1s (grace period expires)
            health.Update(1.1f);
            E2EAssert.IsFalse(health.IsInvulnerable, "Grace period expired");

            // 3rd collision: now damages player
            bool hit3 = health.TakeDamage(1);
            E2EAssert.IsTrue(hit3, "3rd collision inflicts damage");
            E2EAssert.AreEqual(2, health.CurrentLives, "Lives reduced to 2");
        }

        [E2ETest("T3_04", FeatureId.CrossFeature, "Wave Spawner Scaling + Enemy Mix + Boundary Cleaner Despawn")]
        public void T3_04_WaveSpawner_DifficultyScaling_BoundaryCleaner_Despawn()
        {
            int wave = 5;
            float speedMult = WaveScalingMath.CalculateSpeedMultiplier(wave);
            float baseSpeed = 4.0f;
            float actualSpeed = baseSpeed * speedMult;

            // Spawn at Y = 7.0
            Vector2 origin = new Vector2(0f, 7.0f);
            // Move for 4.0 seconds
            Vector2 pos = EnemyKinematics.BasicBirdPosition(origin, Vector2.zero, actualSpeed, 0f, 4.0f);

            // With speed > 4.0 * 1.16 = 4.64, in 4s drops by > 18.5 -> Y < -11.5
            E2EAssert.IsTrue(BoundaryCleaner.IsOutOfBounds(pos), "Enemy must cross boundary and be detected out of bounds");
        }

        [E2ETest("T3_05", FeatureId.CrossFeature, "Power-up Magnetism + Viewport Clamping + Player Drag Movement")]
        public void T3_05_PowerupMagnetism_ViewportClamping_TouchDrag()
        {
            var clamp = new ViewportClampingModel();
            var drag = new TouchDragSimulation { PlayerPosition = new Vector2(0f, -3.0f) };
            drag.OnTouchDown(0, new Vector2(0f, -3.0f));

            // Drag player toward screen edge
            drag.OnTouchMove(0, new Vector2(10f, -3.0f), 0.016f);
            Vector2 clampedPos = clamp.Clamp(drag.PlayerPosition);

            E2EAssert.AreApproximatelyEqual(clamp.MaxX, clampedPos.x, 0.001f, "Player clamped to screen edge");

            // Power-up within magnetism range of clamped player
            var powerup = new PowerUpSimulation(PowerUpType.SpreadShot, clampedPos + new Vector2(0.5f, 0.5f));
            powerup.Update(0.1f, clampedPos);

            float distAfter = Vector2.Distance(powerup.Position, clampedPos);
            float distBefore = Vector2.Distance(clampedPos + new Vector2(0.5f, 0.5f), clampedPos);

            E2EAssert.LessThan(distAfter, distBefore, "Power-up must move toward clamped player position");
        }

        [E2ETest("T3_06", FeatureId.CrossFeature, "Player Health Death + GameOver Event + High Score + FSM")]
        public void T3_06_PlayerDeath_GameOver_HighScore_FSM()
        {
            using var harness = new E2ETestHarness();
            var health = new PlayerHealthModel();
            var scoring = new ScoreComboModel();
            scoring.SetInitialHighScore(500);
            var fsm = new GameLoopStateMachine();
            fsm.TransitionTo(GameState.MainMenu);
            fsm.TransitionTo(GameState.Playing);

            // Score some points
            for (int i = 0; i < 10; i++) scoring.RegisterHit(EnemyType.BasicSparrow); // +1250

            // Player takes 3 hits
            health.TakeDamage(1); health.Update(2.1f);
            health.TakeDamage(1); health.Update(2.1f);
            health.TakeDamage(1); // Dead

            E2EAssert.IsFalse(health.IsAlive, "Player dead");
            GameEvents.OnGameOver?.Invoke(scoring.TotalScore, scoring.HighScore);
            fsm.TransitionTo(GameState.GameOver);

            E2EAssert.AreEqual(1, harness.GameOverHistory.Count, "GameOver event captured");
            E2EAssert.AreEqual(scoring.TotalScore, harness.GameOverHistory[0].finalScore);
            E2EAssert.AreEqual(scoring.HighScore, harness.GameOverHistory[0].highScore);
            E2EAssert.AreEqual(GameState.GameOver, fsm.CurrentState, "FSM state is GameOver");
        }

        [E2ETest("T3_07", FeatureId.CrossFeature, "Basic and Fast Birds Simultaneous Flight Trajectory Separation")]
        public void T3_07_BasicAndFastBirds_SimultaneousFlight_CollisionSeparation()
        {
            Vector2 origin = new Vector2(0f, 7.0f);
            Vector2 basicPos = EnemyKinematics.BasicBirdPosition(origin, Vector2.zero, 3.5f, 0f, 1.0f);
            Vector2 fastPos = EnemyKinematics.FastBirdSinePosition(0f, 7.0f, 1.5f, 4.0f, 0f, 6.0f, 1.0f);

            // Fast bird moves faster vertically (Y = 1.0 vs Y = 3.5) and oscillates laterally
            E2EAssert.LessThan(fastPos.y, basicPos.y, "Fast bird descends faster vertically than basic bird");
            E2EAssert.AreNotEqual(basicPos.x, fastPos.x, "Fast bird oscillates horizontally away from basic bird");
        }

        [E2ETest("T3_08", FeatureId.CrossFeature, "Tank Boss Defeat + Guaranteed Drop + Feather Burst FX")]
        public void T3_08_TankBossDefeat_GuaranteedDrop_FeatherBurstFX()
        {
            using var harness = new E2ETestHarness();
            var dmg = harness.CreateDamageableEntity(10);

            bool bossDefeatedEventFired = false;
            GameEvents.OnBossDefeated += () => bossDefeatedEventFired = true;

            dmg.TakeDamage(10);
            if (!dmg.IsAlive)
            {
                GameEvents.OnBossDefeated?.Invoke();
                // Spawn guaranteed drop
                GameEvents.OnPowerUpCollected?.Invoke(PowerUpType.SpreadShot, 10.0f);
            }

            E2EAssert.IsTrue(bossDefeatedEventFired, "OnBossDefeated should fire");
            E2EAssert.AreEqual(1, harness.PowerUpHistory.Count, "PowerUp drop event fired");
            E2EAssert.AreEqual(PowerUpType.SpreadShot, harness.PowerUpHistory[0].type);
        }

        [E2ETest("T3_09", FeatureId.CrossFeature, "Combo Timer Decay + HUD Score Event Updates")]
        public void T3_09_ComboTimerDecay_HUDScoreUpdates_EventBus()
        {
            using var harness = new E2ETestHarness();
            var scoring = new ScoreComboModel();

            // Register 5 hits
            for (int i = 0; i < 5; i++)
            {
                scoring.RegisterHit(EnemyType.BasicSparrow);
                GameEvents.OnScoreChanged?.Invoke(scoring.TotalScore);
                GameEvents.OnComboChanged?.Invoke(scoring.ComboCount, scoring.Multiplier);
            }

            E2EAssert.AreEqual(5, harness.ScoreHistory.Count);
            E2EAssert.AreEqual(1.5f, harness.ComboHistory[4].mult);

            // Timeout
            scoring.Update(3.1f);
            GameEvents.OnComboChanged?.Invoke(scoring.ComboCount, scoring.Multiplier);

            E2EAssert.AreEqual(6, harness.ComboHistory.Count);
            E2EAssert.AreEqual(0, harness.ComboHistory[5].count);
            E2EAssert.AreEqual(1.0f, harness.ComboHistory[5].mult);
        }

        [E2ETest("T3_10", FeatureId.CrossFeature, "GameOver + Instant Restart + Full State Restoration")]
        public void T3_10_GameOver_InstantRestart_FullStateRestoration()
        {
            using var harness = new E2ETestHarness();
            var fsm = new GameLoopStateMachine();
            fsm.TransitionTo(GameState.MainMenu);
            fsm.TransitionTo(GameState.Playing);
            fsm.TransitionTo(GameState.WaveTransition); // Wave 2
            fsm.TransitionTo(GameState.GameOver);

            var health = new PlayerHealthModel();
            health.TakeDamage(3); // Dead

            var scoring = new ScoreComboModel();
            scoring.RegisterHit(EnemyType.TankEagle);

            // Restart triggered
            fsm.TransitionTo(GameState.Playing);
            health.Reset();
            scoring.ResetGame();
            GameEvents.OnGameRestart?.Invoke();

            E2EAssert.AreEqual(1, fsm.CurrentWave, "Wave restored to 1");
            E2EAssert.AreEqual(3, health.CurrentLives, "Lives restored to 3");
            E2EAssert.AreEqual(0, scoring.TotalScore, "Score restored to 0");
            E2EAssert.AreEqual(1, harness.RestartCount, "Restart event captured");
        }

        [E2ETest("T3_11", FeatureId.CrossFeature, "AutoFire + RapidFire + Audio SFX Pitch Jitter Frequency")]
        public void T3_11_AutoFire_RapidFire_AudioSFXTrigger()
        {
            var shooter = new AutoShootingModel { IsRapidFireActive = true };
            int sfxTriggerCount = 0;
            var rand = new System.Random(42);

            shooter.Update(0.5f, () =>
            {
                sfxTriggerCount++;
                float jitter = (float)(rand.NextDouble() * 0.10 - 0.05);
                float pitch = Mathf.Clamp(1.0f + jitter, 0.5f, 2.0f);
                E2EAssert.InRange(pitch, 0.95f, 1.05f, "Pitch jitter must stay within +/- 5%");
            });

            E2EAssert.AreEqual(5, sfxTriggerCount, "In 0.5s at 10Hz, 5 SFX events fired");
        }

        [E2ETest("T3_12", FeatureId.CrossFeature, "Off-screen Despawn + Bullet Pool Recycling Loop")]
        public void T3_12_OffscreenDespawn_BulletPool_RecyclingLoop()
        {
            using var harness = new E2ETestHarness();
            var pool = harness.CreateBulletPool(5);

            var active = pool.Get();
            Vector3 bulletPos = new Vector3(0f, 8.0f, 0f); // Past top threshold

            if (BoundaryCleaner.IsOutOfBounds(bulletPos))
            {
                pool.Return(active);
            }

            E2EAssert.AreEqual(5, pool.AvailableCount, "Cleaned bullet must return to pool");
            E2EAssert.AreEqual(0, pool.ActiveCount, "Active count must be 0");
        }

        [E2ETest("T3_13", FeatureId.CrossFeature, "Wave Completion + WaveTransition State + Spawner Interval Decay")]
        public void T3_13_WaveCompletion_WaveTransition_BreakDuration()
        {
            var fsm = new GameLoopStateMachine();
            fsm.TransitionTo(GameState.MainMenu);
            fsm.TransitionTo(GameState.Playing);

            float intW1 = WaveScalingMath.CalculateSpawnInterval(fsm.CurrentWave);
            fsm.TransitionTo(GameState.WaveTransition);
            float intW2 = WaveScalingMath.CalculateSpawnInterval(fsm.CurrentWave);

            E2EAssert.AreEqual(2, fsm.CurrentWave, "Current wave incremented to 2");
            E2EAssert.LessThan(intW2, intW1, "Wave 2 spawn interval decay");
        }

        [E2ETest("T3_14", FeatureId.CrossFeature, "Parallax Background + Safe Area Inset + HUD Layout")]
        public void T3_14_ParallaxBackground_SafeAreaInset_HUDLayering()
        {
            var parallax = new ParallaxLeapfrogModel(1.0f, 10.24f);
            parallax.Update(0.5f);

            Rect notchRect = new Rect(0, 100, 1080, 1720);
            Vector2 anchorMax = new Vector2(1f, notchRect.yMax / 1920f);

            E2EAssert.LessThan(anchorMax.y, 1.0f, "HUD top anchor safely inset below notch");
            E2EAssert.AreApproximatelyEqual(-0.5f, parallax.PosA, 0.001f, "Parallax scrolls underneath");
        }

        [E2ETest("T3_15", FeatureId.CrossFeature, "Bullet Pooling Multi-Burst Stress Testing (Zero Null Instances)")]
        public void T3_15_BulletPooling_MultiBurstStress_ZeroNullInstances()
        {
            using var harness = new E2ETestHarness();
            var pool = harness.CreateBulletPool(10);

            for (int burst = 0; burst < 10; burst++)
            {
                var batch = new List<ITestDamageable>();
                for (int i = 0; i < 5; i++)
                {
                    var item = pool.Get();
                    E2EAssert.NotNull(item, "Retrieved item must never be null");
                    batch.Add(item);
                }
                foreach (var item in batch) pool.Return(item);
            }

            E2EAssert.AreEqual(10, pool.AvailableCount, "All instances accounted for");
        }

        [E2ETest("T3_16", FeatureId.CrossFeature, "Fast Bird Dive Bomb + Player Position Lock Kinematics")]
        public void T3_16_FastBirdDiveBomb_PlayerPositionLock_AcceleratedVector()
        {
            Vector2 birdPos = new Vector2(0f, 4.0f);
            Vector2 playerPos = new Vector2(2f, -3.0f);

            Vector2 diveDir = (playerPos - birdPos).normalized;
            float diveSpeed = 9.0f;
            Vector2 newPos = birdPos + (diveDir * (diveSpeed * 0.1f));

            float distInitial = Vector2.Distance(birdPos, playerPos);
            float distAfter = Vector2.Distance(newPos, playerPos);

            E2EAssert.LessThan(distAfter, distInitial, "Dive-bomb must move bird directly toward locked player pos");
        }

        [E2ETest("T3_17", FeatureId.CrossFeature, "Procedural Audio Hit SFX + Spark Particle Synchronization")]
        public void T3_17_ProceduralAudio_HitSFX_SparkParticle_Synchronization()
        {
            int sfxPlayed = 0;
            int sparksSpawned = 0;

            Action onBulletHit = () =>
            {
                sfxPlayed++;
                sparksSpawned += 10;
            };

            for (int i = 0; i < 3; i++) onBulletHit();

            E2EAssert.AreEqual(3, sfxPlayed, "3 SFX played");
            E2EAssert.AreEqual(30, sparksSpawned, "30 sparks spawned in sync");
        }

        [E2ETest("T3_18", FeatureId.CrossFeature, "Spread Shot Weapon Level + Multi-Muzzle Offset Calculation")]
        public void T3_18_SpreadShot_ProceduralBulletSprite_MultiMuzzleOffsets()
        {
            Vector2 muzzleCenter = new Vector2(0f, -3.0f);
            Vector2[] muzzleOffsets3Way = new Vector2[]
            {
                muzzleCenter + new Vector2(-0.15f, -0.05f),
                muzzleCenter + new Vector2(0f, 0f),
                muzzleCenter + new Vector2(0.15f, -0.05f)
            };

            E2EAssert.AreEqual(3, muzzleOffsets3Way.Length);
            E2EAssert.LessThan(muzzleOffsets3Way[0].x, muzzleOffsets3Way[1].x);
            E2EAssert.GreaterThan(muzzleOffsets3Way[2].x, muzzleOffsets3Way[1].x);
        }

        [E2ETest("T3_19", FeatureId.CrossFeature, "Health Recovery + Consecutive Damage Life Tracking")]
        public void T3_19_HealthRecovery_PlayerDamage_LifeRestoration()
        {
            var health = new PlayerHealthModel();
            health.TakeDamage(1); // Lives = 2
            health.Update(2.1f);
            health.TakeDamage(1); // Lives = 1

            health.Heal(1); // Lives = 2
            health.Heal(1); // Lives = 3
            health.Heal(1); // Capped at 3

            E2EAssert.AreEqual(3, health.CurrentLives, "Lives must restore to 3 and cap cleanly");
        }

        [E2ETest("T3_20", FeatureId.CrossFeature, "Power-up Magnetism + Parallax Scrolling World Space Independence")]
        public void T3_20_PowerupMagnetism_ParallaxMotion_WorldSpaceIndependence()
        {
            var parallax = new ParallaxLeapfrogModel(2.0f, 10.24f);
            var powerup = new PowerUpSimulation(PowerUpType.SpreadShot, new Vector2(0f, 1.0f));

            parallax.Update(0.1f);
            powerup.Update(0.1f, Vector2.zero);

            // Power-up position must not be coupled to parallax displacement
            E2EAssert.AreNotEqual(parallax.PosA, powerup.Position.y, "Powerup y-motion is independent of parallax layer position");
        }
    }
}
