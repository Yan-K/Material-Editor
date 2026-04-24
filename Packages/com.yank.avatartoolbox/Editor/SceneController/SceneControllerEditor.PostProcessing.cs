using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YanK
{
	public partial class SceneControllerEditor
	{
		// Cache of post-processing profile assets in the shipped folder.
		// Nullable — lazy-reloaded on first read and after mutation.
		private List<ScriptableObject> _ppProfiles;
		private List<ScriptableObject> PpProfiles => _ppProfiles ??= LoadProfiles();

		// Add/Remove workflow state (shared pattern with camera / dir-light).
		private readonly PresetWorkflow<ScriptableObject> _ppWorkflow = new PresetWorkflow<ScriptableObject>();

		private void OnEnablePostProcessing() => _ppProfiles = null;

		private void ReloadProfiles() => _ppProfiles = null;

		private void DrawPostProcessingSection()
		{
			GUILayout.Space(4);
			EditorGUILayout.LabelField(
				YanKLocalization.L("scPostSection", "Post Processing"), EditorStyles.boldLabel);

			if (!PostProcessingReflection.IsAvailable)
			{
				EditorGUILayout.HelpBox(
					YanKLocalization.L("scPpMissingHelp",
						"Post Processing package is not installed. Click below to install com.unity.postprocessing."),
					MessageType.Warning);
				if (GUILayout.Button(YanKLocalization.L("scInstallPp", "Install Post Processing")))
				{
					UnityEditor.PackageManager.Client.Add("com.unity.postprocessing");
				}
				return;
			}

			bool newEnabled = EditorGUILayout.ToggleLeft(
				YanKLocalization.L("scPpEnable", "Enable Post Processing"), sc.postProcessingEnabled);
			if (newEnabled != sc.postProcessingEnabled)
			{
				Undo.RecordObject(sc, "Toggle Post Processing");
				sc.postProcessingEnabled = newEnabled;
				if (newEnabled)
				{
					var profiles = PpProfiles;
					if (sc.postProcessingProfile == null && profiles.Count > 0)
						sc.postProcessingProfile = profiles[0];
					SceneControllerRig.EnsurePostProcessVolume(sc);
				}
				else
				{
					SceneControllerRig.SetPostProcessVolumeActive(sc, false);
				}
			}

			using (new EditorGUI.DisabledScope(!sc.postProcessingEnabled))
			{
				DrawProfileRow();
			}
		}

		private void DrawProfileRow()
		{
			var profiles = PpProfiles;

			string currentLabel = sc.postProcessingProfile != null
				? sc.postProcessingProfile.name
				: YanKLocalization.L("scPickPreset", "— Pick preset —");

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(YanKLocalization.L("scProfile", "Profile"), GUILayout.Width(60));

			// Flat list — no sub-group / sub-folder labels per user spec.
			var items = new List<YanKSearchableDropdown.Item>(profiles.Count);
			foreach (var p in profiles)
			{
				items.Add(new YanKSearchableDropdown.Item { label = p.name, payload = p });
			}

			YanKSearchableDropdown.Field(currentLabel, items, payload =>
			{
				var profile = (ScriptableObject)payload;
				Undo.RecordObject(sc, "Change PP Profile");
				sc.postProcessingProfile = profile;
				// Rebuild / reassign the volume through the rig helper so the profile
				// is guaranteed to be applied to the live component.
				SceneControllerRig.EnsurePostProcessVolume(sc);
				// NOTE: Intentionally no PingObject() — switching profile in the UI
				// must not yank focus into the Project view.
			}, GUILayout.ExpandWidth(true));

			if (GUILayout.Button(YanKLocalization.L("scPing", "Ping"),
				EditorStyles.miniButton, GUILayout.Width(48)))
			{
				if (sc.postProcessingProfile != null)
					EditorGUIUtility.PingObject(sc.postProcessingProfile);
			}

			// Add / Remove buttons live on the right of Ping per user request.
			if (!_ppWorkflow.addOpen)
			{
				if (GUILayout.Button(YanKLocalization.L("scAddNewPreset", "Add New"),
					EditorStyles.miniButton, GUILayout.Width(70)))
				{
					_ppWorkflow.OpenAdd(NextDefaultProfileName("NewProfile"));
				}

				using (new EditorGUI.DisabledScope(_ppWorkflow.addOpen))
				{
					if (GUILayout.Button(YanKLocalization.L("scRemove", "Remove"),
						EditorStyles.miniButton, GUILayout.Width(70)))
					{
						_ppWorkflow.ToggleRemove();
					}
				}
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(4);

			if (_ppWorkflow.addOpen)
			{
				var act = SceneControllerPresetUI.DrawAddRow(ref _ppWorkflow.addName);
				if (act == SceneControllerPresetUI.AddAction.Save)
				{
					AddBlankProfile(_ppWorkflow.addName.Trim());
					_ppWorkflow.addOpen = false;
					GUIUtility.ExitGUI();
				}
				else if (act == SceneControllerPresetUI.AddAction.Cancel)
				{
					_ppWorkflow.addOpen = false;
				}
			}

			if (_ppWorkflow.removeOpen && !_ppWorkflow.addOpen) DrawPpRemovePanel();
		}

		private void DrawPpRemovePanel()
		{
			var profiles = PpProfiles;
			var removable = new List<SceneControllerPresetUI.RemoveEntry<ScriptableObject>>(profiles.Count);
			foreach (var p in profiles)
			{
				if (p == null) continue;
				removable.Add(new SceneControllerPresetUI.RemoveEntry<ScriptableObject>(p, p.name));
			}

			var action = SceneControllerPresetUI.DrawRemovePanel(
				removable, _ppWorkflow.removeSel, out var toRemove,
				YanKLocalization.L("scPpNoProfiles", "No profiles to remove."));

			if (action == SceneControllerPresetUI.RemoveAction.Confirm)
			{
				bool activeProfileRemoved = toRemove.Contains(sc.postProcessingProfile);
				foreach (var p in toRemove)
				{
					if (p == null) continue;
					var path = AssetDatabase.GetAssetPath(p);
					if (!string.IsNullOrEmpty(path)) AssetDatabase.DeleteAsset(path);
				}
				AssetDatabase.Refresh();
				ReloadProfiles();

				if (activeProfileRemoved)
				{
					var remaining = PpProfiles;
					sc.postProcessingProfile = remaining.Count > 0 ? remaining[0] : null;
					if (sc.postProcessingEnabled) SceneControllerRig.EnsurePostProcessVolume(sc);
				}

				_ppWorkflow.CloseAll();
				GUIUtility.ExitGUI();
			}
			else if (action == SceneControllerPresetUI.RemoveAction.Cancel)
			{
				_ppWorkflow.CloseAll();
			}
		}

		// ---------------- Profile asset ops ----------------

		private static List<ScriptableObject> LoadProfiles()
		{
			var result = new List<ScriptableObject>();
			if (!PostProcessingReflection.IsAvailable) return result;

			if (!AssetDatabase.IsValidFolder(SceneControllerConstants.ProfileFolder))
			{
				Directory.CreateDirectory(SceneControllerConstants.ProfileFolder);
				AssetDatabase.Refresh();
			}

			var guids = AssetDatabase.FindAssets("t:" + PostProcessingReflection.ProfileType.Name,
				new[] { SceneControllerConstants.ProfileFolder });
			foreach (var g in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(g);
				var so = AssetDatabase.LoadAssetAtPath(path, PostProcessingReflection.ProfileType) as ScriptableObject;
				if (so != null) result.Add(so);
			}
			result.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
			return result;
		}

		private string MakeUniqueProfileName(string requested)
		{
			if (string.IsNullOrWhiteSpace(requested)) requested = "Profile";
			var profiles = PpProfiles;
			bool Exists(string n) => profiles.Exists(p => p != null && p.name == n);
			if (!Exists(requested)) return requested;
			int i = 1;
			while (Exists(requested + i)) i++;
			return requested + i;
		}

		/// <summary>Auto-numbered default name for the Add text field ("NewProfile1", "NewProfile2" …).</summary>
		private string NextDefaultProfileName(string baseName)
		{
			if (string.IsNullOrWhiteSpace(baseName)) baseName = "NewProfile";
			var profiles = PpProfiles;
			bool Exists(string n) => profiles.Exists(p => p != null && p.name == n);
			int i = 1;
			while (Exists(baseName + i)) i++;
			return baseName + i;
		}

		private void AddBlankProfile(string requested)
		{
			if (!PostProcessingReflection.IsAvailable) return;
			if (!AssetDatabase.IsValidFolder(SceneControllerConstants.ProfileFolder))
			{
				Directory.CreateDirectory(SceneControllerConstants.ProfileFolder);
				AssetDatabase.Refresh();
			}

			// Auto-rename on duplicate unless user typed an EXACT existing name (overwrite case).
			var profiles = PpProfiles;
			bool overwrite = profiles.Exists(p => p != null && p.name == requested);
			string finalName = overwrite ? requested : MakeUniqueProfileName(requested);

			string path = overwrite
				? AssetDatabase.GetAssetPath(profiles.Find(p => p.name == finalName))
				: AssetDatabase.GenerateUniqueAssetPath($"{SceneControllerConstants.ProfileFolder}/{finalName}.asset");

			if (!overwrite || string.IsNullOrEmpty(path))
			{
				var asset = ScriptableObject.CreateInstance(PostProcessingReflection.ProfileType);
				asset.name = finalName;
				AssetDatabase.CreateAsset(asset, path);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
			ReloadProfiles();

			var created = AssetDatabase.LoadAssetAtPath(path, PostProcessingReflection.ProfileType) as ScriptableObject;
			if (created != null)
			{
				Undo.RecordObject(sc, "Add PP Profile");
				sc.postProcessingProfile = created;
				if (sc.postProcessingEnabled)
					SceneControllerRig.EnsurePostProcessVolume(sc);
			}
		}
	}
}
