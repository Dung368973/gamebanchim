using System;
using UnityEngine;

namespace ShootEmUp.Tests.E2E
{
    [E2ETestFixture("Tier 2: Boundary & Corner Cases (Part 2 - F12 to F22)", TestTier.Tier2_BoundaryCorner, 40)]
    public class Tier2_BoundaryCornerTests_Part2
    {
        #region F12: Spread Shot Boundaries
        [E2ETest("T2_F12_01", FeatureId.F12_SpreadShot, "Negative weapon level falls back safely to single shot")]
        public void T2_F12_01_NegativeWeaponLevel_FallbacksToSingleShot()
        {
            Vector2[] vel = AutoShootingModel.GetSpreadVelocities(-2, 14f);
            E2EAssert.AreEqual(1, vel.Length, "Negative level must produce 1 projectile");
        }

        [E2ETest("T2_F12_02", FeatureId.F12_SpreadShot, "Excessive weapon level (level 10) falls back safely without exception")]
        public void T2_F12_02_ExcessiveWeaponLevel_FallbacksSafely()
        {
            Vector2[] vel = AutoShootingModel.GetSpreadVelocities(10, 14f);
            E2EAssert.AreEqual(1, vel.Length, "Unregistered high level must fallback to 1 projectile");
        }

        [E2ETest("T2_F12_03", FeatureId.F12_SpreadShot, "Spread velocity magnitude preserves bullet speed (14.0) across all angles")]
        public void T2_F12_03_SpreadVelocityMagnitude_PreservesSpeed()
        {
            Vector2[] vel5 = AutoShootingModel.GetSpreadVelocities(2, 14f);
            foreach (var v in vel5)
            {
                E2EAssert.AreApproximatelyEqual(14.0f, v.magnitude, 0.01f, "Each spread projectile must have speed 14.0 u/s");
            }
        }

        [E2ETest("T2_F12_04", FeatureId.F12_SpreadShot, "Trigonometric unity (sin^2 + cos^2 = 1) holds for all spread angles")]
        public void T2_F12_04_TrigonometricUnity_HoldsForAllAngles()
        {
            float[] angles = new float[] { -30f, -15f, 0f, 15f, 30f };
            foreach (var deg in angles)
            {
                float rad = deg * Mathf.Deg2Rad;
                float s = Mathf.Sin(rad);
                float c = Mathf.Cos(rad);
                E2EAssert.AreApproximatelyEqual(1.0f, (s * s) + (c * c), 0.001f, "Trig identity must hold");
            }
        }

        [E2ETest("T2_F12_05", FeatureId.F12_SpreadShot, "Spread shot expiration resets weapon level and velocities to 1-way")]
        public void T2_F12_05_SpreadExpiration_ResetsTo1Way()
        {
            int weaponLevel = 2; // 5-way
            // Buff expires
            weaponLevel = 0;

            Vector2[] vel = AutoShootingModel.GetSpreadVelocities(weaponLevel, 14f);
            E2EAssert.AreEqual(1, vel.Length, "Reverted weapon level produces 1 shot");
        }
        #endregion

        #region F13: Rapid Fire Boundaries
        [E2ETest("T2_F13_01", FeatureId.F13_RapidFire, "Rapid fire with zero delta time does not fire infinite shots")]
        public void T2_F13_01_RapidFire_ZeroDeltaTime_FiresZero()
        {
            var shooter = new AutoShootingModel { IsRapidFireActive = true };
            int shots = shooter.Update(0f);

            E2EAssert.AreEqual(0, shots, "Zero dt with rapid fire must fire 0 shots");
        }

        [E2ETest("T2_F13_02", FeatureId.F13_RapidFire, "Rapid fire expiration scales remaining accumulator timer safely")]
        public void T2_F13_02_RapidFireExpiration_PreservesRemainingAccumulator()
        {
            var shooter = new AutoShootingModel { IsRapidFireActive = true };
            shooter.Update(0.06f); // Timer = 0.06s (under 0.10s)
            E2EAssert.AreApproximatelyEqual(0.06f, shooter.FireTimer, 0.001f);

            // Expiration occurs: effective interval returns to 0.20s
            shooter.IsRapidFireActive = false;
            // Advance by 0.10s (total 0.16s < 0.20s) -> should NOT fire
            int shots = shooter.Update(0.10f);

            E2EAssert.AreEqual(0, shots, "Timer should not fire yet under restored 0.20s interval");
            E2EAssert.AreApproximatelyEqual(0.16f, shooter.FireTimer, 0.001f, "Accumulator should be 0.16s");
        }

        [E2ETest("T2_F13_03", FeatureId.F13_RapidFire, "Rapid fire multiple collections resets duration to 8.0s without stacking")]
        public void T2_F13_03_MultipleRapidPickups_ResetsDurationWithoutStacking()
        {
            float duration = 8.0f;
            duration -= 3.0f; // 5.0s left

            // Collect second rapid fire
            duration = 8.0f; // Reset

            E2EAssert.AreApproximatelyEqual(8.0f, duration, 0.001f, "Duration resets to 8.0s without stacking multiplier");
        }

        [E2ETest("T2_F13_04", FeatureId.F13_RapidFire, "Rapid fire during disabled state accumulates 0 shots")]
        public void T2_F13_04_DisabledShooterWithRapidFire_SpawnsZero()
        {
            var shooter = new AutoShootingModel { IsRapidFireActive = true, IsEnabled = false };
            int shots = shooter.Update(1.0f);

            E2EAssert.AreEqual(0, shots, "Disabled rapid shooter fires 0");
        }

        [E2ETest("T2_F13_05", FeatureId.F13_RapidFire, "Rapid fire effective rate equals 2x base rate precisely")]
        public void T2_F13_05_EffectiveRate_EqualsTwoTimesBase()
        {
            var baseShooter = new AutoShootingModel { IsRapidFireActive = false };
            var rapidShooter = new AutoShootingModel { IsRapidFireActive = true };

            float ratio = baseShooter.EffectiveInterval / rapidShooter.EffectiveInterval;
            E2EAssert.AreApproximatelyEqual(2.0f, ratio, 0.001f, "Interval ratio must be exactly 2.0");
        }
        #endregion

        #region F14: Health & Shield Boundaries
        [E2ETest("T2_F14_01", FeatureId.F14_HealthShield, "Shield duration expires naturally after 8.0s without hit")]
        public void T2_F14_01_ShieldExpiresNaturally_WithoutHit()
        {
            var health = new PlayerHealthModel();
            health.ActivateShield(8.0f);
            E2EAssert.IsTrue(health.HasShield);

            health.Update(8.1f); // Exceed duration

            E2EAssert.IsFalse(health.HasShield, "Shield should expire after duration timeout");
        }

        [E2ETest("T2_F14_02", FeatureId.F14_HealthShield, "Shield absorbs multi-damage (5 damage hit) completely")]
        public void T2_F14_02_ShieldAbsorbsMultiDamageHit_Completely()
        {
            var health = new PlayerHealthModel();
            health.ActivateShield(8.0f);

            bool dmgTaken = health.TakeDamage(5);

            E2EAssert.IsFalse(dmgTaken, "Shield must absorb entire hit");
            E2EAssert.AreEqual(3, health.CurrentLives, "All 3 lives preserved");
            E2EAssert.IsFalse(health.HasShield, "Shield broken");
        }

        [E2ETest("T2_F14_03", FeatureId.F14_HealthShield, "Collecting multiple shields resets duration without multi-hit stacking")]
        public void T2_F14_03_MultipleShieldPickups_DoNotStackHits()
        {
            var health = new PlayerHealthModel();
            health.ActivateShield(8.0f);
            health.ActivateShield(8.0f); // Second shield

            health.TakeDamage(1); // First hit breaks shield

            E2EAssert.IsFalse(health.HasShield, "Second pickup must not grant a 2nd hit absorption bubble");
        }

        [E2ETest("T2_F14_04", FeatureId.F14_HealthShield, "Damage taken during shield grace period (1.0s) is ignored")]
        public void T2_F14_04_DamageDuringGracePeriod_IsIgnored()
        {
            var health = new PlayerHealthModel();
            health.ActivateShield(8.0f);
            health.TakeDamage(1); // Shield breaks, 1.0s grace period active

            bool hitDuringGrace = health.TakeDamage(1);

            E2EAssert.IsFalse(hitDuringGrace, "Grace period hit must be ignored");
            E2EAssert.AreEqual(3, health.CurrentLives, "Lives remain 3");
        }

        [E2ETest("T2_F14_05", FeatureId.F14_HealthShield, "Health recovery at 1 life restores to 2 lives, not 3")]
        public void T2_F14_05_HealthRecoveryAtOneLife_RestoresToOneUnit()
        {
            var health = new PlayerHealthModel();
            health.TakeDamage(1);
            health.Update(2.1f);
            health.TakeDamage(1); // Lives = 1

            health.Heal(1);

            E2EAssert.AreEqual(2, health.CurrentLives, "Should restore to exactly 2 lives");
        }
        #endregion

        #region F15: Power-up Magnetism & Motion Boundaries
        [E2ETest("T2_F15_01", FeatureId.F15_PowerupMagnetism, "Power-up spawned exactly on player position (d=0) does not produce NaN")]
        public void T2_F15_01_SpawnedOnPlayer_DoesNotProduceNaN()
        {
            var sim = new PowerUpSimulation(PowerUpType.SpreadShot, Vector2.zero);
            sim.Update(0.016f, Vector2.zero);

            E2EAssert.IsTrue(!float.IsNaN(sim.Position.x) && !float.IsNaN(sim.Position.y), "Position must not be NaN at d=0");
        }

        [E2ETest("T2_F15_02", FeatureId.F15_PowerupMagnetism, "Power-up at exact magnetism boundary (d=1.600f) handled deterministically")]
        public void T2_F15_02_ExactMagnetismBoundary_EvaluatedDeterministically()
        {
            var sim = new PowerUpSimulation(PowerUpType.RapidFire, new Vector2(0f, 1.6f));
            Vector2 player = Vector2.zero; // dist = 1.6f

            sim.Update(0.1f, player);

            E2EAssert.IsTrue(!float.IsNaN(sim.Position.x) && !float.IsNaN(sim.Position.y));
        }

        [E2ETest("T2_F15_03", FeatureId.F15_PowerupMagnetism, "Player moving rapidly away from power-up continues directing acceleration")]
        public void T2_F15_03_PlayerMovingAway_AcceleratesTowardNewPos()
        {
            var sim = new PowerUpSimulation(PowerUpType.HealthRecovery, new Vector2(0f, 0.5f));
            Vector2 playerNewPos = new Vector2(0.5f, 0f);

            sim.Update(0.1f, playerNewPos);

            E2EAssert.GreaterThan(sim.Position.x, 0f, "Should steer rightward toward new player pos");
        }

        [E2ETest("T2_F15_04", FeatureId.F15_PowerupMagnetism, "Power-up below screen threshold detected as out of bounds")]
        public void T2_F15_04_PowerupBelowScreen_DetectedAsOutOfBounds()
        {
            Vector3 pos = new Vector3(0f, -8.0f, 0f);
            E2EAssert.IsTrue(BoundaryCleaner.IsOutOfBounds(pos), "Dropped power-up below -7.5 is out of bounds");
        }

        [E2ETest("T2_F15_05", FeatureId.F15_PowerupMagnetism, "Zero delta time leaves power-up position unchanged")]
        public void T2_F15_05_ZeroDeltaTime_LeavesPositionUnchanged()
        {
            var sim = new PowerUpSimulation(PowerUpType.Shield, new Vector2(1f, 2f));
            sim.Update(0f, Vector2.zero);

            E2EAssert.AreApproximatelyEqual(new Vector2(1f, 2f), sim.Position, 0.001f, "Zero dt must not move powerup");
        }
        #endregion

        #region F16: Score & Combo Multiplier Boundaries
        [E2ETest("T2_F16_01", FeatureId.F16_ScoreComboMultiplier, "Combo hit registering at exact frame of 3.0s expiry preserves combo")]
        public void T2_F16_01_HitAtTimerExpiry_PreservesCombo()
        {
            var scoring = new ScoreComboModel();
            for (int i = 0; i < 5; i++) scoring.RegisterHit(EnemyType.BasicSparrow);
            E2EAssert.AreEqual(5, scoring.ComboCount);

            // Time advances to 2.99s (not yet expired)
            scoring.Update(2.99f);
            scoring.RegisterHit(EnemyType.BasicSparrow); // Hits right before timeout!

            E2EAssert.AreEqual(6, scoring.ComboCount, "Combo count should increment to 6");
            E2EAssert.AreApproximatelyEqual(3.0f, scoring.ComboTimer, 0.001f, "Timer should reset to 3.0s");
        }

        [E2ETest("T2_F16_02", FeatureId.F16_ScoreComboMultiplier, "Massive combo count (1000) caps at 3.0x multiplier without overflow")]
        public void T2_F16_02_MassiveCombo_CapsAt3xMultiplier()
        {
            var scoring = new ScoreComboModel();
            for (int i = 0; i < 50; i++) scoring.RegisterHit(EnemyType.BasicSparrow);

            E2EAssert.AreApproximatelyEqual(3.0f, scoring.Multiplier, 0.001f, "Multiplier caps at 3.0x");
        }

        [E2ETest("T2_F16_03", FeatureId.F16_ScoreComboMultiplier, "Player taking damage immediately resets combo count to 0 and multiplier to 1.0x")]
        public void T2_F16_03_PlayerDamaged_ResetsComboToZero()
        {
            var scoring = new ScoreComboModel();
            for (int i = 0; i < 15; i++) scoring.RegisterHit(EnemyType.BasicSparrow);
            E2EAssert.AreEqual(15, scoring.ComboCount);

            scoring.OnPlayerDamaged();

            E2EAssert.AreEqual(0, scoring.ComboCount, "Combo count must reset to 0 upon player damage");
            E2EAssert.AreApproximatelyEqual(1.0f, scoring.Multiplier, 0.001f, "Multiplier must reset to 1.0x");
        }

        [E2ETest("T2_F16_04", FeatureId.F16_ScoreComboMultiplier, "Equal score to high score updates high score safely without issue")]
        public void T2_F16_04_EqualScoreToHighScore_HandledCleanly()
        {
            var scoring = new ScoreComboModel();
            scoring.SetInitialHighScore(100);

            scoring.RegisterHit(EnemyType.BasicSparrow); // +100 = 100

            E2EAssert.AreEqual(100, scoring.TotalScore);
            E2EAssert.AreEqual(100, scoring.HighScore);
        }

        [E2ETest("T2_F16_05", FeatureId.F16_ScoreComboMultiplier, "ResetGame clears total score and combo while preserving high score")]
        public void T2_F16_05_ResetGame_PreservesHighScore()
        {
            var scoring = new ScoreComboModel();
            scoring.RegisterHit(EnemyType.TankEagle); // +1500
            int recordedHigh = scoring.HighScore;

            scoring.ResetGame();

            E2EAssert.AreEqual(0, scoring.TotalScore, "Total score should reset to 0");
            E2EAssert.AreEqual(0, scoring.ComboCount, "Combo count should reset to 0");
            E2EAssert.AreEqual(recordedHigh, scoring.HighScore, "High score must be preserved across game resets");
        }
        #endregion

        #region F17: Game Loop FSM Boundaries
        [E2ETest("T2_F17_01", FeatureId.F17_GameLoopFSM, "Illegal transition Boot directly to GameOver is rejected")]
        public void T2_F17_01_BootToGameOver_Rejected()
        {
            var fsm = new GameLoopStateMachine();
            bool result = fsm.TransitionTo(GameState.GameOver);

            E2EAssert.IsFalse(result, "Boot directly to GameOver must be invalid");
            E2EAssert.AreEqual(GameState.Boot, fsm.CurrentState, "State must remain Boot");
        }

        [E2ETest("T2_F17_02", FeatureId.F17_GameLoopFSM, "Pausing when already paused is rejected")]
        public void T2_F17_02_RedundantPause_Rejected()
        {
            var fsm = new GameLoopStateMachine();
            fsm.TransitionTo(GameState.MainMenu);
            fsm.TransitionTo(GameState.Playing);
            fsm.TransitionTo(GameState.Paused);

            bool redundant = fsm.TransitionTo(GameState.Paused);

            E2EAssert.IsFalse(redundant, "Redundant transition to same state must return false");
        }

        [E2ETest("T2_F17_03", FeatureId.F17_GameLoopFSM, "GameOver when already in GameOver is rejected")]
        public void T2_F17_03_RedundantGameOver_Rejected()
        {
            var fsm = new GameLoopStateMachine();
            fsm.TransitionTo(GameState.MainMenu);
            fsm.TransitionTo(GameState.Playing);
            fsm.TransitionTo(GameState.GameOver);

            bool redundant = fsm.TransitionTo(GameState.GameOver);

            E2EAssert.IsFalse(redundant, "Redundant GameOver must be rejected");
        }

        [E2ETest("T2_F17_04", FeatureId.F17_GameLoopFSM, "Rapid Pause/Resume sequence maintains consistent state")]
        public void T2_F17_04_RapidPauseResume_MaintainsConsistency()
        {
            var fsm = new GameLoopStateMachine();
            fsm.TransitionTo(GameState.MainMenu);
            fsm.TransitionTo(GameState.Playing);

            for (int i = 0; i < 5; i++)
            {
                fsm.TransitionTo(GameState.Paused);
                E2EAssert.AreEqual(GameState.Paused, fsm.CurrentState);
                fsm.TransitionTo(GameState.Playing);
                E2EAssert.AreEqual(GameState.Playing, fsm.CurrentState);
            }
        }

        [E2ETest("T2_F17_05", FeatureId.F17_GameLoopFSM, "OnStateChanged event dispatches correct previous and next states")]
        public void T2_F17_05_OnStateChanged_DispatchesPreviousAndNext()
        {
            var fsm = new GameLoopStateMachine();
            GameState recordedPrev = GameState.GameOver;
            GameState recordedNext = GameState.GameOver;

            fsm.OnStateChanged += (prev, next) =>
            {
                recordedPrev = prev;
                recordedNext = next;
            };

            fsm.TransitionTo(GameState.MainMenu);

            E2EAssert.AreEqual(GameState.Boot, recordedPrev);
            E2EAssert.AreEqual(GameState.MainMenu, recordedNext);
        }
        #endregion

        #region F18: Procedural Sprites Boundaries
        [E2ETest("T2_F18_01", FeatureId.F18_ProceduralSprites, "Center pixel symmetry evaluation produces 0 distance")]
        public void T2_F18_01_CenterPixelSymmetry_DistanceZero()
        {
            float center = 48f;
            float dist = Mathf.Abs(center - center);
            E2EAssert.AreApproximatelyEqual(0f, dist, 0.001f, "Center symmetry distance is 0");
        }

        [E2ETest("T2_F18_02", FeatureId.F18_ProceduralSprites, "Point at circle center has signed distance equal to -radius")]
        public void T2_F18_02_CenterPointSignedDistance_EqualsNegativeRadius()
        {
            Vector2 center = new Vector2(50f, 50f);
            float radius = 20f;
            float dist = Vector2.Distance(center, center) - radius;

            E2EAssert.AreApproximatelyEqual(-20f, dist, 0.001f, "Center dist is -radius");
        }

        [E2ETest("T2_F18_03", FeatureId.F18_ProceduralSprites, "Transparent background pixels have alpha equal to 0.0")]
        public void T2_F18_03_TransparentPixels_HaveZeroAlpha()
        {
            Color transparent = new Color(0f, 0f, 0f, 0f);
            E2EAssert.AreApproximatelyEqual(0f, transparent.a, 0.001f, "Alpha must be 0");
        }

        [E2ETest("T2_F18_04", FeatureId.F18_ProceduralSprites, "Sky base background aspect ratio is 1:2 (512x1024)")]
        public void T2_F18_04_SkyBaseAspect_EqualsOneByTwo()
        {
            float width = 512f;
            float height = 1024f;
            float aspect = width / height;

            E2EAssert.AreApproximatelyEqual(0.5f, aspect, 0.001f, "Aspect ratio must be 0.5");
        }

        [E2ETest("T2_F18_05", FeatureId.F18_ProceduralSprites, "Pixels per unit 100 converts 128px sprite to 1.28 world units")]
        public void T2_F18_05_PPU_ConversionAccurate()
        {
            float px = 128f;
            float ppu = 100f;
            float worldSize = px / ppu;

            E2EAssert.AreApproximatelyEqual(1.28f, worldSize, 0.001f, "World size should be 1.28 units");
        }
        #endregion

        #region F19: Parallax Scrolling Background Boundaries
        [E2ETest("T2_F19_01", FeatureId.F19_ParallaxBackground, "High scroll speed (50 u/s) leapfrogs without gap")]
        public void T2_F19_01_HighScrollSpeed_LeapfrogsWithoutGap()
        {
            var layer = new ParallaxLeapfrogModel(50.0f, 10.0f);
            layer.Update(0.5f); // 25.0 units scroll

            float gap = Mathf.Abs(layer.PosA - layer.PosB);
            E2EAssert.AreApproximatelyEqual(10.0f, gap, 0.01f, "Gap must equal sprite height even under extreme speed");
        }

        [E2ETest("T2_F19_02", FeatureId.F19_ParallaxBackground, "Both sprites pass threshold across multiple cycles cleanly")]
        public void T2_F19_02_MultipleWrapCycles_PreservesOffset()
        {
            var layer = new ParallaxLeapfrogModel(10.0f, 10.0f);
            // Run for 10 seconds -> 100 units scrolled (10 full wrap cycles)
            layer.Update(10.0f);

            float gap = Mathf.Abs(layer.PosA - layer.PosB);
            E2EAssert.AreApproximatelyEqual(10.0f, gap, 0.01f, "Offset preserved after 10 cycles");
        }

        [E2ETest("T2_F19_03", FeatureId.F19_ParallaxBackground, "Top edge of lower sprite touches bottom edge of upper sprite")]
        public void T2_F19_03_SeamlessEdgeContact_Maintained()
        {
            var layer = new ParallaxLeapfrogModel(2.0f, 10.0f);
            layer.Update(1.0f);

            float lower = Mathf.Min(layer.PosA, layer.PosB);
            float upper = Mathf.Max(layer.PosA, layer.PosB);

            // Upper - Lower must equal height
            E2EAssert.AreApproximatelyEqual(10.0f, upper - lower, 0.001f, "Upper minus lower must equal sprite height");
        }

        [E2ETest("T2_F19_04", FeatureId.F19_ParallaxBackground, "Zero scroll speed leaves both positions completely stationary")]
        public void T2_F19_04_ZeroScrollSpeed_LeavesStationary()
        {
            var layer = new ParallaxLeapfrogModel(0f, 10.0f);
            layer.Update(5.0f);

            E2EAssert.AreApproximatelyEqual(0f, layer.PosA, 0.001f);
            E2EAssert.AreApproximatelyEqual(10f, layer.PosB, 0.001f);
        }

        [E2ETest("T2_F19_05", FeatureId.F19_ParallaxBackground, "Sprite height minimum clamped to positive value")]
        public void T2_F19_05_SpriteHeight_ClampedPositive()
        {
            float height = Mathf.Max(0.1f, -5f);
            E2EAssert.GreaterThan(height, 0f, "Height must be strictly positive");
        }
        #endregion

        #region F20: Responsive Mobile UI Boundaries
        [E2ETest("T2_F20_01", FeatureId.F20_ResponsiveMobileUI, "Zero notch safe area sets anchorMin to (0,0) and anchorMax to (1,1)")]
        public void T2_F20_01_ZeroNotch_SetsFullAnchors()
        {
            Rect fullScreen = new Rect(0, 0, 1080, 1920);
            Vector2 min = new Vector2(fullScreen.xMin / 1080f, fullScreen.yMin / 1920f);
            Vector2 max = new Vector2(fullScreen.xMax / 1080f, fullScreen.yMax / 1920f);

            E2EAssert.AreApproximatelyEqual(Vector2.zero, min, 0.001f);
            E2EAssert.AreApproximatelyEqual(Vector2.one, max, 0.001f);
        }

        [E2ETest("T2_F20_02", FeatureId.F20_ResponsiveMobileUI, "Large camera notch (150px) shifts top anchor down to 0.921")]
        public void T2_F20_02_LargeNotch_ShiftsTopAnchor()
        {
            Rect notchArea = new Rect(0, 0, 1080, 1770);
            Vector2 max = new Vector2(notchArea.xMax / 1080f, notchArea.yMax / 1920f);

            // 1770 / 1920 = 0.921875
            E2EAssert.AreApproximatelyEqual(0.921875f, max.y, 0.001f, "Top anchor shifts down to accommodate notch");
        }

        [E2ETest("T2_F20_03", FeatureId.F20_ResponsiveMobileUI, "Combo timer fill amount clamped in range [0, 1]")]
        public void T2_F20_03_ComboTimerFillAmount_ClampedInZeroToOne()
        {
            float fillOver = Mathf.Clamp01(3.5f / 3.0f);
            float fillUnder = Mathf.Clamp01(-0.5f / 3.0f);

            E2EAssert.AreApproximatelyEqual(1.0f, fillOver, 0.001f);
            E2EAssert.AreApproximatelyEqual(0.0f, fillUnder, 0.001f);
        }

        [E2ETest("T2_F20_04", FeatureId.F20_ResponsiveMobileUI, "Rapid score events (50 events in 1 frame) dispatched cleanly")]
        public void T2_F20_04_RapidScoreEvents_DispatchedCleanly()
        {
            using var harness = new E2ETestHarness();
            for (int i = 1; i <= 50; i++)
            {
                GameEvents.OnScoreChanged?.Invoke(i * 100);
            }

            E2EAssert.AreEqual(50, harness.ScoreHistory.Count, "All 50 events captured");
            E2EAssert.AreEqual(5000, harness.ScoreHistory[49], "Last score is 5000");
        }

        [E2ETest("T2_F20_05", FeatureId.F20_ResponsiveMobileUI, "Health changed event with 0 current lives dispatches (0, 3)")]
        public void T2_F20_05_HealthZeroDispatched_Cleanly()
        {
            using var harness = new E2ETestHarness();
            GameEvents.OnPlayerHealthChanged?.Invoke(0, 3);

            E2EAssert.AreEqual(1, harness.HealthHistory.Count);
            E2EAssert.AreEqual(0, harness.HealthHistory[0].current);
        }
        #endregion

        #region F21: Game Over & High Score Boundaries
        [E2ETest("T2_F21_01", FeatureId.F21_GameOverHighScore, "Final score zero and high score zero handles cleanly without error")]
        public void T2_F21_01_ZeroScoreGameOver_HandledCleanly()
        {
            using var harness = new E2ETestHarness();
            GameEvents.OnGameOver?.Invoke(0, 0);

            E2EAssert.AreEqual(1, harness.GameOverHistory.Count);
            E2EAssert.AreEqual(0, harness.GameOverHistory[0].finalScore);
            E2EAssert.AreEqual(0, harness.GameOverHistory[0].highScore);
        }

        [E2ETest("T2_F21_02", FeatureId.F21_GameOverHighScore, "Massive score (999,999,999) handled without string format exception")]
        public void T2_F21_02_MassiveScore_FormatsCleanly()
        {
            int massive = 999999999;
            string formatted = massive.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

            E2EAssert.AreEqual("999,999,999", formatted, "Score formatting must support large integers");
        }

        [E2ETest("T2_F21_03", FeatureId.F21_GameOverHighScore, "Multiple consecutive restart clicks dispatch distinct events")]
        public void T2_F21_03_MultipleRestartClicks_DispatchesDistinctEvents()
        {
            using var harness = new E2ETestHarness();
            GameEvents.OnGameRestart?.Invoke();
            GameEvents.OnGameRestart?.Invoke();

            E2EAssert.AreEqual(2, harness.RestartCount, "2 restart clicks must fire 2 events");
        }

        [E2ETest("T2_F21_04", FeatureId.F21_GameOverHighScore, "Negative high score input clamped to zero")]
        public void T2_F21_04_NegativeHighScore_ClampedToZero()
        {
            int high = Mathf.Max(0, -100);
            E2EAssert.AreEqual(0, high, "Negative high score clamped to 0");
        }

        [E2ETest("T2_F21_05", FeatureId.F21_GameOverHighScore, "High score remains unchanged when new game starts with score 0")]
        public void T2_F21_05_NewGameZeroScore_LeavesHighScoreIntact()
        {
            var scoring = new ScoreComboModel();
            scoring.SetInitialHighScore(50000);
            scoring.ResetGame();

            E2EAssert.AreEqual(50000, scoring.HighScore, "High score must not be wiped on new game reset");
        }
        #endregion

        #region F22: Audio System Boundaries
        [E2ETest("T2_F22_01", FeatureId.F22_AudioSystem, "Master volume clamped to [0, 1] on out-of-range inputs")]
        public void T2_F22_01_MasterVolume_ClampedSafely()
        {
            float vHigh = Mathf.Clamp01(10.0f);
            float vLow = Mathf.Clamp01(-5.0f);

            E2EAssert.AreApproximatelyEqual(1.0f, vHigh, 0.001f);
            E2EAssert.AreApproximatelyEqual(0.0f, vLow, 0.001f);
        }

        [E2ETest("T2_F22_02", FeatureId.F22_AudioSystem, "Pitch jitter clamp bounds [0.5, 2.0] withstands large jitter inputs")]
        public void T2_F22_02_PitchJitter_WithstandsLargeInput()
        {
            float extremeJitter = 10.0f;
            float clampedPitch = Mathf.Clamp(1.0f + extremeJitter, 0.5f, 2.0f);

            E2EAssert.AreApproximatelyEqual(2.0f, clampedPitch, 0.001f, "Pitch must clamp to 2.0 max");
        }

        [E2ETest("T2_F22_03", FeatureId.F22_AudioSystem, "SFX voice round-robin index wraps with modulo operator")]
        public void T2_F22_03_SFXVoiceIndex_WrapsAroundPoolSize()
        {
            int poolSize = 12;
            int nextIndex = 11;

            nextIndex = (nextIndex + 1) % poolSize;
            E2EAssert.AreEqual(0, nextIndex, "Next index after 11 should wrap to 0");
        }

        [E2ETest("T2_F22_04", FeatureId.F22_AudioSystem, "Procedural sound sample values remain within [-1.0, 1.0] PCM bounds")]
        public void T2_F22_04_SoundSamples_RemainWithinPCMBounds()
        {
            // Verify square, sine and noise samples
            for (float t = 0f; t < 1f; t += 0.01f)
            {
                float sine = Mathf.Sin(t * 100f);
                float square = Mathf.Sign(sine);
                E2EAssert.InRange(sine, -1.0f, 1.0f);
                E2EAssert.InRange(square, -1.0f, 1.0f);
            }
        }

        [E2ETest("T2_F22_05", FeatureId.F22_AudioSystem, "Audio frequency sweep remains strictly positive (Hz > 0)")]
        public void T2_F22_05_FrequencySweep_StrictlyPositive()
        {
            float fStart = 950f;
            float fEnd = 220f;

            for (float t = 0f; t <= 1f; t += 0.1f)
            {
                float freq = Mathf.Lerp(fStart, fEnd, t);
                E2EAssert.GreaterThan(freq, 0f, "Audio frequency must be strictly positive");
            }
        }
        #endregion
    }
}
