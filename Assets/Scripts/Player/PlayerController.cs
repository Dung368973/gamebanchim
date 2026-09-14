using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Controls player ship motion with unified touch dragging, mouse dragging, and keyboard fallback.
/// Uses the New Input System exclusively.
/// Implements ergonomic vertical finger offset, exponential smoothing, and strict orthographic viewport bounds clamping.
/// </summary>
public class PlayerController : MonoBehaviour
{
    public const float DefaultPlayerSpeed = 8.0f;
    public const float DefaultSmoothingLambda = 25.0f;
    public const float DefaultErgonomicLift = 0.6f;
    public const float DefaultOrthoSize = 6.0f;
    public const float DefaultAspectRatio = 9.0f / 16.0f; // 0.5625

    [Header("Camera & Frustum")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 playerExtents = new Vector2(0.4f, 0.4f);
    [SerializeField] private Vector2 padding = new Vector2(0.1f, 0.1f);
    [SerializeField] private float bottomBarPadding = 0.3f;
    [SerializeField] private float maxYFactor = 0.25f; // Clamps to lower 62.5% of viewport

    [Header("Movement & Ergonomics")]
    [SerializeField] private float playerSpeed = DefaultPlayerSpeed;
    [SerializeField] private float smoothingLambda = DefaultSmoothingLambda;
    [SerializeField] private bool useSmoothing = true;
    [SerializeField] private bool useErgonomicLift = true;
    [SerializeField] private float ergonomicLift = DefaultErgonomicLift;

    [Header("Default Spawn")]
    [SerializeField] private Vector2 defaultSpawnPosition = new Vector2(0f, -3.5f);

    private PlayerHealth _playerHealth;
    private bool _isDragging = false;
    private int _activeFingerId = -1;
    private Vector2 _touchOffset = Vector2.zero;
    private Vector2 _targetPosition;

    public bool IsDragging => _isDragging;
    public int ActiveFingerId => _activeFingerId;
    public Vector2 TouchOffset
    {
        get => _touchOffset;
        set => _touchOffset = value;
    }

    public float PlayerSpeed
    {
        get => playerSpeed;
        set => playerSpeed = Mathf.Max(0.1f, value);
    }

    public float SmoothingLambda
    {
        get => smoothingLambda;
        set => smoothingLambda = Mathf.Max(0.1f, value);
    }

    public bool UseSmoothing
    {
        get => useSmoothing;
        set => useSmoothing = value;
    }

    public bool UseErgonomicLift
    {
        get => useErgonomicLift;
        set => useErgonomicLift = value;
    }

    public float ErgonomicLift
    {
        get => ergonomicLift;
        set => ergonomicLift = value;
    }

    public Camera ActiveCamera => targetCamera != null ? targetCamera : Camera.main;

    public float OrthoSize => ActiveCamera != null && ActiveCamera.orthographic ? ActiveCamera.orthographicSize : DefaultOrthoSize;
    public float AspectRatio => ActiveCamera != null ? ActiveCamera.aspect : DefaultAspectRatio;
    public Vector2 CameraCenter => ActiveCamera != null ? (Vector2)ActiveCamera.transform.position : Vector2.zero;

    public float OrthoHalfWidth => OrthoSize * AspectRatio;
    public float MinX => CameraCenter.x - OrthoHalfWidth + playerExtents.x + padding.x;
    public float MaxX => CameraCenter.x + OrthoHalfWidth - playerExtents.x - padding.x;
    public float MinY => CameraCenter.y - OrthoSize + playerExtents.y + padding.y + bottomBarPadding;
    public float MaxY => CameraCenter.y + (OrthoSize * maxYFactor);

    private void Awake()
    {
        _playerHealth = GetComponent<PlayerHealth>();
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        _targetPosition = transform.position;
    }

    private void Start()
    {
        GameEvents.OnGameRestart += HandleGameRestart;
    }

    private void OnDestroy()
    {
        GameEvents.OnGameRestart -= HandleGameRestart;
    }

    private void Update()
    {
        // Suppress controls if player is deceased
        if (_playerHealth != null && !_playerHealth.IsAlive)
        {
            _isDragging = false;
            _activeFingerId = -1;
            return;
        }

        // Process pointer drag (Touchscreen or Mouse) via New Input System
        bool pointerActive = ProcessPointerInput(Time.deltaTime);

        // Fallback to keyboard WASD / Arrow keys if no pointer drag is occurring
        if (!pointerActive)
        {
            ProcessKeyboardInput(Time.deltaTime);
        }
    }

    /// <summary>
    /// Evaluates Touchscreen and Mouse inputs via New Input System.
    /// Returns true if a pointer drag is active.
    /// </summary>
    private bool ProcessPointerInput(float dt)
    {
        Camera cam = ActiveCamera;
        if (cam == null) return false;

        // 1. Check Mobile Touchscreen
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var primaryTouch = touchscreen.primaryTouch;
            if (primaryTouch.press.isPressed)
            {
                int fingerId = primaryTouch.touchId.ReadValue();
                Vector2 screenPos = primaryTouch.position.ReadValue();

                if (!_isDragging)
                {
                    if (!IsPointerOverUI(fingerId))
                    {
                        Vector2 worldPos = ScreenToWorld(cam, screenPos);
                        OnTouchDown(fingerId, worldPos, useErgonomicLift);
                    }
                }
                else if (fingerId == _activeFingerId)
                {
                    Vector2 worldPos = ScreenToWorld(cam, screenPos);
                    OnTouchMove(fingerId, worldPos, dt, useSmoothing);
                }

                return _isDragging;
            }
            else if (_isDragging && _activeFingerId != -1)
            {
                OnTouchUp(_activeFingerId);
            }
        }

        // 2. Check Mouse Pointer (for PC / Editor testing)
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.isPressed)
            {
                Vector2 screenPos = mouse.position.ReadValue();

                if (!_isDragging)
                {
                    if (!IsPointerOverUI(-1))
                    {
                        Vector2 worldPos = ScreenToWorld(cam, screenPos);
                        OnTouchDown(0, worldPos, useErgonomicLift);
                    }
                }
                else if (_activeFingerId == 0)
                {
                    Vector2 worldPos = ScreenToWorld(cam, screenPos);
                    OnTouchMove(0, worldPos, dt, useSmoothing);
                }

                return _isDragging;
            }
            else if (_isDragging && _activeFingerId == 0)
            {
                OnTouchUp(0);
            }
        }

        return false;
    }

    /// <summary>
    /// Processes fallback keyboard vector input (WASD / Arrows).
    /// </summary>
    private void ProcessKeyboardInput(float dt)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || dt <= 0f) return;

        float h = 0f;
        float v = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;

        Vector2 inputDir = new Vector2(h, v);
        if (inputDir.sqrMagnitude > 0.001f)
        {
            OnKeyboardInput(inputDir, dt);
        }
    }

    #region Simulation & Motion Methods
    /// <summary>
    /// Initiates a touch drag operation with dynamic offset calculation.
    /// </summary>
    public void OnTouchDown(int fingerId, Vector2 touchWorldPos, bool withErgonomicLift = false)
    {
        if (_isDragging) return; // Prevent secondary finger jumping

        _activeFingerId = fingerId;
        _isDragging = true;

        if (withErgonomicLift)
        {
            _touchOffset = new Vector2(0f, ergonomicLift);
        }
        else
        {
            _touchOffset = (Vector2)transform.position - touchWorldPos;
        }

        _targetPosition = ClampPosition(touchWorldPos + _touchOffset);
    }

    /// <summary>
    /// Updates position during drag, applying optional exponential smoothing.
    /// </summary>
    public void OnTouchMove(int fingerId, Vector2 touchWorldPos, float dt, bool smooth = false)
    {
        if (!_isDragging || fingerId != _activeFingerId) return;

        Vector2 target = ClampPosition(touchWorldPos + _touchOffset);
        _targetPosition = target;

        if (smooth && dt > 0f)
        {
            float factor = 1f - Mathf.Exp(-smoothingLambda * dt);
            transform.position = Vector2.Lerp(transform.position, target, factor);
        }
        else
        {
            transform.position = target;
        }
    }

    /// <summary>
    /// Terminates the touch drag state.
    /// </summary>
    public void OnTouchUp(int fingerId)
    {
        if (fingerId == _activeFingerId)
        {
            _isDragging = false;
            _activeFingerId = -1;
        }
    }

    /// <summary>
    /// Applies velocity movement from keyboard or virtual axes.
    /// </summary>
    public void OnKeyboardInput(Vector2 inputDirection, float dt)
    {
        if (_isDragging || dt <= 0f) return;

        Vector2 clampedDir = Vector2.ClampMagnitude(inputDirection, 1.0f);
        Vector2 nextPos = (Vector2)transform.position + clampedDir * (playerSpeed * dt);
        transform.position = ClampPosition(nextPos);
    }

    /// <summary>
    /// Restricts world coordinates within orthographic camera bounds.
    /// </summary>
    public Vector2 ClampPosition(Vector2 pos)
    {
        float clampedX = Mathf.Clamp(pos.x, MinX, MaxX);
        float clampedY = Mathf.Clamp(pos.y, MinY, MaxY);
        return new Vector2(clampedX, clampedY);
    }

    /// <summary>
    /// Teleports player ship to a designated position within bounds.
    /// </summary>
    public void ResetPosition(Vector2 position)
    {
        _isDragging = false;
        _activeFingerId = -1;
        transform.position = ClampPosition(position);
        _targetPosition = transform.position;
    }
    #endregion

    private Vector2 ScreenToWorld(Camera cam, Vector2 screenPos)
    {
        Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
        return new Vector2(worldPoint.x, worldPoint.y);
    }

    private bool IsPointerOverUI(int pointerId)
    {
        if (EventSystem.current == null) return false;
        if (pointerId >= 0)
        {
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void HandleGameRestart()
    {
        ResetPosition(defaultSpawnPosition);
    }
}
