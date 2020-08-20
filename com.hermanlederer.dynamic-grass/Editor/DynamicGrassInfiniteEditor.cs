using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DynamicGrass
{
	[CustomEditor(typeof(DynamicGrassInfinitePopulator))]
	public class DynamicGrassInfiniteEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			DynamicGrassInfinitePopulator populator = (DynamicGrassInfinitePopulator) target;
			if (GUILayout.Button("populate"))
			{
				populator.Populate();
			}
		}
	}

}