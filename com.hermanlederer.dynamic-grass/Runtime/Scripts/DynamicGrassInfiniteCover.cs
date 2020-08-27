//#define USE_FOV // turned out to harm the performance instead of helping it, may still be useful with static or almost static cameras

using System.Collections.Generic;
using UnityEngine;

namespace DynamicGrass
{
	[ExecuteAlways]
	public class DynamicGrassInfiniteCover : MonoBehaviour
	{
		//
		// Editor varaibles
		[SerializeField] private int gridSize = 5;
		[SerializeField] private int cellSize = 8;
		[SerializeField] private float fieldOfView = 90;
		[SerializeField] private GameObject populatorPrefab = null;

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
			m_Pool = new ChunkPool(transform, populatorPrefab);
			activePopulators = new List<DynamicGrassInfinitePopulator>();
			populatorMap = new Dictionary<Vector2Int, DynamicGrassInfinitePopulator>();

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

						Debug.DrawRay(populator.transform.position, new Vector3(cellSize / 2, 0, cellSize / 2), Color.red, 0.3f);
						Debug.DrawRay(populator.transform.position, new Vector3(-cellSize / 2, 0, -cellSize / 2), Color.red, 0.3f);

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

						Debug.DrawRay(populator.transform.position, new Vector3(cellSize/2, 0, cellSize/2), Color.green, 0.3f);
						Debug.DrawRay(populator.transform.position, new Vector3(-cellSize/2, 0, -cellSize/2), Color.green, 0.3f);

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
			return new Vector2Int(Mathf.RoundToInt(worldPos.x / cellSize), Mathf.RoundToInt(worldPos.y / cellSize));
		}

		private Vector2 ChunkToWorldPosition(Vector2Int worldPos)
		{
			return worldPos * cellSize;
		}

		private bool IsChunkVisible(Camera cam, Vector2 populatorPos, int gridSize, float fieldOfView)
		{
			var camPosition = FlattenPosition(cam.transform.position);
#if USE_FOV
			var camVec = cam.transform.forward;
			var popVec = ExtrudePosition(populatorPos) - cam.transform.position;
#endif

			if (Vector2.Distance(camPosition, populatorPos) > gridSize * cellSize) return false;
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
			private GameObject m_populatorPrefab;

			private Stack<DynamicGrassInfinitePopulator> m_Stack;

			public ChunkPool(Transform parent, GameObject populatorPrefab)
			{
				m_parent = parent;
				m_populatorPrefab = populatorPrefab;

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

				var newPopulator = Instantiate(m_populatorPrefab, m_parent).GetComponent<DynamicGrassInfinitePopulator>();
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

