using UnityEngine;

/// <summary>
/// Basic Bird Archetype (Sparrow).
/// Moves in straight descending flock formations with uniform downward velocity.
/// Default stats: Speed 3.0 u/s, 1 HP, 100 points.
/// </summary>
public class BasicBird : EnemyBase
{
    public const float DefaultSpeed = 3.0f;
    public const int DefaultHealth = 1;
    public const int DefaultScore = 100;

    [Header("Flock Kinematics")]
    [SerializeField] private float horizontalDrift = 0f;

    public float HorizontalDrift
    {
        get => horizontalDrift;
        set => horizontalDrift = value;
    }

    protected override void Awake()
    {
        speed = DefaultSpeed;
        maxHealth = DefaultHealth;
        currentHealth = DefaultHealth;
        scoreValue = DefaultScore;
        enemyType = EnemyType.BasicSparrow;
        isBoss = false;
        birdColor = new Color(0.4f, 0.8f, 0.4f, 1f); // Vibrant light green

        base.Awake();
    }

    /// <summary>
    /// Configures flock origin, relative offset, speed, and horizontal drift velocity.
    /// </summary>
    public void InitializeFlock(Vector2 origin, Vector2 flockOffset, float spd = DefaultSpeed, float driftX = 0f)
    {
        transform.position = new Vector3(origin.x + flockOffset.x, origin.y + flockOffset.y, 0f);
        speed = spd;
        horizontalDrift = driftX;
        Initialize(DefaultHealth, spd);
    }

    /// <summary>
    /// Linear movement: moves downward at constant speed with optional horizontal drift.
    /// </summary>
    protected override void Move(float dt)
    {
        transform.position += new Vector3(horizontalDrift, -speed, 0f) * dt;
    }

    public override void OnSpawnFromPool()
    {
        base.OnSpawnFromPool();
        horizontalDrift = 0f;
    }
}
