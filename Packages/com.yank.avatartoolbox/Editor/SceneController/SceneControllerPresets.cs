using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YanK
{
	/// <summary>
	/// EditorPrefs-backed JSON storage for cross-scene SceneController presets.
	/// Per-machine, per-user storage (not shared with team).
	/// </summary>
	internal static class SceneControllerPresets
	{
		private const string CameraKey = "YSC_CustomCameraPresets";
		private const string DirLightKey = "YSC_DirLightPresets";

		// ---------- Camera presets ----------

		[Serializable]
		public class CameraPreset
		{
			public string name;
			public Vector3 localPosition;
			public Vector3 localEulerAngles;
			public float fieldOfView = 60f;
		}

		[Serializable]
		private class CameraPresetList
		{
			public List<CameraPreset> items = new List<CameraPreset>();
		}

		public static List<CameraPreset> LoadCameraPresets()
		{
			string json = EditorPrefs.GetString(CameraKey, "");
			if (string.IsNullOrEmpty(json)) return new List<CameraPreset>();
			try
			{
				var wrap = JsonUtility.FromJson<CameraPresetList>(json);
				return wrap?.items ?? new List<CameraPreset>();
			}
			catch
			{
				return new List<CameraPreset>();
			}
		}

		public static void SaveCameraPresets(List<CameraPreset> items)
		{
			var wrap = new CameraPresetList { items = items ?? new List<CameraPreset>() };
			EditorPrefs.SetString(CameraKey, JsonUtility.ToJson(wrap));
		}

		public static void AddCameraPreset(CameraPreset preset)
		{
			if (preset == null || string.IsNullOrWhiteSpace(preset.name)) return;
			var list = LoadCameraPresets();
			// Replace if name exists.
			int idx = list.FindIndex(p => p.name == preset.name);
			if (idx >= 0) list[idx] = preset;
			else list.Add(preset);
			SaveCameraPresets(list);
		}

		public static void RemoveCameraPreset(string name)
		{
			var list = LoadCameraPresets();
			list.RemoveAll(p => p.name == name);
			SaveCameraPresets(list);
		}

		/// <summary>Returns a unique camera-preset name by suffixing 1, 2, 3… as needed.</summary>
		public static string MakeUniqueCameraPresetName(string desired)
		{
			if (string.IsNullOrWhiteSpace(desired)) desired = "Camera";
			var list = LoadCameraPresets();
			if (!list.Exists(p => p.name == desired)) return desired;
			int i = 1;
			while (list.Exists(p => p.name == desired + i)) i++;
			return desired + i;
		}

		/// <summary>
		/// Default name for the "Add New Preset" text field. Always suffixed with
		/// the next free integer so repeatedly clicking Add produces "NewCamera1",
		/// "NewCamera2", … regardless of whether the bare base name is free.
		/// </summary>
		public static string NextDefaultCameraPresetName(string baseName,
			IEnumerable<string> extraExisting = null)
		{
			if (string.IsNullOrWhiteSpace(baseName)) baseName = "NewCamera";
			var list = LoadCameraPresets();
			var taken = new HashSet<string>();
			foreach (var p in list) if (p != null && !string.IsNullOrEmpty(p.name)) taken.Add(p.name);
			if (extraExisting != null)
				foreach (var n in extraExisting) if (!string.IsNullOrEmpty(n)) taken.Add(n);
			int i = 1;
			while (taken.Contains(baseName + i)) i++;
			return baseName + i;
		}

		// ---------- Directional light H/V presets ----------

		[Serializable]
		public class DirLightPreset
		{
			public string name;
			public float horizontal; // yaw degrees
			public float vertical;   // pitch degrees
		}

		[Serializable]
		private class DirLightPresetList
		{
			public List<DirLightPreset> items = new List<DirLightPreset>();
		}

		public static List<DirLightPreset> LoadDirLightPresets()
		{
			string json = EditorPrefs.GetString(DirLightKey, "");
			if (string.IsNullOrEmpty(json)) return new List<DirLightPreset>();
			try
			{
				var wrap = JsonUtility.FromJson<DirLightPresetList>(json);
				return wrap?.items ?? new List<DirLightPreset>();
			}
			catch
			{
				return new List<DirLightPreset>();
			}
		}

		public static void SaveDirLightPresets(List<DirLightPreset> items)
		{
			var wrap = new DirLightPresetList { items = items ?? new List<DirLightPreset>() };
			EditorPrefs.SetString(DirLightKey, JsonUtility.ToJson(wrap));
		}

		public static void AddDirLightPreset(DirLightPreset preset)
		{
			if (preset == null || string.IsNullOrWhiteSpace(preset.name)) return;
			var list = LoadDirLightPresets();
			int idx = list.FindIndex(p => p.name == preset.name);
			if (idx >= 0) list[idx] = preset;
			else list.Add(preset);
			SaveDirLightPresets(list);
		}

		public static void RemoveDirLightPreset(string name)
		{
			var list = LoadDirLightPresets();
			list.RemoveAll(p => p.name == name);
			SaveDirLightPresets(list);
		}

		/// <summary>Returns a unique dir-light-preset name considering user + built-in entries.</summary>
		public static string MakeUniqueDirLightPresetName(string desired)
		{
			if (string.IsNullOrWhiteSpace(desired)) desired = "Lighting";
			var user = LoadDirLightPresets();
			var builtIns = GetBuiltInDirLightPresets();
			bool Taken(string n) => user.Exists(p => p.name == n) || builtIns.Exists(p => p.name == n);
			if (!Taken(desired)) return desired;
			int i = 1;
			while (Taken(desired + i)) i++;
			return desired + i;
		}

		/// <summary>Next-free auto-numbered default name for dir-light presets.</summary>
		public static string NextDefaultDirLightPresetName(string baseName)
		{
			if (string.IsNullOrWhiteSpace(baseName)) baseName = "MyLight";
			var user = LoadDirLightPresets();
			var builtIns = GetBuiltInDirLightPresets();
			bool Taken(string n) => user.Exists(p => p.name == n) || builtIns.Exists(p => p.name == n);
			int i = 1;
			while (Taken(baseName + i)) i++;
			return baseName + i;
		}

		// ---------- Built-in directional-light presets ----------

		/// <summary>
		/// Built-in presets that are always available. User presets with the same
		/// <see cref="DirLightPreset.name"/> override the built-in entry.
		/// </summary>
		public static List<DirLightPreset> GetBuiltInDirLightPresets()
		{
			return new List<DirLightPreset>
			{
				new DirLightPreset { name = "Normal",     horizontal = 120f, vertical = 50f },
				new DirLightPreset { name = "Backlight",  horizontal = 0f,   vertical = 15f },
				new DirLightPreset { name = "Frontlight", horizontal = 90f,  vertical = 0f  },
			};
		}

		/// <summary>Returns built-ins and user presets merged; user entries override by name.</summary>
		public static List<DirLightPreset> LoadMergedDirLightPresets(out System.Collections.Generic.HashSet<string> builtInNames)
		{
			var builtIns = GetBuiltInDirLightPresets();
			builtInNames = new System.Collections.Generic.HashSet<string>();
			foreach (var b in builtIns) builtInNames.Add(b.name);

			var user = LoadDirLightPresets();
			var userNames = new System.Collections.Generic.HashSet<string>();
			foreach (var u in user) if (!string.IsNullOrEmpty(u.name)) userNames.Add(u.name);

			var merged = new List<DirLightPreset>();
			foreach (var b in builtIns)
			{
				if (userNames.Contains(b.name)) continue; // overridden below
				merged.Add(b);
			}
			merged.AddRange(user);
			return merged;
		}

		public static bool IsBuiltInDirLightPreset(string name)
		{
			foreach (var b in GetBuiltInDirLightPresets())
				if (b.name == name) return true;
			return false;
		}
	}
}
