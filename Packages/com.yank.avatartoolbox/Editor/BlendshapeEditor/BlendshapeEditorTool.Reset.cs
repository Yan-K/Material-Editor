using UnityEngine;
using UnityEditor;

namespace YanK
{
	public partial class BlendshapeEditorTool
	{
		private void ConfirmResetAllZero()
		{
			if (targetRenderer == null) return;
			if (!EditorUtility.DisplayDialog(
				L("bseResetConfirmTitle", "Confirm Reset"),
				L("bseResetZeroConfirm", "Set all blendshape weights to 0?"),
				"OK", "Cancel")) return;

			if (inPreview) CancelPreview();

			Undo.RegisterCompleteObjectUndo(targetRenderer, "YBE: Reset All to Zero");
			foreach (var s in slots)
			{
				if (s.isSeparator) continue;
				s.current = 0f;
				targetRenderer.SetBlendShapeWeight(s.index, 0f);
			}
			EditorUtility.SetDirty(targetRenderer);
		}

		private void ConfirmResetDefault()
		{
			if (targetRenderer == null) return;
			if (!EditorUtility.DisplayDialog(
				L("bseResetConfirmTitle", "Confirm Reset"),
				L("bseResetDefaultConfirm", "Reset all blendshapes to the values they had when the renderer was first assigned?"),
				"OK", "Cancel")) return;

			if (inPreview) CancelPreview();

			Undo.RegisterCompleteObjectUndo(targetRenderer, "YBE: Reset to Default");
			foreach (var s in slots)
			{
				if (s.isSeparator) continue;
				s.current = s.defaultValue;
				targetRenderer.SetBlendShapeWeight(s.index, s.defaultValue);
			}
			EditorUtility.SetDirty(targetRenderer);
		}

		// Sets the current values as the new "default" baseline, clearing the modified (yellow) state.
		// This is a metadata-only operation — the renderer weights are already correct.
		private void ApplyCurrentAsDefault()
		{
			foreach (var s in slots)
			{
				if (s.isSeparator) continue;
				s.defaultValue = s.current;
			}
		}

		// Reverts all modified blendshapes back to their stored default values.
		private void CancelAllToDefault()
		{
			if (targetRenderer == null) return;
			if (inPreview) CancelPreview();

			bool anyModified = false;
			foreach (var s in slots)
			{
				if (!s.isSeparator && Mathf.Abs(s.current - s.defaultValue) > 0.01f) { anyModified = true; break; }
			}
			if (!anyModified) return;

			Undo.RegisterCompleteObjectUndo(targetRenderer, "YBE: Cancel Changes");
			foreach (var s in slots)
			{
				if (s.isSeparator) continue;
				s.current = s.defaultValue;
				targetRenderer.SetBlendShapeWeight(s.index, s.defaultValue);
			}
			EditorUtility.SetDirty(targetRenderer);
		}

		// Resets currently selected blendshapes to 0.
		private void ResetSelectedToZero()
		{
			if (targetRenderer == null) return;
			bool any = false;
			foreach (var s in slots) { if (!s.isSeparator && s.selected) { any = true; break; } }
			if (!any) return;

			Undo.RegisterCompleteObjectUndo(targetRenderer, "YBE: Reset Selected to Zero");
			foreach (var s in slots)
			{
				if (s.isSeparator || !s.selected) continue;
				s.current = 0f;
				targetRenderer.SetBlendShapeWeight(s.index, 0f);
			}
			EditorUtility.SetDirty(targetRenderer);
		}

		// Resets currently selected blendshapes to their stored default values.
		private void ResetSelectedToDefault()
		{
			if (targetRenderer == null) return;
			bool any = false;
			foreach (var s in slots) { if (!s.isSeparator && s.selected) { any = true; break; } }
			if (!any) return;

			Undo.RegisterCompleteObjectUndo(targetRenderer, "YBE: Reset Selected to Default");
			foreach (var s in slots)
			{
				if (s.isSeparator || !s.selected) continue;
				s.current = s.defaultValue;
				targetRenderer.SetBlendShapeWeight(s.index, s.defaultValue);
			}
			EditorUtility.SetDirty(targetRenderer);
		}
	}
}
