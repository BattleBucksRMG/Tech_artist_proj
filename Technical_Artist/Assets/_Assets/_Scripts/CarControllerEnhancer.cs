
using UnityEngine;

public class CarControllerEnhancer : MonoBehaviour
{
    [Header("Rotation Settings (Degrees)")]
    [SerializeField] public float pitchAngleX = 0f;
    [SerializeField] public float steeringAngleY = -25f;
    [SerializeField] public float rollAngleZ = 0f;

    [Header("Responsiveness")]
    [SerializeField] private float tiltSpeed = 15f;
    [SerializeField] private float tiltSensitivity = 2.0f;

    [Header("Indicators")]
    public GameObject leftIndicator;
    public GameObject rightIndicator;

    [Header("VFX & Particles")]
    public ParticleSystem leftDustVFX;
    public ParticleSystem rightDustVFX;

    [Header("References")]
    [SerializeField] private Transform carMeshTransform;
    private Vector3 _previousPosition;
    private Quaternion _targetRotation;
    private Quaternion _currentTiltRotation;
    private float _horizontalDelta;
    private float _turnFactor;
    private bool _wasTurningLeft = false;
    private bool _wasTurningRight = false;

    private void Awake()
    {
        if (carMeshTransform == null)
        {
            carMeshTransform = transform;
        }

        _previousPosition = transform.position;
        _currentTiltRotation = Quaternion.identity;
        _targetRotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        float horizontalVelocity = (transform.position.x - _previousPosition.x) / Time.deltaTime;
        _previousPosition = transform.position;
        _turnFactor = Mathf.Clamp((horizontalVelocity * tiltSensitivity) / 10f, -1f, 1f);
        float targetX = _turnFactor * pitchAngleX;
        float targetY = _turnFactor * steeringAngleY;
        float targetZ = _turnFactor * rollAngleZ;

        _targetRotation = Quaternion.Euler(targetX, targetY, targetZ);
        _currentTiltRotation = Quaternion.Lerp(
            _currentTiltRotation,
            _targetRotation,
            Time.deltaTime * tiltSpeed
        );
        carMeshTransform.localRotation = _currentTiltRotation;
        bool isTurningLeft = _turnFactor < -0.2f;
        bool isTurningRight = _turnFactor > 0.2f;

        if (leftIndicator != null && rightIndicator != null)
        {
            leftIndicator.SetActive(isTurningLeft);
            rightIndicator.SetActive(isTurningRight);
        }
        if (isTurningLeft && !_wasTurningLeft)
        {
            if (leftDustVFX != null)
            {
                ParticleSystem puff = Instantiate(leftDustVFX, transform.position, Quaternion.identity);
                puff.Play();
                Destroy(puff.gameObject, puff.main.duration + puff.main.startLifetime.constantMax);
            }
        }
        else if (isTurningRight && !_wasTurningRight)
        {
            if (rightDustVFX != null)
            {
                ParticleSystem puff = Instantiate(rightDustVFX, transform.position, Quaternion.identity);
                puff.Play();
                Destroy(puff.gameObject, puff.main.duration + puff.main.startLifetime.constantMax);
            }
        }

        _wasTurningLeft = isTurningLeft;
        _wasTurningRight = isTurningRight;
    }
}



