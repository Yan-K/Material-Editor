using UnityEngine;

namespace YanK
{
	internal static class SceneControllerInput
	{
		private static bool _rmbWasDown;

		public static void HandleInput(SceneController sc, float dt)
		{
			if (sc == null) return;
			if (!Application.isPlaying) return;

			bool rmb = Input.GetMouseButton(1);

			if (rmb && !_rmbWasDown)
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
			else if (!rmb && _rmbWasDown)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
			_rmbWasDown = rmb;

			Camera cam = sc.GetActiveCamera();
			if (cam == null) return;

			float mx = Input.GetAxisRaw("Mouse X");
			float my = Input.GetAxisRaw("Mouse Y");
			float wheel = Input.GetAxis("Mouse ScrollWheel");
			float h = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
			float v = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
			float y = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);

			// Shift = 2× speed boost — applies to both avatar movement and camera free-fly.
			bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			float speedMult = shift ? 2f : 1f;

		bool mmb = Input.GetMouseButton(2);

		if (sc.GetEffectiveCameraMode() == CameraControlMode.Orbit)
			HandleOrbit(sc, cam, dt, rmb, mmb, mx, my, wheel, h, v, y, speedMult);
			else
				HandleFreeFly(sc, cam, dt, rmb, mx, my, h, v, y, speedMult);
		}

		private static void HandleOrbit(SceneController sc, Camera cam, float dt, bool rmb, bool mmb,
			float mx, float my, float wheel, float h, float v, float y, float speedMult)
		{
			if (mmb && (mx != 0f || my != 0f))
			{
				// MMB held: pan the camera pivot in camera space.
				float panSpeed = Mathf.Max(0.2f, sc.cameraDistance) * 0.005f * sc.mouseSensitivity;
				Transform ct = cam.transform;
				Vector3 delta = (ct.right * mx + ct.up * my) * panSpeed;
				if (sc.cameraPivot != null) sc.cameraPivot.position += delta;
			}

			if (rmb)
			{
				sc.cameraYaw += mx * sc.mouseSensitivity;
				// Mouse up = orbit rises (camera moves above avatar).
				// invertMouseY flips this back for users who prefer the opposite.
				sc.cameraPitch += my * sc.mouseSensitivity * (sc.invertMouseY ? -1f : 1f);
			}

			sc.cameraYaw = Mathf.Repeat(sc.cameraYaw + 180f, 360f) - 180f;
			sc.cameraPitch = Mathf.Clamp(sc.cameraPitch, -89f, 89f);

			if (!Mathf.Approximately(wheel, 0f))
			{
				// Distance-proportional zoom — scrolling feels uniform at any distance
				// and small wheel deltas no longer over-correct (reduces perceived jitter).
				float step = wheel * Mathf.Max(0.2f, sc.cameraDistance) * 0.9f;
				sc.cameraDistance = Mathf.Clamp(sc.cameraDistance - step, 0.2f, 20f);
			}

			if (h != 0f || v != 0f || y != 0f)
			{
				Transform ct = cam.transform;
				Vector3 forward = ct.forward; forward.y = 0f; forward.Normalize();
				Vector3 right = ct.right; right.y = 0f; right.Normalize();
				Vector3 delta = (right * h + forward * v) * sc.moveSpeed * speedMult * dt
				                + Vector3.up * (y * sc.verticalSpeed * speedMult * dt);

				if (sc.avatarRoot != null)
				{
					sc.avatarHomePosition += delta;
					sc.avatarRoot.transform.position += delta;
				}
				if (sc.cameraPivot != null) sc.cameraPivot.position += delta;
			}
		}

		// wheel parameter removed from free-fly — scroll no longer changes fly speed.
		private static void HandleFreeFly(SceneController sc, Camera cam, float dt, bool rmb,
			float mx, float my, float h, float v, float y, float speedMult)
		{
			Transform ct = cam.transform;

			if (rmb)
			{
				Vector3 e = ct.localEulerAngles;
				float pitch = e.x > 180f ? e.x - 360f : e.x;
				float yaw = e.y;

				yaw += mx * sc.mouseSensitivity;
				// Standard FPS look: mouse up = pitch down (look up).
				// invertMouseY reverses this for flight-sim style.
				pitch -= my * sc.mouseSensitivity * (sc.invertMouseY ? -1f : 1f);
				pitch = Mathf.Clamp(pitch, -89f, 89f);
				yaw = Mathf.Repeat(yaw + 180f, 360f) - 180f;

				ct.localEulerAngles = new Vector3(pitch, yaw, 0f);

				if (h != 0f || v != 0f || y != 0f)
				{
					Vector3 delta = (ct.right * h + ct.forward * v) * sc.moveSpeed * speedMult * dt
					                + Vector3.up * (y * sc.verticalSpeed * speedMult * dt);
					ct.position += delta;
				}
			}
			else
			{
				// Not holding RMB: WASD/QE moves the avatar, or the camera if no avatar.
				if (h != 0f || v != 0f || y != 0f)
				{
					Vector3 forward = ct.forward; forward.y = 0f; forward.Normalize();
					Vector3 right = ct.right; right.y = 0f; right.Normalize();
					Vector3 delta = (right * h + forward * v) * sc.moveSpeed * speedMult * dt
					                + Vector3.up * (y * sc.verticalSpeed * speedMult * dt);

					if (sc.avatarRoot != null)
					{
						sc.avatarHomePosition += delta;
						sc.avatarRoot.transform.position += delta;
						if (sc.cameraPivot != null) sc.cameraPivot.position += delta;
					}
					else
					{
						ct.position += delta;
					}
				}
			}
		}
	}
}
