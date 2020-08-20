using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicGrass
{
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
		// Editor varaibles
		[Header("Grass")]
		[SerializeField] private bool useLODs = true;
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
		private Vector3 lodCenter = Vector3.zero;

		//--------------------------
		// MonoBehaviour methods
		//--------------------------
		private void Awake()
		{
			meshFilter = GetComponent<MeshFilter>();
			meshRenderer = GetComponent<MeshRenderer>();

			Populate();
		}

		private void Update()
		{
			lodCenter = Camera.main.transform.position - transform.position;
		}

		void LateUpdate()
		{
			if (_sheet == null) _sheet = new MaterialPropertyBlock();

			var espace_obj = Matrix4x4.identity * meshRenderer.transform.localToWorldMatrix;

			Vector4 lodCenterVec4 = new Vector4(lodCenter.x, lodCenter.y, lodCenter.z, 0f);

			meshRenderer.GetPropertyBlock(_sheet);
			_sheet.SetMatrix(ShaderIDs.EffSpace, espace_obj);
			_sheet.SetFloat(ShaderIDs.GrassSize, grassSize);
			_sheet.SetVector(ShaderIDs.LodCenter, lodCenterVec4);
			meshRenderer.SetPropertyBlock(_sheet);
		}

		private void OnDrawGizmos()
		{
			// LOD 0
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(lodCenter, boxSize.x);

			// LOD 1
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(lodCenter, boxSize.x * 2);

			// LOD 2
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(lodCenter, boxSize.x * 4);
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