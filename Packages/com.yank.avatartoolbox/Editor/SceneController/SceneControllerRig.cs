using UnityEditor;
using UnityEngine;

namespace YanK
{
	/// <summary>
	/// Idempotent rig builder for the SceneController GameObject hierarchy.
	/// Custom cameras are parented to <c>cameraPivot</c> so they follow the avatar
	/// with their relative offset preserved.
	/// </summary>
	internal static class SceneControllerRig
	{
		private const string DebugFloorName = "DebugFloor";

		public static void Build(SceneController sc)
		{
			if (sc == null) return;
			EnsureCameraRig(sc);
			EnsureDefaultMaterials(sc);
		}

		// ---------- Materials ----------

		public static void EnsureDefaultMaterials(SceneController sc)
		{
			EnsureFolder(SceneControllerConstants.MaterialFolder);
			if (sc.skyboxProceduralMaterial == null)
				sc.skyboxProceduralMaterial = LoadOrCreateMaterial(SceneControllerConstants.SkyboxProcMatPath, "Skybox/Procedural");
		}

		private static Material LoadOrCreateMaterial(string path, string shaderName)
		{
			var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (mat != null) return mat;

			var shader = Shader.Find(shaderName);
			if (shader == null) return null;

			mat = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
			AssetDatabase.CreateAsset(mat, path);
			AssetDatabase.SaveAssets();
			return mat;
		}

		private static void EnsureFolder(string assetFolder)
		{
			if (AssetDatabase.IsValidFolder(assetFolder)) return;
			System.IO.Directory.CreateDirectory(assetFolder);
			AssetDatabase.Refresh();
		}

		// ---------- Camera ----------

		public static void EnsureCameraRig(SceneController sc)
		{
			if (sc.cameraPivot == null)
			{
				var existing = sc.transform.Find(SceneController.CameraPivotName);
				if (existing != null) sc.cameraPivot = existing;
			}
			if (sc.cameraPivot == null)
			{
				var pivotGo = new GameObject(SceneController.CameraPivotName);
				Undo.RegisterCreatedObjectUndo(pivotGo, "Create Camera Pivot");
				pivotGo.transform.SetParent(sc.transform, false);
				// Default to eye-level at world origin so orbit works without an avatar.
				pivotGo.transform.position = new Vector3(0f, 1f, 0f);
				sc.cameraPivot = pivotGo.transform;
			}

			if (sc.defaultCameraGo == null)
			{
				var t = sc.cameraPivot.Find(SceneController.DefaultCameraName);
				if (t != null) sc.defaultCameraGo = t.gameObject;
			}
			if (sc.defaultCameraGo == null)
			{
				var camGo = new GameObject(SceneController.DefaultCameraName);
				Undo.RegisterCreatedObjectUndo(camGo, "Create Default Camera");
				camGo.transform.SetParent(sc.cameraPivot, false);
				var cam = camGo.AddComponent<Camera>();
				cam.nearClipPlane = 0.01f;
				sc.defaultCameraGo = camGo;
			}

			// ---- FreeFlyCamera ----
			if (sc.freeFlyCamera == null)
			{
				var t = sc.cameraPivot.Find(SceneController.FreeFlyCamera);
				if (t != null) sc.freeFlyCamera = t.gameObject;
			}
			if (sc.freeFlyCamera == null)
			{
				var ffGo = new GameObject(SceneController.FreeFlyCamera);
				Undo.RegisterCreatedObjectUndo(ffGo, "Create FreeFly Camera");
				ffGo.transform.SetParent(sc.cameraPivot, false);
				ffGo.transform.localPosition = new Vector3(0f, 0f, -1.5f);
				var ffCam = ffGo.AddComponent<Camera>();
				ffCam.nearClipPlane = 0.01f;
				if (sc.defaultCameraGo != null)
				{
					var src = sc.defaultCameraGo.GetComponent<Camera>();
					if (src != null)
					{
						ffCam.clearFlags      = src.clearFlags;
						ffCam.backgroundColor = src.backgroundColor;
						ffCam.nearClipPlane   = src.nearClipPlane;
						ffCam.farClipPlane    = src.farClipPlane;
						ffCam.cullingMask     = src.cullingMask;
						ffCam.fieldOfView     = src.fieldOfView;
					}
				}
				// Start inactive — DefaultCamera is active in Orbit mode by default.
				ffGo.SetActive(false);
				sc.freeFlyCamera = ffGo;
			}

			if (PostProcessingReflection.IsAvailable)
			{
				PostProcessingReflection.EnsureLayer(sc.defaultCameraGo, 0);
				if (sc.freeFlyCamera != null)
					PostProcessingReflection.EnsureLayer(sc.freeFlyCamera, 0);
				foreach (var entry in sc.sceneCustomCameras)
				{
					if (entry?.gameObject != null)
						PostProcessingReflection.EnsureLayer(entry.gameObject, 0);
				}
			}
		}

		// ---------- Directional light ----------

		public static void EnsureDirectionalLight(SceneController sc)
		{
			if (sc.directionalLight == null)
			{
				var t = sc.transform.Find(SceneController.DirLightName);
				if (t != null) sc.directionalLight = t.GetComponent<Light>();
			}
			if (sc.directionalLight == null)
			{
				var go = new GameObject(SceneController.DirLightName);
				Undo.RegisterCreatedObjectUndo(go, "Create Directional Light");
				go.transform.SetParent(sc.transform, false);
				var l = go.AddComponent<Light>();
				l.type = LightType.Directional;
				l.color = sc.dirLightColor;
				l.intensity = sc.dirLightIntensity;
				l.shadows = sc.dirLightShadows;
				l.shadowStrength = sc.dirLightShadowStrength;
				sc.directionalLight = l;
			}
			sc.directionalLight.gameObject.SetActive(true);
		}

		public static void DestroyDirectionalLight(SceneController sc)
		{
			if (sc.directionalLight != null)
			{
				Undo.DestroyObjectImmediate(sc.directionalLight.gameObject);
				sc.directionalLight = null;
			}
		}

		// ---------- Point lights ----------

		public static void EnsurePointLights(SceneController sc)
		{
			if (sc.pointLightsRoot == null)
			{
				var t = sc.transform.Find(SceneController.PointLightsRootName);
				if (t != null) sc.pointLightsRoot = t;
			}
			if (sc.pointLightsRoot == null)
			{
				var go = new GameObject(SceneController.PointLightsRootName);
				Undo.RegisterCreatedObjectUndo(go, "Create Rotating Point Lights");
				go.transform.SetParent(sc.transform, false);
				sc.pointLightsRoot = go.transform;
			}

			if (sc.pointLights == null || sc.pointLights.Length != 5)
				sc.pointLights = new Light[5];

			for (int i = 0; i < 5; i++)
			{
				if (sc.pointLights[i] != null) continue;

				string name = $"PointLight_{i}";
				var existing = sc.pointLightsRoot.Find(name);
				if (existing != null)
				{
					sc.pointLights[i] = existing.GetComponent<Light>();
					continue;
				}

				var go = new GameObject(name);
				Undo.RegisterCreatedObjectUndo(go, "Create Point Light");
				go.transform.SetParent(sc.pointLightsRoot, false);
				float a = i * (Mathf.PI * 2f / 5f);
				go.transform.localPosition = new Vector3(
					Mathf.Cos(a) * sc.pointLightsRadius, 0f, Mathf.Sin(a) * sc.pointLightsRadius);
				var l = go.AddComponent<Light>();
				l.type = LightType.Point;
				l.range = sc.pointLightsRadius * 2.5f;
				l.color = sc.pointLightColors[i];
				l.intensity = sc.pointLightIntensities[i];
				l.shadows = sc.pointLightsShadows;
				l.shadowStrength = sc.pointLightsShadowStrength;
				sc.pointLights[i] = l;
			}
			sc.pointLightsRoot.gameObject.SetActive(true);
		}

		public static void DestroyPointLights(SceneController sc)
		{
			if (sc.pointLightsRoot != null)
			{
				Undo.DestroyObjectImmediate(sc.pointLightsRoot.gameObject);
				sc.pointLightsRoot = null;
			}
			sc.pointLights = new Light[5];
		}

		public static void RefreshPointLightPositions(SceneController sc)
		{
			if (sc.pointLightsRoot == null) return;
			for (int i = 0; i < sc.pointLights.Length; i++)
			{
				var l = sc.pointLights[i];
				if (l == null) continue;
				float a = i * (Mathf.PI * 2f / 5f);
				l.transform.localPosition = new Vector3(
					Mathf.Cos(a) * sc.pointLightsRadius, 0f, Mathf.Sin(a) * sc.pointLightsRadius);
				l.range = sc.pointLightsRadius * 2.5f;
				l.color = sc.pointLightColors[i];
				l.intensity = sc.pointLightIntensities[i];
				l.shadows = sc.pointLightsShadows;
				l.shadowStrength = sc.pointLightsShadowStrength;
			}
		}

		// ---------- Post processing ----------

		public static void EnsurePostProcessVolume(SceneController sc)
		{
			if (!PostProcessingReflection.IsAvailable) return;

			if (sc.postProcessVolumeGo == null)
			{
				var t = sc.transform.Find(SceneController.PostProcessVolumeName);
				if (t != null) sc.postProcessVolumeGo = t.gameObject;
			}
			if (sc.postProcessVolumeGo == null)
			{
				var go = new GameObject(SceneController.PostProcessVolumeName);
				Undo.RegisterCreatedObjectUndo(go, "Create Post Process Volume");
				go.transform.SetParent(sc.transform, false);
				PostProcessingReflection.AddVolume(go, sc.postProcessingProfile);
				sc.postProcessVolumeGo = go;
			}
			// Always (re)assign the profile — selection may have changed since creation.
			var vol = sc.postProcessVolumeGo.GetComponent(PostProcessingReflection.VolumeType);
			if (vol != null)
				PostProcessingReflection.SetVolumeProfile(vol, sc.postProcessingProfile);

			if (sc.defaultCameraGo != null)
				PostProcessingReflection.EnsureLayer(sc.defaultCameraGo, 0);
			foreach (var entry in sc.sceneCustomCameras)
			{
				if (entry?.gameObject != null)
					PostProcessingReflection.EnsureLayer(entry.gameObject, 0);
			}

			sc.postProcessVolumeGo.SetActive(true);
		}

		public static void SetPostProcessVolumeActive(SceneController sc, bool active)
		{
			if (sc.postProcessVolumeGo == null) return;
			Undo.RecordObject(sc.postProcessVolumeGo, "Toggle Post Process Volume");
			sc.postProcessVolumeGo.SetActive(active);
		}

		public static void DestroyPostProcessVolume(SceneController sc)
		{
			if (sc.postProcessVolumeGo != null)
			{
				Undo.DestroyObjectImmediate(sc.postProcessVolumeGo);
				sc.postProcessVolumeGo = null;
			}
		}

		// ---------- Debug floor ----------

		public static void EnsureDebugFloor(SceneController sc)
		{
			if (sc.debugFloorGo == null)
			{
				var existing = sc.transform.Find(DebugFloorName);
				if (existing != null) sc.debugFloorGo = existing.gameObject;
			}

			if (sc.debugFloorGo == null)
			{
				var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
				Undo.RegisterCreatedObjectUndo(go, "Create Debug Floor");
				go.name = DebugFloorName;
				go.transform.SetParent(sc.transform, false);
				go.transform.localScale = new Vector3(100f, 0.02f, 100f);
				go.transform.localPosition = Vector3.zero;
				sc.debugFloorGo = go;
			}

			var mr = sc.debugFloorGo.GetComponent<MeshRenderer>();
			if (mr != null)
			{
				EnsureFolder(SceneControllerConstants.MaterialFolder);
				var shipped = AssetDatabase.LoadAssetAtPath<Material>(SceneControllerConstants.DebugFloorMatPath);
				if (shipped == null)
				{
					// Only create the shipped asset when our dedicated shader is available —
					// avoid persisting a wrong-shader asset that would stick around.
					var shader = Shader.Find(SceneControllerConstants.DebugFloorShaderName);
					if (shader != null)
					{
						shipped = new Material(shader) { name = "YSC_DebugFloor" };
						AssetDatabase.CreateAsset(shipped, SceneControllerConstants.DebugFloorMatPath);
						AssetDatabase.SaveAssets();
					}
				}
				if (shipped != null)
				{
					mr.sharedMaterial = shipped;
				}
				else if (mr.sharedMaterial == null)
				{
					Debug.LogWarning("[SceneController] Debug floor shader '" + SceneControllerConstants.DebugFloorShaderName +
						"' not found; falling back to Standard shader.");
					var std = Shader.Find("Standard");
					if (std != null) mr.sharedMaterial = new Material(std) { name = "YSC_DebugFloor (runtime)" };
				}
			}

			ApplyDebugFloorColors(sc);
			sc.debugFloorGo.SetActive(true);
		}

		public static void DestroyDebugFloor(SceneController sc)
		{
			if (sc.debugFloorGo != null)
			{
				Undo.DestroyObjectImmediate(sc.debugFloorGo);
				sc.debugFloorGo = null;
			}
		}

		public static void ApplyDebugFloorColors(SceneController sc)
		{
			if (sc.debugFloorGo == null) return;
			var mr = sc.debugFloorGo.GetComponent<MeshRenderer>();
			if (mr == null || mr.sharedMaterial == null) return;
			var m = mr.sharedMaterial;
			if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", sc.debugFloorBaseColor);
			if (m.HasProperty("_LineColor")) m.SetColor("_LineColor", sc.debugFloorLineColor);
		}

		// ---------- Recommended defaults ----------

		/// <summary>
		/// Yan-K's recommended defaults applied when the user enables Scene Settings:
		/// grey Color skybox (R50 G50 B50), linear fog 10→50 on, Directional Light at
		/// the "Normal" preset (H 120, V 50), and the debug floor on.
		/// </summary>
		public static void ApplyRecommendedDefaults(SceneController sc)
		{
			if (sc == null) return;

			EnsureDefaultMaterials(sc);

			sc.skyboxEnabled = true;
			sc.skyboxKind = SkyboxKind.Color;
			sc.skyboxColor = SceneControllerConstants.DarkBase;

			sc.fogEnabled = true;
			sc.fogMode = FogMode.Linear;
			sc.fogColor = SceneControllerConstants.DarkBase;
			sc.fogLinearStart = 8f;
			sc.fogLinearEnd = 30f;
			RenderSettings.fog = true;
			RenderSettings.fogMode = FogMode.Linear;
			RenderSettings.fogColor = sc.fogColor;
			RenderSettings.fogStartDistance = sc.fogLinearStart;
			RenderSettings.fogEndDistance = sc.fogLinearEnd;

			sc.dirLightEnabled = true;
			sc.dirLightHorizontal = 120f;
			sc.dirLightVertical = 50f;
			EnsureDirectionalLight(sc);

			sc.debugFloorEnabled = true;
			sc.debugFloorBaseColor = SceneControllerConstants.DarkBase;
			sc.debugFloorLineColor = SceneControllerConstants.DarkLine;
			EnsureDebugFloor(sc);

			sc.ApplySkybox();
		}
	}
}
