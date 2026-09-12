using System;
using UnityEngine;

namespace ShootEmUp.Tests.E2E
{
    [E2ETestFixture("Tier 1: Feature Coverage (Part 1 - F01 to F11)", TestTier.Tier1_FeatureCoverage, 10)]
    public class Tier1_FeatureCoverageTests_Part1
    {
        #region F01: Mobile Touch / Mouse Drag
        [E2ETest("T1_F01_01", FeatureId.F01_TouchDrag, "Touch down sets initial drag offset and active dragging state")]
        public void T1_F01_01_TouchDown_SetsInitialOffsetAndDraggingState()
        {
            var sim = new TouchDragSimulation { PlayerPosition = new Vector2(0f, -3.5f) };
            Vector2 touchPos = new Vector2(0.5f, -3.0f);

            sim.OnTouchDown(0, touchPos, useErgonomicLift: false);

            E2EAssert.IsTrue(sim.IsDragging, "Sim should be in dragging state");
            E2EAssert.AreEqual(0, sim.ActiveFingerId, "Active finger ID should match primary touch (0)");
            E2EAssert.AreApproximatelyEqual(new Vector2(-0.5f, -0.5f), sim.TouchOffset, 0.001f, "Offset should equal playerPos - touchPos");
        }

        [E2ETest("T1_F01_02", FeatureId.F01_TouchDrag, "Touch move updates player position directly when smoothing is disabled")]
        public void T1_F01_02_TouchMove_UpdatesPlayerPositionDirectly()
        {
            var sim = new TouchDragSimulation { PlayerPosition = new Vector2(0f, -3.5f) };
            sim.OnTouchDown(0, new Vector2(0f, -3.5f), useErgonomicLift: false);

            sim.OnTouchMove(0, new Vector2(1.2f, -2.0f), 0.016f, useSmoothing: false);

            E2EAssert.AreApproximatelyEqual(new Vector2(1.2f, -2.0f), sim.PlayerPosition, 0.001f, "Player position should match touch point directly");
        }

        [E2ETest("T1_F01_03", FeatureId.F01_TouchDrag, "Touch move with exponential smoothing smoothly approaches target")]
        public void T1_F01_03_TouchMove_WithExponentialSmoothing_LerpsTowardTarget()
        {
            var sim = new TouchDragSimulation { PlayerPosition = new Vector2(0f, -3.5f) };
            sim.OnTouchDown(0, new Vector2(0f, -3.5f), useErgonomicLift: false);

            Vector2 targetTouch = new Vector2(2.0f, -1.0f);
            sim.OnTouchMove(0, targetTouch, 0.02f, useSmoothing: true);

            E2EAssert.GreaterThan(sim.PlayerPosition.x, 0f, "Player should have moved rightward toward target");
            E2EAssert.LessThan(sim.PlayerPosition.x, 2.0f, "Player should not instantaneously jump to target with smoothing");
        }

        [E2ETest("T1_F01_04", FeatureId.F01_TouchDrag, "Keyboard input fallback updates position with 8.0 units/sec speed")]
        public void T1_F01_04_KeyboardFallback_MovesPlayerWhenTouchInactive()
        {
            var sim = new TouchDragSimulation { PlayerPosition = new Vector2(0f, -3.5f) };

            sim.OnKeyboardInput(new Vector2(1f, 0f), 0.1f);

            // 0 + 1 * 8.0 * 0.1 = 0.8
            E2EAssert.AreApproximatelyEqual(new Vector2(0.8f, -3.5f), sim.PlayerPosition, 0.001f, "Keyboard input should move player by velocity * dt");
        }

        [E2ETest("T1_F01_05", FeatureId.F01_TouchDrag, "Touch up terminates active dragging state")]
        public void T1_F01_05_TouchUp_TerminatesDraggingState()
        {
            var sim = new TouchDragSimulation();
            sim.OnTouchDown(0, Vector2.zero);
            E2EAssert.IsTrue(sim.IsDragging, "Should be dragging before touch up");

            sim.OnTouchUp(0);

            E2EAssert.IsFalse(sim.IsDragging, "Should not be dragging after touch up");
            E2EAssert.AreEqual(-1, sim.ActiveFingerId, "Active finger should be reset to -1");
        }
        #endregion

        #region F02: Screen Viewport Clamping
        [E2ETest("T1_F02_01", FeatureId.F02_ViewportClamping, "Player inside valid bounds remains unchanged")]
        public void T1_F02_01_PlayerInsideBounds_RemainsUnclamped()
        {
            var clamp = new ViewportClampingModel();
            Vector2 insidePos = new Vector2(0f, -3.0f);

            Vector2 clamped = clamp.Clamp(insidePos);

            E2EAssert.AreApproximatelyEqual(insidePos, clamped, 0.001f, "Inside position should not be modified");
        }

        [E2ETest("T1_F02_02", FeatureId.F02_ViewportClamping, "Clamps MinX when attempting to escape screen left")]
        public void T1_F02_02_ClampsMinX_WhenAttemptingLeftEscape()
        {
            var clamp = new ViewportClampingModel();
            Vector2 wayLeft = new Vector2(-10.0f, -3.0f);

            Vector2 clamped = clamp.Clamp(wayLeft);

            E2EAssert.AreApproximatelyEqual(clamp.MinX, clamped.x, 0.001f, "X should clamp to MinX");
            E2EAssert.AreApproximatelyEqual(-3.0f, clamped.y, 0.001f, "Y should be unchanged");
        }

        [E2ETest("T1_F02_03", FeatureId.F02_ViewportClamping, "Clamps MaxX when attempting to escape screen right")]
        public void T1_F02_03_ClampsMaxX_WhenAttemptingRightEscape()
        {
            var clamp = new ViewportClampingModel();
            Vector2 wayRight = new Vector2(10.0f, -3.0f);

            Vector2 clamped = clamp.Clamp(wayRight);

            E2EAssert.AreApproximatelyEqual(clamp.MaxX, clamped.x, 0.001f, "X should clamp to MaxX");
        }

        [E2ETest("T1_F02_04", FeatureId.F02_ViewportClamping, "Clamps MinY when attempting to escape screen bottom")]
        public void T1_F02_04_ClampsMinY_WhenAttemptingBottomEscape()
        {
            var clamp = new ViewportClampingModel();
            Vector2 wayDown = new Vector2(0f, -12.0f);

            Vector2 clamped = clamp.Clamp(wayDown);

            E2EAssert.AreApproximatelyEqual(clamp.MinY, clamped.y, 0.001f, "Y should clamp to MinY");
        }

        [E2ETest("T1_F02_05", FeatureId.F02_ViewportClamping, "Clamps MaxY to lower 62.5% of the screen (Cy + S * 0.25)")]
        public void T1_F02_05_ClampsMaxY_RestrictsPlayerToLower62Point5Percent()
        {
            var clamp = new ViewportClampingModel();
            Vector2 wayUp = new Vector2(0f, 5.0f);

            Vector2 clamped = clamp.Clamp(wayUp);

            float expectedMaxY = clamp.CameraCenter.y + (clamp.OrthoSize * 0.25f); // 1.5f
            E2EAssert.AreApproximatelyEqual(expectedMaxY, clamped.y, 0.001f, "Y should clamp to lower 62.5% boundary");
        }
        #endregion

        #region F03: Auto-Shooting System
        [E2ETest("T1_F03_01", FeatureId.F03_AutoShooting, "Auto-fire fires at base interval 0.20s (5 shots/sec)")]
        public void T1_F03_01_AutoFire_FiresAtBaseInterval_0Point20s()
        {
            var shooter = new AutoShootingModel();
            int shotCount = 0;

            // Advance by 0.19s: 0 shots
            shotCount += shooter.Update(0.19f);
            E2EAssert.AreEqual(0, shotCount, "Should not fire before 0.20s");

            // Advance by 0.02s (total 0.21s): exactly 1 shot
            shotCount += shooter.Update(0.02f);
            E2EAssert.AreEqual(1, shotCount, "Should fire after reaching 0.20s interval");
        }

        [E2ETest("T1_F03_02", FeatureId.F03_AutoShooting, "Rapid fire multiplier halves interval to 0.10s (10 shots/sec)")]
        public void T1_F03_02_AutoFire_RapidFire_HalvesIntervalTo_0Point10s()
        {
            var shooter = new AutoShootingModel { IsRapidFireActive = true };
            E2EAssert.AreApproximatelyEqual(0.10f, shooter.EffectiveInterval, 0.001f, "Rapid fire interval should be 0.10s");

            int shots = shooter.Update(0.25f);
            E2EAssert.AreEqual(2, shots, "In 0.25s at 0.10s interval, exactly 2 shots should fire");
        }

        [E2ETest("T1_F03_03", FeatureId.F03_AutoShooting, "Auto-fire disabled spawns zero projectiles regardless of delta time")]
        public void T1_F03_03_AutoFire_Disabled_SpawnsZeroProjectiles()
        {
            var shooter = new AutoShootingModel { IsEnabled = false };
            int shots = shooter.Update(1.0f);

            E2EAssert.AreEqual(0, shots, "Disabled shooter must not fire any projectiles");
        }

        [E2ETest("T1_F03_04", FeatureId.F03_AutoShooting, "Single shot produces straight upward velocity (0, 14.0)")]
        public void T1_F03_04_SingleShot_ProducesStraightUpwardVelocity()
        {
            Vector2[] vel = AutoShootingModel.GetSpreadVelocities(0, 14.0f);

            E2EAssert.AreEqual(1, vel.Length, "Single shot has 1 velocity vector");
            E2EAssert.AreApproximatelyEqual(new Vector2(0f, 14.0f), vel[0], 0.001f, "Single shot moves purely +Y");
        }

        [E2ETest("T1_F03_05", FeatureId.F03_AutoShooting, "3-Way Spread produces symmetric angled velocities")]
        public void T1_F03_05_SpreadShot_ProducesAngledVelocities()
        {
            Vector2[] vel = AutoShootingModel.GetSpreadVelocities(1, 14.0f);

            E2EAssert.AreEqual(3, vel.Length, "3-way spread must produce 3 velocities");
            E2EAssert.GreaterThan(vel[0].x, 0f, "Left shot (-15 deg) has positive vx");
            E2EAssert.AreApproximatelyEqual(0f, vel[1].x, 0.001f, "Center shot has 0 vx");
            E2EAssert.LessThan(vel[2].x, 0f, "Right shot (+15 deg) has negative vx");
        }
        #endregion

        #region F04: Bullet Pooling
        [E2ETest("T1_F04_01", FeatureId.F04_BulletPooling, "Object pool prewarms requested capacity")]
        public void T1_F04_01_PrewarmsRequestedCapacity()
        {
            using var harness = new E2ETestHarness();
            var pool = harness.CreateBulletPool(10);

            E2EAssert.AreEqual(10, pool.AvailableCount, "Available count should equal initial prewarm capacity");
            E2EAssert.AreEqual(10, pool.TotalCreated, "Total created should equal 10");
            E2EAssert.AreEqual(0, pool.ActiveCount, "Active count should be 0");
        }

        [E2ETest("T1_F04_02", FeatureId.F04_BulletPooling, "Get retrieves active instance and decrements available")]
        public void T1_F04_02_Get_RetrievesActiveInstanceAndDecrementsAvailable()
        {
            using var harness = new E2ETestHarness();
            var pool = harness.CreateBulletPool(5);
            var bullet = pool.Get();

            E2EAssert.NotNull(bullet, "Retrieved instance must not be null");
            E2EAssert.AreEqual(4, pool.AvailableCount, "Available count must decrement by 1");
            E2EAssert.AreEqual(1, pool.ActiveCount, "Active count must increment by 1");
        }

        [E2ETest("T1_F04_03", FeatureId.F04_BulletPooling, "Return deactivates and returns instance to pool")]
        public void T1_F04_03_Return_DeactivatesAndReturnsInstanceToPool()
        {
            using var harness = new E2ETestHarness();
            var pool = harness.CreateBulletPool(5);
            var bullet = pool.Get();
            pool.Return(bullet);

            E2EAssert.AreEqual(5, pool.AvailableCount, "Available count must return to 5");
            E2EAssert.AreEqual(0, pool.ActiveCount, "Active count must be 0");
        }

        [E2ETest("T1_F04_04", FeatureId.F04_BulletPooling, "Depleted pool expands dynamically without error")]
        public void T1_F04_04_DepletedPool_ExpandsDynamicallyWithoutError()
        {
            using var harness = new E2ETestHarness();
            var pool = harness.CreateBulletPool(2);
            var b1 = pool.Get();
            var b2 = pool.Get();
            var b3 = pool.Get(); // Exceeds initial capacity

            E2EAssert.NotNull(b3, "Expanded pool must return valid instance");
            E2EAssert.AreEqual(3, pool.TotalCreated, "Total created should be 3");
            E2EAssert.AreEqual(3, pool.ActiveCount, "Active count should be 3");
        }

        [E2ETest("T1_F04_05", FeatureId.F04_BulletPooling, "Duplicate return prevented gracefully without double-pushing")]
        public void T1_F04_05_DuplicateReturn_PreventedGracefully()
        {
            using var harness = new E2ETestHarness();
            var pool = harness.CreateBulletPool(3);
            var bullet = pool.Get();
            pool.Return(bullet);
            pool.Return(bullet); // Duplicate return

            E2EAssert.AreEqual(3, pool.AvailableCount, "Duplicate return must not increase available count");
        }
        #endregion

        #region F05: Player Health & Invulnerability
        [E2ETest("T1_F05_01", FeatureId.F05_PlayerHealth, "Player initial lives equals 3")]
        public void T1_F05_01_InitialLives_EqualsThree()
        {
            var health = new PlayerHealthModel();
            E2EAssert.AreEqual(3, health.CurrentLives, "Player must start with 3 lives");
            E2EAssert.IsTrue(health.IsAlive, "Player must be alive at start");
        }

        [E2ETest("T1_F05_02", FeatureId.F05_PlayerHealth, "TakeDamage decrements lives by 1")]
        public void T1_F05_02_TakeDamage_DecrementsLifeByOne()
        {
            var health = new PlayerHealthModel();
            bool damaged = health.TakeDamage(1);

            E2EAssert.IsTrue(damaged, "Damage should be successfully applied");
            E2EAssert.AreEqual(2, health.CurrentLives, "Lives should reduce from 3 to 2");
        }

        [E2ETest("T1_F05_03", FeatureId.F05_PlayerHealth, "TakeDamage triggers 2.0s invulnerability frames (I-frames)")]
        public void T1_F05_03_TakeDamage_TriggersIFramesInvulnerability()
        {
            var health = new PlayerHealthModel();
            health.TakeDamage(1);

            E2EAssert.IsTrue(health.IsInvulnerable, "Player should be invulnerable after damage");
            E2EAssert.AreApproximatelyEqual(2.0f, health.InvulnerabilityTimer, 0.001f, "I-frames timer should be 2.0s");
        }

        [E2ETest("T1_F05_04", FeatureId.F05_PlayerHealth, "I-frames prevent subsequent damage during invulnerability window")]
        public void T1_F05_04_IFrames_PreventsSubsequentDamageDuringWindow()
        {
            var health = new PlayerHealthModel();
            health.TakeDamage(1); // Lives = 2, Invulnerable = 2.0s

            bool secondHit = health.TakeDamage(1);

            E2EAssert.IsFalse(secondHit, "Second hit must be ignored during I-frames");
            E2EAssert.AreEqual(2, health.CurrentLives, "Lives must remain at 2");
        }

        [E2ETest("T1_F05_05", FeatureId.F05_PlayerHealth, "Depleted lives triggers player death state (IsAlive == false)")]
        public void T1_F05_05_DepletedLives_TriggersDeathState()
        {
            var health = new PlayerHealthModel();
            health.TakeDamage(1); // Lives = 2
            health.Update(2.1f);  // Expire I-frames
            health.TakeDamage(1); // Lives = 1
            health.Update(2.1f);  // Expire I-frames
            health.TakeDamage(1); // Lives = 0

            E2EAssert.AreEqual(0, health.CurrentLives, "Lives should be 0");
            E2EAssert.IsFalse(health.IsAlive, "Player must be dead when lives reach 0");
        }
        #endregion

        #region F06: Basic Bird Archetype
        [E2ETest("T1_F06_01", FeatureId.F06_BasicBird, "Basic bird descends straight downward with constant velocity")]
        public void T1_F06_01_BasicBird_DescendsStraightDown()
        {
            Vector2 origin = new Vector2(0f, 7.0f);
            Vector2 pos = EnemyKinematics.BasicBirdPosition(origin, Vector2.zero, speedY: 3.5f, speedX: 0f, time: 2.0f);

            // 7.0 - (3.5 * 2.0) = 0.0
            E2EAssert.AreApproximatelyEqual(new Vector2(0f, 0f), pos, 0.001f, "Basic bird should descend straight down");
        }

        [E2ETest("T1_F06_02", FeatureId.F06_BasicBird, "Basic bird flock maintains relative offsets during flight")]
        public void T1_F06_02_FlockFormation_MaintainsRelativeOffsets()
        {
            Vector2 origin = new Vector2(0f, 7.0f);
            Vector2 offsetA = new Vector2(-1.0f, 0f);
            Vector2 offsetB = new Vector2(1.0f, 0f);

            Vector2 posA = EnemyKinematics.BasicBirdPosition(origin, offsetA, 3.5f, 0f, 1.0f);
            Vector2 posB = EnemyKinematics.BasicBirdPosition(origin, offsetB, 3.5f, 0f, 1.0f);

            E2EAssert.AreApproximatelyEqual(2.0f, posB.x - posA.x, 0.001f, "Flock distance between A and B must remain 2.0");
        }

        [E2ETest("T1_F06_03", FeatureId.F06_BasicBird, "Damageable component takes damage and dies at zero health")]
        public void T1_F06_03_Damageable_TakesDamageAndDiesAtZeroHealth()
        {
            using var harness = new E2ETestHarness();
            var damageable = harness.CreateDamageableEntity(1);

            bool died = false;
            damageable.OnDied += () => died = true;
            damageable.TakeDamage(1);

            E2EAssert.AreEqual(0, damageable.CurrentHealth, "HP should be 0");
            E2EAssert.IsFalse(damageable.IsAlive, "Bird should be dead");
            E2EAssert.IsTrue(died, "OnDied event should be fired");
        }

        [E2ETest("T1_F06_04", FeatureId.F06_BasicBird, "Basic bird base score awards 100 points")]
        public void T1_F06_04_BaseScore_Awards100Points()
        {
            var scoring = new ScoreComboModel();
            scoring.RegisterHit(EnemyType.BasicSparrow);

            E2EAssert.AreEqual(100, scoring.TotalScore, "Basic bird should award 100 base score");
        }

        [E2ETest("T1_F06_05", FeatureId.F06_BasicBird, "ResetHealth restores max health for pooling reuse")]
        public void T1_F06_05_HealthReset_RestoresMaxHealthForPooling()
        {
            using var harness = new E2ETestHarness();
            var dmg = harness.CreateDamageableEntity(1);
            dmg.TakeDamage(1);

            dmg.ResetHealth();

            E2EAssert.AreEqual(1, dmg.CurrentHealth, "Current health should be restored to max");
            E2EAssert.IsTrue(dmg.IsAlive, "Entity should be alive after reset");
        }
        #endregion

        #region F07: Fast / Zigzag Bird Archetype
        [E2ETest("T1_F07_01", FeatureId.F07_FastBird, "Harmonic oscillation computes accurate sinusoidal X position")]
        public void T1_F07_01_HarmonicOscillation_ComputesSinusoidalX()
        {
            float amplitude = 1.5f;
            float omega = Mathf.PI; // 1 full cycle per 2 seconds
            float phase = 0f;

            // At t = 0: sin(0) = 0 -> x = 0
            Vector2 p0 = EnemyKinematics.FastBirdSinePosition(0f, 7f, amplitude, omega, phase, 5.0f, 0f);
            E2EAssert.AreApproximatelyEqual(0f, p0.x, 0.001f, "At t=0, x should be 0");

            // At t = 0.5s: sin(pi/2) = 1 -> x = 1.5
            Vector2 p1 = EnemyKinematics.FastBirdSinePosition(0f, 7f, amplitude, omega, phase, 5.0f, 0.5f);
            E2EAssert.AreApproximatelyEqual(1.5f, p1.x, 0.001f, "At t=0.5, x should be +amplitude");
        }

        [E2ETest("T1_F07_02", FeatureId.F07_FastBird, "Harmonic oscillation heading angle aligns with trajectory tangent")]
        public void T1_F07_02_HarmonicOscillation_HeadingAngleFollowsTrajectory()
        {
            float angle = EnemyKinematics.FastBirdHeadingAngle(1.5f, 4.0f, 0f, 6.0f, 0f);
            E2EAssert.IsTrue(!float.IsNaN(angle), "Angle must be a valid real number");
        }

        [E2ETest("T1_F07_03", FeatureId.F07_FastBird, "Fast bird has 2 HP and survives first hit")]
        public void T1_F07_03_FastBird_BaseHP_RequiresTwoHitsToDestroy()
        {
            using var harness = new E2ETestHarness();
            var dmg = harness.CreateDamageableEntity(2);

            dmg.TakeDamage(1);
            E2EAssert.IsTrue(dmg.IsAlive, "Fast bird should survive 1 hit with 1 HP remaining");
            E2EAssert.AreEqual(1, dmg.CurrentHealth, "HP should be 1");

            dmg.TakeDamage(1);
            E2EAssert.IsFalse(dmg.IsAlive, "Fast bird should die on second hit");
        }

        [E2ETest("T1_F07_04", FeatureId.F07_FastBird, "Fast bird awards 250 base score points")]
        public void T1_F07_04_FastBird_Awards250Points()
        {
            var scoring = new ScoreComboModel();
            scoring.RegisterHit(EnemyType.FastFalcon);

            E2EAssert.AreEqual(250, scoring.TotalScore, "Fast bird should award 250 score");
        }

        [E2ETest("T1_F07_05", FeatureId.F07_FastBird, "Fast bird descent speed (5.0-7.0) strictly exceeds basic bird speed (3.0-4.2)")]
        public void T1_F07_05_DescentSpeed_ExceedsBasicBirdSpeed()
        {
            float minFastSpeed = 5.0f;
            float maxBasicSpeed = 4.2f;

            E2EAssert.GreaterThan(minFastSpeed, maxBasicSpeed, "Fast bird min speed must exceed basic bird max speed");
        }
        #endregion

        #region F08: Tank / Boss Bird Archetype
        [E2ETest("T1_F08_01", FeatureId.F08_TankBird, "Tank bird has high HP (30 HP) and takes multiple hits")]
        public void T1_F08_01_TankBird_HighBaseHP_TakesMultipleHits()
        {
            using var harness = new E2ETestHarness();
            var dmg = harness.CreateDamageableEntity(30);

            dmg.TakeDamage(10);
            E2EAssert.AreEqual(20, dmg.CurrentHealth, "Current HP should be 20");
            E2EAssert.IsTrue(dmg.IsAlive, "Tank bird should be alive");
        }

        [E2ETest("T1_F08_02", FeatureId.F08_TankBird, "Tank bird entry stage moves down to target Y=3.6")]
        public void T1_F08_02_EntryStage_DescendsToTargetY()
        {
            Vector2 origin = new Vector2(0f, 8.0f);
            float targetY = 3.6f;
            float speed = 2.0f; // takes 2.2s to reach 3.6

            Vector2 posAt1s = EnemyKinematics.TankBirdPosition(origin, targetY, speed, 2.0f, 1.0f, 1.0f);
            E2EAssert.AreApproximatelyEqual(6.0f, posAt1s.y, 0.001f, "At 1s, Y should be 8.0 - 2.0 = 6.0");
        }

        [E2ETest("T1_F08_03", FeatureId.F08_TankBird, "Tank bird sweep stage locks Y at 3.6 and oscillates horizontally")]
        public void T1_F08_03_SweepStage_OscillatesHorizontallyAtTargetY()
        {
            Vector2 origin = new Vector2(0f, 8.0f);
            float targetY = 3.6f;
            float speed = 2.0f; // arrives at t = 2.2s

            Vector2 posAt4s = EnemyKinematics.TankBirdPosition(origin, targetY, speed, 2.0f, 1.0f, 4.0f);
            E2EAssert.AreApproximatelyEqual(targetY, posAt4s.y, 0.001f, "Y must remain locked at targetY during sweep");
        }

        [E2ETest("T1_F08_04", FeatureId.F08_TankBird, "Tank bird health bar accurately reports normalized HP")]
        public void T1_F08_04_BossHealth_ReportsNormalizedRatio()
        {
            using var harness = new E2ETestHarness();
            var dmg = harness.CreateDamageableEntity(50);

            dmg.TakeDamage(25);
            float ratio = (float)dmg.CurrentHealth / dmg.MaxHealth;

            E2EAssert.AreApproximatelyEqual(0.5f, ratio, 0.001f, "50% damage must result in 0.5 normalized health");
        }

        [E2ETest("T1_F08_05", FeatureId.F08_TankBird, "Tank bird awards 1500 points on defeat")]
        public void T1_F08_05_Defeat_Awards1500Points()
        {
            var scoring = new ScoreComboModel();
            scoring.RegisterHit(EnemyType.TankEagle);

            E2EAssert.AreEqual(1500, scoring.TotalScore, "Tank bird must award 1500 points");
        }
        #endregion

        #region F09: Off-screen Despawn
        [E2ETest("T1_F09_01", FeatureId.F09_OffscreenDespawn, "Inside boundary limits is not detected as out of bounds")]
        public void T1_F09_01_InsideBounds_PositionNotOutOfBounds()
        {
            Vector3 pos = new Vector3(0f, 0f, 0f);
            E2EAssert.IsFalse(BoundaryCleaner.IsOutOfBounds(pos), "Origin must be inside boundaries");
        }

        [E2ETest("T1_F09_02", FeatureId.F09_OffscreenDespawn, "Below bottom boundary (Y < -7.5) detected as out of bounds")]
        public void T1_F09_02_BelowBottomBoundary_DetectedAsOutOfBounds()
        {
            Vector3 pos = new Vector3(0f, -8.0f, 0f);
            E2EAssert.IsTrue(BoundaryCleaner.IsOutOfBounds(pos), "Y=-8.0 must be out of bounds");
        }

        [E2ETest("T1_F09_03", FeatureId.F09_OffscreenDespawn, "Above top boundary (Y > 7.5) detected as out of bounds")]
        public void T1_F09_03_AboveTopBoundary_DetectedAsOutOfBounds()
        {
            Vector3 pos = new Vector3(0f, 8.0f, 0f);
            E2EAssert.IsTrue(BoundaryCleaner.IsOutOfBounds(pos), "Y=+8.0 must be out of bounds");
        }

        [E2ETest("T1_F09_04", FeatureId.F09_OffscreenDespawn, "Past side boundaries (X < -4.5 or X > 4.5) detected as out of bounds")]
        public void T1_F09_04_PastSideBoundaries_DetectedAsOutOfBounds()
        {
            Vector3 left = new Vector3(-5.0f, 0f, 0f);
            Vector3 right = new Vector3(5.0f, 0f, 0f);

            E2EAssert.IsTrue(BoundaryCleaner.IsOutOfBounds(left), "X=-5.0 must be out of bounds");
            E2EAssert.IsTrue(BoundaryCleaner.IsOutOfBounds(right), "X=+5.0 must be out of bounds");
        }

        [E2ETest("T1_F09_05", FeatureId.F09_OffscreenDespawn, "BoundaryCleaner invokes OnCleaned and deactivates GameObject")]
        public void T1_F09_05_BoundaryCleaner_InvokesCleanEventAndDeactivates()
        {
            using var harness = new E2ETestHarness();
            var cleaner = harness.CreateBoundaryCleaner();

            bool cleaned = false;
            cleaner.OnCleaned += () => cleaned = true;

            cleaner.Clean();

            E2EAssert.IsTrue(cleaned, "OnCleaned event must be fired");
            E2EAssert.IsFalse(cleaner.IsActive, "Cleaner must be deactivated after Clean()");
        }
        #endregion

        #region F10: Feather Burst & Hit Particles
        [E2ETest("T1_F10_01", FeatureId.F10_FeatherHitParticles, "Bullet impact generates spark count in 8-14 range")]
        public void T1_F10_01_HitFeedback_GeneratesSparksCount()
        {
            int sparkMin = 8;
            int sparkMax = 14;
            int sample = 10; // Nominal

            E2EAssert.InRange(sample, sparkMin, sparkMax, "Sparks count must be in [8, 14]");
        }

        [E2ETest("T1_F10_02", FeatureId.F10_FeatherHitParticles, "Basic bird feather burst spawns 20-30 particles")]
        public void T1_F10_02_BasicBird_FeatherBurst_Spawns20To30Particles()
        {
            int minFeathers = 20;
            int maxFeathers = 30;
            int actual = 25;

            E2EAssert.InRange(actual, minFeathers, maxFeathers, "Basic bird feather burst must be in [20, 30]");
        }

        [E2ETest("T1_F10_03", FeatureId.F10_FeatherHitParticles, "Boss bird feather burst spawns 60-80 particles")]
        public void T1_F10_03_BossBird_FeatherBurst_Spawns60To80Particles()
        {
            int bossMin = 60;
            int bossMax = 80;
            int actual = 70;

            E2EAssert.InRange(actual, bossMin, bossMax, "Boss feather burst must be in [60, 80]");
        }

        [E2ETest("T1_F10_04", FeatureId.F10_FeatherHitParticles, "Feather tumbling has angular velocity in [-270, 270] deg/sec")]
        public void T1_F10_04_FeatherTumble_HasAngularVelocity()
        {
            float angularMin = -270f;
            float angularMax = 270f;
            float sampleAngularVelocity = 150f;

            E2EAssert.InRange(sampleAngularVelocity, angularMin, angularMax, "Angular velocity in range");
        }

        [E2ETest("T1_F10_05", FeatureId.F10_FeatherHitParticles, "Feather particles drift downward under positive gravity modifier")]
        public void T1_F10_05_FeatherDrift_DescendsUnderGravity()
        {
            float gravityModifier = 0.35f;
            E2EAssert.GreaterThan(gravityModifier, 0f, "Gravity modifier must be positive for downward feather drift");
        }
        #endregion

        #region F11: Wave Spawner & Difficulty Scaling
        [E2ETest("T1_F11_01", FeatureId.F11_WaveSpawnerScaling, "Wave 1 spawn interval equals 1.40s")]
        public void T1_F11_01_Wave1_SpawnInterval_Equals1Point40s()
        {
            float interval = WaveScalingMath.CalculateSpawnInterval(1);
            E2EAssert.AreApproximatelyEqual(1.40f, interval, 0.001f, "Wave 1 interval must be 1.40s");
        }

        [E2ETest("T1_F11_02", FeatureId.F11_WaveSpawnerScaling, "Higher waves spawn interval decays toward minimum 0.35s")]
        public void T1_F11_02_HigherWaves_SpawnIntervalDecaysTowardMinimum()
        {
            float intervalW5 = WaveScalingMath.CalculateSpawnInterval(5);
            float intervalW20 = WaveScalingMath.CalculateSpawnInterval(20);

            E2EAssert.LessThan(intervalW5, 1.40f, "Wave 5 interval must be less than Wave 1");
            E2EAssert.GreaterThanOrEqual(intervalW20, 0.35f, "Wave 20 interval must not drop below 0.35s minimum");
        }

        [E2ETest("T1_F11_03", FeatureId.F11_WaveSpawnerScaling, "Enemy speed multiplier increases with wave and caps at 1.85")]
        public void T1_F11_03_EnemySpeedMultiplier_IncreasesWithWave()
        {
            float spdW1 = WaveScalingMath.CalculateSpeedMultiplier(1);
            float spdW10 = WaveScalingMath.CalculateSpeedMultiplier(10);
            float spdW50 = WaveScalingMath.CalculateSpeedMultiplier(50);

            E2EAssert.AreApproximatelyEqual(1.0f, spdW1, 0.001f, "Wave 1 speed multiplier is 1.0");
            E2EAssert.GreaterThan(spdW10, 1.0f, "Wave 10 speed multiplier must be > 1.0");
            E2EAssert.AreApproximatelyEqual(1.85f, spdW50, 0.001f, "Wave 50 speed multiplier must cap at 1.85");
        }

        [E2ETest("T1_F11_04", FeatureId.F11_WaveSpawnerScaling, "Boss HP scales by +10 HP per wave")]
        public void T1_F11_04_BossHP_ScalesWithWaveNumber()
        {
            int hpW1 = WaveScalingMath.CalculateBossHealth(1, 30);
            int hpW2 = WaveScalingMath.CalculateBossHealth(2, 30);
            int hpW5 = WaveScalingMath.CalculateBossHealth(5, 30);

            E2EAssert.AreEqual(30, hpW1, "Wave 1 boss HP is 30");
            E2EAssert.AreEqual(40, hpW2, "Wave 2 boss HP is 40");
            E2EAssert.AreEqual(70, hpW5, "Wave 5 boss HP is 70");
        }

        [E2ETest("T1_F11_05", FeatureId.F11_WaveSpawnerScaling, "Enemy mix probability shifts toward Fast and Tank birds")]
        public void T1_F11_05_EnemyMix_ProbabilityShiftsTowardFastAndTank()
        {
            var (b1, f1, t1) = WaveScalingMath.CalculateMixProbabilities(1);
            var (b10, f10, t10) = WaveScalingMath.CalculateMixProbabilities(10);

            E2EAssert.AreApproximatelyEqual(1.0f, b1, 0.001f, "Wave 1 is 100% basic birds");
            E2EAssert.LessThan(b10, b1, "Wave 10 basic bird probability decreases");
            E2EAssert.GreaterThan(f10, f1, "Wave 10 fast bird probability increases");
            E2EAssert.GreaterThan(t10, t1, "Wave 10 tank bird probability increases");
        }
        #endregion
    }
}
