using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Elevation))]
public class ElevationEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var obj = target as Elevation;
		base.OnInspectorGUI();
		var editor = Editor.CreateEditor(obj.config);
		var root = editor.CreateInspectorGUI();
		editor.OnInspectorGUI();
	}

}