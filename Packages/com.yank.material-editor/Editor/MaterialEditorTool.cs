using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YanK
{
	public class MaterialEditorTool : EditorWindow
	{
		private const string Version = "v0.2.0";

		private GameObject rootObject;
		private readonly Dictionary<Material, List<Renderer>> materialToRenderers = new Dictionary<Material, List<Renderer>>();
		private readonly List<Material> materialDisplayOrder = new List<Material>();
		private Vector2 scrollPosition;
		private bool includeInactive;

		private readonly HashSet<Material> selectedMaterials = new HashSet<Material>();
		private bool selectAll;
		private Material batchReplaceMaterial;
		private readonly Dictionary<Material, Material> originalMaterials = new Dictionary<Material, Material>();
		private readonly Dictionary<Material, bool> materialFoldouts = new Dictionary<Material, bool>();

		private readonly List<string> availableLanguages = new List<string>();
		private int selectedLanguageIndex;
		private Dictionary<string, string> localizedStrings = new Dictionary<string, string>();

		private GUIStyle rightAlignBoldStyle;

		[MenuItem("Tools/Yan-K/Material Editor")]
		public static void ShowWindow()
		{
			GetWindow<MaterialEditorTool>("Yan-K Material Editor");
		}

		private void OnEnable()
		{
			RefreshLanguageFiles();
			LoadDefaultLanguage();
			LoadLocalizedStrings();
			Undo.undoRedoPerformed += OnUndoRedoPerformed;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
		}

		private void OnUndoRedoPerformed()
		{
			ScanMaterials();
			Repaint();
		}

		private void OnGUI()
		{
			if (rightAlignBoldStyle == null)
				rightAlignBoldStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleRight };

			DrawHeader();
			DrawRootObjectField();
			DrawControlPanel();
			DrawMaterialList();
		}

		private void DrawHeader()
		{
			GUILayout.Space(10);
			EditorGUILayout.BeginHorizontal();

			string title = GetLocalizedString("title", "Yan-K Material Editor (YME)") + "  " + Version;
			EditorGUILayout.LabelField(title, EditorStyles.boldLabel, GUILayout.ExpandWidth(true), GUILayout.MinWidth(300));

			GUILayout.FlexibleSpace();

			int newIndex = EditorGUILayout.Popup(selectedLanguageIndex, availableLanguages.ToArray(), GUILayout.Width(150));
			if (newIndex != selectedLanguageIndex)
			{
				selectedLanguageIndex = newIndex;
				EditorPrefs.SetInt("YME_Language", selectedLanguageIndex);
				LoadLocalizedStrings();
			}

			EditorGUILayout.EndHorizontal();
			GUILayout.Space(10);
			DrawSeparator();
		}

		private void DrawRootObjectField()
		{
			EditorGUILayout.LabelField(GetLocalizedString("rootObject", "Root Object"), EditorStyles.boldLabel);
			var newRoot = (GameObject)EditorGUILayout.ObjectField(rootObject, typeof(GameObject), true);
			if (newRoot != rootObject)
			{
				rootObject = newRoot;
				if (rootObject != null) ScanMaterials();
			}
			GUILayout.Space(5);
		}

		private void DrawControlPanel()
		{
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(GetLocalizedString("forceScan", "Rescan Materials"), GUILayout.Height(25)))
			{
				includeInactive = false;
				ScanMaterials();
			}
			if (GUILayout.Button(GetLocalizedString("forceScanInactive", "Scan Include Inactive"), GUILayout.Height(25)))
			{
				includeInactive = true;
				ScanMaterials();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);
			DrawSeparator();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(GetLocalizedString("cloneSelected", "Clone Selected"), GUILayout.Height(25)))
				BatchCloneSelected();
			if (GUILayout.Button(GetLocalizedString("replaceSelected", "Replace Selected"), GUILayout.Height(25)))
				BatchReplaceSelected();
			if (GUILayout.Button(GetLocalizedString("resetSelected", "Reset Selected"), GUILayout.Height(25)))
				BatchResetSelected();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(GetLocalizedString("batchReplaceMaterial", "Batch Replace Material"), GUILayout.Width(180));
			batchReplaceMaterial = (Material)EditorGUILayout.ObjectField(batchReplaceMaterial, typeof(Material), false);
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);
			DrawSeparator();
		}

		private void DrawMaterialList()
		{
			if (materialDisplayOrder.Count == 0)
			{
				EditorGUILayout.HelpBox(GetLocalizedString("noMaterials", "No materials found. Drop a GameObject in the Root Object field and scan again."), MessageType.Info);
				GUILayout.Space(10);
				return;
			}

			EditorGUILayout.BeginHorizontal();

			bool newSelectAll = EditorGUILayout.ToggleLeft(GetLocalizedString("selectAll", "Select All"), selectAll, GUILayout.Width(100));
			if (newSelectAll != selectAll)
			{
				selectAll = newSelectAll;
				if (selectAll)
				{
					foreach (var mat in materialDisplayOrder)
						selectedMaterials.Add(mat);
				}
				else
				{
					selectedMaterials.Clear();
				}
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.LabelField(GetLocalizedString("materialsInUse", "Materials in Use"), rightAlignBoldStyle);
			EditorGUILayout.EndHorizontal();

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

			foreach (var material in new List<Material>(materialDisplayOrder))
			{
				if (!materialToRenderers.TryGetValue(material, out var renderers)) continue;

				GUILayout.Space(5);
				EditorGUILayout.BeginVertical("box");
				DrawMaterialItem(material, renderers);
				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.EndScrollView();
			GUILayout.Space(10);
			DrawSeparator();
		}

		private void DrawMaterialItem(Material material, List<Renderer> renderers)
		{
			EditorGUILayout.BeginHorizontal();

			bool isSelected = selectedMaterials.Contains(material);
			bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
			if (newSelected != isSelected)
			{
				if (newSelected) selectedMaterials.Add(material);
				else selectedMaterials.Remove(material);
				UpdateSelectAllState();
			}

			var newMaterial = (Material)EditorGUILayout.ObjectField(material, typeof(Material), false);
			if (newMaterial != null && newMaterial != material)
				ReplaceMaterial(material, newMaterial);

			if (GUILayout.Button(GetLocalizedString("clone", "Clone"), GUILayout.Width(60)))
				CloneMaterial(material);

			if (GUILayout.Button(GetLocalizedString("reset", "Reset"), GUILayout.Width(60)))
				ResetMaterial(material);

			EditorGUILayout.EndHorizontal();

			if (!materialFoldouts.TryGetValue(material, out bool foldout))
				materialFoldouts[material] = foldout = false;

			bool newFoldout = EditorGUILayout.Foldout(foldout, string.Format(GetLocalizedString("usedBy", "Used by {0} renderer(s)"), renderers.Count), true);
			if (newFoldout != foldout)
				materialFoldouts[material] = newFoldout;

			if (newFoldout)
			{
				EditorGUI.indentLevel++;
				foreach (var renderer in renderers)
					EditorGUILayout.ObjectField(renderer, typeof(Renderer), true);
				EditorGUI.indentLevel--;
			}
		}

		private void RefreshLanguageFiles()
		{
			availableLanguages.Clear();

			foreach (var file in Resources.LoadAll<TextAsset>("YMELocalization"))
				availableLanguages.Add(Path.GetFileNameWithoutExtension(file.name));

			if (!availableLanguages.Contains("English"))
				availableLanguages.Insert(0, "English");
		}

		private void LoadDefaultLanguage()
		{
			selectedLanguageIndex = EditorPrefs.GetInt("YME_Language", availableLanguages.IndexOf("English"));
			if (selectedLanguageIndex < 0 || selectedLanguageIndex >= availableLanguages.Count)
				selectedLanguageIndex = 0;
		}

		private void LoadLocalizedStrings()
		{
			localizedStrings.Clear();
			if (availableLanguages.Count == 0) return;

			string lang = availableLanguages[selectedLanguageIndex];
			var jsonFile = Resources.Load<TextAsset>($"YMELocalization/{lang}");

			if (jsonFile != null)
			{
				try { localizedStrings = JsonUtility.FromJson<SerializableDictionary>(jsonFile.text).ToDictionary(); }
				catch { Debug.LogError($"Failed to parse localization file: {lang}"); }
			}
			else
			{
				Debug.LogWarning($"Localization file for {lang} not found.");
			}

			Repaint();
		}

		private void ScanMaterials()
		{
			materialToRenderers.Clear();
			materialDisplayOrder.Clear();
			originalMaterials.Clear();
			selectedMaterials.Clear();
			materialFoldouts.Clear();
			selectAll = false;

			if (rootObject == null) return;

			foreach (var renderer in rootObject.GetComponentsInChildren<Renderer>(includeInactive))
			{
				foreach (var material in renderer.sharedMaterials)
				{
					if (material == null) continue;

					if (!materialToRenderers.TryGetValue(material, out var renderers))
					{
						renderers = new List<Renderer>();
						materialToRenderers[material] = renderers;
						materialDisplayOrder.Add(material);
						originalMaterials[material] = material;
					}
					renderers.Add(renderer);
				}
			}
		}

		private void ReplaceMaterial(Material oldMaterial, Material newMaterial)
		{
			if (!materialToRenderers.TryGetValue(oldMaterial, out var renderers)) return;

			foreach (var renderer in renderers)
			{
				Undo.RecordObject(renderer, $"Replace Material {oldMaterial.name} with {newMaterial.name}");
				var mats = renderer.sharedMaterials;
				for (int i = 0; i < mats.Length; i++)
				{
					if (mats[i] == oldMaterial)
						mats[i] = newMaterial;
				}
				renderer.sharedMaterials = mats;
				EditorUtility.SetDirty(renderer);
			}

			materialToRenderers.Remove(oldMaterial);
			if (!materialToRenderers.ContainsKey(newMaterial))
				materialToRenderers[newMaterial] = renderers;

			int index = materialDisplayOrder.IndexOf(oldMaterial);
			if (index != -1)
				materialDisplayOrder[index] = newMaterial;

			if (originalMaterials.TryGetValue(oldMaterial, out var original))
			{
				originalMaterials.Remove(oldMaterial);
				originalMaterials[newMaterial] = original;
			}

			if (selectedMaterials.Remove(oldMaterial))
				selectedMaterials.Add(newMaterial);

			if (materialFoldouts.TryGetValue(oldMaterial, out bool foldState))
			{
				materialFoldouts.Remove(oldMaterial);
				materialFoldouts[newMaterial] = foldState;
			}

			Repaint();
		}

		private void CloneMaterial(Material material)
		{
			string path = AssetDatabase.GetAssetPath(material);
			if (string.IsNullOrEmpty(path)) return;

			string dir = Path.GetDirectoryName(path);
			string baseName = Path.GetFileNameWithoutExtension(path);
			string ext = Path.GetExtension(path);

			int n = 1;
			string newPath;
			do { newPath = Path.Combine(dir, $"{baseName} (YMEClone {n++}){ext}"); }
			while (AssetDatabase.LoadAssetAtPath<Material>(newPath) != null);

			AssetDatabase.CreateAsset(new Material(material), newPath);
			AssetDatabase.SaveAssets();

			ReplaceMaterial(material, AssetDatabase.LoadAssetAtPath<Material>(newPath));
		}

		private void ResetMaterial(Material currentMaterial)
		{
			if (!originalMaterials.TryGetValue(currentMaterial, out var original)) return;
			if (original == null || original == currentMaterial) return;
			ReplaceMaterial(currentMaterial, original);
		}

		private void BatchReplaceSelected()
		{
			if (batchReplaceMaterial == null) return;
			foreach (var mat in selectedMaterials.Where(m => materialToRenderers.ContainsKey(m)).ToList())
				ReplaceMaterial(mat, batchReplaceMaterial);
			selectedMaterials.Clear();
			selectAll = false;
		}

		private void BatchCloneSelected()
		{
			foreach (var mat in selectedMaterials.Where(m => materialToRenderers.ContainsKey(m)).ToList())
				CloneMaterial(mat);
			selectedMaterials.Clear();
			selectAll = false;
		}

		private void BatchResetSelected()
		{
			foreach (var mat in selectedMaterials.Where(m => materialToRenderers.ContainsKey(m)).ToList())
				ResetMaterial(mat);
			selectedMaterials.Clear();
			selectAll = false;
		}

		private void UpdateSelectAllState()
		{
			selectAll = materialDisplayOrder.Count > 0 && materialDisplayOrder.All(m => selectedMaterials.Contains(m));
		}

		private string GetLocalizedString(string key, string defaultValue)
		{
			return localizedStrings.TryGetValue(key, out string value) ? value : defaultValue;
		}

		private void DrawSeparator()
		{
			GUILayout.Space(5);
			EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
			GUILayout.Space(5);
		}

		[System.Serializable]
		private class SerializableDictionary
		{
			public List<string> keys = new List<string>();
			public List<string> values = new List<string>();

			public Dictionary<string, string> ToDictionary()
			{
				var dict = new Dictionary<string, string>();
				for (int i = 0; i < keys.Count; i++)
					dict[keys[i]] = values[i];
				return dict;
			}
		}
	}
}
