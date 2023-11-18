using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(CameraController))]
class CameraControllerEditor : Editor
{

	public override void OnInspectorGUI()
	{
		var obj = target as CameraController;
		base.OnInspectorGUI();
		var editor = Editor.CreateEditor(obj.config);
		var root = editor.CreateInspectorGUI();
		editor.OnInspectorGUI();
	}

}