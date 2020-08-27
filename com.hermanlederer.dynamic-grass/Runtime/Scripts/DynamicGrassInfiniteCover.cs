//#define USE_FOV // turned out to harm the performance instead of helping it, may still be useful with static or almost static cameras
#define DEBUG

using System.Collections.Generic;
using UnityEngine;

namespace DynamicGrass
{
	[ExecuteAlways]
	public class DynamicGrassInfiniteCover : MonoBehaviour
	{
		//
		// Editor varaibles
		[Header("Chunk grid")]
		[SerializeField] private int gridSize = 3;
		[SerializeField] private Vector2Int chunkSize = Vector2Int.one;
		[SerializeField] private float fieldOfView = 90;
		[SerializeField] private LayerMask layerMask = 0;

		[Header("Grass")]
		[SerializeField] private Material material = null;
		[SerializeField] private Vector4 lodCascades = Vector4.zero;
		[SerializeField] private Vector2 grassSize = Vector2.one;
		[SerializeField] [Range(0, 65536)] private int grassAmount = 0;
		[SerializeField] [Range(0, 180)] private float slopeThreshold = 45;

		[Header("Wind")]
		[SerializeField] private Vector2 windStrength = Vector2.zero;
		[SerializeField] private float windSpeed = 0f;
		[SerializeField] private float windScale = 1f;

		//
		// Private variables
		private ChunkPool m_Pool;
		private List<DynamicGrassInfinitePopulator> activePopulators;
		private Dictionary<Vector2Int, DynamicGrassInfinitePopulator> populatorMap;

		private Vector3 lastCamPos = Vector3.zero;
#if USE_FOV
		private Quaternion lastCamRot = Quaternion.identity;
#endif

		//--------------------------
		// MonoBehaviour methods
		//--------------------------
		void OnEnable()
		{
			// Populator params
			var populatorParameters = new DynamicGrassInfinitePopulatorParameters();
			populatorParameters.chunkSize = chunkSize;
			populatorParameters.layerMask = layerMask;
			populatorParameters.material = material;
			populatorParameters.lodCascades = lodCascades;
			populatorParameters.grassSize = grassSize;
			populatorParameters.grassAmount = grassAmount;
			populatorParameters.slopeThreshold = slopeThreshold;
			populatorParameters.windParams = new Vector4(windStrength.x, windStrength.y, windSpeed, windScale);

			// Initialization
			m_Pool = new ChunkPool(transform, populatorParameters);
			activePopulators = new List<DynamicGrassInfinitePopulator>();
			populatorMap = new Dictionary<Vector2Int, DynamicGrassInfinitePopulator>();

			// Removing all childrend in case some chunks were not removed for whatever reason
			for (int i = transform.childCount - 1; i >= 0; --i)
				DestroyImmediate(transform.GetChild(i).gameObject);
		}

		void Update()
		{
			Camera cam = Camera.main;

			bool updateChinks;
			updateChinks = (lastCamPos != cam.transform.position);
#if USE_FOV
			updateChinks = updateChinks || ( lastCamRot != cam.transform.rotation);
#endif

			if (updateChinks)
			{
				lastCamPos = cam.transform.position;
#if USE_FOV
				lastCamRot = cam.transform.rotation;
#endif

				// Recycling chunks too far from camera
				for (int i = 0; i < activePopulators.Count; ++i)
				{
					var populator = activePopulators[i];
					var flatPopPosition = FlattenPosition(populator.transform.position);

					if (!IsChunkVisible(cam, flatPopPosition, gridSize+1, fieldOfView + 10))
					{
						var populatorChunkPosition = WorldToChunkPosition(flatPopPosition);

#if DEBUG
						Debug.DrawRay(populator.transform.position, new Vector3(chunkSize.x / 2, 0, chunkSize.x / 2), Color.red, 0.3f);
						Debug.DrawRay(populator.transform.position, new Vector3(-chunkSize.x / 2, 0, -chunkSize.x / 2), Color.red, 0.3f);
#endif

						m_Pool.Recycle(populator);
						activePopulators.RemoveAt(i);
						populatorMap.Remove(populatorChunkPosition);
					}
				}

				// Filling in missing chunks around camera
				for (int x = -gridSize; x <= gridSize; ++x)
				{
					for (int z = -gridSize; z <= gridSize; ++z)
					{
						var camPosition = FlattenPosition(cam.transform.position);
						var popChunkPosition = WorldToChunkPosition(camPosition) + new Vector2Int(x, z);
						var popPosition = ChunkToWorldPosition(popChunkPosition);

						if (!IsChunkVisible(cam, popPosition, gridSize, fieldOfView)) continue;
						if (populatorMap.ContainsKey(popChunkPosition)) continue;
						
						var populator = m_Pool.Get();
						populator.transform.position = ExtrudePosition(popPosition);
						activePopulators.Add(populator);
						populatorMap.Add(popChunkPosition, populator);

#if DEBUG
						Debug.DrawRay(populator.transform.position, new Vector3(chunkSize.x / 2, 0, chunkSize.x / 2), Color.green, 0.3f);
						Debug.DrawRay(populator.transform.position, new Vector3(-chunkSize.x / 2, 0, -chunkSize.x / 2), Color.green, 0.3f);
#endif

						populator.Populate();
					}
				}
			}
		}

		//--------------------------
		// DynamicGrassInfiniteCover methods
		//--------------------------
		private Vector2 FlattenPosition(Vector3 pos)
		{
			return new Vector2(pos.x, pos.z);
		}

		private Vector3 ExtrudePosition(Vector2 pos)
		{
			return new Vector3(pos.x, transform.position.y, pos.y);
		}

		private Vector2Int WorldToChunkPosition(Vector2 worldPos)
		{
			return new Vector2Int(Mathf.RoundToInt(worldPos.x / chunkSize.x), Mathf.RoundToInt(worldPos.y / chunkSize.x));
		}

		private Vector2 ChunkToWorldPosition(Vector2Int worldPos)
		{
			return worldPos * chunkSize.x;
		}

		private bool IsChunkVisible(Camera cam, Vector2 populatorPos, int gridSize, float fieldOfView)
		{
			var camPosition = FlattenPosition(cam.transform.position);
#if USE_FOV
			var camVec = cam.transform.forward;
			var popVec = ExtrudePosition(populatorPos) - cam.transform.position;
#endif

			if (Vector2.Distance(camPosition, populatorPos) > gridSize * chunkSize.x) return false;
#if USE_FOV
			if (Vector2.Distance(camPosition, populatorPos) < cellSize) return true;
			if (Vector3.Angle(camVec, popVec) > fieldOfView) return false;
#endif

			return true;
		}

		//--------------------------
		// Chunk pool util
		//--------------------------
		private class ChunkPool
		{
			private Transform m_parent;
			private DynamicGrassInfinitePopulatorParameters m_populatorParams;

			private Stack<DynamicGrassInfinitePopulator> m_Stack;

			public ChunkPool(Transform parent, DynamicGrassInfinitePopulatorParameters populatorParams)
			{
				m_parent = parent;
				m_populatorParams = populatorParams;

				m_Stack = new Stack<DynamicGrassInfinitePopulator>();
			}

			~ChunkPool()
			{
				while (m_Stack.Count > 0)
				{
					DestroyImmediate(m_Stack.Pop());
				}
			}

			public DynamicGrassInfinitePopulator Get()
			{
				if (m_Stack.Count > 0)
				{
					var populator = m_Stack.Pop();
					populator.gameObject.SetActive(true);
					return populator;
				}

				var newPopulator = new GameObject().AddComponent<DynamicGrassInfinitePopulator>();
				newPopulator.transform.parent = m_parent;
				newPopulator.parameters = m_populatorParams;
				newPopulator.SetMaterial(m_populatorParams.material);
				return newPopulator;
			}

			public void Recycle(DynamicGrassInfinitePopulator populator)
			{
				populator.gameObject.SetActive(false);
				m_Stack.Push(populator);
			}
		}
	}
}

