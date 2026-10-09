using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Funlab.MergeMaster
{
	[DisallowMultipleComponent]
	public class Outline : MonoBehaviour
	{
	public enum Mode
	{
		OutlineAll = 0,
		OutlineVisible = 1,
		OutlineHidden = 2,
		OutlineAndSilhouette = 3,
		SilhouetteOnly = 4
	}

	[Serializable]
	private class ListVector3
	{
		public List<Vector3> data;
	}

	private const string MaskMaterialPath = "materials/OutlineMask";

	private const string FillMaterialPath = "materials/OutlineFill";

	private static readonly int ZTestId = Shader.PropertyToID("_ZTest");

	private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

	private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

	private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

	[SerializeField]
	private Mode outlineMode;

	[SerializeField]
	public Color outlineColor = Color.white;

	[SerializeField]
	[Range(0f, 10f)]
	public float outlineWidth = 2f;

	[Header("Optional")]
	[SerializeField]
	[Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
	private bool precomputeOutline;

	[SerializeField]
	[HideInInspector]
	private List<Mesh> bakeKeys = new List<Mesh>();

	[SerializeField]
	[HideInInspector]
	private List<ListVector3> bakeValues = new List<ListVector3>();

	private Renderer[] renderers;

	private Material outlineMaskMaterial;

	private Material outlineFillMaterial;

	private bool needsUpdate;

	public Mode OutlineMode
	{
		get
		{
			return outlineMode;
		}
		set
		{
			outlineMode = value;
			needsUpdate = true;
		}
	}

	public Color OutlineColor
	{
		get
		{
			return outlineColor;
		}
		set
		{
			outlineColor = value;
			needsUpdate = true;
		}
	}

	public float OutlineWidth
	{
		get
		{
			return outlineWidth;
		}
		set
		{
			outlineWidth = value;
			needsUpdate = true;
		}
	}

	private void Awake()
	{
		renderers = GetComponentsInChildren<Renderer>();

		var maskSource = Resources.Load<Material>(MaskMaterialPath);
		var fillSource = Resources.Load<Material>(FillMaterialPath);
		if (maskSource != null)
		{
			outlineMaskMaterial = Instantiate(maskSource);
			outlineMaskMaterial.name = "OutlineMask (Instance)";
		}
		if (fillSource != null)
		{
			outlineFillMaterial = Instantiate(fillSource);
			outlineFillMaterial.name = "OutlineFill (Instance)";
		}

		LoadSmoothNormals();
		needsUpdate = true;
	}

	private void OnEnable()
	{
		if (renderers == null || outlineMaskMaterial == null || outlineFillMaterial == null)
		{
			return;
		}

		foreach (var renderer in renderers)
		{
			if (renderer == null)
			{
				continue;
			}

			var materials = new List<Material>(renderer.sharedMaterials);
			materials.Add(outlineMaskMaterial);
			materials.Add(outlineFillMaterial);
			renderer.materials = materials.ToArray();
		}

		needsUpdate = true;
	}

	private void OnValidate()
	{
		needsUpdate = true;

		if (bakeKeys == null) bakeKeys = new List<Mesh>();
		if (bakeValues == null) bakeValues = new List<ListVector3>();

		if ((!precomputeOutline && bakeKeys.Count != 0) || bakeKeys.Count != bakeValues.Count)
		{
			bakeKeys.Clear();
			bakeValues.Clear();
		}

		if (precomputeOutline && bakeKeys.Count == 0)
		{
			Bake();
		}
	}

	private void Update()
	{
		if (needsUpdate)
		{
			needsUpdate = false;
			UpdateMaterialProperties();
		}
	}

	private void OnDisable()
	{
		if (renderers == null)
		{
			return;
		}

		foreach (var renderer in renderers)
		{
			if (renderer == null)
			{
				continue;
			}

			var materials = new List<Material>(renderer.sharedMaterials);
			bool removed = materials.Remove(outlineMaskMaterial);
			removed |= materials.Remove(outlineFillMaterial);
			if (removed)
			{
				renderer.materials = materials.ToArray();
			}
		}
	}

	private void OnDestroy()
	{
		if (outlineMaskMaterial != null)
		{
			Destroy(outlineMaskMaterial);
		}
		if (outlineFillMaterial != null)
		{
			Destroy(outlineFillMaterial);
		}
	}

	private static bool IsMeshReadable(Mesh mesh)
	{
#if UNITY_LUNA
		return true;
#else
		return mesh != null && mesh.isReadable;
#endif
	}

	private void Bake()
	{
		var bakedMeshes = new HashSet<Mesh>();
		foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
		{
			var mesh = meshFilter.sharedMesh;
			if (mesh == null || !IsMeshReadable(mesh) || !bakedMeshes.Add(mesh))
			{
				continue;
			}

			bakeKeys.Add(mesh);
			bakeValues.Add(new ListVector3 { data = SmoothNormals(mesh) });
		}
	}

	private void LoadSmoothNormals()
	{
		foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
		{
			var mesh = meshFilter.sharedMesh;
			if (mesh == null || !IsMeshReadable(mesh) || !registeredMeshes.Add(mesh))
			{
				continue;
			}

			int index = bakeKeys != null ? bakeKeys.IndexOf(mesh) : -1;
			var smoothNormals = index >= 0 && index < bakeValues.Count && bakeValues[index].data != null
				? bakeValues[index].data
				: SmoothNormals(mesh);
			mesh.SetUVs(3, smoothNormals.ToArray());
		}

		// Skinned meshes get zeroed UV3 so the fill shader falls back to the vertex normal.
		foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
		{
			var mesh = skinnedMeshRenderer.sharedMesh;
			if (mesh == null || !IsMeshReadable(mesh) || !registeredMeshes.Add(mesh))
			{
				continue;
			}

			mesh.uv4 = new Vector2[mesh.vertexCount];
		}
	}

	private List<Vector3> SmoothNormals(Mesh mesh)
	{
		var vertices = mesh.vertices;
		var normals = mesh.normals;
		var smoothNormals = new List<Vector3>(normals);
		if (normals.Length != vertices.Length)
		{
			return smoothNormals;
		}

		var groups = new Dictionary<Vector3, List<int>>();
		for (int i = 0; i < vertices.Length; i++)
		{
			if (!groups.TryGetValue(vertices[i], out var group))
			{
				group = new List<int>(2);
				groups.Add(vertices[i], group);
			}
			group.Add(i);
		}

		foreach (var group in groups.Values)
		{
			if (group.Count == 1)
			{
				continue;
			}

			var smoothNormal = Vector3.zero;
			foreach (int index in group)
			{
				smoothNormal += normals[index];
			}
			smoothNormal.Normalize();

			foreach (int index in group)
			{
				smoothNormals[index] = smoothNormal;
			}
		}

		return smoothNormals;
	}

	private void UpdateMaterialProperties()
	{
		if (outlineMaskMaterial == null || outlineFillMaterial == null)
		{
			return;
		}

		outlineFillMaterial.SetColor(OutlineColorId, outlineColor);

		switch (outlineMode)
		{
			case Mode.OutlineAll:
				SetZTests(CompareFunction.Always, CompareFunction.Always);
				outlineFillMaterial.SetFloat(OutlineWidthId, outlineWidth);
				break;
			case Mode.OutlineVisible:
				SetZTests(CompareFunction.Always, CompareFunction.LessEqual);
				outlineFillMaterial.SetFloat(OutlineWidthId, outlineWidth);
				break;
			case Mode.OutlineHidden:
				SetZTests(CompareFunction.Always, CompareFunction.Greater);
				outlineFillMaterial.SetFloat(OutlineWidthId, outlineWidth);
				break;
			case Mode.OutlineAndSilhouette:
				SetZTests(CompareFunction.LessEqual, CompareFunction.Always);
				outlineFillMaterial.SetFloat(OutlineWidthId, outlineWidth);
				break;
			case Mode.SilhouetteOnly:
				SetZTests(CompareFunction.LessEqual, CompareFunction.Greater);
				outlineFillMaterial.SetFloat(OutlineWidthId, 0f);
				break;
		}
	}

	private void SetZTests(CompareFunction mask, CompareFunction fill)
	{
		outlineMaskMaterial.SetFloat(ZTestId, (float)mask);
		outlineFillMaterial.SetFloat(ZTestId, (float)fill);
	}
}
}
