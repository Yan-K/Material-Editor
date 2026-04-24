using System.Collections.Generic;

namespace YanK
{
	/// <summary>
	/// Holds the transient UI state for a single Add / Remove preset workflow
	/// (name buffer for Add, selection set for Remove, open/closed panel flags).
	/// Camera, Directional-Light and Post-Processing editors each hold one
	/// instance instead of four parallel fields.
	/// </summary>
	/// <typeparam name="TKey">Key identity type used by the Remove selection
	/// set — <c>string</c> for name-keyed presets, <c>ScriptableObject</c> for
	/// asset-keyed profiles, etc.</typeparam>
	internal sealed class PresetWorkflow<TKey>
	{
		public bool addOpen;
		public string addName = "";

		public bool removeOpen;
		public readonly HashSet<TKey> removeSel = new HashSet<TKey>();

		/// <summary>Opens the Add panel and seeds the name field with a default value.
		/// Automatically closes any open Remove panel for mutual exclusion.</summary>
		public void OpenAdd(string defaultName)
		{
			addOpen = true;
			addName = defaultName ?? "";
			removeOpen = false;
		}

		/// <summary>Flips the Remove panel open/closed and resets the selection set.</summary>
		public void ToggleRemove()
		{
			removeOpen = !removeOpen;
			removeSel.Clear();
		}

		/// <summary>Closes both panels and clears any pending remove selection.</summary>
		public void CloseAll()
		{
			addOpen = false;
			removeOpen = false;
			removeSel.Clear();
		}
	}
}
