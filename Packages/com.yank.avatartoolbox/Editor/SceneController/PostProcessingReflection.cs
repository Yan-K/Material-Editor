using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace YanK
{
	/// <summary>
	/// Reflection wrapper around Unity Post Processing v3 (com.unity.postprocessing).
	/// Returns false / no-ops gracefully if the package is not installed.
	/// </summary>
	internal static class PostProcessingReflection
	{
		private static bool _probed;
		private static Type _volumeType;
		private static Type _layerType;
		private static Type _profileType;
		private static FieldInfo _volumeIsGlobalField;
		private static FieldInfo _volumeWeightField;
		private static FieldInfo _volumePriorityField;
		// PostProcessVolume.sharedProfile is a public *field* in v3, not a property.
		// We probe both so the code is forward-compatible if a future release promotes it.
		private static PropertyInfo _volumeSharedProfileProp;
		private static FieldInfo _volumeSharedProfileField;
		private static FieldInfo _layerVolumeLayerField;

		public static bool IsAvailable
		{
			get
			{
				Probe();
				return _volumeType != null && _layerType != null && _profileType != null;
			}
		}

		public static Type VolumeType
		{
			get
			{
				Probe();
				return _volumeType;
			}
		}

		public static Type LayerType
		{
			get
			{
				Probe();
				return _layerType;
			}
		}

		public static Type ProfileType
		{
			get
			{
				Probe();
				return _profileType;
			}
		}

		public static Component AddVolume(GameObject go, ScriptableObject profile, int layerIndex = 0)
		{
			Probe();
			if (_volumeType == null || go == null) return null;

			var comp = go.AddComponent(_volumeType);
			if (_volumeIsGlobalField != null) _volumeIsGlobalField.SetValue(comp, true);
			if (_volumeWeightField != null) _volumeWeightField.SetValue(comp, 1f);
			if (_volumePriorityField != null) _volumePriorityField.SetValue(comp, 0f);
			SetSharedProfile(comp, profile);
			go.layer = layerIndex;
			return comp;
		}

		public static void SetVolumeProfile(Component volume, ScriptableObject profile)
		{
			Probe();
			if (volume == null) return;
			SetSharedProfile(volume, profile);
		}

		private static void SetSharedProfile(object target, ScriptableObject profile)
		{
			if (target == null || profile == null) return;
			if (_volumeSharedProfileProp != null)
				_volumeSharedProfileProp.SetValue(target, profile);
			else if (_volumeSharedProfileField != null)
				_volumeSharedProfileField.SetValue(target, profile);
		}

		public static Component EnsureLayer(GameObject cameraGo, int layerMaskIndex)
		{
			Probe();
			if (_layerType == null || cameraGo == null) return null;

			var existing = cameraGo.GetComponent(_layerType);
			if (existing == null)
			{
				existing = cameraGo.AddComponent(_layerType);
			}

			if (_layerVolumeLayerField != null)
			{
				_layerVolumeLayerField.SetValue(existing, (LayerMask)(1 << layerMaskIndex));
			}
			return existing;
		}

		private static void Probe()
		{
			if (_probed) return;
			_probed = true;

			_volumeType = FindType("UnityEngine.Rendering.PostProcessing.PostProcessVolume");
			_layerType = FindType("UnityEngine.Rendering.PostProcessing.PostProcessLayer");
			_profileType = FindType("UnityEngine.Rendering.PostProcessing.PostProcessProfile");

			if (_volumeType != null)
			{
				_volumeIsGlobalField = _volumeType.GetField("isGlobal", BindingFlags.Public | BindingFlags.Instance);
				_volumeWeightField = _volumeType.GetField("weight", BindingFlags.Public | BindingFlags.Instance);
				_volumePriorityField = _volumeType.GetField("priority", BindingFlags.Public | BindingFlags.Instance);
				// Try property first (future-proof), fall back to field (v3 uses a public field).
				_volumeSharedProfileProp = _volumeType.GetProperty("sharedProfile", BindingFlags.Public | BindingFlags.Instance);
				if (_volumeSharedProfileProp == null)
					_volumeSharedProfileField = _volumeType.GetField("sharedProfile", BindingFlags.Public | BindingFlags.Instance);
			}

			if (_layerType != null)
			{
				_layerVolumeLayerField = _layerType.GetField("volumeLayer", BindingFlags.Public | BindingFlags.Instance);
			}
		}

		private static Type FindType(string fullName)
		{
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				var t = asm.GetType(fullName, false);
				if (t != null) return t;
			}
			return null;
		}
	}
}
