using UnityEngine;

public class SpawnerLoop : MonoBehaviour
{
    [SerializeField] private Transform[] movingBoddies;
	[SerializeField] private float startZ = 50f;
	[SerializeField] private float offCameraZ = 50f;
	[SerializeField] private float segmentLength = 10f;

	private void Update()
	{
		LoopBodies();
	}

	private void SpawnMovingBody()
	{
		foreach (Transform body in movingBoddies)
		{
			if(body.transform.position.z <= offCameraZ)
			{
				body.transform.position = new Vector3(body.transform.position.x, body.transform.position.y, startZ);
			}
			
		}
	}

	private void LoopBodies()
	{
		if (movingBoddies == null || movingBoddies.Length == 0)
		{
			return;
		}
		foreach (Transform body in movingBoddies)
		{
			if (body.position.z <= offCameraZ)
			{
				body.position = new Vector3(body.position.x, body.position.y, startZ);
			}
		}
	}


}
