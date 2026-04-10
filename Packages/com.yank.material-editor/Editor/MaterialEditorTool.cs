using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YanK
{
	public partial class MaterialEditorTool : EditorWindow
	{
		private const string Version = "v0.3.1";

		private UnityEngine.Object rootObject;
		private Vector2 scrollPosition;
		private bool includeInactive;
		private int currentTab;

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
			if (currentTab == 1) ScanTextures();
			Repaint();
		}

		private void OnGUI()
		{
			if (rightAlignBoldStyle == null)
				rightAlignBoldStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleRight };

			DrawHeader();
			DrawRootObjectField();
			DrawTabSwitcher();

			if (currentTab == 0)
			{
				DrawMaterialControlPanel();
				DrawMaterialList();
			}
			else
			{
				DrawTextureControlPanel();
				DrawTextureList();
			}
		}

		private void DrawHeader()
		{
			GUILayout.Space(10);
			EditorGUILayout.BeginHorizontal();

			string title = L("title", "Yan-K Material Editor (YME)") + "  " + Version;
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
			EditorGUILayout.LabelField(L("rootObject", "Root Object"), EditorStyles.boldLabel);
			var newRoot = EditorGUILayout.ObjectField(rootObject, typeof(UnityEngine.Object), true);
			if (newRoot != rootObject)
			{
				if (newRoot == null || newRoot is GameObject || newRoot is Material)
				{
					rootObject = newRoot;
					inspectedMaterials.Clear();
					if (rootObject != null)
					{
						ScanMaterials();
						if (currentTab == 1) ScanTextures();
					}
					else
					{
						ClearAllData();
					}
				}
			}
			GUILayout.Space(5);
		}

		private void DrawTabSwitcher()
		{
			string[] tabNames = {
				L("materialMode", "Material Mode"),
				L("textureMode", "Texture Mode")
			};
			int newTab = GUILayout.Toolbar(currentTab, tabNames);
			if (newTab != currentTab)
			{
				currentTab = newTab;
				if (currentTab == 0)
					inspectedMaterials.Clear();
				else
					ScanTextures();
			}
			GUILayout.Space(5);
			DrawSeparator();
		}

		// --- Localization ---

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

		// --- Utilities ---

		private void ClearAllData()
		{
			materialSlots.Clear();
			selectAll = false;
			textureSlots.Clear();
			textureSelectAll = false;
			inspectedMaterials.Clear();
		}

		private string L(string key, string defaultValue)
		{
			return localizedStrings.TryGetValue(key, out string value) ? value : defaultValue;
		}

		private void DrawSeparator()
		{
			GUILayout.Space(5);
			EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), Color.gray);
			GUILayout.Space(5);
		}

		private static string GenerateClonePath(string originalPath)
		{
			string dir = Path.GetDirectoryName(originalPath);
			string baseName = Path.GetFileNameWithoutExtension(originalPath);
			string ext = Path.GetExtension(originalPath);

			int n = 1;
			string newPath;
			do { newPath = Path.Combine(dir, $"{baseName} (YMEClone {n++}){ext}"); }
			while (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newPath) != null);
			return newPath;
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
