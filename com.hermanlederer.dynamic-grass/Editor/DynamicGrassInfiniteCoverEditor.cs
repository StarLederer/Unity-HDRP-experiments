#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace DynamicGrass
{
	[CustomEditor(typeof(DynamicGrassInfiniteCover))]
	public class DynamicGrassInfiniteCoverEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (GUILayout.Button("Apply"))
			{
				DynamicGrassInfiniteCover cover = (DynamicGrassInfiniteCover)target;
				cover.OnEnable();
			}
		}
	}
}

#endif