using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
	bool doTarget;

	public void ToggleFollow(bool follow)
	{
		doTarget = follow;
	}

	void Update ()
	{
		if (doTarget == true && Camera.main != null)
		{
			transform.LookAt(Camera.main.transform);
		}
	}
}
