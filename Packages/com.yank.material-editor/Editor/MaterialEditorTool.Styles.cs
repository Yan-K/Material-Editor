using UnityEngine;
using UnityEditor;
using System;

namespace YanK
{
	public partial class MaterialEditorTool
	{
		// --- Spacing Constants ---
		private const float SectionPadding = 10f;
		private const float ItemSpacing = 4f;
		private const float GroupPadding = 6f;

		// --- Cached Styles ---
		private GUIStyle sectionHeaderStyle;
		private GUIStyle statusLabelStyle;
		private GUIStyle versionBadgeStyle;
		private GUIStyle searchFieldStyle;
		private GUIStyle dimLabelStyle;
		private GUIStyle cardStyle;
		private GUIStyle modifiedCardStyle;

		private bool stylesInitialized;

		private void InitStyles()
		{
			if (stylesInitialized && cardStyle != null) return;

			sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 13,
				padding = new RectOffset(4, 0, 2, 2)
			};

			statusLabelStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleRight,
				normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.4f, 0.4f, 0.4f) }
			};

			versionBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				normal = {
					textColor = EditorGUIUtility.isProSkin ? new Color(0.6f, 0.75f, 1f) : new Color(0.2f, 0.35f, 0.7f),
					background = MakeTex(1, 1, EditorGUIUtility.isProSkin ? new Color(0.25f, 0.3f, 0.4f, 0.5f) : new Color(0.8f, 0.85f, 0.95f, 0.7f))
				},
				padding = new RectOffset(6, 6, 2, 2),
				margin = new RectOffset(4, 0, 3, 0)
			};

			searchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField);

			dimLabelStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleRight,
				normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.55f, 0.55f, 0.55f) : new Color(0.45f, 0.45f, 0.45f) }
			};

			cardStyle = new GUIStyle("box")
			{
				padding = new RectOffset(6, 6, 4, 4),
				margin = new RectOffset(0, 0, 2, 2)
			};

			modifiedCardStyle = new GUIStyle(cardStyle);
			var modBg = EditorGUIUtility.isProSkin ? new Color(0.35f, 0.32f, 0.2f, 0.4f) : new Color(1f, 0.95f, 0.8f, 0.6f);
			modifiedCardStyle.normal.background = MakeTex(1, 1, modBg);

			stylesInitialized = true;
		}

		// --- Colors ---

		private static Color AccentColor => EditorGUIUtility.isProSkin
			? new Color(0.45f, 0.65f, 1f)
			: new Color(0.2f, 0.4f, 0.8f);

		private static Color SeparatorColor => EditorGUIUtility.isProSkin
			? new Color(0.2f, 0.2f, 0.2f)
			: new Color(0.7f, 0.7f, 0.7f);

		private static Color HeaderBarColor => EditorGUIUtility.isProSkin
			? new Color(0.35f, 0.55f, 0.9f, 0.8f)
			: new Color(0.3f, 0.5f, 0.85f, 0.8f);

		// --- Drawing Helpers ---

		private void DrawSectionHeader(string title)
		{
			GUILayout.Space(GroupPadding);
			var rect = EditorGUILayout.GetControlRect(false, 20);
			var barRect = new Rect(rect.x, rect.y, 3, rect.height);
			EditorGUI.DrawRect(barRect, HeaderBarColor);
			rect.x += 8;
			rect.width -= 8;
			GUI.Label(rect, title, sectionHeaderStyle);
			GUILayout.Space(2);
		}

		private void DrawGroupBox(Action content)
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			GUILayout.Space(GroupPadding / 2);
			content?.Invoke();
			GUILayout.Space(GroupPadding / 2);
			EditorGUILayout.EndVertical();
		}

		private void DrawStyledSeparator()
		{
			GUILayout.Space(4);
			EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), SeparatorColor);
			GUILayout.Space(4);
		}

		private string DrawSearchField(string currentFilter, string placeholderKey = "search", string placeholderDefault = "Search...")
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(2);

			var newFilter = EditorGUILayout.TextField(currentFilter, searchFieldStyle);

			if (!string.IsNullOrEmpty(newFilter))
			{
				if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
					newFilter = "";
			}

			GUILayout.Space(2);
			EditorGUILayout.EndHorizontal();
			return newFilter;
		}

		private static Texture2D MakeTex(int width, int height, Color color)
		{
			var pixels = new Color[width * height];
			for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
			var tex = new Texture2D(width, height);
			tex.SetPixels(pixels);
			tex.Apply();
			tex.hideFlags = HideFlags.HideAndDontSave;
			return tex;
		}
	}
}
