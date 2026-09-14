using System;
using UnityEngine;

/// <summary>
/// Central Particle & Visual FX Manager matching PROJECT.md interface contracts.
/// Manages high-performance zero-allocation particle emitters for feather bursts and hit sparks.
/// Emits procedural particle bursts matching the gameplay and kinematics specifications.
/// </summary>
public class FXManager : MonoBehaviour
{
    public static FXManager Instance { get; private set; }

    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem featherParticleSystem;
    [SerializeField] private ParticleSystem sparkParticleSystem;

    [Header("Feather Particle Configuration")]
    [SerializeField] private float featherMinSpeed = 2.0f;
    [SerializeField] private float featherMaxSpeed = 4.0f;
    [SerializeField] private float featherGravityModifier = 0.35f;
    [SerializeField] private float featherMinLifetime = 0.8f;
    [SerializeField] private float featherMaxLifetime = 1.2f;
    [SerializeField] private float featherMinAngularVelocity = -270f;
    [SerializeField] private float featherMaxAngularVelocity = 270f;

    [Header("Spark Particle Configuration")]
    [SerializeField] private float sparkMinSpeed = 4.0f;
    [SerializeField] private float sparkMaxSpeed = 8.0f;
    [SerializeField] private float sparkLifetime = 0.25f;

    // Diagnostic event & metrics
    public int TotalBurstsSpawned { get; private set; }
    public int TotalSparksSpawned { get; private set; }

    public event Action<Vector3, Color, int> OnFeatherBurstEmitted;
    public event Action<Vector3, int> OnSparksEmitted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeParticleSystems();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Ensures particle systems are set up and pre-configured.
    /// Automatically builds procedural emitters if unassigned in inspector.
    /// </summary>
    private void InitializeParticleSystems()
    {
        if (featherParticleSystem == null)
        {
            GameObject featherObj = new GameObject("FeatherParticleSystem");
            featherObj.transform.SetParent(transform, false);
            featherParticleSystem = featherObj.AddComponent<ParticleSystem>();
            ConfigureFeatherEmitter(featherParticleSystem);
        }

        if (sparkParticleSystem == null)
        {
            GameObject sparkObj = new GameObject("SparkParticleSystem");
            sparkObj.transform.SetParent(transform, false);
            sparkParticleSystem = sparkObj.AddComponent<ParticleSystem>();
            ConfigureSparkEmitter(sparkParticleSystem);
        }
    }

    private void ConfigureFeatherEmitter(ParticleSystem ps)
    {
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startSpeed = new ParticleSystem.MinMaxCurve(featherMinSpeed, featherMaxSpeed);
        main.startLifetime = new ParticleSystem.MinMaxCurve(featherMinLifetime, featherMaxLifetime);
        main.gravityModifier = featherGravityModifier;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 500;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(featherMinAngularVelocity * Mathf.Deg2Rad, featherMaxAngularVelocity * Mathf.Deg2Rad);
    }

    private void ConfigureSparkEmitter(ParticleSystem ps)
    {
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startSpeed = new ParticleSystem.MinMaxCurve(sparkMinSpeed, sparkMaxSpeed);
        main.startLifetime = sparkLifetime;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = new Color(1f, 0.85f, 0.3f, 1f); // Golden sparks
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
    }

    /// <summary>
    /// Spawns a radial burst of fluttering feather particles upon bird death.
    /// Feather count: 20-30 for basic/fast birds, 60-80 for tank/boss birds.
    /// </summary>
    public void PlayFeatherBurst(Vector3 position, Color birdColor, int count = 25)
    {
        TotalBurstsSpawned++;
        OnFeatherBurstEmitted?.Invoke(position, birdColor, count);

        if (featherParticleSystem == null) return;

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = position,
            applyShapeToPosition = true,
            startColor = birdColor
        };

        for (int i = 0; i < count; i++)
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float speed = UnityEngine.Random.Range(featherMinSpeed, featherMaxSpeed);
            Vector3 velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);

            emitParams.velocity = velocity;
            emitParams.startLifetime = UnityEngine.Random.Range(featherMinLifetime, featherMaxLifetime);
            emitParams.startSize = UnityEngine.Random.Range(0.15f, 0.35f);
            emitParams.rotation = UnityEngine.Random.Range(0f, 360f);
            emitParams.angularVelocity = UnityEngine.Random.Range(featherMinAngularVelocity, featherMaxAngularVelocity);

            featherParticleSystem.Emit(emitParams, 1);
        }
    }

    /// <summary>
    /// Spawns high-velocity directional impact spark particles upon projectile hit.
    /// Nominal spark count: 8 to 14 sparks.
    /// </summary>
    public void PlayHitSparks(Vector3 position, int count = 10)
    {
        TotalSparksSpawned++;
        OnSparksEmitted?.Invoke(position, count);

        if (sparkParticleSystem == null) return;

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = position,
            applyShapeToPosition = true,
            startColor = new Color(1f, 0.9f, 0.4f, 1f)
        };

        for (int i = 0; i < count; i++)
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float speed = UnityEngine.Random.Range(sparkMinSpeed, sparkMaxSpeed);
            emitParams.velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);
            emitParams.startLifetime = sparkLifetime * UnityEngine.Random.Range(0.8f, 1.2f);
            sparkParticleSystem.Emit(emitParams, 1);
        }
    }

    /// <summary>
    /// Spawns player death explosion effect combining feather burst and sparks.
    /// </summary>
    public void PlayPlayerDeathExplosion(Vector3 position)
    {
        PlayHitSparks(position, 20);
        PlayFeatherBurst(position, new Color(1f, 0.3f, 0.3f, 1f), 40);
    }
}
