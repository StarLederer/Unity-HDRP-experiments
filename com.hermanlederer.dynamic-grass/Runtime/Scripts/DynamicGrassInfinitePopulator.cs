using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
		[SerializeField] private Vector4 lodCascades = Vector4.zero;
		[SerializeField] private Vector2 grassSize = Vector2.one;
		[SerializeField] [Range(0, 65536)] private int grassAmount = 0;
		[SerializeField] [Range(0, 180)] private float slopeThreshold = 45;

		[Header("Wind")]
		[SerializeField] private Vector2 windStrength = Vector2.zero;
		[SerializeField] private float windSpeed = 0f;
		[SerializeField] private float windScale = 1f;

		[Header("Region")]
		[SerializeField] private Vector3 boxSize = Vector3.zero;

		//
		// Private variables
		private Mesh mesh;
		private MaterialPropertyBlock _sheet;
		private Vector3 lodCenterOS = Vector3.zero; // In object space

		//
		// Public variables

		//--------------------------
		// MonoBehaviour methods
		//--------------------------
		private void Awake()
		{
			meshFilter = GetComponent<MeshFilter>();
			meshRenderer = GetComponent<MeshRenderer>();
		}

		private void OnEnable()
		{
			//Populate();
		}

		private void Update()
		{
			lodCenterOS = Camera.main.transform.position - transform.position;
		}

		void LateUpdate()
		{
			if (_sheet == null) _sheet = new MaterialPropertyBlock();

			var espace_obj = Matrix4x4.identity * meshRenderer.transform.localToWorldMatrix;

			Vector4 lodCenterVec4 = new Vector4(lodCenterOS.x, lodCenterOS.y, lodCenterOS.z, 0f);
			meshRenderer.GetPropertyBlock(_sheet);
			_sheet.SetMatrix(ShaderIDs.EffSpace, espace_obj);
			_sheet.SetVector(ShaderIDs.LodCenter, lodCenterVec4);
			_sheet.SetVector(Shader.PropertyToID("_LodCascades"), lodCascades);
			_sheet.SetVector(ShaderIDs.GrassSize, grassSize);
			_sheet.SetVector(Shader.PropertyToID("_TimeParameters"), new Vector4(Time.time, Mathf.Sin(Time.time), 0, 0));
			_sheet.SetVector(Shader.PropertyToID("_WindParams"), new Vector4(windStrength.x, windStrength.y, windSpeed, windScale));
			meshRenderer.SetPropertyBlock(_sheet);
		}

		private void OnDrawGizmos()
		{
			//DrawLodCascadeGizmos();
			DrawChunkGizmos();
		}

		private void DrawChunkGizmos()
		{
			Gizmos.color = Color.white;
			Gizmos.DrawWireCube(transform.position, boxSize);
		}

		private void DrawLodCascadeGizmos()
		{
			var lodCenter = Camera.main.transform.position;

			// LOD 0
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(lodCenter, lodCascades.x);

			// LOD 1
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(lodCenter, lodCascades.y);

			// LOD 2
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(lodCenter, lodCascades.z);
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
		}
	}
}