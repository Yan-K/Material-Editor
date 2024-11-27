using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace YanK
{
	public class MaterialEditorTool : EditorWindow
	{
		private GameObject rootObject;
		private readonly Dictionary<Material, List<Renderer>> materialToRenderers = new Dictionary<Material, List<Renderer>>();
		private readonly List<Material> materialDisplayOrder = new List<Material>();
		private Vector2 scrollPosition;
		private bool includeInactive = false;

		private List<string> availableLanguages = new List<string>();
		private int selectedLanguageIndex = 0;
		private int previousLanguageIndex = -1;
		private Dictionary<string, string> localizedStrings = new Dictionary<string, string>();

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
			DrawHeader();
			DrawRootObjectField();
			DrawControlPanel();
			DrawMaterialList();
		}

		private void DrawHeader()
		{
			GUILayout.Space(10);
			EditorGUILayout.BeginHorizontal();

			string title = GetLocalizedString("title", "Yan-K Material Editor (YME)");
			EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

			GUILayout.FlexibleSpace();

			int newLanguageIndex = EditorGUILayout.Popup(selectedLanguageIndex, availableLanguages.ToArray(), GUILayout.Width(150));
			if (newLanguageIndex != selectedLanguageIndex)
			{
				selectedLanguageIndex = newLanguageIndex;
				previousLanguageIndex = selectedLanguageIndex;

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
			GameObject newRootObject = (GameObject)EditorGUILayout.ObjectField(rootObject, typeof(GameObject), true);

			if (newRootObject != rootObject)
			{
				rootObject = newRootObject;
				if (rootObject != null) ScanMaterials();
			}

			GUILayout.Space(5);
		}

		private void DrawControlPanel()
		{
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(GetLocalizedString("forceScan", "Force Scan Materials"), GUILayout.Height(25)))
			{
				ScanMaterials();
			}

			GUILayout.FlexibleSpace();
			includeInactive = EditorGUILayout.ToggleLeft(GetLocalizedString("includeInactive", "Include Inactive"), includeInactive, GUILayout.Width(150));
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);
			DrawSeparator();
		}

		private void DrawMaterialList()
		{
			if (materialDisplayOrder.Count > 0)
			{
				EditorGUILayout.LabelField(GetLocalizedString("materialsInUse", "Materials in Use"), EditorStyles.boldLabel);

				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

				// Iterate over a copy of the materialDisplayOrder list
				foreach (Material material in new List<Material>(materialDisplayOrder))
				{
					if (!materialToRenderers.TryGetValue(material, out List<Renderer> renderers)) continue;

					GUILayout.Space(5);
					EditorGUILayout.BeginVertical("box");

					DrawMaterialItem(material, renderers);

					EditorGUILayout.EndVertical();
				}

				EditorGUILayout.EndScrollView();

				GUILayout.Space(10);
				DrawSeparator();
			}
			else
			{
				EditorGUILayout.HelpBox(GetLocalizedString("noMaterials", "No materials found. Drop a GameObject in the Root Object field and scan again."), MessageType.Info);
			}

			GUILayout.Space(10);
		}


		private void DrawMaterialItem(Material material, List<Renderer> renderers)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(material.name, EditorStyles.boldLabel, GUILayout.Width(150));
			EditorGUILayout.ObjectField(material, typeof(Material), false);
			EditorGUILayout.EndHorizontal();

			Material newMaterial = (Material)EditorGUILayout.ObjectField(GetLocalizedString("replaceWith", "Replace With"), null, typeof(Material), false);
			if (newMaterial != null && newMaterial != material)
			{
				ReplaceMaterial(material, newMaterial);
			}

			EditorGUILayout.LabelField(string.Format(GetLocalizedString("usedBy", "Used by {0} renderer(s)"), renderers.Count), EditorStyles.miniLabel);
		}

		private void RefreshLanguageFiles()
		{
			availableLanguages.Clear();
			const string localizationPath = "YMELocalization";

			foreach (var file in Resources.LoadAll<TextAsset>(localizationPath))
			{
				availableLanguages.Add(Path.GetFileNameWithoutExtension(file.name));
			}

			if (!availableLanguages.Contains("English"))
			{
				availableLanguages.Insert(0, "English");
			}

			if (availableLanguages.Count == 0)
			{
				Debug.LogWarning("No localization files found in Resources/YMELocalization.");
			}
		}

		
		private void LoadDefaultLanguage()
		{
			selectedLanguageIndex = EditorPrefs.GetInt("YME_Language", availableLanguages.IndexOf("English"));
		    if (selectedLanguageIndex < 0 || selectedLanguageIndex >= availableLanguages.Count)
		    {
			    selectedLanguageIndex = 0;
		    }
		
		    previousLanguageIndex = selectedLanguageIndex;
		}

		private void LoadLocalizedStrings()
		{
			localizedStrings.Clear();

			if (availableLanguages.Count == 0) return;

			string selectedLanguage = availableLanguages[selectedLanguageIndex];
			string localizationPath = $"YMELocalization/{selectedLanguage}";
			TextAsset jsonFile = Resources.Load<TextAsset>(localizationPath);

			if (jsonFile != null)
			{
				try
				{
					localizedStrings = JsonUtility.FromJson<SerializableDictionary>(jsonFile.text).ToDictionary();
				}
					catch
					{
						Debug.LogError($"Failed to parse localization file: {selectedLanguage}");
					}
			}
			else
			{
				Debug.LogWarning($"Localization file for {selectedLanguage} not found.");
			}

			Repaint();
		}

		private void ScanMaterials()
		{
			materialToRenderers.Clear();
			materialDisplayOrder.Clear();

			if (rootObject == null)
			{
				Debug.LogWarning(GetLocalizedString("rootObject", "Root Object"));
				return;
			}

			foreach (Renderer renderer in rootObject.GetComponentsInChildren<Renderer>(includeInactive))
			{
				foreach (Material material in renderer.sharedMaterials)
				{
					if (material == null) continue;

					if (!materialToRenderers.TryGetValue(material, out var renderers))
					{
						renderers = new List<Renderer>();
						materialToRenderers[material] = renderers;
						materialDisplayOrder.Add(material);
					}
					renderers.Add(renderer);
				}
			}

			Debug.Log($"Scanned {materialToRenderers.Count} unique materials in the hierarchy.");
		}

		private void ReplaceMaterial(Material oldMaterial, Material newMaterial)
		{
			if (!materialToRenderers.TryGetValue(oldMaterial, out List<Renderer> renderers)) return;

			foreach (Renderer renderer in renderers)
			{
				Undo.RecordObject(renderer, $"Replace Material {oldMaterial.name} with {newMaterial.name}");

				Material[] materials = renderer.sharedMaterials;
				for (int i = 0; i < materials.Length; i++)
				{
					if (materials[i] == oldMaterial)
					{
						materials[i] = newMaterial;
					}
				}
				renderer.sharedMaterials = materials;

				EditorUtility.SetDirty(renderer);
			}

			if (materialToRenderers.ContainsKey(oldMaterial))
			{
				materialToRenderers.Remove(oldMaterial);
			}

			if (!materialToRenderers.ContainsKey(newMaterial))
			{
				materialToRenderers[newMaterial] = renderers;
			}

			int index = materialDisplayOrder.IndexOf(oldMaterial);
			if (index != -1)
			{
				materialDisplayOrder[index] = newMaterial;
			}

			Debug.Log($"Replaced material '{oldMaterial.name}' with '{newMaterial.name}'.");

			Repaint();
		}



		private string GetLocalizedString(string key, string defaultValue)
		{
			return localizedStrings.TryGetValue(key, out string value) ? value : defaultValue;
		}

		private void DrawSeparator()
		{
			GUILayout.Space(5);
			Rect rect = EditorGUILayout.GetControlRect(false, 1);
			EditorGUI.DrawRect(rect, Color.gray);
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
				{
					dict[keys[i]] = values[i];
				}
				return dict;
			}
		}
	}
}
