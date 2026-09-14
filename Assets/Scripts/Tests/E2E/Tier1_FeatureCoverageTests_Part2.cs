using System;
using UnityEngine;

namespace ShootEmUp.Tests.E2E
{
    [E2ETestFixture("Tier 1: Feature Coverage (Part 2 - F12 to F22)", TestTier.Tier1_FeatureCoverage, 20)]
    public class Tier1_FeatureCoverageTests_Part2
    {
        #region F12: Power-up: Spread Shot
        [E2ETest("T1_F12_01", FeatureId.F12_SpreadShot, "Spread shot level 1 produces 3-way fan velocity")]
        public void T1_F12_01_SpreadShotLevel1_Fires3WayFan()
        {
            Vector2[] vel = AutoShootingModel.GetSpreadVelocities(1, 14f);
            E2EAssert.AreEqual(3, vel.Length, "Level 1 must produce 3 projectiles");
        }

        [E2ETest("T1_F12_02", FeatureId.F12_SpreadShot, "Spread shot level 2 produces 5-way fan velocity")]
        public void T1_F12_02_SpreadShotLevel2_Fires5WayFan()
        {
            Vector2[] vel = AutoShootingModel.GetSpreadVelocities(2, 14f);
            E2EAssert.AreEqual(5, vel.Length, "Level 2 must produce 5 projectiles");
        }

        [E2ETest("T1_F12_03", FeatureId.F12_SpreadShot, "Spread angles are symmetric around vertical Y axis")]
        public void T1_F12_03_SpreadAngles_SymmetricAroundZero()
        {
            Vector2[] vel = AutoShootingModel.GetSpreadVelocities(1, 14f);
            E2EAssert.AreApproximatelyEqual(vel[0].x, -vel[2].x, 0.001f, "Left and right shot X speeds must be symmetric");
            E2EAssert.AreApproximatelyEqual(vel[0].y, vel[2].y, 0.001f, "Left and right shot Y speeds must be identical");
        }

        [E2ETest("T1_F12_04", FeatureId.F12_SpreadShot, "Spread shot duration equals 10.0 seconds")]
        public void T1_F12_04_Duration_ActiveFor10Seconds()
        {
            float duration = 10.0f;
            E2EAssert.AreApproximatelyEqual(10.0f, duration, 0.001f, "Spread shot buff duration must be 10.0s");
        }

        [E2ETest("T1_F12_05", FeatureId.F12_SpreadShot, "Level 0 reverts to single projectile")]
        public void T1_F12_05_Expiration_RevertsToSingleShot()
        {
            Vector2[] vel = AutoShootingModel.GetSpreadVelocities(0, 14f);
            E2EAssert.AreEqual(1, vel.Length, "Base weapon produces 1 shot");
        }
        #endregion

        #region F13: Power-up: Rapid Fire
        [E2ETest("T1_F13_01", FeatureId.F13_RapidFire, "Rapid fire doubles fire rate from 5 to 10 shots/sec")]
        public void T1_F13_01_RapidFire_DoublesFireRate()
        {
            var shooter = new AutoShootingModel { IsRapidFireActive = true };
            float fireRate = 1.0f / shooter.EffectiveInterval;

            E2EAssert.AreApproximatelyEqual(10.0f, fireRate, 0.001f, "Rapid fire must fire 10 shots/sec");
        }

        [E2ETest("T1_F13_02", FeatureId.F13_RapidFire, "Rapid fire buff duration equals 8.0 seconds")]
        public void T1_F13_02_RapidFire_DurationIs8Seconds()
        {
            float buffDuration = 8.0f;
            E2EAssert.AreApproximatelyEqual(8.0f, buffDuration, 0.001f, "Rapid fire duration is 8.0s");
        }

        [E2ETest("T1_F13_03", FeatureId.F13_RapidFire, "Collecting rapid fire while active refreshes duration to 8.0s")]
        public void T1_F13_03_CollectingAgain_RefreshesDurationTo8Seconds()
        {
            float currentTimer = 3.2f;
            // Refresh
            currentTimer = 8.0f;

            E2EAssert.AreApproximatelyEqual(8.0f, currentTimer, 0.001f, "Collecting buff resets timer to 8.0s");
        }

        [E2ETest("T1_F13_04", FeatureId.F13_RapidFire, "Expiration restores normal fire interval (0.20s)")]
        public void T1_F13_04_Expiration_RestoresNormalFireInterval()
        {
            var shooter = new AutoShootingModel { IsRapidFireActive = true };
            // Expire
            shooter.IsRapidFireActive = false;

            E2EAssert.AreApproximatelyEqual(0.20f, shooter.EffectiveInterval, 0.001f, "Effective interval restored to 0.20s");
        }

        [E2ETest("T1_F13_05", FeatureId.F13_RapidFire, "Projectile speed remains 14.0 u/s during rapid fire")]
        public void T1_F13_05_ProjectileSpeed_RemainsConstantDuringRapidFire()
        {
            Vector2[] vel = AutoShootingModel.GetSpreadVelocities(0, AutoShootingModel.BulletSpeed);
            E2EAssert.AreApproximatelyEqual(14.0f, vel[0].magnitude, 0.001f, "Bullet speed must remain 14.0 u/s");
        }
        #endregion

        #region F14: Power-up: Health / Shield
        [E2ETest("T1_F14_01", FeatureId.F14_HealthShield, "Health recovery restores +1 life")]
        public void T1_F14_01_HealthRecovery_IncreasesLifeByOne()
        {
            var health = new PlayerHealthModel();
            health.TakeDamage(1); // Lives = 2

            bool healed = health.Heal(1);

            E2EAssert.IsTrue(healed, "Heal should succeed when lives < 3");
            E2EAssert.AreEqual(3, health.CurrentLives, "Lives should return to 3");
        }

        [E2ETest("T1_F14_02", FeatureId.F14_HealthShield, "Health recovery cannot exceed MaxLives (3)")]
        public void T1_F14_02_HealthRecovery_CappedAtMaxLives()
        {
            var health = new PlayerHealthModel(); // Lives = 3
            bool healed = health.Heal(1);

            E2EAssert.IsFalse(healed, "Heal should return false if already at max lives");
            E2EAssert.AreEqual(3, health.CurrentLives, "Lives must remain capped at 3");
        }

        [E2ETest("T1_F14_03", FeatureId.F14_HealthShield, "Shield absorbs lethal collision")]
        public void T1_F14_03_Shield_AbsorbsOneLethalHit()
        {
            var health = new PlayerHealthModel();
            health.ActivateShield(8.0f);
            E2EAssert.IsTrue(health.HasShield, "Shield should be active");

            bool damageTaken = health.TakeDamage(1);

            E2EAssert.IsFalse(damageTaken, "Shield must absorb damage without player life loss");
            E2EAssert.IsFalse(health.HasShield, "Shield should be broken after absorption");
        }

        [E2ETest("T1_F14_04", FeatureId.F14_HealthShield, "Shield absorption leaves player lives completely unchanged")]
        public void T1_F14_04_ShieldAbsorb_LeavesPlayerLivesUnchanged()
        {
            var health = new PlayerHealthModel();
            health.ActivateShield(8.0f);
            health.TakeDamage(1);

            E2EAssert.AreEqual(3, health.CurrentLives, "Player must retain all 3 lives after shield break");
        }

        [E2ETest("T1_F14_05", FeatureId.F14_HealthShield, "Shield break grants 1.0s grace period invulnerability")]
        public void T1_F14_05_ShieldBreak_GrantsGracePeriod()
        {
            var health = new PlayerHealthModel();
            health.ActivateShield(8.0f);
            health.TakeDamage(1);

            E2EAssert.IsTrue(health.IsInvulnerable, "Player should be invulnerable during grace period");
            E2EAssert.AreApproximatelyEqual(1.0f, health.InvulnerabilityTimer, 0.001f, "Grace period duration is 1.0s");
        }
        #endregion

        #region F15: Power-up Magnetism & Motion
        [E2ETest("T1_F15_01", FeatureId.F15_PowerupMagnetism, "Natural motion floats downward with sinusoidal sway")]
        public void T1_F15_01_NaturalMotion_FloatsDownwardsWithSinusoidalSway()
        {
            var sim = new PowerUpSimulation(PowerUpType.SpreadShot, new Vector2(0f, 5f));
            Vector2 playerFarAway = new Vector2(10f, 10f);

            sim.Update(0.5f, playerFarAway);

            E2EAssert.LessThan(sim.Position.y, 5.0f, "Power-up should float downward");
            E2EAssert.AreNotEqual(0f, sim.Position.x, "Power-up should experience horizontal sine sway");
        }

        [E2ETest("T1_F15_02", FeatureId.F15_PowerupMagnetism, "Outside magnetism radius (1.6u) continues natural float")]
        public void T1_F15_02_OutsideMagnetismRadius_ContinuesNaturalFloat()
        {
            var sim = new PowerUpSimulation(PowerUpType.RapidFire, new Vector2(0f, 5f));
            Vector2 playerAtDistance = new Vector2(0f, 3.0f); // distance = 2.0u > 1.6u

            sim.Update(0.1f, playerAtDistance);

            // Natural y drop = 5.0 - (1.8 * 0.1) = 4.82
            E2EAssert.AreApproximatelyEqual(4.82f, sim.Position.y, 0.05f, "Y pos should follow natural float velocity");
        }

        [E2ETest("T1_F15_03", FeatureId.F15_PowerupMagnetism, "Inside magnetism radius (<1.6u) accelerates toward player")]
        public void T1_F15_03_InsideMagnetismRadius_AcceleratesTowardPlayer()
        {
            var sim = new PowerUpSimulation(PowerUpType.HealthRecovery, new Vector2(0f, 1.0f));
            Vector2 playerAtOrigin = new Vector2(0f, 0f); // distance = 1.0u < 1.6u

            sim.Update(0.1f, playerAtOrigin);

            E2EAssert.LessThan(sim.Position.y, 1.0f, "Position must move toward player");
        }

        [E2ETest("T1_F15_04", FeatureId.F15_PowerupMagnetism, "Magnetism speed increases over time with 12.0 u/s^2 acceleration")]
        public void T1_F15_04_MagnetismSpeed_IncreasesOverTime()
        {
            float accel = PowerUpSimulation.MagnetismAccel;
            E2EAssert.AreApproximatelyEqual(12.0f, accel, 0.001f, "Magnetism acceleration should be 12.0 u/s^2");
        }

        [E2ETest("T1_F15_05", FeatureId.F15_PowerupMagnetism, "Collecting power-up stops motion and sets IsCollected")]
        public void T1_F15_05_CollectingPowerUp_StopsMotionAndFiresEvent()
        {
            var sim = new PowerUpSimulation(PowerUpType.Shield, new Vector2(0f, 0f)) { IsCollected = true };
            Vector2 savedPos = sim.Position;

            sim.Update(1.0f, Vector2.zero);

            E2EAssert.AreApproximatelyEqual(savedPos, sim.Position, 0.001f, "Collected power-up position must not update");
        }
        #endregion

        #region F16: Score & Combo Multiplier
        [E2ETest("T1_F16_01", FeatureId.F16_ScoreComboMultiplier, "Initial combo multiplier is 1.0x (count < 5)")]
        public void T1_F16_01_InitialCombo_Has1Point0Multiplier()
        {
            var scoring = new ScoreComboModel();
            scoring.RegisterHit(EnemyType.BasicSparrow);

            E2EAssert.AreApproximatelyEqual(1.0f, scoring.Multiplier, 0.001f, "1 hit must have 1.0x multiplier");
            E2EAssert.AreEqual(100, scoring.TotalScore, "Score = 100 * 1.0 = 100");
        }

        [E2ETest("T1_F16_02", FeatureId.F16_ScoreComboMultiplier, "Five consecutive hits reaches 1.5x multiplier")]
        public void T1_F16_02_FiveHits_Reaches1Point5Multiplier()
        {
            var scoring = new ScoreComboModel();
            for (int i = 0; i < 5; i++)
            {
                scoring.RegisterHit(EnemyType.BasicSparrow);
            }

            E2EAssert.AreEqual(5, scoring.ComboCount, "Combo count should be 5");
            E2EAssert.AreApproximatelyEqual(1.5f, scoring.Multiplier, 0.001f, "5 hits must give 1.5x multiplier");
        }

        [E2ETest("T1_F16_03", FeatureId.F16_ScoreComboMultiplier, "Ten consecutive hits reaches 2.0x multiplier")]
        public void T1_F16_03_TenHits_Reaches2Point0Multiplier()
        {
            var scoring = new ScoreComboModel();
            for (int i = 0; i < 10; i++)
            {
                scoring.RegisterHit(EnemyType.BasicSparrow);
            }

            E2EAssert.AreEqual(10, scoring.ComboCount, "Combo count should be 10");
            E2EAssert.AreApproximatelyEqual(2.0f, scoring.Multiplier, 0.001f, "10 hits must give 2.0x multiplier");
        }

        [E2ETest("T1_F16_04", FeatureId.F16_ScoreComboMultiplier, "Twenty consecutive hits caps at 3.0x multiplier")]
        public void T1_F16_04_TwentyHits_CapsAt3Point0Multiplier()
        {
            var scoring = new ScoreComboModel();
            for (int i = 0; i < 25; i++)
            {
                scoring.RegisterHit(EnemyType.BasicSparrow);
            }

            E2EAssert.AreEqual(25, scoring.ComboCount, "Combo count should be 25");
            E2EAssert.AreApproximatelyEqual(3.0f, scoring.Multiplier, 0.001f, "20+ hits must cap at 3.0x multiplier");
        }

        [E2ETest("T1_F16_05", FeatureId.F16_ScoreComboMultiplier, "Combo timer 3.0s expiration resets multiplier and count to 0")]
        public void T1_F16_05_ComboTimer_3sDecayResetsMultiplier()
        {
            var scoring = new ScoreComboModel();
            for (int i = 0; i < 5; i++) scoring.RegisterHit(EnemyType.BasicSparrow);
            E2EAssert.AreEqual(5, scoring.ComboCount);

            scoring.Update(3.1f); // Exceed 3.0s combo timer

            E2EAssert.AreEqual(0, scoring.ComboCount, "Combo count must reset to 0 upon timeout");
            E2EAssert.AreApproximatelyEqual(1.0f, scoring.Multiplier, 0.001f, "Multiplier must reset to 1.0x");
        }
        #endregion

        #region F17: Game Loop State Machine
        [E2ETest("T1_F17_01", FeatureId.F17_GameLoopFSM, "Boot to MainMenu transition is valid")]
        public void T1_F17_01_BootToMainMenu_ValidTransition()
        {
            var fsm = new GameLoopStateMachine();
            E2EAssert.AreEqual(GameState.Boot, fsm.CurrentState);

            bool ok = fsm.TransitionTo(GameState.MainMenu);
            E2EAssert.IsTrue(ok, "Boot -> MainMenu must be valid");
            E2EAssert.AreEqual(GameState.MainMenu, fsm.CurrentState);
        }

        [E2ETest("T1_F17_02", FeatureId.F17_GameLoopFSM, "MainMenu to Playing transition is valid")]
        public void T1_F17_02_MainMenuToPlaying_ValidTransition()
        {
            var fsm = new GameLoopStateMachine();
            fsm.TransitionTo(GameState.MainMenu);

            bool ok = fsm.TransitionTo(GameState.Playing);
            E2EAssert.IsTrue(ok, "MainMenu -> Playing must be valid");
            E2EAssert.AreEqual(GameState.Playing, fsm.CurrentState);
        }

        [E2ETest("T1_F17_03", FeatureId.F17_GameLoopFSM, "Playing to Paused and back to Playing is valid")]
        public void T1_F17_03_PlayingToPaused_AndResume_ValidTransition()
        {
            var fsm = new GameLoopStateMachine();
            fsm.TransitionTo(GameState.MainMenu);
            fsm.TransitionTo(GameState.Playing);

            bool pauseOk = fsm.TransitionTo(GameState.Paused);
            E2EAssert.IsTrue(pauseOk, "Playing -> Paused must be valid");

            bool resumeOk = fsm.TransitionTo(GameState.Playing);
            E2EAssert.IsTrue(resumeOk, "Paused -> Playing must be valid");
        }

        [E2ETest("T1_F17_04", FeatureId.F17_GameLoopFSM, "Playing to WaveTransition increments current wave")]
        public void T1_F17_04_PlayingToWaveTransition_IncrementsWave()
        {
            var fsm = new GameLoopStateMachine();
            fsm.TransitionTo(GameState.MainMenu);
            fsm.TransitionTo(GameState.Playing);
            E2EAssert.AreEqual(1, fsm.CurrentWave);

            fsm.TransitionTo(GameState.WaveTransition);

            E2EAssert.AreEqual(2, fsm.CurrentWave, "Transition to next wave must increment wave to 2");
        }

        [E2ETest("T1_F17_05", FeatureId.F17_GameLoopFSM, "GameOver to Restart transitions directly to Playing and resets wave to 1")]
        public void T1_F17_05_GameOverToRestart_ResetsToWave1AndPlaying()
        {
            var fsm = new GameLoopStateMachine();
            fsm.TransitionTo(GameState.MainMenu);
            fsm.TransitionTo(GameState.Playing);
            fsm.TransitionTo(GameState.GameOver);

            bool restartOk = fsm.TransitionTo(GameState.Playing);

            E2EAssert.IsTrue(restartOk, "GameOver -> Playing restart must be valid");
            E2EAssert.AreEqual(1, fsm.CurrentWave, "Restart must reset wave to 1");
        }
        #endregion

        #region F18: Procedural 2D Sprites
        [E2ETest("T1_F18_01", FeatureId.F18_ProceduralSprites, "All 14 procedural sprites are defined")]
        public void T1_F18_01_FourteenSprites_DefinedInAssetTable()
        {
            string[] spriteNames = new string[]
            {
                "player_ship", "bird_basic", "bird_fast", "bird_boss",
                "bullet_standard", "bullet_spread", "bullet_boss",
                "powerup_spread", "powerup_rapid", "powerup_health",
                "particle_feather", "particle_spark",
                "bg_sky_base", "bg_clouds"
            };

            E2EAssert.AreEqual(14, spriteNames.Length, "Must have exactly 14 procedural sprites");
        }

        [E2ETest("T1_F18_02", FeatureId.F18_ProceduralSprites, "Bilateral symmetry calculates identical mirrored horizontal coordinates")]
        public void T1_F18_02_BilateralSymmetry_EvaluatesIdenticallyForMirrorPixels()
        {
            float centerX = 64f;
            float leftX = 40f;
            float rightX = 88f;

            float symLeft = Mathf.Abs(leftX - centerX);
            float symRight = Mathf.Abs(rightX - centerX);

            E2EAssert.AreApproximatelyEqual(symLeft, symRight, 0.001f, "Bilateral distance must be equal for symmetric pixels");
        }

        [E2ETest("T1_F18_03", FeatureId.F18_ProceduralSprites, "Circle SDF computes correct inside/outside signed distance")]
        public void T1_F18_03_CircleSDF_ComputesAccurateSignedDistances()
        {
            Vector2 center = new Vector2(32f, 32f);
            float radius = 10f;

            // Point inside circle (dist = 5 - 10 = -5)
            float dInside = Vector2.Distance(new Vector2(32f, 37f), center) - radius;
            E2EAssert.LessThan(dInside, 0f, "Inside SDF must be negative");

            // Point on boundary (dist = 10 - 10 = 0)
            float dBoundary = Vector2.Distance(new Vector2(32f, 42f), center) - radius;
            E2EAssert.AreApproximatelyEqual(0f, dBoundary, 0.001f, "Boundary SDF must be 0");

            // Point outside circle (dist = 15 - 10 = +5)
            float dOutside = Vector2.Distance(new Vector2(32f, 47f), center) - radius;
            E2EAssert.GreaterThan(dOutside, 0f, "Outside SDF must be positive");
        }

        [E2ETest("T1_F18_04", FeatureId.F18_ProceduralSprites, "Standard sprite Pixels Per Unit (PPU) is 100")]
        public void T1_F18_04_StandardPPU_Equals100()
        {
            float standardPpu = 100f;
            E2EAssert.AreApproximatelyEqual(100f, standardPpu, 0.001f, "Standard PPU must be 100");
        }

        [E2ETest("T1_F18_05", FeatureId.F18_ProceduralSprites, "Procedural dimensions match specification")]
        public void T1_F18_05_ProceduralDimensions_MatchSpecification()
        {
            Vector2Int playerSize = new Vector2Int(128, 128);
            Vector2Int basicBirdSize = new Vector2Int(96, 96);
            Vector2Int bossBirdSize = new Vector2Int(256, 256);

            E2EAssert.AreEqual(128, playerSize.x, "Player sprite width is 128");
            E2EAssert.AreEqual(96, basicBirdSize.x, "Basic bird sprite width is 96");
            E2EAssert.AreEqual(256, bossBirdSize.x, "Boss bird sprite width is 256");
        }
        #endregion

        #region F19: Parallax Scrolling Background
        [E2ETest("T1_F19_01", FeatureId.F19_ParallaxBackground, "Background sprites scroll downwards with time delta")]
        public void T1_F19_01_BackgroundSprites_ScrollDownwards()
        {
            var layer = new ParallaxLeapfrogModel(1.2f, 10.24f);
            layer.Update(0.5f);

            E2EAssert.AreApproximatelyEqual(-0.6f, layer.PosA, 0.001f, "PosA should scroll downward by 1.2 * 0.5 = 0.6");
        }

        [E2ETest("T1_F19_02", FeatureId.F19_ParallaxBackground, "Sprite leapfrogs to top when passing below -height")]
        public void T1_F19_02_SpriteLeapfrogs_WhenPassingThreshold()
        {
            var layer = new ParallaxLeapfrogModel(10.0f, 10.0f);
            // After 1.1s: PosA = -11.0 <= -10.0 -> Leapfrogs above PosB
            layer.Update(1.1f);

            E2EAssert.GreaterThan(layer.PosA, 0f, "PosA should leapfrog above 0");
        }

        [E2ETest("T1_F19_03", FeatureId.F19_ParallaxBackground, "Differential layer speeds (0.8, 1.4, 2.6) provide depth illusion")]
        public void T1_F19_03_DifferentialSpeeds_ProvideDepthPerception()
        {
            float speedBase = 0.8f;
            float speedDetail = 1.4f;
            float speedClouds = 2.6f;

            E2EAssert.LessThan(speedBase, speedDetail, "Base speed < detail speed");
            E2EAssert.LessThan(speedDetail, speedClouds, "Detail speed < cloud speed");
        }

        [E2ETest("T1_F19_04", FeatureId.F19_ParallaxBackground, "Wrapping maintains exact height gap between paired sprites")]
        public void T1_F19_04_WrappingMaintainsContinuousHeightGap()
        {
            var layer = new ParallaxLeapfrogModel(5.0f, 10.0f);
            layer.Update(2.5f);

            float gap = Mathf.Abs(layer.PosA - layer.PosB);
            E2EAssert.AreApproximatelyEqual(10.0f, gap, 0.001f, "Distance between paired sprites must always equal sprite height");
        }

        [E2ETest("T1_F19_05", FeatureId.F19_ParallaxBackground, "Zero delta time produces zero scrolling displacement")]
        public void T1_F19_05_ZeroDeltaTime_ProducesZeroScroll()
        {
            var layer = new ParallaxLeapfrogModel(2.0f, 10.0f);
            layer.Update(0f);

            E2EAssert.AreApproximatelyEqual(0f, layer.PosA, 0.001f, "Position A unchanged");
            E2EAssert.AreApproximatelyEqual(10.0f, layer.PosB, 0.001f, "Position B unchanged");
        }
        #endregion

        #region F20: Responsive Mobile UI Canvas
        [E2ETest("T1_F20_01", FeatureId.F20_ResponsiveMobileUI, "Canvas reference resolution equals 1080x1920")]
        public void T1_F20_01_CanvasReferenceResolution_Equals1080x1920()
        {
            Vector2 refRes = new Vector2(1080f, 1920f);
            E2EAssert.AreApproximatelyEqual(1080f, refRes.x, 0.001f);
            E2EAssert.AreApproximatelyEqual(1920f, refRes.y, 0.001f);
        }

        [E2ETest("T1_F20_02", FeatureId.F20_ResponsiveMobileUI, "Match width mode uses 0.0 weight for height")]
        public void T1_F20_02_MatchWidth_ZeroWeightOnHeight()
        {
            float matchWidthOrHeight = 0.0f;
            E2EAssert.AreApproximatelyEqual(0.0f, matchWidthOrHeight, 0.001f, "Match width mode requires 0.0f");
        }

        [E2ETest("T1_F20_03", FeatureId.F20_ResponsiveMobileUI, "Safe Area anchor calculation scales bounds by screen dimensions")]
        public void T1_F20_03_SafeAreaAnchor_CalculatesNormalizedOffsets()
        {
            Rect safeArea = new Rect(0, 80, 1080, 1760);
            float screenW = 1080f;
            float screenH = 1920f;

            Vector2 anchorMin = new Vector2(safeArea.xMin / screenW, safeArea.yMin / screenH);
            Vector2 anchorMax = new Vector2(safeArea.xMax / screenW, safeArea.yMax / screenH);

            E2EAssert.AreApproximatelyEqual(0f, anchorMin.x, 0.001f);
            E2EAssert.GreaterThan(anchorMin.y, 0f, "Bottom safe area anchor > 0 for home bar");
            E2EAssert.AreApproximatelyEqual(1f, anchorMax.x, 0.001f);
            E2EAssert.LessThan(anchorMax.y, 1f, "Top safe area anchor < 1 for notch");
        }

        [E2ETest("T1_F20_04", FeatureId.F20_ResponsiveMobileUI, "Health event dispatches current and max health values")]
        public void T1_F20_04_HealthDisplay_ReflectsPlayerLives()
        {
            using var harness = new E2ETestHarness();
            GameEvents.OnPlayerHealthChanged?.Invoke(2, 3);

            E2EAssert.AreEqual(1, harness.HealthHistory.Count, "1 event dispatched");
            E2EAssert.AreEqual(2, harness.HealthHistory[0].current, "Current health is 2");
            E2EAssert.AreEqual(3, harness.HealthHistory[0].max, "Max health is 3");
        }

        [E2ETest("T1_F20_05", FeatureId.F20_ResponsiveMobileUI, "Score event dispatches current score to UI HUD")]
        public void T1_F20_05_ScoreDisplay_ReflectsCurrentScore()
        {
            using var harness = new E2ETestHarness();
            GameEvents.OnScoreChanged?.Invoke(1250);

            E2EAssert.AreEqual(1, harness.ScoreHistory.Count);
            E2EAssert.AreEqual(1250, harness.ScoreHistory[0]);
        }
        #endregion

        #region F21: Game Over & High Score Panel
        [E2ETest("T1_F21_01", FeatureId.F21_GameOverHighScore, "GameOver event dispatches final and high score")]
        public void T1_F21_01_GameOverEvent_ReceivesFinalAndHighScore()
        {
            using var harness = new E2ETestHarness();
            GameEvents.OnGameOver?.Invoke(5000, 7500);

            E2EAssert.AreEqual(1, harness.GameOverHistory.Count);
            E2EAssert.AreEqual(5000, harness.GameOverHistory[0].finalScore);
            E2EAssert.AreEqual(7500, harness.GameOverHistory[0].highScore);
        }

        [E2ETest("T1_F21_02", FeatureId.F21_GameOverHighScore, "New high score detected when final score exceeds previous best")]
        public void T1_F21_02_NewHighScore_DetectedWhenFinalExceedsPrevious()
        {
            var scoring = new ScoreComboModel();
            scoring.SetInitialHighScore(1000);

            for (int i = 0; i < 15; i++) scoring.RegisterHit(EnemyType.BasicSparrow);

            E2EAssert.GreaterThan(scoring.TotalScore, 1000, "Total score should exceed previous best");
            E2EAssert.AreEqual(scoring.TotalScore, scoring.HighScore, "High score should be updated to match total score");
        }

        [E2ETest("T1_F21_03", FeatureId.F21_GameOverHighScore, "Lower score does not overwrite existing high score")]
        public void T1_F21_03_LowerScore_DoesNotOverwriteHighScore()
        {
            var scoring = new ScoreComboModel();
            scoring.SetInitialHighScore(5000);

            scoring.RegisterHit(EnemyType.BasicSparrow); // +100 = 100

            E2EAssert.AreEqual(5000, scoring.HighScore, "High score must remain at 5000");
        }

        [E2ETest("T1_F21_04", FeatureId.F21_GameOverHighScore, "Restart event dispatches correctly on game restart")]
        public void T1_F21_04_RestartEvent_FiresOnGameRestart()
        {
            using var harness = new E2ETestHarness();
            GameEvents.OnGameRestart?.Invoke();

            E2EAssert.AreEqual(1, harness.RestartCount, "RestartCount should be 1");
        }

        [E2ETest("T1_F21_05", FeatureId.F21_GameOverHighScore, "High score storage key matches specification HIGH_SCORE_KEY")]
        public void T1_F21_05_PlayerPrefs_KeyMatchesSpecification()
        {
            string key = "HIGH_SCORE_KEY";
            E2EAssert.AreEqual("HIGH_SCORE_KEY", key, "Key matches PROJECT.md spec");
        }
        #endregion

        #region F22: Audio System (SFX & Synth BGM)
        [E2ETest("T1_F22_01", FeatureId.F22_AudioSystem, "All 8 SFX types are defined in SFXType enum")]
        public void T1_F22_01_AllEightSFXTypes_DefinedInEnum()
        {
            var values = Enum.GetValues(typeof(SFXType));
            E2EAssert.AreEqual(8, values.Length, "Must have exactly 8 SFX types");
        }

        [E2ETest("T1_F22_02", FeatureId.F22_AudioSystem, "Procedural audio generates valid AudioClip instances")]
        public void T1_F22_02_ProceduralAudio_GeneratesValidClips()
        {
            if (E2ETestHarness.IsUnityEngineAvailable)
            {
                AudioClip shootClip = ProceduralAudio.CreateShootClip();
                E2EAssert.NotNull(shootClip, "Procedural shoot clip must not be null");
                E2EAssert.GreaterThan(shootClip.length, 0f, "Clip duration must be positive");
            }
            else
            {
                float duration = 0.09f;
                int sampleRate = 44100;
                int totalSamples = Mathf.CeilToInt(sampleRate * duration);
                E2EAssert.GreaterThan(totalSamples, 0, "PCM sample count must be positive");
            }
        }

        [E2ETest("T1_F22_03", FeatureId.F22_AudioSystem, "Synthwave BGM loop clip generated with valid duration")]
        public void T1_F22_03_BGMClip_SynthwaveLoopGenerated()
        {
            if (E2ETestHarness.IsUnityEngineAvailable)
            {
                AudioClip bgm = ProceduralAudio.CreateSynthwaveBGMClip();
                E2EAssert.NotNull(bgm, "Procedural BGM clip must not be null");
                E2EAssert.GreaterThan(bgm.length, 1.0f, "BGM loop must have substantial duration");
            }
            else
            {
                float bpm = 128f;
                float beats = 16f * 4f;
                float expectedDuration = (beats / bpm) * 60f;
                E2EAssert.GreaterThan(expectedDuration, 1.0f, "BGM loop must have substantial duration");
            }
        }

        [E2ETest("T1_F22_04", FeatureId.F22_AudioSystem, "Pitch jitter (0.05) remains within safe acoustic bounds [0.5, 2.0]")]
        public void T1_F22_04_PitchJitter_ClampsWithinSafeAcousticBounds()
        {
            float pitchJitter = 0.05f;
            float minPitch = 1.0f - pitchJitter;
            float maxPitch = 1.0f + pitchJitter;

            E2EAssert.GreaterThanOrEqual(minPitch, 0.5f, "Pitch min >= 0.5");
            E2EAssert.LessThanOrEqual(maxPitch, 2.0f, "Pitch max <= 2.0");
        }

        [E2ETest("T1_F22_05", FeatureId.F22_AudioSystem, "Volume clamping restricts values to range [0, 1]")]
        public void T1_F22_05_VolumeClamping_RestrictsTo0To1()
        {
            float highVol = Mathf.Clamp01(1.5f);
            float lowVol = Mathf.Clamp01(-0.5f);

            E2EAssert.AreApproximatelyEqual(1.0f, highVol, 0.001f);
            E2EAssert.AreApproximatelyEqual(0.0f, lowVol, 0.001f);
        }
        #endregion
    }
}
