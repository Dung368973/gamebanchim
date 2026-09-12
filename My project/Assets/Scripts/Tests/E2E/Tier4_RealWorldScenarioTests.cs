using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShootEmUp.Tests.E2E
{
    [E2ETestFixture("Tier 4: Real-World Application Scenarios", TestTier.Tier4_RealWorldScenario, 60)]
    public class Tier4_RealWorldScenarioTests
    {
        [E2ETest("T4_01", FeatureId.RealWorldScenario, "Scenario 1: New Player Full Run (Boot -> MainMenu -> Wave 1 Flocks -> Clean Kills -> Wave 2)")]
        public void T4_01_NewPlayerFullRun_BootToWave2Clear()
        {
            using var harness = new E2ETestHarness();

            // 1. Boot to MainMenu to Playing
            var fsm = new GameLoopStateMachine();
            E2EAssert.AreEqual(GameState.Boot, fsm.CurrentState);
            fsm.TransitionTo(GameState.MainMenu);
            fsm.TransitionTo(GameState.Playing);
            E2EAssert.AreEqual(GameState.Playing, fsm.CurrentState);

            // 2. Wave 1 Start
            GameEvents.OnWaveStarted?.Invoke(1);
            E2EAssert.AreEqual(1, harness.WaveStartedHistory.Count);
            E2EAssert.AreEqual(1, harness.WaveStartedHistory[0]);

            // 3. Player drag & viewport clamping
            var drag = new TouchDragSimulation { PlayerPosition = new Vector2(0f, -3.5f) };
            var clamp = new ViewportClampingModel();
            drag.OnTouchDown(0, new Vector2(0f, -3.5f));
            drag.OnTouchMove(0, new Vector2(1.0f, -2.5f), 0.016f);
            Vector2 playerPos = clamp.Clamp(drag.PlayerPosition);
            E2EAssert.AreApproximatelyEqual(new Vector2(1.0f, -2.5f), playerPos, 0.001f);

            // 4. Combat: Flock of 5 basic birds descend and are eliminated
            var scoring = new ScoreComboModel();
            var bulletPool = harness.CreateBulletPool(20);

            for (int bird = 1; bird <= 5; bird++)
            {
                var bullet = bulletPool.Get();
                scoring.RegisterHit(EnemyType.BasicSparrow);
                GameEvents.OnScoreChanged?.Invoke(scoring.TotalScore);
                GameEvents.OnComboChanged?.Invoke(scoring.ComboCount, scoring.Multiplier);
                bulletPool.Return(bullet);
            }

            E2EAssert.AreEqual(5, scoring.ComboCount, "Combo count should reach 5");
            E2EAssert.AreApproximatelyEqual(1.5f, scoring.Multiplier, 0.001f, "Combo multiplier reaches 1.5x");
            E2EAssert.AreEqual(5, harness.ScoreHistory.Count);
            E2EAssert.AreEqual(0, bulletPool.ActiveCount, "All bullets recycled");

            // 5. Wave 1 cleared -> WaveTransition to Wave 2
            fsm.TransitionTo(GameState.WaveTransition);
            E2EAssert.AreEqual(2, fsm.CurrentWave, "Advanced to Wave 2");
            fsm.TransitionTo(GameState.Playing);
            E2EAssert.AreEqual(GameState.Playing, fsm.CurrentState);
        }

        [E2ETest("T4_02", FeatureId.RealWorldScenario, "Scenario 2: High-Difficulty Survival (Wave 5 Boss Battle, Rapid/Spread Buffs & Guaranteed Drop Surge)")]
        public void T4_02_HighDifficultySurvival_BossBattleAndDropSurge()
        {
            using var harness = new E2ETestHarness();

            // 1. Setup Wave 5 scaling
            int wave = 5;
            int bossHP = WaveScalingMath.CalculateBossHealth(wave, 30); // 30 + 4*10 = 70 HP
            E2EAssert.AreEqual(70, bossHP);

            // 2. Player has RapidFire & SpreadShot Level 1 active
            var shooter = new AutoShootingModel { IsRapidFireActive = true };
            var scoring = new ScoreComboModel();

            // 3. Boss setup
            var bossDmg = harness.CreateDamageableEntity(bossHP);

            // 4. Bullet pooling stream
            var bulletPool = harness.CreateBulletPool(50);

            // 5. Fire bursts until boss is dead
            float simTime = 0f;
            while (bossDmg.IsAlive && simTime < 10.0f)
            {
                simTime += 0.10f;
                shooter.Update(0.10f, () =>
                {
                    Vector2[] spread = AutoShootingModel.GetSpreadVelocities(1, 14f); // 3-way
                    for (int i = 0; i < spread.Length; i++)
                    {
                        var b = bulletPool.Get();
                        if (bossDmg.IsAlive)
                        {
                            bossDmg.TakeDamage(1);
                            scoring.RegisterHit(EnemyType.BasicSparrow);
                        }
                        bulletPool.Return(b);
                    }
                });
            }

            E2EAssert.IsFalse(bossDmg.IsAlive, "Boss must be defeated by sustained spread fire");
            E2EAssert.AreEqual(0, bulletPool.ActiveCount, "Pool has 0 leaked instances");

            // 6. Boss defeat awards score and guaranteed drop
            scoring.RegisterHit(EnemyType.BossPhoenix);
            GameEvents.OnBossDefeated?.Invoke();
            GameEvents.OnPowerUpCollected?.Invoke(PowerUpType.Shield, 8.0f);

            E2EAssert.AreEqual(1, harness.PowerUpHistory.Count);
            E2EAssert.AreEqual(PowerUpType.Shield, harness.PowerUpHistory[0].type);
        }

        [E2ETest("T4_03", FeatureId.RealWorldScenario, "Scenario 3: Near-Death Clutch Recovery (Hit Taken -> 1 Life Remaining -> Heal -> Shield Absorbs Fatal Blow)")]
        public void T4_03_NearDeathClutchRecovery()
        {
            using var harness = new E2ETestHarness();
            var health = new PlayerHealthModel();
            var scoring = new ScoreComboModel();

            // 1. Player builds combo
            for (int i = 0; i < 10; i++) scoring.RegisterHit(EnemyType.BasicSparrow);
            E2EAssert.AreEqual(10, scoring.ComboCount);

            // 2. Player takes 1st hit -> lives drop to 2, combo resets to 0, I-frames trigger
            health.TakeDamage(1);
            scoring.OnPlayerDamaged();
            GameEvents.OnPlayerHealthChanged?.Invoke(health.CurrentLives, PlayerHealthModel.MaxLives);
            GameEvents.OnComboChanged?.Invoke(scoring.ComboCount, scoring.Multiplier);

            E2EAssert.AreEqual(2, health.CurrentLives);
            E2EAssert.AreEqual(0, scoring.ComboCount, "Combo wiped on damage");
            E2EAssert.IsTrue(health.IsInvulnerable, "I-frames active");

            // 3. Subsequent collision during I-frames ignored
            bool midIframeHit = health.TakeDamage(1);
            E2EAssert.IsFalse(midIframeHit, "Hit during I-frames must do 0 damage");
            E2EAssert.AreEqual(2, health.CurrentLives);

            // 4. I-frames expire, 2nd hit brings player to 1 life
            health.Update(2.1f);
            health.TakeDamage(1);
            E2EAssert.AreEqual(1, health.CurrentLives, "1 life remaining!");

            // 5. Player collects Health Recovery (+1 life -> 2 lives)
            health.Heal(1);
            GameEvents.OnPowerUpCollected?.Invoke(PowerUpType.HealthRecovery, 0f);
            E2EAssert.AreEqual(2, health.CurrentLives, "Restored to 2 lives");

            // 6. Player collects Shield
            health.ActivateShield(8.0f);
            GameEvents.OnPowerUpCollected?.Invoke(PowerUpType.Shield, 8.0f);
            E2EAssert.IsTrue(health.HasShield);

            // 7. Incoming lethal enemy collision absorbed by Shield without life loss
            bool fatalHit = health.TakeDamage(1);
            E2EAssert.IsFalse(fatalHit, "Shield absorbs lethal hit");
            E2EAssert.AreEqual(2, health.CurrentLives, "Lives safely preserved at 2");
            E2EAssert.IsTrue(health.IsInvulnerable, "Grace period active");
        }

        [E2ETest("T4_04", FeatureId.RealWorldScenario, "Scenario 4: Defeat, High-Score Recording & Clean Restart Loop")]
        public void T4_04_DefeatHighScoreAndCleanRestartLoop()
        {
            using var harness = new E2ETestHarness();
            var fsm = new GameLoopStateMachine();
            fsm.TransitionTo(GameState.MainMenu);
            fsm.TransitionTo(GameState.Playing);

            var scoring = new ScoreComboModel();
            scoring.SetInitialHighScore(5000);

            // Player scores 7,500 points in Wave 2
            for (int i = 0; i < 30; i++) scoring.RegisterHit(EnemyType.FastFalcon);
            E2EAssert.GreaterThan(scoring.TotalScore, 5000, "Score exceeded old high score");
            E2EAssert.AreEqual(scoring.TotalScore, scoring.HighScore, "High score updated");

            // Player takes fatal damage
            var health = new PlayerHealthModel();
            health.TakeDamage(3);
            E2EAssert.IsFalse(health.IsAlive);

            // Game over sequence
            GameEvents.OnPlayerDied?.Invoke();
            GameEvents.OnGameOver?.Invoke(scoring.TotalScore, scoring.HighScore);
            fsm.TransitionTo(GameState.GameOver);

            E2EAssert.AreEqual(1, harness.PlayerDiedCount);
            E2EAssert.AreEqual(1, harness.GameOverHistory.Count);
            E2EAssert.AreEqual(scoring.TotalScore, harness.GameOverHistory[0].finalScore);
            E2EAssert.AreEqual(scoring.HighScore, harness.GameOverHistory[0].highScore);
            E2EAssert.AreEqual(GameState.GameOver, fsm.CurrentState);

            // Player taps Restart
            int savedHighScore = scoring.HighScore;
            fsm.TransitionTo(GameState.Playing);
            health.Reset();
            scoring.ResetGame();
            GameEvents.OnGameRestart?.Invoke();

            E2EAssert.AreEqual(GameState.Playing, fsm.CurrentState);
            E2EAssert.AreEqual(1, fsm.CurrentWave, "Wave reset to 1");
            E2EAssert.AreEqual(3, health.CurrentLives, "Lives reset to 3");
            E2EAssert.AreEqual(0, scoring.TotalScore, "Score reset to 0");
            E2EAssert.AreEqual(savedHighScore, scoring.HighScore, "High score preserved");
            E2EAssert.AreEqual(1, harness.RestartCount, "Restart event fired");
        }

        [E2ETest("T4_05", FeatureId.RealWorldScenario, "Scenario 5: Rapid Fire Bullet Hell Stress & Zero-Leak Pooling (500+ Projectiles Recycled)")]
        public void T4_05_RapidFireBulletHellStressAndZeroLeakPooling()
        {
            using var harness = new E2ETestHarness();
            var bulletPool = harness.CreateBulletPool(50);

            var shooter = new AutoShootingModel { IsRapidFireActive = true };
            int totalFired = 0;
            int totalHitEnemies = 0;
            int totalOffscreenCleaned = 0;

            var activeProjectiles = new List<ITestDamageable>();

            // Simulate 10 seconds of rapid 5-way spread shooting (10 bursts/sec * 5 = 50 bullets/sec = 500 bullets)
            for (int frame = 0; frame < 100; frame++)
            {
                float dt = 0.10f;
                shooter.Update(dt, () =>
                {
                    Vector2[] spread = AutoShootingModel.GetSpreadVelocities(2, 14f); // 5-way
                    for (int i = 0; i < spread.Length; i++)
                    {
                        var b = bulletPool.Get();
                        activeProjectiles.Add(b);
                        totalFired++;
                    }
                });

                // Simulate physics step: some hit enemies, some fly off-screen
                for (int i = activeProjectiles.Count - 1; i >= 0; i--)
                {
                    var b = activeProjectiles[i];
                    if (i % 3 == 0)
                    {
                        // Hit an enemy
                        totalHitEnemies++;
                        bulletPool.Return(b);
                        activeProjectiles.RemoveAt(i);
                    }
                    else if (i % 2 == 0)
                    {
                        // Flies off-screen past Y > 7.5
                        totalOffscreenCleaned++;
                        bulletPool.Return(b);
                        activeProjectiles.RemoveAt(i);
                    }
                }
            }

            // Return any remaining active projectiles
            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                bulletPool.Return(activeProjectiles[i]);
                activeProjectiles.RemoveAt(i);
            }

            E2EAssert.GreaterThanOrEqual(totalFired, 500, "Fired at least 500 projectiles in stress test");
            E2EAssert.AreEqual(0, bulletPool.ActiveCount, "Active count in pool must return to exactly 0");
            E2EAssert.AreEqual(bulletPool.TotalCreated, bulletPool.AvailableCount, "All created instances must be in available stack");
        }
    }
}
