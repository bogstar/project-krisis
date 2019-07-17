using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utility {

	/// <summary>
	/// Returns a Quaternion clamped around a specified axis between angles.
	/// </summary>
	/// <param name="q">The angle to clamp</param>
	/// <param name="axis">Specified axis</param>
	/// <param name="min">Minimum angle value</param>
	/// <param name="max">Maximum angle value</param>
	/// <returns></returns>
	public static Quaternion ClampRotationAroundAxis(Quaternion q, Axis axis, float min, float max)
	{
		q.x /= q.w;
		q.y /= q.w;
		q.z /= q.w;
		q.w = 1.0f;

		float atan = 0f;

		switch (axis)
		{
			case Axis.X:
				atan = Mathf.Atan(q.x);
				break;
			case Axis.Y:
				atan = Mathf.Atan(q.y);
				break;
			case Axis.Z:
				atan = Mathf.Atan(q.z);
				break;
		}

		float angle = 2.0f * Mathf.Rad2Deg * atan;

		angle = Mathf.Clamp(angle, min, max);

		float tan = Mathf.Tan(0.5f * Mathf.Deg2Rad * angle);

		switch (axis)
		{
			case Axis.X:
				q.x = tan;
				break;
			case Axis.Y:
				q.y = tan;
				break;
			case Axis.Z:
				q.z = tan;
				break;
		}

		return q;
	}

	/// <summary>
	/// Copies all components from a GameObject to a GameObject.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="destination"></param>
	public static void CopyAllComponents(GameObject source, GameObject destination)
	{
		Component[] components = source.GetComponents(typeof(Component));
		foreach(Component c in components)
		{
			CopyComponent(c, destination);
		}
	}

	/// <summary>
	/// Copies a component to a GameObject.
	/// </summary>
	/// <param name="original"></param>
	/// <param name="destination"></param>
	/// <returns></returns>
	public static Component CopyComponent(Component original, GameObject destination)
	{
		System.Type type = original.GetType();
		Component copy = destination.AddComponent(type);
		// Copied fields can be restricted with BindingFlags
		System.Reflection.FieldInfo[] fields = type.GetFields();
		foreach (System.Reflection.FieldInfo field in fields)
		{
			field.SetValue(copy, field.GetValue(original));
		}
		return copy;
	}

	public enum Axis
	{
		X, Y, Z
	}
}