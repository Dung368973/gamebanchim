using UnityEngine;

/// <summary>
/// Fast / Zigzag Bird Archetype (Falcon).
/// Executes lateral harmonic sine-wave oscillation while diving downward at high velocity.
/// Trajectory: x(t) = x0 + A * sin(omega * t + phase), y(t) = y(t - dt) - speed * dt.
/// Default stats: Speed 5.0 u/s, 1 HP, 200 points.
/// </summary>
public class FastBird : EnemyBase
{
    public const float DefaultSpeed = 5.0f;
    public const int DefaultHealth = 1;
    public const int DefaultScore = 200;
    public const float DefaultAmplitude = 0.8f;
    public const float DefaultOmega = 4.0f;

    [Header("Oscillation Parameters")]
    [SerializeField] private float amplitude = DefaultAmplitude;
    [SerializeField] private float omega = DefaultOmega;
    [SerializeField] private float phase = 0f;
    [SerializeField] private bool alignRotationWithTangent = true;

    private float _initialX = 0f;
    private float _elapsedTime = 0f;

    public float Amplitude
    {
        get => amplitude;
        set => amplitude = value;
    }

    public float Omega
    {
        get => omega;
        set => omega = value;
    }

    public float Phase
    {
        get => phase;
        set => phase = value;
    }

    public float ElapsedTime => _elapsedTime;
    public float InitialX => _initialX;

    protected override void Awake()
    {
        speed = DefaultSpeed;
        maxHealth = DefaultHealth;
        currentHealth = DefaultHealth;
        scoreValue = DefaultScore;
        enemyType = EnemyType.FastFalcon;
        isBoss = false;
        birdColor = new Color(0.2f, 0.75f, 1f, 1f); // Electric cyan

        base.Awake();

        _initialX = transform.position.x;
        _elapsedTime = 0f;
    }

    /// <summary>
    /// Initializes oscillation parameters and flight position.
    /// </summary>
    public void InitializeTrajectory(Vector2 spawnPosition, float amp = DefaultAmplitude, float freq = DefaultOmega, float phi = 0f, float spd = DefaultSpeed)
    {
        transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        _initialX = spawnPosition.x;
        _elapsedTime = 0f;
        amplitude = amp;
        omega = freq;
        phase = phi;
        speed = spd;

        Initialize(DefaultHealth, spd);
    }

    /// <summary>
    /// Harmonic sine-wave kinematics:
    /// x(t) = x0 + A * sin(omega * t + phase)
    /// y(t) moves downward with constant speed
    /// </summary>
    protected override void Move(float dt)
    {
        _elapsedTime += dt;

        float targetX = _initialX + (amplitude * Mathf.Sin((omega * _elapsedTime) + phase));
        float targetY = transform.position.y - (speed * dt);

        transform.position = new Vector3(targetX, targetY, transform.position.z);

        if (alignRotationWithTangent)
        {
            UpdateHeadingRotation();
        }
    }

    /// <summary>
    /// Rotates the bird sprite so its beak points in the direction of instantaneous velocity.
    /// </summary>
    private void UpdateHeadingRotation()
    {
        float vx = amplitude * omega * Mathf.Cos((omega * _elapsedTime) + phase);
        float vy = -speed;
        float angleDeg = (Mathf.Atan2(vy, vx) * Mathf.Rad2Deg) + 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
    }

    public override void OnSpawnFromPool()
    {
        base.OnSpawnFromPool();
        _initialX = transform.position.x;
        _elapsedTime = 0f;
        transform.rotation = Quaternion.identity;
    }

    public override void OnReturnToPool()
    {
        base.OnReturnToPool();
        transform.rotation = Quaternion.identity;
    }
}
