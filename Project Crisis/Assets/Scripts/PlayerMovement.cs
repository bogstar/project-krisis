using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
	[Header("Movement")]
	public Transform head;
	public Transform body;
	public float sensitivity { get { return GameManager.GetMouseSensitivity(); } }
	public float xClampMin = -80f;
	public float xClampMax = 40f;
	public float downScopeMultiplier = .8666f;
	public float gravityMultiplier = 1f;
	public float stickToGroundForce = 10f;

	new Camera camera { get { return player.camera; } }

	Quaternion bodyTargetRot;
	Quaternion headTargetRot;
	CharacterController characterController;
	new Rigidbody rigidbody;
	Vector2 cameraInput;
	Vector2 movementInput;

	public Vector3 moveDir { get { return m_moveDir; } }

	Vector3 m_moveDir;
	bool isWalking;
	bool jump;
	bool groundedPreviousFrame;
	bool m_jumping;

	public bool jumping { get { return (m_jumping || jump); } }

	Player player
	{
		get
		{
			if (m_player == null)
			{
				m_player = GetComponent<Player>();
			}
			return m_player;
		}
	}
	Player m_player;


	private void Start()
	{
		characterController = GetComponent<CharacterController>();
		bodyTargetRot = body.localRotation;
		headTargetRot = head.localRotation;
		isRecoiling = false;
	}

	void Update()
	{
		if (!hasAuthority)
		{
			return;
		}

		// If not already jumping, jump
		if (!jump && !InGameGUI.Instance.lockCamera)
		{
			if (!player.playerShoot.lookingDownScope && player.isAlive)
			{
				jump = Input.GetKeyDown(KeyCode.Space);
			}
		}

		// Character has landed this frame
		if (!groundedPreviousFrame && characterController.isGrounded)
		{
			m_jumping = false;
			m_moveDir.y = 0f;
		}

		// ???????
		if (!characterController.isGrounded && !m_jumping && groundedPreviousFrame)
		{
			//moveDir.y = 0f;
		}

		groundedPreviousFrame = characterController.isGrounded;

		if (!InGameGUI.Instance.lockCamera)
		{
			float multiplier = 1f / (60f / player.playerShoot.targetFov);
			cameraInput.x = Input.GetAxisRaw("Mouse X") * sensitivity * multiplier;
			cameraInput.y = Input.GetAxisRaw("Mouse Y") * sensitivity * multiplier;
		}
		else
		{
			cameraInput.x = 0;
			cameraInput.y = 0;
		}
	}

	void FixedUpdate()
	{
		if (!hasAuthority)
		{
			return;
		}

		bodyTargetRot *= Quaternion.Euler(0f, cameraInput.x, 0f);
		headTargetRot *= Quaternion.Euler(-cameraInput.y, 0f, 0f);

		if (isRecoiling)
		{
			bodyTargetRot *= Quaternion.Euler(0f, recoilVector.x, 0f);
			headTargetRot *= Quaternion.Euler(-recoilVector.y, 0f, 0f);
		}

		headTargetRot = Utility.ClampRotationAroundAxis(headTargetRot, Utility.Axis.X, xClampMin, xClampMax);
		
		if (player.isAlive)
		{
			float speed;

			// Resolve all inputs
			GetInput(out speed);

			// Resolve desired move vector
			Vector3 forwardDirection = head.forward.normalized;
			Vector3 rightDirection = head.right.normalized;
			Vector3 desiredMove = forwardDirection * movementInput.y + rightDirection * movementInput.x;

			// Get normal on the ground
			RaycastHit hit;
			bool ishit = Physics.SphereCast(transform.position, characterController.radius, Vector3.down, out hit,
							   characterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
			desiredMove = Vector3.ProjectOnPlane(desiredMove, Vector3.up).normalized;

			bool overGround = Physics.Raycast(transform.position, Vector3.down, characterController.height);

			// Set movedir to normal multiplied with speed
			m_moveDir.x = desiredMove.x * speed;
			m_moveDir.z = desiredMove.z * speed;

			if (characterController.isGrounded)
			{
				if (!overGround)
				{
					desiredMove = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
					m_moveDir = desiredMove * 10f;
				}
				else
				{
					m_moveDir.y = -stickToGroundForce;
				}

				if (jump)
				{
					m_moveDir.y = player.characterData.jumpStrength;
					jump = false;
					m_jumping = true;
				}
			}
			else
			{
				if (m_moveDir.y > 0f)
				{
					if (Physics.SphereCast(transform.position, characterController.radius, Vector3.up, out hit,
							   characterController.height, Physics.AllLayers, QueryTriggerInteraction.Ignore))
					{
						m_moveDir.y = 0f;
					}
				}

				m_moveDir += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime;

				jump = false;
			}

			if (!characterController.isGrounded)
			{
				player.playerShoot.SpreadReticle(Time.fixedDeltaTime);
			}
			if (Mathf.Abs(m_moveDir.x) > 0 || Mathf.Abs(m_moveDir.z) > 0)
			{
				player.playerShoot.SpreadReticle(Time.fixedDeltaTime);
			}

			characterController.Move(m_moveDir * Time.fixedDeltaTime);
		}

		body.localRotation = bodyTargetRot;
		head.localRotation = headTargetRot;
	}

	bool isRecoiling;
	float recoilTimer;
	Vector2 recoilVector;

	public void TriggerRecoil(float duration)
	{
		StopAllCoroutines();
		StartCoroutine(Recoil(duration));
	}

	IEnumerator Recoil(float duration)
	{
		isRecoiling = true;
		recoilTimer = Time.time + duration;
		float recoilStart = Time.time;
		float xDir = Random.Range(-1f, 1f);

		float xMultiplier = .5f;
		float yMultiplier = 1.5f;

		while (Time.time < recoilTimer)
		{
			float progress = Mathf.InverseLerp(recoilStart, recoilTimer, Time.time);

			recoilVector.y = (GaussianCurve((progress - .5f) * 3, 1, 0) * ((progress > 0.5f) ? -1 : 1)) * yMultiplier;
			recoilVector.x = xDir * ((progress > 0.5f) ? 1 - progress : 1) * xMultiplier;

			yield return null;
		}

		recoilVector = Vector2.zero;
		isRecoiling = false;
	}

	float GaussianCurve(float x, float sigma, float mi)
	{
		return (1 / ((Mathf.Sqrt(2 * Mathf.PI * sigma * sigma))) * Mathf.Exp(-Mathf.Pow((x - mi), 2) / (2 * sigma * sigma)));
	}

	private void GetInput(out float speed)
	{
		if (InGameGUI.Instance.lockCamera)
		{
			speed = 0;
			return;
		}

		// Get Input
		movementInput.x = Input.GetAxisRaw("Horizontal");
		movementInput.y = Input.GetAxisRaw("Vertical");
		
		float downScopeMultiplier = player.playerShoot.lookingDownScope ? .3f : 1f;

		// Set the desired speed to be walking or running
		if (player.playerShoot.lookingDownScope)
		{
			speed = player.characterData.moveSpeed * downScopeMultiplier;
		}
		else
		{
			speed = player.characterData.moveSpeed;
		}

		// Normalize input if it exceeds 1 in combined length
		if (movementInput.sqrMagnitude > 1)
		{
			movementInput.Normalize();
		}
	}
}