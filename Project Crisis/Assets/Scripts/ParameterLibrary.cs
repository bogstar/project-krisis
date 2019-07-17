using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParameterLibrary : Singleton<ParameterLibrary>
{
	Dictionary<Parameter, string> m_parameters = new Dictionary<Parameter, string>();

	protected override void Awake()
	{
		base.Awake();

		m_parameters.Add(Parameter.MOUSE_SENSITIVITY_DEFAULT, "2");
		m_parameters.Add(Parameter.MOUSE_SENSITIVITY_MAX, "6");
		m_parameters.Add(Parameter.MOUSE_SENSITIVITY_MIN, "0.2");
		m_parameters.Add(Parameter.PREGAME_DURATION, "60");
		m_parameters.Add(Parameter.POSTGAME_DURATION, "30");
	}

	public static string GetString(Parameter param, string defaultValue)
	{
		foreach (var kvp in Instance.m_parameters)
		{
			if (kvp.Key == param)
			{
				return kvp.Value;
			}
		}

		Debug.LogError("No Key in Parameter Library: " + param);
		return defaultValue;
	}

	public static float GetFloat(Parameter param, float defaultValue)
	{
		foreach (var kvp in Instance.m_parameters)
		{
			if (kvp.Key == param)
			{
				if (float.TryParse(kvp.Value, out float parsedValue))
				{
					return parsedValue;
				}
				Debug.LogError("Key in Parameter Library cannot be parsed: " + param);
				return defaultValue;
			}
		}

		Debug.LogError("No Key in Parameter Library: " + param);
		return defaultValue;
	}

	public enum Parameter
	{
		MOUSE_SENSITIVITY_MIN,
		MOUSE_SENSITIVITY_MAX,
		MOUSE_SENSITIVITY_DEFAULT,
		PREGAME_DURATION,
		POSTGAME_DURATION
	}
}