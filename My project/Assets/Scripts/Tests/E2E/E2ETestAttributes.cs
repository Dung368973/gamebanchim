using System;

namespace ShootEmUp.Tests.E2E
{
    public enum TestTier
    {
        Tier1_FeatureCoverage,
        Tier2_BoundaryCorner,
        Tier3_CrossFeaturePairwise,
        Tier4_RealWorldScenario
    }

    public enum FeatureId
    {
        F01_TouchDrag,
        F02_ViewportClamping,
        F03_AutoShooting,
        F04_BulletPooling,
        F05_PlayerHealth,
        F06_BasicBird,
        F07_FastBird,
        F08_TankBird,
        F09_OffscreenDespawn,
        F10_FeatherHitParticles,
        F11_WaveSpawnerScaling,
        F12_SpreadShot,
        F13_RapidFire,
        F14_HealthShield,
        F15_PowerupMagnetism,
        F16_ScoreComboMultiplier,
        F17_GameLoopFSM,
        F18_ProceduralSprites,
        F19_ParallaxBackground,
        F20_ResponsiveMobileUI,
        F21_GameOverHighScore,
        F22_AudioSystem,
        CrossFeature,
        RealWorldScenario
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class E2ETestFixtureAttribute : Attribute
    {
        public string Name { get; }
        public TestTier Tier { get; }
        public int Order { get; }

        public E2ETestFixtureAttribute(string name, TestTier tier, int order = 0)
        {
            Name = name;
            Tier = tier;
            Order = order;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class E2ETestAttribute : Attribute
    {
        public string TestId { get; }
        public FeatureId Feature { get; }
        public string Description { get; }

        public E2ETestAttribute(string testId, FeatureId feature, string description)
        {
            TestId = testId;
            Feature = feature;
            Description = description;
        }
    }
}
