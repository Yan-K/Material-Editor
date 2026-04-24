using UnityEditor;
using UnityEngine;

namespace YanK
{
	public partial class SceneControllerEditor
	{
		private void DrawAvatarSection()
		{
			GUILayout.Space(4);

			if (sc.avatarRoot == null)
			{
				EditorGUILayout.HelpBox(
					YanKLocalization.L("scAvatarHelp", "Drop a GameObject with an Animator into the Avatar field above."),
					MessageType.Info);
				return;
			}

			using (new EditorGUI.DisabledScope(sc.autoMoveEnabled))
			{
				Vector3 newOffset = sc.avatarOffset;
				newOffset.x = EditorGUILayout.Slider(YanKLocalization.L("scLeftRight", "Left / Right (X)"), sc.avatarOffset.x, -5f, 5f);
				newOffset.y = EditorGUILayout.Slider(YanKLocalization.L("scUpDown", "Up / Down (Y)"), sc.avatarOffset.y, -5f, 5f);
				newOffset.z = EditorGUILayout.Slider(YanKLocalization.L("scFrontBack", "Front / Back (Z)"), sc.avatarOffset.z, -5f, 5f);

				if (newOffset != sc.avatarOffset)
				{
					Undo.RecordObject(sc, "Avatar Offset");
					sc.avatarOffset = newOffset;
				}

			}

			GUILayout.Space(2);
			// Reset is ALWAYS available — it snaps the avatar back to its home
			// position AND clears auto-move / any scene-gizmo-induced drift.
			if (GUILayout.Button(YanKLocalization.L("scReset", "Reset Position"), GUILayout.Height(22)))
			{
				Undo.RecordObject(sc, "Reset Avatar Position");
				sc.ResetAvatarPosition();
			}

			YanKInspectorGUI.DrawStyledSeparator();

			EditorGUILayout.BeginHorizontal();
			bool newAuto = EditorGUILayout.ToggleLeft(
				YanKLocalization.L("scAutoMove", "Auto Move"), sc.autoMoveEnabled, GUILayout.Width(120));
			if (newAuto != sc.autoMoveEnabled)
			{
				Undo.RecordObject(sc, "Toggle Auto Move");
				sc.autoMoveEnabled = newAuto;
				if (!newAuto) sc.avatarOffset = Vector3.zero;
			}
			GUILayout.FlexibleSpace();
			var newMode = (AutoMoveMode)EditorGUILayout.EnumPopup(sc.autoMoveMode, GUILayout.Width(140));
			if (newMode != sc.autoMoveMode)
			{
				Undo.RecordObject(sc, "Change Auto Move Mode");
				sc.autoMoveMode = newMode;
			}
			EditorGUILayout.EndHorizontal();

			using (new EditorGUI.DisabledScope(!sc.autoMoveEnabled))
			{
				float spd = EditorGUILayout.Slider(YanKLocalization.L("scSpeed", "Speed"), sc.autoMoveSpeed, 0f, 10f);
				float rad = EditorGUILayout.Slider(YanKLocalization.L("scRadius", "Radius / Amplitude"), sc.autoMoveRadius, 0f, 5f);
				if (SceneControllerMath.Changed(spd, sc.autoMoveSpeed) || SceneControllerMath.Changed(rad, sc.autoMoveRadius))
				{
					Undo.RecordObject(sc, "Auto Move Settings");
					sc.autoMoveSpeed = spd;
					sc.autoMoveRadius = rad;
				}
			}
		}
	}
}
