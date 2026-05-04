using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace YanK
{
	public partial class BlendshapeEditorTool
	{
		// AdvancedDropdown provides built-in searching + hierarchical navigation, which is
		// necessary because a single avatar can have 600+ blendshapes and a flat Popup is unusable.

		private sealed class BlendshapeSlotDropdownItem : AdvancedDropdownItem
		{
			public int SlotIndex;
			public BlendshapeSlotDropdownItem(string name, int slotIndex) : base(name) { SlotIndex = slotIndex; }
		}

		private sealed class BlendshapeSlotDropdown : AdvancedDropdown
		{
			private readonly List<BlendshapeSlot> allSlots;
			private readonly Action<int> onPicked;
			private readonly string ignoreLabel;
			private readonly string groupGeneralDisplay;

			public BlendshapeSlotDropdown(AdvancedDropdownState state, List<BlendshapeSlot> slots, string ignoreLabel, string generalLabel, Action<int> onPicked)
				: base(state)
			{
				this.allSlots = slots;
				this.onPicked = onPicked;
				this.ignoreLabel = ignoreLabel;
				this.groupGeneralDisplay = generalLabel;
				minimumSize = new Vector2(280f, 420f);
			}

			protected override AdvancedDropdownItem BuildRoot()
			{
				var root = new AdvancedDropdownItem("Blendshape");
				root.AddChild(new BlendshapeSlotDropdownItem(ignoreLabel, -1));
				root.AddSeparator();

				// Preserve insertion order of groups as encountered in the slots list.
				var groupItems = new Dictionary<string, AdvancedDropdownItem>();
				var groupOrder = new List<string>();

				for (int i = 0; i < allSlots.Count; i++)
				{
					var s = allSlots[i];
					if (s.isSeparator) continue;

					string gKey = string.IsNullOrEmpty(s.groupName) ? GroupGeneralKey : s.groupName;
					string display = gKey == GroupGeneralKey ? groupGeneralDisplay : gKey;

					if (!groupItems.TryGetValue(gKey, out var parent))
					{
						parent = new AdvancedDropdownItem(display);
						groupItems[gKey] = parent;
						groupOrder.Add(gKey);
					}
					parent.AddChild(new BlendshapeSlotDropdownItem(s.name, i));
				}

				// If the only group is "General", flatten so the user doesn't have to click a submenu.
				if (groupOrder.Count == 1)
				{
					var only = groupItems[groupOrder[0]];
					foreach (var child in only.children) root.AddChild(child);
				}
				else
				{
					foreach (var g in groupOrder) root.AddChild(groupItems[g]);
				}

				return root;
			}

			protected override void ItemSelected(AdvancedDropdownItem item)
			{
				int idx = item is BlendshapeSlotDropdownItem bi ? bi.SlotIndex : -1;
				onPicked?.Invoke(idx);
			}
		}

		private AdvancedDropdownState cachedRemapDropdownState;

		private void ShowRemapDropdown(Rect rect, Action<int> onPicked)
		{
			if (cachedRemapDropdownState == null) cachedRemapDropdownState = new AdvancedDropdownState();
			var dd = new BlendshapeSlotDropdown(
				cachedRemapDropdownState,
				slots,
				L("bseRemapIgnore", "(Ignore)"),
				L("bseGroupGeneral", "General"),
				onPicked);
			dd.Show(rect);
		}
	}
}
