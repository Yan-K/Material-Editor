using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YanK
{
	public partial class MaterialEditorTool
	{
		private class MaterialSlot
		{
			public Material current;
			public Material original;
			public List<Renderer> renderers = new List<Renderer>();
			public bool foldout;
			public bool selected;
		}

		private readonly List<MaterialSlot> materialSlots = new List<MaterialSlot>();
		private bool selectAll;
		private Material batchReplaceMaterial;
		private readonly List<Material> inspectedMaterials = new List<Material>();

		// --- UI ---

		private void DrawMaterialControlPanel()
		{
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(L("forceScan", "Rescan Materials"), GUILayout.Height(25)))
			{
				includeInactive = false;
				ScanMaterials();
			}
			if (GUILayout.Button(L("forceScanInactive", "Scan Include Inactive"), GUILayout.Height(25)))
			{
				includeInactive = true;
				ScanMaterials();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(L("inspectSelected", "Inspect Selected"), GUILayout.Height(25)))
				InspectSelectedMaterials();
			if (GUILayout.Button(L("cloneSelected", "Clone Selected"), GUILayout.Height(25)))
				BatchCloneSelected();
			if (GUILayout.Button(L("replaceSelected", "Replace Selected"), GUILayout.Height(25)))
				BatchReplaceSelected();
			if (GUILayout.Button(L("resetSelected", "Reset Selected"), GUILayout.Height(25)))
				BatchResetSelected();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(L("batchReplaceMaterial", "Batch Replace Material"), GUILayout.Width(180));
			batchReplaceMaterial = (Material)EditorGUILayout.ObjectField(batchReplaceMaterial, typeof(Material), false);
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);
			DrawSeparator();
		}

		private void DrawMaterialList()
		{
			if (materialSlots.Count == 0)
			{
				EditorGUILayout.HelpBox(L("noMaterials", "No materials found. Drop a GameObject in the Root Object field and scan again."), MessageType.Info);
				GUILayout.Space(10);
				return;
			}

			EditorGUILayout.BeginHorizontal();

			bool newSelectAll = EditorGUILayout.ToggleLeft(L("selectAll", "Select All"), selectAll, GUILayout.Width(100));
			if (newSelectAll != selectAll)
			{
				selectAll = newSelectAll;
				foreach (var slot in materialSlots)
					slot.selected = selectAll;
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.LabelField(string.Format(L("materialsInUse", "{0} Materials in Use"), materialSlots.Count), rightAlignBoldStyle);
			EditorGUILayout.EndHorizontal();

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

			foreach (var slot in materialSlots)
			{
				GUILayout.Space(5);
				EditorGUILayout.BeginVertical("box");
				DrawMaterialItem(slot);
				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.EndScrollView();
			GUILayout.Space(10);
			DrawSeparator();
		}

		private void DrawMaterialItem(MaterialSlot slot)
		{
			EditorGUILayout.BeginHorizontal();

			bool newSelected = EditorGUILayout.Toggle(slot.selected, GUILayout.Width(20));
			if (newSelected != slot.selected)
			{
				slot.selected = newSelected;
				UpdateSelectAllState();
			}

			var newMaterial = (Material)EditorGUILayout.ObjectField(slot.current, typeof(Material), false);
			if (newMaterial != null && newMaterial != slot.current)
				ReplaceMaterial(slot, newMaterial);

			if (GUILayout.Button(L("inspect", "Inspect"), GUILayout.Width(60)))
				InspectMaterial(slot.current);

			if (GUILayout.Button(L("clone", "Clone"), GUILayout.Width(60)))
				CloneMaterial(slot);

			if (GUILayout.Button(L("reset", "Reset"), GUILayout.Width(60)))
				ResetMaterial(slot);

			EditorGUILayout.EndHorizontal();

			bool newFoldout = EditorGUILayout.Foldout(slot.foldout, string.Format(L("usedBy", "Used by {0} renderer(s)"), slot.renderers.Count), true);
			if (newFoldout != slot.foldout)
				slot.foldout = newFoldout;

			if (newFoldout)
			{
				EditorGUI.indentLevel++;
				foreach (var renderer in slot.renderers)
					EditorGUILayout.ObjectField(renderer, typeof(Renderer), true);
				EditorGUI.indentLevel--;
			}
		}

		// --- Data ---

		private void ScanMaterials()
		{
			materialSlots.Clear();
			selectAll = false;

			if (rootObject == null) return;

			if (rootObject is GameObject go)
			{
				var seen = new Dictionary<Material, MaterialSlot>();
				foreach (var renderer in go.GetComponentsInChildren<Renderer>(includeInactive))
				{
					foreach (var material in renderer.sharedMaterials)
					{
						if (material == null) continue;
						if (!seen.TryGetValue(material, out var slot))
						{
							slot = new MaterialSlot { current = material, original = material };
							seen[material] = slot;
							materialSlots.Add(slot);
						}
						slot.renderers.Add(renderer);
					}
				}
			}
			else if (rootObject is Material rootMat)
			{
				materialSlots.Add(new MaterialSlot { current = rootMat, original = rootMat });
			}
		}

		// --- Operations ---

		private void ReplaceMaterial(MaterialSlot slot, Material newMaterial)
		{
			var oldMaterial = slot.current;
			foreach (var renderer in slot.renderers)
			{
				Undo.RecordObject(renderer, $"Replace Material {oldMaterial.name} with {newMaterial.name}");
				var mats = renderer.sharedMaterials;
				for (int i = 0; i < mats.Length; i++)
				{
					if (mats[i] == oldMaterial)
						mats[i] = newMaterial;
				}
				renderer.sharedMaterials = mats;
				EditorUtility.SetDirty(renderer);
			}
			slot.current = newMaterial;
			Repaint();
		}

		private void CloneMaterial(MaterialSlot slot)
		{
			string path = AssetDatabase.GetAssetPath(slot.current);
			if (string.IsNullOrEmpty(path)) return;

			string newPath = GenerateClonePath(path);
			AssetDatabase.CreateAsset(new Material(slot.current), newPath);
			AssetDatabase.SaveAssets();

			var cloned = AssetDatabase.LoadAssetAtPath<Material>(newPath);
			if (cloned != null)
				ReplaceMaterial(slot, cloned);
		}

		private void ResetMaterial(MaterialSlot slot)
		{
			if (slot.original == null || slot.original == slot.current) return;
			ReplaceMaterial(slot, slot.original);
		}

		// --- Batch Operations ---

		private void BatchReplaceSelected()
		{
			if (batchReplaceMaterial == null) return;
			foreach (var slot in materialSlots.Where(s => s.selected).ToList())
				ReplaceMaterial(slot, batchReplaceMaterial);
			ClearMaterialSelection();
		}

		private void BatchCloneSelected()
		{
			foreach (var slot in materialSlots.Where(s => s.selected).ToList())
				CloneMaterial(slot);
			ClearMaterialSelection();
		}

		private void BatchResetSelected()
		{
			foreach (var slot in materialSlots.Where(s => s.selected).ToList())
				ResetMaterial(slot);
			ClearMaterialSelection();
		}

		private void InspectMaterial(Material material)
		{
			inspectedMaterials.Clear();
			inspectedMaterials.Add(material);
			currentTab = 1;
			ScanTextures();
		}

		private void InspectSelectedMaterials()
		{
			var mats = materialSlots.Where(s => s.selected).Select(s => s.current).ToList();
			if (mats.Count == 0) return;
			inspectedMaterials.Clear();
			inspectedMaterials.AddRange(mats);
			currentTab = 1;
			ScanTextures();
		}

		// --- Helpers ---

		private void ClearMaterialSelection()
		{
			foreach (var slot in materialSlots) slot.selected = false;
			selectAll = false;
		}

		private void UpdateSelectAllState()
		{
			selectAll = materialSlots.Count > 0 && materialSlots.All(s => s.selected);
		}
	}
}
