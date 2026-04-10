using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YanK
{
	public partial class MaterialEditorTool : EditorWindow
	{
		private const string Version = "v0.4.2";

		private UnityEngine.Object rootObject;
		private Vector2 scrollPosition;
		private bool includeInactive;
		private int currentTab;

		private string materialSearchFilter = "";
		private string textureSearchFilter = "";

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
			includeInactive = EditorPrefs.GetBool("YME_IncludeInactive", false);
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
			InitStyles();

			if (rightAlignBoldStyle == null)
				rightAlignBoldStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleRight };

			DrawHeader();

			if (currentTab == 0)
			{
				DrawMaterialControlPanel();
				if (rootObject != null)
					DrawMaterialList();
				else
					DrawEmptyState();
			}
			else
			{
				DrawTextureControlPanel();
				if (rootObject != null)
					DrawTextureList();
				else
					DrawEmptyState();
			}
		}

		private void DrawHeader()
		{
			GUILayout.Space(8);

			// Row 1: Title + Version badge + Language selector
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField(L("title", "Yan-K Material Editor (YME)"), sectionHeaderStyle, GUILayout.ExpandWidth(false));

			GUILayout.FlexibleSpace();

			int newIndex = EditorGUILayout.Popup(selectedLanguageIndex, availableLanguages.ToArray(), GUILayout.Width(120));
			if (newIndex != selectedLanguageIndex)
			{
				selectedLanguageIndex = newIndex;
				EditorPrefs.SetInt("YME_Language", selectedLanguageIndex);
				LoadLocalizedStrings();
			}

			GUILayout.Space(4);
			GUILayout.Label(Version, versionBadgeStyle);

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(8);

			// Row 2: Root Object field (merged into header)
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(L("rootObject", "Root Object"), EditorStyles.boldLabel, GUILayout.Width(80));
			var newRoot = EditorGUILayout.ObjectField(rootObject, typeof(UnityEngine.Object), true);
			if (rootObject != null)
			{
				if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
					newRoot = null;
			}
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
			EditorGUILayout.EndHorizontal();
		}

		private void DrawTabBar()
		{
			string[] tabNames = {
				L("materialMode", "Material Mode"),
				L("textureMode", "Texture Mode")
			};
			int newTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(22));
			if (newTab != currentTab)
			{
				currentTab = newTab;
				if (currentTab == 0)
					inspectedMaterials.Clear();
				else
					ScanTextures();
			}
		}

		private void DrawEmptyState()
		{
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginVertical();

			var iconRect = GUILayoutUtility.GetRect(48, 48, GUILayout.ExpandWidth(false));
			GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("d_GameObject Icon").image, ScaleMode.ScaleToFit);

			GUILayout.Space(8);
			var centeredStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
			EditorGUILayout.LabelField(L("noRootObject", "Drop a GameObject or Material in the Root Object field above to begin."), centeredStyle, GUILayout.Width(300));

			EditorGUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
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
			DrawStyledSeparator();
		}

		private static string GenerateClonePath(string originalPath)
		{
			string dir = Path.GetDirectoryName(originalPath);
			string baseName = Path.GetFileNameWithoutExtension(originalPath);
			string ext = Path.GetExtension(originalPath);

			// Strip existing (YMEClone N) suffix to avoid stacking
			baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"\s*\(YMEClone \d+\)$", "");

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
