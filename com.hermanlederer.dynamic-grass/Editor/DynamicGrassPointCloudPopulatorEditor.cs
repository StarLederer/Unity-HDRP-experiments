using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DynamicGrass
{
	[CustomEditor(typeof(DynamicGrassPointCloudPopulator))]
	public class DynamicGrassPoinCloudPopulatorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			DynamicGrassPointCloudPopulator populator = (DynamicGrassPointCloudPopulator) target;
			if (GUILayout.Button("populate"))
			{
				populator.Populate();
			}
		}
	}

}