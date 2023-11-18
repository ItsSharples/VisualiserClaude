using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(ClaudeWindParticles))]
class ClaudeWindParticlesEditor : Editor
{

	public override void OnInspectorGUI()
	{
		var obj = target as ClaudeWindParticles;
		base.OnInspectorGUI();

		if (GUILayout.Button("Update Data"))
		{
			obj.rebuildBuffers(true);
		}
	}

}