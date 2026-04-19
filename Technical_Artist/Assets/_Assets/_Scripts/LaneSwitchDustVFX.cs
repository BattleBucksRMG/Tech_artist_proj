
using UnityEngine;

public class LaneSwitchDustVFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem dustParticles;

    [Header("Detection")]
    [SerializeField] private float laneSwitchThreshold = 0.05f;
    private float _previousX;
    private float _deltaX;
    private bool _wasMoving;

    private void Awake()
    {
        if (dustParticles == null)
        {
            dustParticles = GetComponentInChildren<ParticleSystem>();
        }

        if (dustParticles != null)
        {
            dustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        _previousX = transform.position.x;
        _wasMoving = false;
    }

    private void Update()
    {
        _deltaX = Mathf.Abs(transform.position.x - _previousX);

        bool isMoving = _deltaX > laneSwitchThreshold;
        if (isMoving && !_wasMoving && dustParticles != null)
        {
            dustParticles.Play();
        }

        _wasMoving = isMoving;
        _previousX = transform.position.x;
    }
}



