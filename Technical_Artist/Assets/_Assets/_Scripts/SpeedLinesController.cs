
using UnityEngine;

public class SpeedLinesController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem speedLinesParticles;

    [Header("Emission Settings")]
    [SerializeField] private float normalEmissionRate = 10f;
    [SerializeField] private float boostEmissionRate = 50f;
    [SerializeField] private float emissionTransitionSpeed = 5f;
    private ParticleSystem.EmissionModule _emissionModule;
    private float _currentRate;
    private float _targetRate;
    private bool _isBoosting;

    private void Awake()
    {
        if (speedLinesParticles == null)
        {
            speedLinesParticles = GetComponent<ParticleSystem>();
        }

        _emissionModule = speedLinesParticles.emission;
        _currentRate = normalEmissionRate;
        _targetRate = normalEmissionRate;
        _emissionModule.rateOverTime = _currentRate;
    }

    private void Update()
    {
        if (GameManager.Instance != null)
        {
            float speedRatio = GameManager.Instance.CurrentSpeed / 40f; 
            _targetRate = normalEmissionRate * speedRatio;
            
            if (_isBoosting) _targetRate += boostEmissionRate;
        }

        _currentRate = Mathf.Lerp(_currentRate, _targetRate, Time.deltaTime * emissionTransitionSpeed);
        _emissionModule.rateOverTime = _currentRate;
    }

    /// <summary>
    /// Toggle the speed lines effect on or off.
    /// Call from ArcadeCameraController or GameManager when speed state changes.
    /// </summary>
    public void SetBoosting(bool boosting)
    {
        _isBoosting = boosting;
    }
}





