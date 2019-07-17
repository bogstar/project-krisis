using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : Krisis.UI.MenuPanel
{
	[Header("References")]
	[SerializeField] Slider mouseSensitivitySlider;

	float tempSensitivity;

	public override void Show()
	{
		base.Show();

		mouseSensitivitySlider.minValue = ParameterLibrary.GetFloat(ParameterLibrary.Parameter.MOUSE_SENSITIVITY_MIN, .2f);
		mouseSensitivitySlider.maxValue = ParameterLibrary.GetFloat(ParameterLibrary.Parameter.MOUSE_SENSITIVITY_MAX, 6f);
		mouseSensitivitySlider.value = GameManager.GetMouseSensitivity();
	}

    public void Button_Back()
	{
		GameManager.SetMouseSensitivity(tempSensitivity);
		Hide();
	}

	public void Slider_MouseSensitivityChange(float newSensitivity)
	{
		tempSensitivity = newSensitivity;
	}
}