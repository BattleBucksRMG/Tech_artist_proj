using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CarController : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
	[SerializeField] private Vector2 minMaxXPos = new Vector2(5f, -5f);

	[SerializeField] private float horizontalStepCount = 5f;
	[SerializeField] private float swipeThreshold = 50f;

	private bool isMoving = false;
	private int currentStep = 0;
	private Vector3 targetPosition;
	private Vector2 swipeStartPosMouse;
	private bool isSwipingMouse = false;
	private Vector2 swipeStartPosTouch;
	private bool isSwipingTouch = false;
	private void Update()
	{

		HandleMouseSwipe();
		HandleTouchSwipe();
		if (isMoving)
		{
			Vector3 smoothMovePos = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * speed);
			transform.position = smoothMovePos;
		}
		else
		{
			transform.position = new Vector3(targetPosition.x, transform.position.y, transform.position.z);
		}
	}

	private void HandleMouseSwipe()
	{
		if (Mouse.current == null)
		{
			return;
		}

		if (Mouse.current.leftButton.wasPressedThisFrame)
		{
			isSwipingMouse = true;
			swipeStartPosMouse = Mouse.current.position.ReadValue();
		}
		else if (Mouse.current.leftButton.isPressed && isSwipingMouse)
		{
		}
		else if (Mouse.current.leftButton.wasReleasedThisFrame && isSwipingMouse)
		{
			Vector2 endPos = Mouse.current.position.ReadValue();
			Vector2 delta = endPos - swipeStartPosMouse;
			isSwipingMouse = false;

			if (Mathf.Abs(delta.x) > swipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
			{
				if (delta.x < 0f){
					MoveHorizontal(1);
				}
				else{
					MoveHorizontal(-1);
				}
			}
		}
	}

	private void HandleTouchSwipe()
	{
		if (Touchscreen.current == null)
		{
			return;
		}

		var primary = Touchscreen.current.primaryTouch;
		if (!primary.press.isPressed && !isSwipingTouch)
		{
			return;
		}

		if (primary.press.wasPressedThisFrame)
		{
			isSwipingTouch = true;
			swipeStartPosTouch = primary.position.ReadValue();
		}
		else if (primary.press.isPressed && isSwipingTouch)
		{
		}
		else if (primary.press.wasReleasedThisFrame && isSwipingTouch)
		{
			Vector2 endPos = primary.position.ReadValue();
			Vector2 delta = endPos - swipeStartPosTouch;
			isSwipingTouch = false;

			if (Mathf.Abs(delta.x) > swipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) {
				if (delta.x < 0f) {

					MoveHorizontal(1);
				}else {
					MoveHorizontal(-1);
				}
			}
		}
	}
	private void MoveHorizontal(int direction)
	{
		float stepSize = (minMaxXPos.y - minMaxXPos.x) / horizontalStepCount;
		currentStep += direction;
		currentStep = Mathf.Clamp(currentStep, 0, (int)horizontalStepCount);
		float newXPos = minMaxXPos.x + currentStep * stepSize;
		targetPosition = new Vector3(newXPos, transform.position.y, transform.position.z);
		if(Vector3.Distance(transform.position, targetPosition) > 0.1f)
		{
			isMoving = true;
		}
		else
		{
			isMoving = false;
		}
	}

    [Header("Crash & Invincibility")]
    public float invincibilityDuration = 2.0f;
    public float blinkSpeed = 0.1f;
    private bool _isInvincible = false;
    private Renderer[] _carRenderers;

    [Header("Collision VFX")]
    public ParticleSystem collisionSparksVFX;
    private ParticleSystem _sparkInstance;

    private void Start()
    {
        _carRenderers = GetComponentsInChildren<Renderer>();

        if (collisionSparksVFX != null)
        {
            _sparkInstance = Instantiate(collisionSparksVFX, transform.position, Quaternion.identity);
            _sparkInstance.transform.SetParent(this.transform);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground")) return;
        if (_isInvincible) return;

        if (_sparkInstance != null)
        {
            Vector3 pointOfImpact = other.ClosestPoint(transform.position);
            
            Quaternion rotationOutward = Quaternion.identity;
            Vector3 directionOutward = (pointOfImpact - transform.position).normalized;
            if (directionOutward != Vector3.zero)
            {
                rotationOutward = Quaternion.LookRotation(directionOutward);
            }
            
            _sparkInstance.transform.position = pointOfImpact;
            _sparkInstance.transform.rotation = rotationOutward;
            _sparkInstance.Play();
        }
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.cameraController != null)
                GameManager.Instance.cameraController.TriggerCollisionShake();
            
            GameManager.Instance.TriggerCrash();
        }
        StartCoroutine(CrashRecoveryRoutine());
    }

    private IEnumerator CrashRecoveryRoutine()
    {
        _isInvincible = true;
        
        float elapsedTimer = 0f;
        bool isVisible = true;

        while (elapsedTimer < invincibilityDuration)
        {
            elapsedTimer += blinkSpeed;
            isVisible = !isVisible;
            if (_carRenderers != null)
            {
                foreach (var r in _carRenderers)
                {
                    if (r != null) r.enabled = isVisible;
                }
            }

            yield return new WaitForSeconds(blinkSpeed);
        }
        if (_carRenderers != null)
        {
            foreach (var r in _carRenderers)
            {
                if (r != null) r.enabled = true;
            }
        }

        _isInvincible = false;
    }
}



