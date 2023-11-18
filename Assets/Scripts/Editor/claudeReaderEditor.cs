using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(claudeReader))]
class claudeReaderEditor : Editor
{

	public override void OnInspectorGUI()
	{
		var obj = target as claudeReader;
		base.OnInspectorGUI();

		if (GUILayout.Button("Update Data"))
		{
			obj.UpdateData();
		}
		if (GUILayout.Button("Update Materials"))
		{
			obj.UpdateMaterials();
		}
	}

}