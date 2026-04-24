using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace YanK
{
	/// <summary>
	/// Lightweight searchable dropdown for string lists. Uses <see cref="AdvancedDropdown"/>
	/// so callers get a native, filterable popup with minimal setup.
	/// </summary>
	internal static class YanKSearchableDropdown
	{
		public sealed class Item
		{
			public string label;
			public string group; // optional header
			public object payload;
		}

		/// <summary>
		/// Draws a single-line button that opens a searchable dropdown when clicked.
		/// Invokes <paramref name="onSelect"/> with the chosen item's payload.
		/// </summary>
		public static void Field(string currentLabel, List<Item> items, Action<object> onSelect,
			params GUILayoutOption[] options)
		{
			var rect = GUILayoutUtility.GetRect(new GUIContent(currentLabel ?? "—"),
				EditorStyles.popup, options);
			if (EditorGUI.DropdownButton(rect, new GUIContent(currentLabel ?? "—"), FocusType.Keyboard))
			{
				var state = new AdvancedDropdownState();
				var dropdown = new _Impl(state, items, onSelect);
				dropdown.Show(rect);
			}
		}

		private sealed class _Impl : AdvancedDropdown
		{
			private readonly List<Item> _items;
			private readonly Action<object> _onSelect;

			public _Impl(AdvancedDropdownState state, List<Item> items, Action<object> onSelect)
				: base(state)
			{
				_items = items ?? new List<Item>();
				_onSelect = onSelect;
				minimumSize = new Vector2(260f, 320f);
			}

			protected override AdvancedDropdownItem BuildRoot()
			{
				var root = new AdvancedDropdownItem("Select");
				var groups = new Dictionary<string, AdvancedDropdownItem>();
				int id = 0;
				foreach (var it in _items)
				{
					if (it == null) continue;
					AdvancedDropdownItem parent;
					if (string.IsNullOrEmpty(it.group))
					{
						parent = root;
					}
					else if (!groups.TryGetValue(it.group, out parent))
					{
						parent = new AdvancedDropdownItem(it.group);
						root.AddChild(parent);
						groups[it.group] = parent;
					}
					var leaf = new _Leaf(it.label, id++) { Payload = it.payload };
					parent.AddChild(leaf);
				}
				return root;
			}

			protected override void ItemSelected(AdvancedDropdownItem item)
			{
				if (item is _Leaf l) _onSelect?.Invoke(l.Payload);
			}

			private sealed class _Leaf : AdvancedDropdownItem
			{
				public object Payload;
				public _Leaf(string name, int id) : base(name) { this.id = id; }
			}
		}
	}
}
