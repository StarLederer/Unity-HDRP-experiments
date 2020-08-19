using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DynamicGrass
{
	[CustomEditor(typeof(DynamicGrassMeshPopulator))]
	public class DynamicGrassPopulatorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			DynamicGrassMeshPopulator populator = (DynamicGrassMeshPopulator) target;
			populator.Populate();
		}
	}

}