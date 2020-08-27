using System.Collections.Generic;
using UnityEngine;

namespace DynamicGrass
{
	public class DynamicGrassInfinitePopulatorParameters
	{
		// Region
		public Vector2Int chunkSize;
		public LayerMask layerMask;

		// Grass
		public Material material;
		public Vector4 lodCascades;
		public Vector2 grassSize;
		public int grassAmount;
		public float slopeThreshold;

		// Wind
		public Vector4 windParams;
	}

	[ExecuteAlways]
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	public class DynamicGrassInfinitePopulator : MonoBehaviour
	{
		//
		// Other components
		private MeshFilter meshFilter;
		private MeshRenderer meshRenderer;

		//
		// Public variables
		public DynamicGrassInfinitePopulatorParameters parameters;

		//
		// Private variables
		private Mesh mesh;
		private MaterialPropertyBlock _sheet;
		private Vector3 lodCenterOS = Vector3.zero; // In object space

		//--------------------------
		// MonoBehaviour methods
		//--------------------------
		private void Awake()
		{
			meshFilter = GetComponent<MeshFilter>();
			meshRenderer = GetComponent<MeshRenderer>();
		}

		void LateUpdate()
		{
			if (_sheet == null) _sheet = new MaterialPropertyBlock();
			lodCenterOS = Camera.main.transform.position - transform.position;
			var espace_obj = Matrix4x4.identity * meshRenderer.transform.localToWorldMatrix;

			Vector4 lodCenterVec4 = new Vector4(lodCenterOS.x, lodCenterOS.y, lodCenterOS.z, 0f);
			meshRenderer.GetPropertyBlock(_sheet);
			_sheet.SetMatrix(ShaderIDs.EffSpace, espace_obj);
			_sheet.SetVector(ShaderIDs.LodCenter, lodCenterVec4);
			_sheet.SetVector(Shader.PropertyToID("_LodCascades"), parameters.lodCascades);
			_sheet.SetVector(ShaderIDs.GrassSize, parameters.grassSize);
			_sheet.SetVector(Shader.PropertyToID("_TimeParameters"), new Vector4(Time.time, Mathf.Sin(Time.time), 0, 0));
			_sheet.SetVector(Shader.PropertyToID("_WindParams"), parameters.windParams);
			meshRenderer.SetPropertyBlock(_sheet);
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.white;
			Gizmos.DrawWireCube(transform.position, new Vector3(parameters.chunkSize.x, parameters.chunkSize.y, parameters.chunkSize.x));
		}

		//--------------------------
		// DynamicGrassPopulator methods
		//--------------------------
		public void SetMaterial(Material material)
		{
			meshRenderer.material = material;
		}

		public void Populate()
		{
			Random.InitState((int)(transform.position.x + transform.position.z));
			List<Vector3> vertexPositions = new List<Vector3>();
			List<int> indicies = new List<int>();
			List<Vector3> normals = new List<Vector3>();

			int index = 0;
			for (int i = 0; i < parameters.grassAmount; ++i)
			{
				Vector3 origin = transform.position;

				origin.x += parameters.chunkSize.x * Random.Range(-0.5f, 0.5f);
				origin.z += parameters.chunkSize.x * Random.Range(-0.5f, 0.5f);
				origin.y += parameters.chunkSize.y / 2;

				Ray ray = new Ray(origin, Vector3.down);
				RaycastHit hit;
				if (Physics.Raycast(ray, out hit, parameters.chunkSize.y, parameters.layerMask))
				{
					DynamicGrassSurface surface = hit.transform.gameObject.GetComponent<DynamicGrassSurface>();
					if (surface != null)
					{
						if (Vector3.Angle(hit.normal, Vector3.up) <= parameters.slopeThreshold)
						{
							origin = hit.point;

							vertexPositions.Add(origin - transform.position);
							indicies.Add(index);
							++index;
							normals.Add(hit.normal);
						}
					}
				}
			}

			mesh = new Mesh();
			mesh.SetVertices(vertexPositions);
			mesh.SetIndices(indicies, MeshTopology.Points, 0);
			mesh.SetNormals(normals);
			meshFilter.mesh = mesh;
		}
	}
}