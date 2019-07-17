using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponModelSetup : MonoBehaviour
{
	public Transform firePoint { get { return m_firePoint; } }

	[Header("References")]
	[SerializeField]
	Transform m_firePoint;
}