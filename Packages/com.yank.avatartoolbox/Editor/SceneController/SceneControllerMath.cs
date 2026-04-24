using UnityEngine;

namespace YanK
{
	/// <summary>
	/// Small math helpers for Scene Controller editor change-detection. Collapses
	/// the repeated multi-line <c>!Mathf.Approximately(a, b) || !Mathf.Approximately(c, d)</c>
	/// chains into readable single expressions.
	/// </summary>
	internal static class SceneControllerMath
	{
		/// <summary>Inverse of <see cref="Mathf.Approximately"/> — true when values differ beyond epsilon.</summary>
		public static bool Changed(float a, float b) => !Mathf.Approximately(a, b);

		public static bool Changed(Vector3 a, Vector3 b) =>
			!Mathf.Approximately(a.x, b.x) ||
			!Mathf.Approximately(a.y, b.y) ||
			!Mathf.Approximately(a.z, b.z);

		public static bool Changed(Color a, Color b) =>
			!Mathf.Approximately(a.r, b.r) ||
			!Mathf.Approximately(a.g, b.g) ||
			!Mathf.Approximately(a.b, b.b) ||
			!Mathf.Approximately(a.a, b.a);
	}
}
