using UnityEngine;

public class TrafficCarBehavior : MonoBehaviour
{
    [Header("Indicator References")]
    public GameObject leftIndicator;
    public GameObject rightIndicator;

    [Header("Lane Change Settings")]
    public float laneChangeCooldownMin = 3f;
    public float laneChangeCooldownMax = 8f;
    public float blinkDuration = 1.5f; 
    public float laneChangeSpeed = 2f;

    private float _actionTimer;
    private bool _isPreparingToChange;
    private bool _isChangingLane;
    private float _targetX;
    
    private void OnEnable()
    {
        ResetState();
    }

    public void ResetState()
    {
        if (leftIndicator != null) leftIndicator.SetActive(false);
        if (rightIndicator != null) rightIndicator.SetActive(false);
        
        _isPreparingToChange = false;
        _isChangingLane = false;
        _actionTimer = Random.Range(laneChangeCooldownMin, laneChangeCooldownMax);
    }

    private void Update()
    {
        if (_isChangingLane)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(pos.x, _targetX, Time.deltaTime * laneChangeSpeed);
            transform.position = pos;
            if (Mathf.Abs(transform.position.x - _targetX) < 0.1f)
            {
                pos.x = _targetX;
                transform.position = pos;
                ResetState();
            }
            return;
        }
        if (_isPreparingToChange)
        {
            _actionTimer -= Time.deltaTime;
            if (_actionTimer <= 0f)
            {
                _isPreparingToChange = false;
                _isChangingLane = true;
            }
            return;
        }
        _actionTimer -= Time.deltaTime;
        if (_actionTimer <= 0f)
        {
            DecideLaneChange();
        }
    }

    private void DecideLaneChange()
    {
        if (GameManager.Instance == null) return;

        float currentX = transform.position.x;
        float[] lanes = GameManager.Instance.lanePositionsX;
        int currentIndex = 1; 
        float minDst = float.MaxValue;
        for (int i = 0; i < lanes.Length; i++)
        {
            float dst = Mathf.Abs(lanes[i] - currentX);
            if (dst < minDst)
            {
                minDst = dst;
                currentIndex = i;
            }
        }
        bool canGoLeft = currentIndex > 0;
        bool canGoRight = currentIndex < lanes.Length - 1;

        if (!canGoLeft && !canGoRight) return; 
        int targetIndex = currentIndex;
        if (canGoLeft && canGoRight) targetIndex += Random.value > 0.5f ? 1 : -1;
        else if (canGoLeft) targetIndex -= 1;
        else if (canGoRight) targetIndex += 1;

        float proposedX = lanes[targetIndex];
        float safeDistance = GameManager.Instance.trafficSafeDistance;
        Transform[] allCars = GameManager.Instance.ActiveTrafficCars; 
        
        if (allCars != null)
        {
            foreach (Transform otherCar in allCars)
            {
                if (otherCar == null || otherCar == this.transform) continue;
                if (Mathf.Abs(otherCar.position.x - proposedX) < 1.0f)
                {
                    if (Mathf.Abs(otherCar.position.z - transform.position.z) < safeDistance * 0.8f) 
                    {
                        _actionTimer = 2f; 
                        return;
                    }
                }
            }
        }
        _targetX = proposedX;
        _isPreparingToChange = true;
        _actionTimer = blinkDuration;
        
        int currentLaneNumber = currentIndex + 1;
        int targetLaneNumber = targetIndex + 1;

        if (targetLaneNumber > currentLaneNumber)
        {
            if (rightIndicator != null) rightIndicator.SetActive(true);
        }
        else
        {
            if (leftIndicator != null) leftIndicator.SetActive(true);
        }
    }
}



