using UnityEngine;

namespace YanK
{
	/// <summary>
	/// Shared constants for the Scene Controller feature. Kept separate from
	/// behaviour-carrying code so colour palettes, asset paths, and UI labels
	/// have a single source of truth.
	/// </summary>
	internal static class SceneControllerConstants
	{
		// ---------- Dark / Light palette ----------
		// Values deliberately round to simple integer RGB values in 0-255 space
		// so the colour pickers show a clean hex value when inspected.

		public static readonly Color DarkBase = new Color(50f / 255f, 50f / 255f, 50f / 255f, 1f);
		public static readonly Color DarkLine = new Color(120f / 255f, 120f / 255f, 120f / 255f, 1f);
		public static readonly Color LightBase = new Color(242f / 255f, 242f / 255f, 242f / 255f, 1f);
		public static readonly Color LightFloor = new Color(204f / 255f, 204f / 255f, 204f / 255f, 1f);
		public static readonly Color LightLine = new Color(80f / 255f, 80f / 255f, 80f / 255f, 1f);

		// ---------- Asset paths ----------
		// All shipped materials live under Runtime so package consumers see them.

		public const string MaterialFolder = "Packages/com.yank.avatartoolbox/Runtime/Materials";
		public const string SkyboxProcMatPath = MaterialFolder + "/YSC_Skybox.mat";
		public const string DebugFloorMatPath = MaterialFolder + "/YSC_DebugFloor.mat";
		public const string DebugFloorShaderName = "YanK/SceneControllerDebugFloor";
		public const string ProfileFolder = "Packages/com.yank.avatartoolbox/Runtime/PostProcessingProfiles";

		// ---------- Bone target labels (static to avoid per-OnGUI string[] alloc) ----------

		public static readonly string[] BoneRow1Labels = { "Head", "Neck", "Chest", "Spine", "Hips" };
		public static readonly string[] BoneRow2Labels = { "L.Hand", "R.Hand", "L.Foot", "R.Foot", "L.Arm", "R.Arm" };
	}
}
