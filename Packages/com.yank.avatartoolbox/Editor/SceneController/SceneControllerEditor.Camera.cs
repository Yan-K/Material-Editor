using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YanK
{
	public partial class SceneControllerEditor
	{
		// Camera preset Add/Remove workflow (Add name buffer + Remove selection set).
		private readonly PresetWorkflow<string> _camWorkflow = new PresetWorkflow<string>();

		// Nullable cache — lazy-reloaded on first read, OnFocus, and after mutation.
		private List<SceneControllerPresets.CameraPreset> _globalCamPresets;
		private List<SceneControllerPresets.CameraPreset> GlobalCamPresets =>
			_globalCamPresets ??= SceneControllerPresets.LoadCameraPresets();

		private void OnEnableCamera()
		{
			_globalCamPresets = null; // force lazy reload on first use
		}

		private void DrawCameraSection()
		{
			GUILayout.Space(4);

			var mode = sc.GetEffectiveCameraMode();
			bool isFreeFly = mode == CameraControlMode.FreeFly;
			bool isOrbit = mode == CameraControlMode.Orbit;

			// ---- Control mode ----
			var newMode = (CameraControlMode)EditorGUILayout.EnumPopup(
				YanKLocalization.L("scCamMode", "Control Mode"), sc.cameraMode);
			if (newMode != sc.cameraMode)
			{
				Undo.RecordObject(sc, "Change Camera Mode");
				sc.cameraMode = newMode;
				if (newMode == CameraControlMode.Orbit && !string.IsNullOrEmpty(sc.activeCustomCameraName))
				{
					sc.activeCustomCameraName = "";
					ApplyActiveCamera();
				}
			}

			GUILayout.Space(8);

			// ---- Inspect Bone + Custom Target (hidden in Free-Fly) ----
			if (!isFreeFly)
			{
				DrawBoneTargetRow();
			}

			YanKInspectorGUI.DrawStyledSeparator();

			// ---- Field of View (always) ----
			float newFov = EditorGUILayout.Slider(YanKLocalization.L("scFov", "Field of View"), sc.cameraFov, 20f, 100f);
			if (SceneControllerMath.Changed(newFov, sc.cameraFov))
			{
				Undo.RecordObject(sc, "Camera FOV");
				sc.cameraFov = newFov;
			}

			// ---- Orbit sliders (Distance / Rotation / Pitch) — hidden in Free-Fly ----
			if (isOrbit)
			{
				EditorGUI.BeginChangeCheck();
				float newDist = EditorGUILayout.Slider(YanKLocalization.L("scDistance", "Distance"), sc.cameraDistance, 0.2f, 20f);
				float newYaw = EditorGUILayout.Slider(YanKLocalization.L("scYaw", "Rotation (Yaw)"), sc.cameraYaw, -180f, 180f);
				float newPitch = EditorGUILayout.Slider(YanKLocalization.L("scPitch", "Pitch"), sc.cameraPitch, -89f, 89f);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(sc, "Camera Orbit");
					sc.cameraDistance = newDist;
					sc.cameraYaw = newYaw;
					sc.cameraPitch = newPitch;
				}
			}

			// ---- Custom Cameras — only shown in Free-Fly mode (Orbit hides it) ----
			if (isFreeFly)
			{
				DrawCustomCameras();
			}
		}

		private void DrawBoneTargetRow()
		{
			using (new EditorGUI.DisabledScope(sc.boneTargetOverride != null))
			{
				EditorGUILayout.LabelField(YanKLocalization.L("scBoneTarget", "Inspect Bone"));
				int curBone = (int)sc.boneTarget;
				int r1Sel = curBone < 5 ? curBone : -1;
				int r2Sel = curBone >= 5 ? curBone - 5 : -1;
				int newR1 = GUILayout.Toolbar(r1Sel, SceneControllerConstants.BoneRow1Labels, EditorStyles.miniButton);
				int newR2 = GUILayout.Toolbar(r2Sel, SceneControllerConstants.BoneRow2Labels, EditorStyles.miniButton);
				int newBoneIdx = -1;
				if (newR1 != r1Sel) newBoneIdx = newR1;
				else if (newR2 != r2Sel) newBoneIdx = newR2 + 5;
				if (newBoneIdx >= 0)
				{
					Undo.RecordObject(sc, "Change Bone Target");
					sc.boneTarget = (BoneTarget)newBoneIdx;
				}
			}

			var newOverride = (Transform)EditorGUILayout.ObjectField(
				YanKLocalization.L("scBoneOverride", "Custom Target (override)"),
				sc.boneTargetOverride, typeof(Transform), true);
			if (newOverride != sc.boneTargetOverride)
			{
				Undo.RecordObject(sc, "Change Bone Override");
				sc.boneTargetOverride = newOverride;
			}
		}

		// ========================================================================
		// Custom camera picker — flat searchable list + Add / Remove.
		// ========================================================================

		private void DrawCustomCameras()
		{
			EditorGUILayout.LabelField(YanKLocalization.L("scCustomCameras", "Custom Cameras"), EditorStyles.boldLabel);

			sc.RefreshSceneCustomCameras();

			DrawCustomCameraPickerRow();

			GUILayout.Space(4);

			// ---- Add-name entry row (only shown while Add is open) ----
			if (_camWorkflow.addOpen)
			{
				var act = SceneControllerPresetUI.DrawAddRow(ref _camWorkflow.addName);
				if (act == SceneControllerPresetUI.AddAction.Save)
				{
					SaveNewCameraPreset(_camWorkflow.addName.Trim());
					_camWorkflow.addOpen = false;
					GUIUtility.ExitGUI();
				}
				else if (act == SceneControllerPresetUI.AddAction.Cancel)
				{
					_camWorkflow.addOpen = false;
					GUIUtility.ExitGUI();
				}
			}

			if (_camWorkflow.removeOpen && !_camWorkflow.addOpen) DrawCamRemovePanel();
		}

		private void DrawCustomCameraPickerRow()
		{
			string currentLabel = string.IsNullOrEmpty(sc.activeCustomCameraName)
				? SceneController.DefaultCameraName
				: sc.activeCustomCameraName;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(YanKLocalization.L("scActiveCam", "Active"), GUILayout.Width(60));

			var items = BuildCustomCameraItems();

			YanKSearchableDropdown.Field(currentLabel, items, HandleCameraPick, GUILayout.ExpandWidth(true));

			if (GUILayout.Button(YanKLocalization.L("scPing", "Ping"), EditorStyles.miniButton, GUILayout.Width(48)))
			{
				var go = GetActiveCameraGo();
				if (go != null) EditorGUIUtility.PingObject(go);
			}

			// Add / Remove buttons live on the right of Ping per user request.
			if (!_camWorkflow.addOpen)
			{
				if (GUILayout.Button(YanKLocalization.L("scAddNewPreset", "Add New"),
					EditorStyles.miniButton, GUILayout.Width(70)))
				{
					var sceneNames = new List<string>(sc.sceneCustomCameras.Count);
					foreach (var e in sc.sceneCustomCameras) if (e != null) sceneNames.Add(e.name);
					_camWorkflow.OpenAdd(
						SceneControllerPresets.NextDefaultCameraPresetName("NewCamera", sceneNames));
				}

				using (new EditorGUI.DisabledScope(_camWorkflow.addOpen))
				{
					if (GUILayout.Button(YanKLocalization.L("scRemove", "Remove"),
						EditorStyles.miniButton, GUILayout.Width(70)))
					{
						_camWorkflow.ToggleRemove();
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private List<YanKSearchableDropdown.Item> BuildCustomCameraItems()
		{
			// O(n+m) dedup via hashset — the previous O(n*m) List.Exists was noticeable
			// with many scene cameras.
			var sceneNames = new HashSet<string>();
			foreach (var entry in sc.sceneCustomCameras)
				if (entry != null && !string.IsNullOrEmpty(entry.name)) sceneNames.Add(entry.name);

			var items = new List<YanKSearchableDropdown.Item>(1 + sc.sceneCustomCameras.Count + GlobalCamPresets.Count)
			{
				new YanKSearchableDropdown.Item
				{
					label = SceneController.DefaultCameraName,
					payload = new CamPick { kind = CamPickKind.Default }
				}
			};
			foreach (var entry in sc.sceneCustomCameras)
			{
				items.Add(new YanKSearchableDropdown.Item
				{
					label = entry.name,
					payload = new CamPick { kind = CamPickKind.Scene, name = entry.name }
				});
			}
			foreach (var p in GlobalCamPresets)
			{
				if (p == null || sceneNames.Contains(p.name)) continue;
				items.Add(new YanKSearchableDropdown.Item
				{
					label = p.name,
					payload = new CamPick { kind = CamPickKind.Global, name = p.name }
				});
			}
			return items;
		}

		private void HandleCameraPick(object payload)
		{
			var pick = (CamPick)payload;
			Undo.RecordObject(sc, "Switch Active Camera");
			switch (pick.kind)
			{
				case CamPickKind.Default:
					sc.activeCustomCameraName = "";
					break;
				case CamPickKind.Scene:
					sc.activeCustomCameraName = pick.name;
					break;
				case CamPickKind.Global:
					var preset = GlobalCamPresets.Find(pp => pp.name == pick.name);
					if (preset != null)
					{
						InstantiatePresetAsSceneCamera(preset);
						sc.activeCustomCameraName = preset.name;
					}
					break;
			}
			ApplyActiveCamera();
		}

		private void DrawCamRemovePanel()
		{
			// Build a single flat list of removable names: scene cameras first,
			// then any global presets that don't have a matching scene camera.
			var sceneNames = new HashSet<string>();
			foreach (var entry in sc.sceneCustomCameras)
				if (entry != null && !string.IsNullOrEmpty(entry.name)) sceneNames.Add(entry.name);

			var removable = new List<SceneControllerPresetUI.RemoveEntry<string>>();
			foreach (var entry in sc.sceneCustomCameras)
			{
				if (entry == null || string.IsNullOrEmpty(entry.name)) continue;
				removable.Add(new SceneControllerPresetUI.RemoveEntry<string>(entry.name, entry.name));
			}
			foreach (var p in GlobalCamPresets)
			{
				if (p == null || string.IsNullOrEmpty(p.name)) continue;
				if (sceneNames.Contains(p.name)) continue;
				removable.Add(new SceneControllerPresetUI.RemoveEntry<string>(p.name, p.name));
			}

			var action = SceneControllerPresetUI.DrawRemovePanel(
				removable, _camWorkflow.removeSel, out var toRemove);

			if (action == SceneControllerPresetUI.RemoveAction.Confirm)
			{
				foreach (var n in toRemove)
				{
					// Destroy the matching scene camera (if any).
					var ent = sc.sceneCustomCameras.Find(e => e != null && e.name == n);
					if (ent?.gameObject != null)
					{
						if (sc.activeCustomCameraName == ent.name) sc.activeCustomCameraName = "";
						Undo.DestroyObjectImmediate(ent.gameObject);
					}
					// Always strip the global preset so it doesn't re-appear.
					SceneControllerPresets.RemoveCameraPreset(n);
				}
				_globalCamPresets = null; // force lazy reload
				sc.RefreshSceneCustomCameras();
				_camWorkflow.CloseAll();
				GUIUtility.ExitGUI();
			}
			else if (action == SceneControllerPresetUI.RemoveAction.Cancel)
			{
				_camWorkflow.CloseAll();
			}
		}

		// ========================================================================
		// Helpers
		// ========================================================================

		private GameObject GetActiveCameraGo()
		{
			if (string.IsNullOrEmpty(sc.activeCustomCameraName)) return sc.defaultCameraGo;
			var ent = sc.sceneCustomCameras.Find(e => e.name == sc.activeCustomCameraName);
			return ent?.gameObject;
		}

		private void ApplyActiveCamera()
		{
			if (sc.defaultCameraGo != null)
				sc.defaultCameraGo.SetActive(string.IsNullOrEmpty(sc.activeCustomCameraName));

			foreach (var entry in sc.sceneCustomCameras)
			{
				if (entry?.gameObject == null) continue;
				entry.gameObject.SetActive(entry.name == sc.activeCustomCameraName);
			}
		}

		private void SaveNewCameraPreset(string requested)
		{
			// Auto-rename on duplicate unless the user typed an EXACT existing name
			// (in which case we overwrite silently).
			var list = SceneControllerPresets.LoadCameraPresets();
			bool overwrite = list.Exists(p => p.name == requested);
			string finalName = overwrite ? requested : SceneControllerPresets.MakeUniqueCameraPresetName(requested);

			// Capture from currently active camera if any, otherwise from defaults.
			var activeGo = GetActiveCameraGo();
			Transform src = activeGo != null ? activeGo.transform : sc.defaultCameraGo?.transform;
			float fov = sc.cameraFov;
			if (activeGo != null)
			{
				var cam = activeGo.GetComponent<Camera>();
				if (cam != null) fov = cam.fieldOfView;
			}

			var preset = new SceneControllerPresets.CameraPreset
			{
				name = finalName,
				localPosition = src != null ? src.localPosition : Vector3.zero,
				localEulerAngles = src != null ? src.localEulerAngles : Vector3.zero,
				fieldOfView = fov
			};
			SceneControllerPresets.AddCameraPreset(preset);
			_globalCamPresets = null; // force lazy reload
		}

		private GameObject InstantiatePresetAsSceneCamera(SceneControllerPresets.CameraPreset preset)
		{
			SceneControllerRig.EnsureCameraRig(sc);
			var existing = sc.cameraPivot.Find(preset.name);
			if (existing != null) return existing.gameObject;

			var go = new GameObject(preset.name);
			Undo.RegisterCreatedObjectUndo(go, "Instantiate Camera Preset");
			go.transform.SetParent(sc.cameraPivot, false);
			go.transform.localPosition = preset.localPosition;
			go.transform.localEulerAngles = preset.localEulerAngles;

			var cam = go.AddComponent<Camera>();
			cam.fieldOfView = preset.fieldOfView;
			if (PostProcessingReflection.IsAvailable)
				PostProcessingReflection.EnsureLayer(go, 0);

			sc.RefreshSceneCustomCameras();
			return go;
		}

		private enum CamPickKind { Default, Scene, Global }
		private struct CamPick { public CamPickKind kind; public string name; }
	}
}
