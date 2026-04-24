using System;
using UnityEditor;
using UnityEngine;

namespace YanK
{
	/// <summary>
	/// Drawing helpers for CustomEditor (Inspector) code that mirror the look-and-feel
	/// used by <see cref="YanKEditorWindow"/>. Styles are lazily built and cached.
	/// </summary>
	public static class YanKInspectorGUI
	{
		public const float ItemSpacing = 4f;
		public const float GroupPadding = 6f;

		private static GUIStyle _sectionHeaderStyle;
		private static GUIStyle _versionBadgeStyle;
		private static GUIStyle _cardStyle;
		private static GUIStyle _centeredMessageStyle;
		private static Texture2D _badgeTex;

		public static GUIStyle SectionHeaderStyle
		{
			get
			{
				EnsureStyles();
				return _sectionHeaderStyle;
			}
		}

		public static GUIStyle VersionBadgeStyle
		{
			get
			{
				EnsureStyles();
				return _versionBadgeStyle;
			}
		}

		public static GUIStyle CardStyle
		{
			get
			{
				EnsureStyles();
				return _cardStyle;
			}
		}

		public static GUIStyle CenteredMessageStyle
		{
			get
			{
				EnsureStyles();
				return _centeredMessageStyle;
			}
		}

		public static Color SeparatorColor => EditorGUIUtility.isProSkin
			? new Color(0.2f, 0.2f, 0.2f)
			: new Color(0.7f, 0.7f, 0.7f);

		public static void EnsureStyles()
		{
			if (_sectionHeaderStyle != null && _badgeTex != null) return;

			_sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 13,
				padding = new RectOffset(4, 0, 2, 2)
			};

			_badgeTex = MakeTex(1, 1, EditorGUIUtility.isProSkin
				? new Color(0.25f, 0.3f, 0.4f, 0.5f)
				: new Color(0.8f, 0.85f, 0.95f, 0.7f));

			_versionBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				normal =
				{
					textColor = EditorGUIUtility.isProSkin
						? new Color(0.6f, 0.75f, 1f)
						: new Color(0.2f, 0.35f, 0.7f),
					background = _badgeTex
				},
				padding = new RectOffset(6, 6, 2, 2),
				margin = new RectOffset(4, 0, 3, 0)
			};

			_cardStyle = new GUIStyle("box")
			{
				padding = new RectOffset(6, 6, 4, 4),
				margin = new RectOffset(0, 0, 2, 2)
			};

			_centeredMessageStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleCenter,
				wordWrap = true
			};
		}

		public static void DrawGroupBox(Action content)
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			GUILayout.Space(GroupPadding / 2);
			content?.Invoke();
			GUILayout.Space(GroupPadding / 2);
			EditorGUILayout.EndVertical();
		}

		public static void DrawStyledSeparator()
		{
			GUILayout.Space(4);
			EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), SeparatorColor);
			GUILayout.Space(4);
		}

		public static bool DrawFoldoutHeader(string label, bool expanded)
		{
			return EditorGUILayout.Foldout(expanded, label, true, EditorStyles.foldoutHeader);
		}

		public static void DrawHeaderRow(string title, string version)
		{
			EnsureStyles();
			GUILayout.Space(8);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(title, _sectionHeaderStyle, GUILayout.ExpandWidth(false));
			GUILayout.FlexibleSpace();

			var langs = YanKLocalization.Languages;
			if (langs.Count > 0)
			{
				int cur = YanKLocalization.SelectedIndex;
				var arr = new string[langs.Count];
				for (int i = 0; i < langs.Count; i++) arr[i] = langs[i];
				int next = EditorGUILayout.Popup(cur, arr, GUILayout.Width(120));
				if (next != cur) YanKLocalization.SelectedIndex = next;
			}

			GUILayout.Space(4);
			GUILayout.Label(version, _versionBadgeStyle);
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(8);
		}

		public static Texture2D MakeTex(int w, int h, Color color)
		{
			var pixels = new Color[w * h];
			for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
			var tex = new Texture2D(w, h);
			tex.SetPixels(pixels);
			tex.Apply();
			tex.hideFlags = HideFlags.HideAndDontSave;
			return tex;
		}
	}
}
