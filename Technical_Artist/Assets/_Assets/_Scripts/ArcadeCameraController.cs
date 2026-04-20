
using UnityEngine;

public class ArcadeCameraController : MonoBehaviour
{

    [Header("Follow Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f);

    [Header("Smooth Follow")]
    [SerializeField] private float smoothTime = 0.25f;
    [SerializeField] private float maxSpeed = 50f;

    [Header("FOV Kick")]
    [SerializeField] private float baseFOV = 60f;
    [SerializeField] private float maxFOV = 75f;
    [SerializeField] private float fovTransitionSpeed = 4f;

    [Header("Look-At")]
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private float lookAtHeightOffset = 1.5f;

    [Header("Camera Shake")]
    [SerializeField] private float collisionShakeMagnitude = 1.5f;
    [SerializeField] private float shakeFadeSpeed = 5.0f;

    private Camera _camera;
    private Vector3 _currentVelocity;
    private Vector3 _desiredPosition;
    private Vector3 _smoothedPosition;
    private float _targetFOV;
    private float _currentFOV;
    private bool _isSpeeding;
    private float _currentShakeIntensity;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            enabled = false;
            return;
        }

        _targetFOV = baseFOV;
        _currentFOV = baseFOV;
        _camera.fieldOfView = baseFOV;
        _currentVelocity = Vector3.zero;
    }

    private void Start()
    {
        if (target != null)
        {
            _desiredPosition = target.position + offset;
            transform.position = _desiredPosition;
        }
    }

    /// <summary>
    /// LateUpdate ensures the camera moves after all game logic and physics
    /// have resolved for the current frame, preventing visual jitter.
    /// </summary>
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }
        _desiredPosition = target.position + offset;
        _smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            _desiredPosition,
            ref _currentVelocity,
            smoothTime,
            maxSpeed,
            Time.deltaTime
        );

        transform.position = _smoothedPosition;
        if (lookAtTarget)
        {
            transform.LookAt(target.position + Vector3.up * lookAtHeightOffset);
        }
        if (_currentShakeIntensity > 0f)
        {
            transform.position += Random.insideUnitSphere * _currentShakeIntensity;
            _currentShakeIntensity = Mathf.Lerp(_currentShakeIntensity, 0f, Time.deltaTime * shakeFadeSpeed);
            if (_currentShakeIntensity < 0.05f) _currentShakeIntensity = 0f;
        }
        _currentFOV = Mathf.Lerp(
            _currentFOV,
            _targetFOV,
            Time.deltaTime * fovTransitionSpeed
        );
        _camera.fieldOfView = _currentFOV;
    }

    /// <summary>
    /// Call this method to toggle the FOV kick on or off.
    /// Typically called by the game manager when the car reaches top speed
    /// or when the player triggers a boost.
    /// </summary>
    /// <param name="isSpeeding">
    /// True = expand FOV to maxFOV (speed emphasis).
    /// False = contract FOV back to baseFOV (normal driving).
    /// </param>
    public void SetSpeedState(bool isSpeeding)
    {
        _isSpeeding = isSpeeding;
        _targetFOV = _isSpeeding ? maxFOV : baseFOV;
    }

    /// <summary>
    /// Allows external scripts to override the follow target at runtime
    /// (e.g., switching camera focus to a different vehicle).
    /// </summary>
    /// <param name="newTarget">The new Transform to follow.</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Performs an instantaneous camera cut to the current target position.
    /// Useful after respawns or scene transitions to avoid long interpolations.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        _desiredPosition = target.position + offset;
        transform.position = _desiredPosition;
        _currentVelocity = Vector3.zero;
    }

    /// <summary>
    /// Triggers a violent screen shake. Call this when colliding with an obstacle!
    /// </summary>
    public void TriggerCollisionShake()
    {
        _currentShakeIntensity = collisionShakeMagnitude;
    }
}



