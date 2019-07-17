using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Krisis.UI
{
	public abstract class MenuPanel : MonoBehaviour
	{
		public virtual void Hide()
		{
			gameObject.SetActive(false);
		}

		public virtual void Show()
		{
			gameObject.SetActive(true);
		}
	}
}