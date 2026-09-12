# Project: 2D Mobile Shoot'em Up Bird Shooting Game ("Game Bắn Chim")

## Architecture
- **Engine**: Unity 6000.5.9f1 with Universal Render Pipeline (URP) v17.5.0.
- **Platform**: Mobile (Android/iOS) + Windows Standalone Playtesting.
- **Input System**: `UnityEngine.InputSystem` (New Input System exclusively, `activeInputHandler: 1`).
- **Aspect Ratio & Camera**: Portrait 9:16 (1080x1920), Orthographic Camera (Size 6.0).
- **Core Design Pattern**: Event-driven decoupled architecture (`GameEvents.cs`), zero-allocation Object Pooling (`ObjectPool.cs`), state-machine game loop (`GameManager.cs`).
- **Rendering & Assets**: 100% self-contained procedural sprite generator creating 14 crisp 2D sprites, multi-layer parallax scrolling, URP unlit particle systems (feather burst & hit sparks).
- **Audio**: Procedural synthesis engine generating laser shots, bird hits, feather bursts, explosions, power-up chimes, and 16-bar synthwave BGM.

## Code Layout
All gameplay scripts compile directly into `Assembly-CSharp.dll` under `Assets/Scripts/`:
- `Assets/Scripts/Core/`: `GameManager.cs`, `GameEvents.cs`, `ObjectPool.cs`, `AudioManager.cs`, `ProceduralAudio.cs`.
- `Assets/Scripts/Player/`: `PlayerController.cs`, `PlayerShooter.cs`, `PlayerHealth.cs`.
- `Assets/Scripts/Enemies/`: `EnemyBase.cs`, `BasicBird.cs`, `FastBird.cs`, `TankBird.cs`, `WaveSpawner.cs`.
- `Assets/Scripts/Combat/`: `Bullet.cs`, `PowerUp.cs`, `Damageable.cs`.
- `Assets/Scripts/Environment/`: `ParallaxBackground.cs`, `BoundaryCleaner.cs`, `FXManager.cs`.
- `Assets/Scripts/UI/`: `HUDController.cs`, `GameOverUI.cs`, `SafeAreaHandler.cs`.
- `Assets/Editor/`: `ProceduralSpriteGenerator.cs`, `SceneSetupHelper.cs`.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Mobile Touch/Mouse Drag | Unified pointer drag with ergonomic touch offset and WASD/Arrow fallback via New Input System | M2 | R1 |
| 2 | Screen Viewport Clamping | Strict orthographic camera bounds clamping to keep player visible and in lower 65% of screen | M2 | R1 |
| 3 | Auto-Shooting System | Continuous configurable rate auto-fire with accumulator timer and spawn points | M2 | R1 |
| 4 | Bullet Pooling | Zero-allocation prewarmed `ObjectPool<Bullet>` for high fire-rate projectile reuse | M2 | R1 |
| 5 | Player Health & Invulnerability | 3-life system, damage I-frames with visual flashing, death event trigger | M2 | R1, R3 |
| 6 | Basic Bird Archetype | Straight descending flock formation with uniform velocity and collision bounds | M3 | R2 |
| 7 | Fast / Zigzag Bird Archetype | Harmonic sine-wave oscillation $x(t) = x_0 + A\sin(\omega t + \phi)$ with fast dive toward player | M3 | R2 |
| 8 | Tank / Boss Bird Archetype | High HP, visual floating world-space HP bar, horizontal sweeping, guaranteed power-up drop | M3 | R2 |
| 9 | Off-screen Despawn | Automatic boundary cleaner despawning birds and bullets past screen edges ($Y < -7.0$, $Y > 7.0$) | M1, M3 | R1, R2 |
| 10 | Feather Burst & Hit Particles | Shuriken particle system with 20-30 feather tumbling particles and directional hit sparks | M1, M3 | R2 |
| 11 | Wave Spawner & Difficulty Scaling | Staged wave spawner with interval decay $I(W)$, speed scaling $V(W)$, and dynamic enemy mix | M3 | R2 |
| 12 | Power-up: Spread Shot | 3-way or 5-way projectile spread pattern with angled trajectory | M4 | R3 |
| 13 | Power-up: Rapid Fire | 2x fire rate multiplier with duration timer | M4 | R3 |
| 14 | Power-up: Health / Shield | Life recovery or 1-hit invulnerable energy shield bubble | M4 | R3 |
| 15 | Power-up Magnetism & Motion | Downward sinusoidal float with proximity player magnetism ($r < 1.6\text{ u}$) | M4 | R3 |
| 16 | Score & Combo Multiplier | Points per bird type, stepped combo multiplier ($1.0\times$ to $3.0\times$), 3.0s decay timer | M4 | R3 |
| 17 | Game Loop State Machine | Full FSM: Boot -> MainMenu -> Playing -> WaveTransition -> GameOver -> Restart | M4 | R3 |
| 18 | Procedural 2D Sprites | 14 procedural high-quality textures generated directly into `Assets/Sprites/` | M1 | R4 |
| 19 | Parallax Scrolling Background | Multi-layer vertical background with seamless leapfrog wrapping | M4 | R4 |
| 20 | Responsive Mobile UI Canvas | Portrait 9:16 Canvas Scaler ($1080\times 1920$), Health bar/icons, Score, Wave, Combo indicator | M4 | R4 |
| 21 | Game Over & High Score Panel | Modal popup displaying final score, high score (PlayerPrefs), and instant Restart button | M4 | R4 |
| 22 | Audio System (SFX & Synth BGM) | Procedural audio generator synthesizing 8 SFX events and a 16-bar synthwave BGM loop | M1 | R4 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Core Foundation & Asset Synthesis | Procedural sprite generator (14 sprites), Audio synthesis engine (8 SFX + BGM), ObjectPool, GameEvents, BoundaryCleaner | None | DONE (Commit 736223b) |
| M2 | Player Controller & Auto-Shooting (R1) | Mobile/mouse drag controller, viewport clamping, auto-fire shooter, Bullet & BulletPool, PlayerHealth & I-frames | M1 | DONE (Commit fd8dc0b) |
| M3 | Bird Enemies, Waves & Particle FX (R2) | Basic bird, Fast/Zigzag bird, Tank bird with HP bar, WaveSpawner with scaling, Feather burst & Hit sparks | M1, M2 | DONE (Commit 66b2a7a) |
| M4 | Power-ups, Scoring, Parallax & Mobile UI (R3, R4) | 4 Power-up types, Score & Combo system, ParallaxBackground, HUDController, GameOverUI, SafeAreaHandler, GameManager loop | M1, M2, M3 | DONE (Commit 2ceb91d) |
| M5 | Final Scene Integration & E2E Acceptance | Full scene assembly, end-to-end game loop verification, 100% test pass, coverage hardening | M1, M2, M3, M4, E2E-Track | DONE |

## Interface Contracts

### `GameEvents.cs`
```csharp
public static class GameEvents
{
    public static Action<int, int> OnPlayerHealthChanged; // (current, max)
    public static Action OnPlayerDied;
    public static Action<int> OnScoreChanged; // (currentScore)
    public static Action<int, float> OnComboChanged; // (comboCount, multiplier)
    public static Action<int> OnWaveStarted; // (waveNumber)
    public static Action<PowerUpType, float> OnPowerUpCollected; // (type, duration)
    public static Action<int, int> OnGameOver; // (finalScore, highScore)
    public static Action OnGameRestart;
}
```

### `Damageable.cs` & `IDamageable.cs`
```csharp
public interface IDamageable
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    void TakeDamage(int amount);
    void Die();
}
```

### `ObjectPool.cs`
```csharp
public class ObjectPool<T> where T : Component
{
    public ObjectPool(T prefab, int initialCapacity, Transform parent = null);
    public T Get();
    public void Return(T obj);
}
```

### `AudioManager.cs`
```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; }
    public void PlaySFX(SFXType type, float volume = 1f, float pitchJitter = 0.05f);
    public void PlayBGM();
    public void StopBGM();
}
```
