using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace YanK
{
	public partial class MaterialEditorTool
	{
		private struct TextureUsageInfo
		{
			public Material material;
			public string propertyName;
		}

		private class TextureSlot
		{
			public Texture current;
			public Texture original;
			public List<TextureUsageInfo> usages = new List<TextureUsageInfo>();
			public bool foldout;
			public bool selected;
		}

		private readonly List<TextureSlot> textureSlots = new List<TextureSlot>();
		private bool textureSelectAll;
		private Texture batchReplaceTexture;
		private Vector2 textureScrollPosition;

		// --- UI ---

		private void DrawTextureControlPanel()
		{
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(L("rescanTextures", "Rescan Textures"), GUILayout.Height(25)))
			{
				includeInactive = false;
				inspectedMaterials.Clear();
				ScanTextures();
			}
			if (GUILayout.Button(L("forceScanInactive", "Scan Include Inactive"), GUILayout.Height(25)))
			{
				includeInactive = true;
				inspectedMaterials.Clear();
				ScanTextures();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(L("cloneSelected", "Clone Selected"), GUILayout.Height(25)))
				BatchCloneSelectedTextures();
			if (GUILayout.Button(L("replaceSelected", "Replace Selected"), GUILayout.Height(25)))
				BatchReplaceSelectedTextures();
			if (GUILayout.Button(L("resetSelected", "Reset Selected"), GUILayout.Height(25)))
				BatchResetSelectedTextures();
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(L("batchReplaceTexture", "Batch Replace Texture"), GUILayout.Width(180));
			batchReplaceTexture = (Texture)EditorGUILayout.ObjectField(batchReplaceTexture, typeof(Texture), false);
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10);
			DrawSeparator();
		}

		private void DrawTextureList()
		{
			if (textureSlots.Count == 0)
			{
				EditorGUILayout.HelpBox(L("noTextures", "No textures found. Drop a GameObject or Material in the Root Object field and scan again."), MessageType.Info);
				GUILayout.Space(10);
				return;
			}

			EditorGUILayout.BeginHorizontal();

			bool newSelectAll = EditorGUILayout.ToggleLeft(L("selectAll", "Select All"), textureSelectAll, GUILayout.Width(100));
			if (newSelectAll != textureSelectAll)
			{
				textureSelectAll = newSelectAll;
				foreach (var slot in textureSlots)
					slot.selected = textureSelectAll;
			}

			GUILayout.FlexibleSpace();
			if (inspectedMaterials.Count > 0)
			{
				string label = string.Format(L("inspecting", "Inspecting {0} material(s)"), inspectedMaterials.Count);
				EditorGUILayout.LabelField(label, rightAlignBoldStyle);
				if (GUILayout.Button(L("clearInspect", "Clear Inspect"), GUILayout.Width(100)))
				{
					inspectedMaterials.Clear();
					ScanTextures();
				}
			}
			else
			{
				EditorGUILayout.LabelField(string.Format(L("texturesInUse", "{0} Textures in Use"), textureSlots.Count), rightAlignBoldStyle);
			}
			EditorGUILayout.EndHorizontal();

			textureScrollPosition = EditorGUILayout.BeginScrollView(textureScrollPosition, GUILayout.ExpandHeight(true));

			foreach (var slot in textureSlots)
			{
				GUILayout.Space(5);
				EditorGUILayout.BeginVertical("box");
				DrawTextureItem(slot);
				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.EndScrollView();
			GUILayout.Space(10);
			DrawSeparator();
		}

		private void DrawTextureItem(TextureSlot slot)
		{
			EditorGUILayout.BeginHorizontal();

			bool newSelected = EditorGUILayout.Toggle(slot.selected, GUILayout.Width(20));
			if (newSelected != slot.selected)
			{
				slot.selected = newSelected;
				UpdateTextureSelectAllState();
			}

			var newTexture = (Texture)EditorGUILayout.ObjectField(slot.current, typeof(Texture), false);
			if (newTexture != null && newTexture != slot.current)
				ReplaceTexture(slot, newTexture);

			if (GUILayout.Button(L("clone", "Clone"), GUILayout.Width(60)))
				CloneTexture(slot);

			if (GUILayout.Button(L("reset", "Reset"), GUILayout.Width(60)))
				ResetTexture(slot);

			EditorGUILayout.EndHorizontal();

			string foldoutLabel = string.Format(L("usedByMaterials", "Used by {0} material(s)"), slot.usages.Select(u => u.material).Distinct().Count());
			bool newFoldout = EditorGUILayout.Foldout(slot.foldout, foldoutLabel, true);
			if (newFoldout != slot.foldout)
				slot.foldout = newFoldout;

			if (newFoldout)
			{
				EditorGUI.indentLevel++;
				foreach (var usage in slot.usages)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.ObjectField(usage.material, typeof(Material), false);
					EditorGUILayout.LabelField(usage.propertyName, GUILayout.Width(150));
					EditorGUILayout.EndHorizontal();
				}
				EditorGUI.indentLevel--;
			}
		}

		// --- Data ---

		private void ScanTextures()
		{
			textureSlots.Clear();
			textureSelectAll = false;

			var materials = new List<Material>();

			if (inspectedMaterials.Count > 0)
			{
				materials.AddRange(inspectedMaterials);
			}
			else if (rootObject is GameObject go)
			{
				foreach (var renderer in go.GetComponentsInChildren<Renderer>(includeInactive))
				{
					foreach (var mat in renderer.sharedMaterials)
					{
						if (mat != null && !materials.Contains(mat))
							materials.Add(mat);
					}
				}
			}
			else if (rootObject is Material rootMat)
			{
				materials.Add(rootMat);
			}

			var seen = new Dictionary<Texture, TextureSlot>();
			foreach (var material in materials)
			{
				if (material.shader == null) continue;
				var shader = material.shader;
				int propCount = ShaderUtil.GetPropertyCount(shader);
				for (int i = 0; i < propCount; i++)
				{
					if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
						continue;

					string propName = ShaderUtil.GetPropertyName(shader, i);
					var texture = material.GetTexture(propName);
					if (texture == null) continue;

					if (!seen.TryGetValue(texture, out var slot))
					{
						slot = new TextureSlot { current = texture, original = texture };
						seen[texture] = slot;
						textureSlots.Add(slot);
					}
					slot.usages.Add(new TextureUsageInfo { material = material, propertyName = propName });
				}
			}
		}

		// --- Operations ---

		private void ReplaceTexture(TextureSlot slot, Texture newTexture)
		{
			var oldTexture = slot.current;
			foreach (var usage in slot.usages)
			{
				Undo.RecordObject(usage.material, $"Replace Texture {oldTexture.name} with {newTexture.name}");
				usage.material.SetTexture(usage.propertyName, newTexture);
				EditorUtility.SetDirty(usage.material);
			}
			slot.current = newTexture;
			Repaint();
		}

		private void CloneTexture(TextureSlot slot)
		{
			string path = AssetDatabase.GetAssetPath(slot.current);
			if (string.IsNullOrEmpty(path)) return;

			string newPath = GenerateClonePath(path);
			AssetDatabase.CopyAsset(path, newPath);
			AssetDatabase.SaveAssets();

			var newTexture = AssetDatabase.LoadAssetAtPath<Texture>(newPath);
			if (newTexture != null)
				ReplaceTexture(slot, newTexture);
		}

		private void ResetTexture(TextureSlot slot)
		{
			if (slot.original == null || slot.original == slot.current) return;
			ReplaceTexture(slot, slot.original);
		}

		// --- Batch Operations ---

		private void BatchReplaceSelectedTextures()
		{
			if (batchReplaceTexture == null) return;
			foreach (var slot in textureSlots.Where(s => s.selected).ToList())
				ReplaceTexture(slot, batchReplaceTexture);
			ClearTextureSelection();
		}

		private void BatchCloneSelectedTextures()
		{
			foreach (var slot in textureSlots.Where(s => s.selected).ToList())
				CloneTexture(slot);
			ClearTextureSelection();
		}

		private void BatchResetSelectedTextures()
		{
			foreach (var slot in textureSlots.Where(s => s.selected).ToList())
				ResetTexture(slot);
			ClearTextureSelection();
		}

		// --- Helpers ---

		private void ClearTextureSelection()
		{
			foreach (var slot in textureSlots) slot.selected = false;
			textureSelectAll = false;
		}

		private void UpdateTextureSelectAllState()
		{
			textureSelectAll = textureSlots.Count > 0 && textureSlots.All(s => s.selected);
		}
	}
}
