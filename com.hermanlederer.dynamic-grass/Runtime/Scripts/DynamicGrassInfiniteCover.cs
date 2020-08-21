using System.Collections.Generic;
using UnityEngine;

namespace DynamicGrass
{
	[ExecuteAlways]
	public class DynamicGrassInfiniteCover : MonoBehaviour
	{
		//
		// Other components

		//
		// Editor varaibles
		[SerializeField] private int gridSize = 5;
		[SerializeField] private int cellSize = 8;
		[SerializeField] private GameObject populatorPrefab = null;

		//
		// Private variables
		private ChunkPool m_Pool;
		private List<DynamicGrassInfinitePopulator> activePopulators;
		private Dictionary<Vector2Int, DynamicGrassInfinitePopulator> populatorMap;

		private Vector3 lastCamPos = Vector3.zero;

		//
		// Public variables

		//--------------------------
		// MonoBehaviour methods
		//--------------------------
		void Awake()
		{
			m_Pool = new ChunkPool(transform, populatorPrefab);
			activePopulators = new List<DynamicGrassInfinitePopulator>();
			populatorMap = new Dictionary<Vector2Int, DynamicGrassInfinitePopulator>();
		}

		void OnEnable()
		{
			Awake();

			for (int i = transform.childCount - 1; i >= 0; --i)
				DestroyImmediate(transform.GetChild(i).gameObject);
		}

		void Update()
		{
			if (!populatorPrefab) return;
			if (m_Pool == null) { OnEnable(); return; }

			Camera cam = Camera.main;

			if (lastCamPos != cam.transform.position)
			{
				lastCamPos = cam.transform.position;

				// Recycling chungs too far from camera
				for (int i = 0; i < activePopulators.Count; ++i)
				{
					var populator = activePopulators[i];

					var camPosition2d = new Vector2(cam.transform.position.x, cam.transform.position.z);
					var populatorPosition2d = new Vector2(populator.transform.position.x, populator.transform.position.z);

					var camChunkPosition = new Vector2Int(Mathf.RoundToInt(camPosition2d.x / cellSize), Mathf.RoundToInt(camPosition2d.y / cellSize));
					var populatorChunkPosition = new Vector2Int(Mathf.RoundToInt(populatorPosition2d.x / cellSize), Mathf.RoundToInt(populatorPosition2d.y / cellSize));

					if (Vector2Int.Distance(camChunkPosition, populatorChunkPosition) > gridSize)
					{
						//Debug.DrawRay(populator.transform.position, new Vector3(cellSize / 2, 0, cellSize / 2), Color.red, 0.3f);
						//Debug.DrawRay(populator.transform.position, new Vector3(-cellSize / 2, 0, -cellSize / 2), Color.red, 0.3f);

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
						var camChunkPosition = new Vector2Int(Mathf.RoundToInt(cam.transform.position.x / cellSize), Mathf.RoundToInt(cam.transform.position.z / cellSize));
						var populatorChunkPosition = camChunkPosition + new Vector2Int(x, z);

						if (Vector2Int.Distance(camChunkPosition, populatorChunkPosition) <= gridSize)
						{
							if (!populatorMap.ContainsKey(populatorChunkPosition))
							{
								var populator = m_Pool.Get();
								populator.transform.position = new Vector3(populatorChunkPosition.x * cellSize, cam.transform.position.y, populatorChunkPosition.y * cellSize);
								activePopulators.Add(populator);
								populatorMap.Add(populatorChunkPosition, populator);

								//Debug.DrawRay(populator.transform.position, new Vector3(cellSize/2, 0, cellSize/2), Color.green, 0.3f);
								//Debug.DrawRay(populator.transform.position, new Vector3(-cellSize/2, 0, -cellSize/2), Color.green, 0.3f);

								populator.Populate();
							}
						}
					}
				}
			}
		}

		//private void OnDrawGizmos()
		//{
		//	Camera cam = Camera.main;
		//	for (int x = -gridSize; x <= gridSize; ++x)
		//	{
		//		for (int z = -gridSize; z <= gridSize; ++z)
		//		{
		//			var populatorChunkPosition = new Vector2Int(Mathf.RoundToInt(cam.transform.position.x / cellSize) + x, Mathf.RoundToInt(cam.transform.position.z / cellSize) + z);
		//			Gizmos.DrawWireCube(new Vector3(populatorChunkPosition.x * cellSize, cam.transform.position.y, populatorChunkPosition.y * cellSize), new Vector3(cellSize, 32, cellSize));
		//		}
		//	}
		//}

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

