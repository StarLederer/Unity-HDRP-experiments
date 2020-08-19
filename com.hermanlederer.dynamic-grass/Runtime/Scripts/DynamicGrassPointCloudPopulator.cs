using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicGrass
{
	static class ShaderIDs
	{
		public static readonly int GrassSize = Shader.PropertyToID("_GrassSize");
		public static readonly int EffSpace = Shader.PropertyToID("_EffSpace");
	}

	[ExecuteAlways]
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	public class DynamicGrassPointCloudPopulator : MonoBehaviour
	{
		//
		// Other components
		private MeshFilter meshFilter;
		private MeshRenderer meshRenderer;

		//
		// Editor varaibles
		[Header("Grass")]
		[SerializeField] private float grassSize = 0.5f;
		[SerializeField] [Range(0, 65536)] private int grassAmount = 0;
		[SerializeField] [Range(0, 180)] private float slopeThreshold = 45;
		[Header("Region")]
		[SerializeField] private Vector3 boxSize = Vector3.zero;

		//
		// Private variables
		private Mesh mesh;
		MaterialPropertyBlock _sheet;

		//
		// Public variables


		//--------------------------
		// MonoBehaviour methods
		//--------------------------
		private void Awake()
		{
			meshFilter = GetComponent<MeshFilter>();
			meshRenderer = GetComponent<MeshRenderer>();
			Populate();
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(transform.position, boxSize);
		}

		void LateUpdate()
		{
			if (_sheet == null) _sheet = new MaterialPropertyBlock();

			//var espace = _origin != null ? _origin.worldToLocalMatrix : Matrix4x4.identity;
			var espace = Matrix4x4.identity;

			var espace_obj = espace * meshRenderer.transform.localToWorldMatrix;

			meshRenderer.GetPropertyBlock(_sheet);
			_sheet.SetMatrix(ShaderIDs.EffSpace, espace_obj);
			_sheet.SetFloat(ShaderIDs.GrassSize, grassSize);
			meshRenderer.SetPropertyBlock(_sheet);
		}

		//--------------------------
		// DynamicGrassPopulator methods
		//--------------------------
		public void Populate()
		{
			Random.InitState((int)(transform.position.x + transform.position.z));
			List<Vector3> vertexPositions = new List<Vector3>();
			List<int> indicies = new List<int>();
			List<Vector3> normals = new List<Vector3>();

			int index = 0;
			for (int i = 0; i < grassAmount; ++i)
			{
				Vector3 origin = transform.position;

				origin.x += boxSize.x * Random.Range(-0.5f, 0.5f);
				origin.z += boxSize.z * Random.Range(-0.5f, 0.5f);
				origin.y += boxSize.y / 2;

				Ray ray = new Ray(origin, Vector3.down);
				RaycastHit hit;
				if (Physics.Raycast(ray, out hit, boxSize.y))
				{
					DynamicGrassSurface surface = hit.transform.gameObject.GetComponent<DynamicGrassSurface>();
					if (surface != null)
					{
						if (Vector3.Angle(hit.normal, Vector3.up) <= slopeThreshold)
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
			Debug.Log("Populated");
		}
	}
}