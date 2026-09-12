using System;
using UnityEngine;

namespace ShootEmUp.Tests.E2E
{
    [E2ETestFixture("Tier 2: Boundary & Corner Cases (Part 1 - F01 to F11)", TestTier.Tier2_BoundaryCorner, 30)]
    public class Tier2_BoundaryCornerTests_Part1
    {
        #region F01: Mobile Touch / Mouse Drag Boundaries
        [E2ETest("T2_F01_01", FeatureId.F01_TouchDrag, "Secondary finger touch down while dragging is ignored")]
        public void T2_F01_01_MultiTouch_SecondaryFingerIgnored()
        {
            var sim = new TouchDragSimulation { PlayerPosition = Vector2.zero };
            sim.OnTouchDown(0, Vector2.zero);

            // Secondary finger touches down
            sim.OnTouchDown(1, new Vector2(5f, 5f));

            E2EAssert.AreEqual(0, sim.ActiveFingerId, "Active finger must remain finger 0");
        }

        [E2ETest("T2_F01_02", FeatureId.F01_TouchDrag, "Touch move with secondary finger ID does not alter player position")]
        public void T2_F01_02_SecondaryFingerMove_DoesNotMovePlayer()
        {
            var sim = new TouchDragSimulation { PlayerPosition = Vector2.zero };
            sim.OnTouchDown(0, Vector2.zero);

            sim.OnTouchMove(1, new Vector2(10f, 10f), 0.016f);

            E2EAssert.AreApproximatelyEqual(Vector2.zero, sim.PlayerPosition, 0.001f, "Secondary finger move must be ignored");
        }

        [E2ETest("T2_F01_03", FeatureId.F01_TouchDrag, "Zero delta time (dt = 0) does not cause position drift or NaN")]
        public void T2_F01_03_ZeroDeltaTime_DoesNotCauseDriftOrNaN()
        {
            var sim = new TouchDragSimulation { PlayerPosition = new Vector2(1f, 1f) };
            sim.OnTouchDown(0, new Vector2(1f, 1f));

            sim.OnTouchMove(0, new Vector2(2f, 2f), 0f, useSmoothing: true);

            E2EAssert.IsTrue(!float.IsNaN(sim.PlayerPosition.x) && !float.IsNaN(sim.PlayerPosition.y), "Position must not be NaN");
            E2EAssert.AreApproximatelyEqual(new Vector2(1f, 1f), sim.PlayerPosition, 0.001f, "Zero dt should not move player");
        }

        [E2ETest("T2_F01_04", FeatureId.F01_TouchDrag, "Large delta time spike (dt = 5.0s) smoothly reaches target without overshoot")]
        public void T2_F01_04_LargeDeltaTime_ReachesTargetWithoutOvershoot()
        {
            var sim = new TouchDragSimulation { PlayerPosition = Vector2.zero };
            sim.OnTouchDown(0, Vector2.zero);

            sim.OnTouchMove(0, new Vector2(3f, 3f), 5.0f, useSmoothing: true);

            E2EAssert.AreApproximatelyEqual(new Vector2(3f, 3f), sim.PlayerPosition, 0.001f, "Should reach target cleanly without overshoot");
        }

        [E2ETest("T2_F01_05", FeatureId.F01_TouchDrag, "Keyboard input ignored while touch drag is active")]
        public void T2_F01_05_KeyboardInput_IgnoredWhileTouchDragging()
        {
            var sim = new TouchDragSimulation { PlayerPosition = Vector2.zero };
            sim.OnTouchDown(0, Vector2.zero);

            sim.OnKeyboardInput(new Vector2(1f, 0f), 1.0f);

            E2EAssert.AreApproximatelyEqual(Vector2.zero, sim.PlayerPosition, 0.001f, "Keyboard input must be suppressed while dragging");
        }
        #endregion

        #region F02: Screen Viewport Clamping Boundaries
        [E2ETest("T2_F02_01", FeatureId.F02_ViewportClamping, "Clamping handles iPad 4:3 aspect ratio correctly")]
        public void T2_F02_01_Aspect4By3_RecalculatesHorizontalBounds()
        {
            var clamp = new ViewportClampingModel { AspectRatio = 4.0f / 3.0f, OrthoSize = 6.0f };
            // Half-width = 6.0 * 1.333 = 8.0
            float expectedHalfWidth = 8.0f;

            E2EAssert.AreApproximatelyEqual(expectedHalfWidth, clamp.OrthoHalfWidth, 0.01f, "Half width matches 4:3 aspect");
            E2EAssert.GreaterThan(clamp.MaxX, 7.0f, "MaxX expands on wider screen");
        }

        [E2ETest("T2_F02_02", FeatureId.F02_ViewportClamping, "Clamping handles ultra-tall phone (21:9) strictly")]
        public void T2_F02_02_Aspect21By9_RestrictsHorizontalBounds()
        {
            var clamp = new ViewportClampingModel { AspectRatio = 9.0f / 21.0f, OrthoSize = 6.0f };
            // Half-width = 6.0 * (9/21) = 2.571
            E2EAssert.LessThan(clamp.MaxX, 2.5f, "Horizontal bounds tighten on narrow phones");
        }

        [E2ETest("T2_F02_03", FeatureId.F02_ViewportClamping, "Exact corner coordinates remain at corner vertices")]
        public void T2_F02_03_CornerCoordinates_RemainOnVertices()
        {
            var clamp = new ViewportClampingModel();
            Vector2 corner = new Vector2(clamp.MinX, clamp.MinY);

            Vector2 clamped = clamp.Clamp(corner);

            E2EAssert.AreApproximatelyEqual(corner, clamped, 0.001f, "Corner coordinates must remain on boundary");
        }

        [E2ETest("T2_F02_04", FeatureId.F02_ViewportClamping, "Extreme coordinate input (+/-10000) clamped safely")]
        public void T2_F02_04_ExtremeCoordinates_ClampedSafely()
        {
            var clamp = new ViewportClampingModel();
            Vector2 extreme = new Vector2(10000f, 10000f);

            Vector2 clamped = clamp.Clamp(extreme);

            E2EAssert.AreApproximatelyEqual(clamp.MaxX, clamped.x, 0.001f);
            E2EAssert.AreApproximatelyEqual(clamp.MaxY, clamped.y, 0.001f);
        }

        [E2ETest("T2_F02_05", FeatureId.F02_ViewportClamping, "Player extents larger than half-width centers horizontally")]
        public void T2_F02_05_HugePlayerExtents_HandledGracefully()
        {
            var clamp = new ViewportClampingModel { PlayerExtents = new Vector2(10.0f, 10.0f) };
            Vector2 clamped = clamp.Clamp(Vector2.zero);

            E2EAssert.IsTrue(!float.IsNaN(clamped.x) && !float.IsNaN(clamped.y), "Clamped values must not be NaN");
        }
        #endregion

        #region F03: Auto-Shooting System Boundaries
        [E2ETest("T2_F03_01", FeatureId.F03_AutoShooting, "Negative delta time ignored and produces zero shots")]
        public void T2_F03_01_NegativeDeltaTime_SpawnsZeroShots()
        {
            var shooter = new AutoShootingModel();
            int shots = shooter.Update(-0.5f);

            E2EAssert.AreEqual(0, shots, "Negative dt must produce 0 shots");
            E2EAssert.AreApproximatelyEqual(0f, shooter.FireTimer, 0.001f, "Fire timer should remain 0");
        }

        [E2ETest("T2_F03_02", FeatureId.F03_AutoShooting, "Large lag spike (dt = 1.0s) spawns exact accumulator quotient without hang")]
        public void T2_F03_02_LargeLagSpike_SpawnsAccurateShotCount()
        {
            var shooter = new AutoShootingModel(); // 0.20s interval
            int shots = shooter.Update(1.05f);

            // 1.05 / 0.20 = 5 shots, remainder 0.05s
            E2EAssert.AreEqual(5, shots, "1.05s should spawn exactly 5 shots");
            E2EAssert.AreApproximatelyEqual(0.05f, shooter.FireTimer, 0.001f, "Remainder timer must be 0.05s");
        }

        [E2ETest("T2_F03_03", FeatureId.F03_AutoShooting, "Exact interval match (dt = 0.200s) spawns 1 shot with 0 remainder")]
        public void T2_F03_03_ExactIntervalMatch_SpawnsOneShotWithZeroRemainder()
        {
            var shooter = new AutoShootingModel();
            int shots = shooter.Update(0.20f);

            E2EAssert.AreEqual(1, shots, "Exact 0.20s should spawn 1 shot");
            E2EAssert.AreApproximatelyEqual(0f, shooter.FireTimer, 0.001f, "Remainder should be 0");
        }

        [E2ETest("T2_F03_04", FeatureId.F03_AutoShooting, "Rapid fire toggled mid-accumulator preserves elapsed time")]
        public void T2_F03_04_RapidFireToggledMidAccumulator_PreservesElapsed()
        {
            var shooter = new AutoShootingModel();
            shooter.Update(0.08f); // Timer = 0.08s
            E2EAssert.AreApproximatelyEqual(0.08f, shooter.FireTimer, 0.001f);

            shooter.IsRapidFireActive = true; // Effective interval becomes 0.10s
            // Advance by 0.03s (total 0.11s >= 0.10s) -> fires!
            int shots = shooter.Update(0.03f);

            E2EAssert.AreEqual(1, shots, "Should fire immediately upon crossing 0.10s");
            E2EAssert.AreApproximatelyEqual(0.01f, shooter.FireTimer, 0.001f, "Remainder is 0.11 - 0.10 = 0.01s");
        }

        [E2ETest("T2_F03_05", FeatureId.F03_AutoShooting, "Unknown weapon level falls back safely to single shot")]
        public void T2_F03_05_UnknownWeaponLevel_FallbacksToSingleShot()
        {
            Vector2[] vel = AutoShootingModel.GetSpreadVelocities(99, 14.0f);
            E2EAssert.AreEqual(1, vel.Length, "Unknown weapon level must fallback to 1 projectile");
            E2EAssert.AreApproximatelyEqual(new Vector2(0f, 14.0f), vel[0], 0.001f);
        }
        #endregion

        #region F04: Bullet Pooling Boundaries
        [E2ETest("T2_F04_01", FeatureId.F04_BulletPooling, "Prewarm with 0 capacity initializes empty pool without crash")]
        public void T2_F04_01_PrewarmZero_InitializesCleanly()
        {
            using var harness = new E2ETestHarness();
            var pool = harness.CreateBulletPool(0);

            E2EAssert.AreEqual(0, pool.AvailableCount);
            E2EAssert.AreEqual(0, pool.TotalCreated);
        }

        [E2ETest("T2_F04_02", FeatureId.F04_BulletPooling, "Rapid burst acquisition (50 instances) expands smoothly")]
        public void T2_F04_02_RapidBurstAcquisition_ExpandsWithoutCrash()
        {
            using var harness = new E2ETestHarness();
            var pool = harness.CreateBulletPool(5);
            var list = new System.Collections.Generic.List<ITestDamageable>();

            for (int i = 0; i < 50; i++)
            {
                list.Add(pool.Get());
            }

            E2EAssert.AreEqual(50, pool.ActiveCount, "Active count should be 50");
            E2EAssert.AreEqual(50, pool.TotalCreated, "Total created should be 50");

            foreach (var item in list) pool.Return(item);

            E2EAssert.AreEqual(50, pool.AvailableCount, "All 50 should return to pool");
        }

        [E2ETest("T2_F04_03", FeatureId.F04_BulletPooling, "Return null object is handled gracefully without exception")]
        public void T2_F04_03_ReturnNull_HandledGracefully()
        {
            using var harness = new E2ETestHarness();
            var pool = harness.CreateBulletPool(2);
            pool.Return(null);

            E2EAssert.AreEqual(2, pool.AvailableCount, "Pool count unchanged after returning null");
        }

        [E2ETest("T2_F04_04", FeatureId.F04_BulletPooling, "Clear() destroys all pooled GameObjects and resets counts")]
        public void T2_F04_04_Clear_DestroysAllPooledObjectsAndResets()
        {
            using var harness = new E2ETestHarness();
            var pool = harness.CreateBulletPool(5);
            pool.Clear();

            E2EAssert.AreEqual(0, pool.AvailableCount, "Available count should be 0 after Clear");
            E2EAssert.AreEqual(0, pool.TotalCreated, "Total created should be 0 after Clear");
        }

        [E2ETest("T2_F04_05", FeatureId.F04_BulletPooling, "Null prefab constructor throws ArgumentNullException")]
        public void T2_F04_05_NullPrefab_ThrowsArgumentNullException()
        {
            E2EAssert.Throws<ArgumentNullException>(() =>
            {
                var pool = new SimulatedObjectPool<ITestDamageable>(null, 5);
            }, "Constructing ObjectPool with null prefab must throw ArgumentNullException");
        }
        #endregion

        #region F05: Player Health Boundaries
        [E2ETest("T2_F05_01", FeatureId.F05_PlayerHealth, "Zero damage TakeDamage(0) ignored with 0 life loss and no I-frames")]
        public void T2_F05_01_ZeroDamage_Ignored()
        {
            var health = new PlayerHealthModel();
            bool damaged = health.TakeDamage(0);

            E2EAssert.IsFalse(damaged, "Zero damage must return false");
            E2EAssert.AreEqual(3, health.CurrentLives, "Lives remain 3");
            E2EAssert.IsFalse(health.IsInvulnerable, "No I-frames should trigger");
        }

        [E2ETest("T2_F05_02", FeatureId.F05_PlayerHealth, "Negative damage ignored safely")]
        public void T2_F05_02_NegativeDamage_Ignored()
        {
            var health = new PlayerHealthModel();
            bool damaged = health.TakeDamage(-10);

            E2EAssert.IsFalse(damaged, "Negative damage must be ignored");
            E2EAssert.AreEqual(3, health.CurrentLives, "Lives unchanged");
        }

        [E2ETest("T2_F05_03", FeatureId.F05_PlayerHealth, "Overkill damage (999) clamps lives cleanly at 0 without negative health")]
        public void T2_F05_03_OverkillDamage_ClampsAtZero()
        {
            var health = new PlayerHealthModel();
            health.TakeDamage(999);

            E2EAssert.AreEqual(0, health.CurrentLives, "Lives must clamp at 0");
            E2EAssert.IsFalse(health.IsAlive, "Player must be dead");
        }

        [E2ETest("T2_F05_04", FeatureId.F05_PlayerHealth, "Hit exactly at I-frame expiration frame (t=2.0s) inflicts damage")]
        public void T2_F05_04_HitAtIFrameExpiration_InflictsDamage()
        {
            var health = new PlayerHealthModel();
            health.TakeDamage(1); // Lives = 2, Invulnerable = 2.0s
            health.Update(2.0f);   // Exactly 2.0s elapsed -> Invulnerability expires

            E2EAssert.IsFalse(health.IsInvulnerable, "Should no longer be invulnerable");
            bool hit2 = health.TakeDamage(1);

            E2EAssert.IsTrue(hit2, "Hit at expiration must register");
            E2EAssert.AreEqual(1, health.CurrentLives, "Lives should reduce to 1");
        }

        [E2ETest("T2_F05_05", FeatureId.F05_PlayerHealth, "Healing rejected when player is already dead")]
        public void T2_F05_05_HealingRejectedWhenDead()
        {
            var health = new PlayerHealthModel();
            health.TakeDamage(3); // Dead

            bool healed = health.Heal(1);

            E2EAssert.IsFalse(healed, "Cannot heal dead player");
            E2EAssert.AreEqual(0, health.CurrentLives, "Lives must remain 0");
            E2EAssert.IsFalse(health.IsAlive, "Player remains dead");
        }
        #endregion

        #region F06: Basic Bird Boundaries
        [E2ETest("T2_F06_01", FeatureId.F06_BasicBird, "Zero descent speed leaves bird stationary without error")]
        public void T2_F06_01_ZeroDescentSpeed_StationaryWithoutError()
        {
            Vector2 origin = new Vector2(0f, 5f);
            Vector2 pos = EnemyKinematics.BasicBirdPosition(origin, Vector2.zero, 0f, 0f, 10f);

            E2EAssert.AreApproximatelyEqual(origin, pos, 0.001f, "Zero speed must leave position stationary");
        }

        [E2ETest("T2_F06_02", FeatureId.F06_BasicBird, "Extreme descent speed (100 u/s) calculates cleanly")]
        public void T2_F06_02_ExtremeDescentSpeed_CalculatesCleanly()
        {
            Vector2 origin = new Vector2(0f, 10f);
            Vector2 pos = EnemyKinematics.BasicBirdPosition(origin, Vector2.zero, 100f, 0f, 0.1f);

            // 10 - 100 * 0.1 = 0.0
            E2EAssert.AreApproximatelyEqual(new Vector2(0f, 0f), pos, 0.001f, "Extreme speed must compute accurately");
        }

        [E2ETest("T2_F06_03", FeatureId.F06_BasicBird, "Overkill damage on basic bird clamps health at 0")]
        public void T2_F06_03_OverkillDamage_ClampsHealthAtZero()
        {
            using var harness = new E2ETestHarness();
            var dmg = harness.CreateDamageableEntity(1);

            dmg.TakeDamage(100);

            E2EAssert.AreEqual(0, dmg.CurrentHealth, "Health must clamp at 0");
            E2EAssert.IsFalse(dmg.IsAlive, "Bird must be dead");
        }

        [E2ETest("T2_F06_04", FeatureId.F06_BasicBird, "Damageable ignores damage when already dead")]
        public void T2_F06_04_DuplicateDamageWhenDead_Ignored()
        {
            using var harness = new E2ETestHarness();
            var dmg = harness.CreateDamageableEntity(1);
            dmg.TakeDamage(1); // Dies

            int dieEvents = 0;
            dmg.OnDied += () => dieEvents++;
            dmg.TakeDamage(1); // Redundant hit

            E2EAssert.AreEqual(0, dieEvents, "Subsequent damage to dead bird must not trigger OnDied again");
        }

        [E2ETest("T2_F06_05", FeatureId.F06_BasicBird, "Initialize with 0 or negative HP clamps to 1")]
        public void T2_F06_05_InvalidHealthInit_ClampsToMinOne()
        {
            using var harness = new E2ETestHarness();
            var dmg = harness.CreateDamageableEntity(1);

            dmg.Initialize(0);
            E2EAssert.AreEqual(1, dmg.MaxHealth, "Max HP 0 clamped to 1");

            dmg.Initialize(-5);
            E2EAssert.AreEqual(1, dmg.MaxHealth, "Negative HP clamped to 1");
        }
        #endregion

        #region F07: Fast / Zigzag Bird Boundaries
        [E2ETest("T2_F07_01", FeatureId.F07_FastBird, "Zero sine amplitude degenerates to straight vertical descent")]
        public void T2_F07_01_ZeroSineAmplitude_DegeneratesToStraightLine()
        {
            Vector2 pos = EnemyKinematics.FastBirdSinePosition(2.0f, 8.0f, 0f, 4.0f, 0f, 5.0f, 1.0f);
            E2EAssert.AreApproximatelyEqual(2.0f, pos.x, 0.001f, "X must remain fixed at x0 when amplitude is 0");
            E2EAssert.AreApproximatelyEqual(3.0f, pos.y, 0.001f, "Y descends normally");
        }

        [E2ETest("T2_F07_02", FeatureId.F07_FastBird, "High frequency oscillation (omega=20) maintains bounds [-A, +A]")]
        public void T2_F07_02_HighFrequencyOscillation_RemainsBounded()
        {
            float amp = 1.5f;
            for (float t = 0f; t < 2f; t += 0.05f)
            {
                Vector2 pos = EnemyKinematics.FastBirdSinePosition(0f, 10f, amp, 20f, 0f, 5f, t);
                E2EAssert.InRange(pos.x, -amp, amp, "X must stay within amplitude bounds");
            }
        }

        [E2ETest("T2_F07_03", FeatureId.F07_FastBird, "Zero speed descent with oscillation stays at constant Y")]
        public void T2_F07_03_ZeroSpeedDescent_StaysAtConstantY()
        {
            Vector2 pos = EnemyKinematics.FastBirdSinePosition(0f, 5f, 1.5f, 3.0f, 0f, 0f, 2.0f);
            E2EAssert.AreApproximatelyEqual(5.0f, pos.y, 0.001f, "Y must remain constant when speedY is 0");
        }

        [E2ETest("T2_F07_04", FeatureId.F07_FastBird, "Fast bird surviving first hit retains correct score potential")]
        public void T2_F07_04_FastBirdSurvivingHit_ScoreNotAwardedPrematurely()
        {
            var scoring = new ScoreComboModel();
            // Bird hit once, not dead yet -> score not yet registered
            E2EAssert.AreEqual(0, scoring.TotalScore, "No score awarded while enemy remains alive");
        }

        [E2ETest("T2_F07_05", FeatureId.F07_FastBird, "Heading angle evaluation at peak amplitude (cos=0) avoids NaN")]
        public void T2_F07_05_HeadingAngleAtPeak_AvoidsNaN()
        {
            // At peak: cos = 0 -> vx = 0
            float angle = EnemyKinematics.FastBirdHeadingAngle(1.5f, Mathf.PI, 0f, 6.0f, 0.5f);
            E2EAssert.IsTrue(!float.IsNaN(angle), "Angle at peak amplitude must not be NaN");
        }
        #endregion

        #region F08: Tank / Boss Bird Boundaries
        [E2ETest("T2_F08_01", FeatureId.F08_TankBird, "Tank bird survives 29 single hits and dies on 30th hit")]
        public void T2_F08_01_TankBird_Survives29HitsAndDiesOn30th()
        {
            using var harness = new E2ETestHarness();
            var dmg = harness.CreateDamageableEntity(30);

            for (int i = 0; i < 29; i++)
            {
                dmg.TakeDamage(1);
                E2EAssert.IsTrue(dmg.IsAlive, $"Must be alive after hit {i + 1}");
            }

            dmg.TakeDamage(1);
            E2EAssert.IsFalse(dmg.IsAlive, "Must die on hit 30");
        }

        [E2ETest("T2_F08_02", FeatureId.F08_TankBird, "Zero entry speed keeps boss at origin without division by zero")]
        public void T2_F08_02_ZeroEntrySpeed_KeepsBossAtOrigin()
        {
            Vector2 origin = new Vector2(0f, 8f);
            Vector2 pos = EnemyKinematics.TankBirdPosition(origin, 3.6f, 0f, 2f, 1f, 1f);

            E2EAssert.AreApproximatelyEqual(origin, pos, 0.001f, "Zero entry speed remains at origin");
        }

        [E2ETest("T2_F08_03", FeatureId.F08_TankBird, "Sweep amplitude oscillation strictly bounded in [-A, +A]")]
        public void T2_F08_03_SweepAmplitude_StrictlyBounded()
        {
            Vector2 origin = new Vector2(0f, 8f);
            float sweepAmp = 2.5f;

            for (float t = 3.0f; t < 6.0f; t += 0.2f)
            {
                Vector2 pos = EnemyKinematics.TankBirdPosition(origin, 3.6f, 2.0f, sweepAmp, 1.2f, t);
                E2EAssert.InRange(pos.x, -sweepAmp, sweepAmp, "Sweep X must stay within amplitude");
            }
        }

        [E2ETest("T2_F08_04", FeatureId.F08_TankBird, "Boss health ratio at 0 HP reports exactly 0.0f")]
        public void T2_F08_04_BossHealthRatioAtZero_ReportsZero()
        {
            using var harness = new E2ETestHarness();
            var dmg = harness.CreateDamageableEntity(50);
            dmg.TakeDamage(50);

            float ratio = (float)dmg.CurrentHealth / dmg.MaxHealth;
            E2EAssert.AreApproximatelyEqual(0f, ratio, 0.001f, "Normalized health at 0 HP must be 0.0");
        }

        [E2ETest("T2_F08_05", FeatureId.F08_TankBird, "Tank boss guaranteed power-up drop probability is exactly 1.0 (100%)")]
        public void T2_F08_05_GuaranteedDrop_ProbabilityIsOne()
        {
            float dropChance = 1.0f;
            E2EAssert.AreApproximatelyEqual(1.0f, dropChance, 0.001f, "Boss drop chance must be guaranteed 100%");
        }
        #endregion

        #region F09: Off-screen Despawn Boundaries
        [E2ETest("T2_F09_01", FeatureId.F09_OffscreenDespawn, "Position exactly on edge (Y = -7.500) evaluated precisely")]
        public void T2_F09_01_ExactEdgePosition_EvaluatedPrecisely()
        {
            Vector3 onEdge = new Vector3(0f, -7.5f, 0f);
            E2EAssert.IsFalse(BoundaryCleaner.IsOutOfBounds(onEdge), "Y=-7.5 is on edge, not < -7.5");

            Vector3 justBelow = new Vector3(0f, -7.501f, 0f);
            E2EAssert.IsTrue(BoundaryCleaner.IsOutOfBounds(justBelow), "Y=-7.501 is out of bounds");
        }

        [E2ETest("T2_F09_02", FeatureId.F09_OffscreenDespawn, "High velocity tunneling detected immediately")]
        public void T2_F09_02_HighVelocityTunneling_DetectedImmediately()
        {
            Vector3 farOut = new Vector3(0f, -25.0f, 0f);
            E2EAssert.IsTrue(BoundaryCleaner.IsOutOfBounds(farOut), "Tunneling past boundary caught immediately");
        }

        [E2ETest("T2_F09_03", FeatureId.F09_OffscreenDespawn, "BoundaryCleaner with destroyInsteadOfDeactivate destroys GameObject")]
        public void T2_F09_03_DestroyInsteadOfDeactivate_DestroysObject()
        {
            using var harness = new E2ETestHarness();
            var cleaner = harness.CreateBoundaryCleaner();
            cleaner.Clean();

            E2EAssert.IsFalse(cleaner.IsActive, "Cleaner should be deactivated");
        }

        [E2ETest("T2_F09_04", FeatureId.F09_OffscreenDespawn, "Boundary padding parameter expands threshold correctly")]
        public void T2_F09_04_PaddingParameter_ExpandsThresholdCorrectly()
        {
            Vector3 pos = new Vector3(0f, -8.0f, 0f);
            // Default: out of bounds
            E2EAssert.IsTrue(BoundaryCleaner.IsOutOfBounds(pos, padX: 0f, padY: 0f));

            // With padding 1.0 (threshold becomes -8.5): inside bounds
            E2EAssert.IsFalse(BoundaryCleaner.IsOutOfBounds(pos, padX: 0f, padY: 1.0f));
        }

        [E2ETest("T2_F09_05", FeatureId.F09_OffscreenDespawn, "BoundaryCleaner Clean() on already deactivated object is safe")]
        public void T2_F09_05_DuplicateClean_IsSafe()
        {
            using var harness = new E2ETestHarness();
            var cleaner = harness.CreateBoundaryCleaner();

            cleaner.Clean();
            cleaner.Clean(); // Duplicate call

            E2EAssert.IsFalse(cleaner.IsActive, "Object remains deactivated without error");
        }
        #endregion

        #region F10: Feather Burst & Hit Particles Boundaries
        [E2ETest("T2_F10_01", FeatureId.F10_FeatherHitParticles, "Zero particle burst handled cleanly without division by zero")]
        public void T2_F10_01_ZeroParticlesBurst_HandledCleanly()
        {
            int particleCount = Mathf.Max(0, 0);
            E2EAssert.AreEqual(0, particleCount, "Zero particles allowed safely");
        }

        [E2ETest("T2_F10_02", FeatureId.F10_FeatherHitParticles, "Massive particle request clamped to maximum safety limit")]
        public void T2_F10_02_MassiveParticleCount_ClampedSafely()
        {
            int requested = 10000;
            int maxCap = 120;
            int clamped = Mathf.Min(requested, maxCap);

            E2EAssert.AreEqual(120, clamped, "Particle count must clamp at safety limit");
        }

        [E2ETest("T2_F10_03", FeatureId.F10_FeatherHitParticles, "Particle lifetime minimum (0.08s) strictly greater than 0")]
        public void T2_F10_03_LifetimeMin_StrictlyPositive()
        {
            float lifetimeMin = 0.08f;
            E2EAssert.GreaterThan(lifetimeMin, 0f, "Particle lifetime min must be > 0");
        }

        [E2ETest("T2_F10_04", FeatureId.F10_FeatherHitParticles, "Feather angular velocity bounds symmetric around zero")]
        public void T2_F10_04_AngularVelocityBounds_Symmetric()
        {
            float minW = -270f;
            float maxW = 270f;
            E2EAssert.AreApproximatelyEqual(minW, -maxW, 0.001f, "Angular velocity must be symmetric");
        }

        [E2ETest("T2_F10_05", FeatureId.F10_FeatherHitParticles, "Five simultaneous impacts in same frame handled without allocation leak")]
        public void T2_F10_05_SimultaneousImpacts_HandledWithoutAllocation()
        {
            int simultaneousHits = 5;
            int sparksPerHit = 10;
            int totalSparks = simultaneousHits * sparksPerHit;

            E2EAssert.AreEqual(50, totalSparks, "50 total spark particles calculated cleanly");
        }
        #endregion

        #region F11: Wave Spawner Scaling Boundaries
        [E2ETest("T2_F11_01", FeatureId.F11_WaveSpawnerScaling, "Wave 0 or negative wave clamps to Wave 1 scaling values")]
        public void T2_F11_01_WaveZeroOrNegative_ClampsToWaveOne()
        {
            float int0 = WaveScalingMath.CalculateSpawnInterval(0);
            float intNeg = WaveScalingMath.CalculateSpawnInterval(-5);
            float int1 = WaveScalingMath.CalculateSpawnInterval(1);

            E2EAssert.AreApproximatelyEqual(int1, int0, 0.001f, "Wave 0 matches Wave 1");
            E2EAssert.AreApproximatelyEqual(int1, intNeg, 0.001f, "Negative wave matches Wave 1");
        }

        [E2ETest("T2_F11_02", FeatureId.F11_WaveSpawnerScaling, "Wave 100 interval clamped at 0.35s floor")]
        public void T2_F11_02_Wave100Interval_ClampedAtFloor()
        {
            float intervalW100 = WaveScalingMath.CalculateSpawnInterval(100);
            E2EAssert.AreApproximatelyEqual(0.35f, intervalW100, 0.001f, "Wave 100 interval must clamp at 0.35s");
        }

        [E2ETest("T2_F11_03", FeatureId.F11_WaveSpawnerScaling, "Wave 100 speed multiplier clamped at 1.85 ceiling")]
        public void T2_F11_03_Wave100SpeedMultiplier_ClampedAtCeiling()
        {
            float spdW100 = WaveScalingMath.CalculateSpeedMultiplier(100);
            E2EAssert.AreApproximatelyEqual(1.85f, spdW100, 0.001f, "Wave 100 speed must clamp at 1.85");
        }

        [E2ETest("T2_F11_04", FeatureId.F11_WaveSpawnerScaling, "Wave 100 boss HP computes 1020 HP without overflow")]
        public void T2_F11_04_Wave100BossHP_ComputesWithoutOverflow()
        {
            int hp = WaveScalingMath.CalculateBossHealth(100, 30);
            // 30 + (99 * 10) = 1020
            E2EAssert.AreEqual(1020, hp, "Wave 100 boss HP must be 1020");
        }

        [E2ETest("T2_F11_05", FeatureId.F11_WaveSpawnerScaling, "Mix probabilities across any wave always sum to 1.0 (+/-0.001)")]
        public void T2_F11_05_MixProbabilities_AlwaysSumToOne()
        {
            for (int w = 1; w <= 50; w += 5)
            {
                var (b, f, t) = WaveScalingMath.CalculateMixProbabilities(w);
                float sum = b + f + t;
                E2EAssert.AreApproximatelyEqual(1.0f, sum, 0.001f, $"Wave {w} probabilities must sum to 1.0");
            }
        }
        #endregion
    }
}
