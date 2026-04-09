using UnityEngine;

public class MovingBody : MonoBehaviour
{
	[SerializeField] private float speed = 10f;
    
    private void Update()
    {
        transform.position -= Vector3.forward * speed * Time.deltaTime;
    }
}
