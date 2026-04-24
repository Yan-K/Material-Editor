using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YanK
{
	/// <summary>
	/// Shared IMGUI helpers for the Scene Controller preset workflow (Add / Remove
	/// inline panels). Camera presets, Directional-Light presets and Post-Processing
	/// profiles all use this so the three flows stay behaviourally identical.
	/// </summary>
	internal static class SceneControllerPresetUI
	{
		public readonly struct RemoveEntry<TKey>
		{
			public readonly TKey key;           // stable key used by the caller
			public readonly string displayLabel; // what the user sees in the toggle list

			public RemoveEntry(TKey key, string displayLabel)
			{
				this.key = key;
				this.displayLabel = displayLabel;
			}
		}

		public enum AddAction { None, Save, Cancel }
		public enum RemoveAction { None, Confirm, Cancel }

		/// <summary>
		/// Draws an inline "Name [____] [Save] [Cancel]" row. The caller owns the
		/// buffer string and re-assigns it from the returned ref parameter.
		/// </summary>
		public static AddAction DrawAddRow(ref string nameField)
		{
			AddAction action = AddAction.None;
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(YanKLocalization.L("scPresetName", "Name"), GUILayout.Width(60));
			nameField = EditorGUILayout.TextField(nameField);
			using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(nameField)))
			{
				if (GUILayout.Button(YanKLocalization.L("scSave", "Save"), GUILayout.Width(64)))
					action = AddAction.Save;
			}
			if (GUILayout.Button(YanKLocalization.L("scCancel", "Cancel"), GUILayout.Width(64)))
				action = AddAction.Cancel;
			EditorGUILayout.EndHorizontal();
			return action;
		}

		/// <summary>
		/// Draws a list of toggles for the removable items plus the
		/// Remove N / Cancel row. The caller owns <paramref name="selection"/>
		/// (mutated as the user ticks boxes) and reacts to the returned action.
		/// On <see cref="RemoveAction.Confirm"/>, <paramref name="toRemove"/>
		/// contains a snapshot of the selected keys.
		/// </summary>
		/// <typeparam name="TKey">Key identity type (<c>string</c> for camera / dir-light,
		/// <c>ScriptableObject</c> for PP profiles, etc.).</typeparam>
		public static RemoveAction DrawRemovePanel<TKey>(
			IList<RemoveEntry<TKey>> removable,
			HashSet<TKey> selection,
			out List<TKey> toRemove,
			string emptyMessage = null)
		{
			toRemove = null;

			YanKInspectorGUI.DrawStyledSeparator();
			EditorGUILayout.LabelField(
				YanKLocalization.L("scRemovePickTitle", "Select items to remove"),
				EditorStyles.miniBoldLabel);

			if (removable == null || removable.Count == 0)
			{
				if (!string.IsNullOrEmpty(emptyMessage))
					EditorGUILayout.LabelField(emptyMessage, EditorStyles.miniLabel);
			}
			else
			{
				// Drop stale keys (selection items no longer present in the list).
				if (selection.Count > 0)
				{
					var valid = new HashSet<TKey>();
					foreach (var e in removable) valid.Add(e.key);
					selection.RemoveWhere(k => !valid.Contains(k));
				}

				foreach (var e in removable)
				{
					bool wasOn = selection.Contains(e.key);
					string shown = string.IsNullOrEmpty(e.displayLabel) ? e.key?.ToString() : e.displayLabel;
					bool nowOn = EditorGUILayout.ToggleLeft("  " + shown, wasOn);
					if (nowOn != wasOn)
					{
						if (nowOn) selection.Add(e.key);
						else selection.Remove(e.key);
					}
				}
			}

			RemoveAction action = RemoveAction.None;
			EditorGUILayout.BeginHorizontal();
			using (new EditorGUI.DisabledScope(selection.Count == 0))
			{
				if (GUILayout.Button(string.Format(
					YanKLocalization.L("scRemoveSelected", "Remove {0} selected"), selection.Count)))
				{
					toRemove = new List<TKey>(selection);
					action = RemoveAction.Confirm;
				}
			}
			if (GUILayout.Button(YanKLocalization.L("scCancel", "Cancel"), GUILayout.Width(80)))
				action = RemoveAction.Cancel;
			EditorGUILayout.EndHorizontal();
			return action;
		}
	}
}
