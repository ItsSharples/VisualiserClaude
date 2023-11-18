using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LayeredWindParticles))]
public class LayeredWindParticlesEditor : Editor
{


	bool texturesVisible;
	public override void OnInspectorGUI()
	{
		var obj = target as LayeredWindParticles;
		if (!Application.isPlaying)
		{
			base.DrawDefaultInspector();

			return;
		}
		base.DrawDefaultInspector();

		if (obj == null) { return; }
		EditorGUILayout.Space();
		texturesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(texturesVisible, "Elevation Config");
		if (texturesVisible)
		{
			foreach (var (elevation, elevationObject) in obj.elevationDict)
			{
				GUILayout.Label($"Elevation: ");
				var indent = EditorGUI.indentLevel;
				EditorGUI.indentLevel++;
				var editor = Editor.CreateEditor(elevationObject);
				var root = editor.CreateInspectorGUI();
				editor.OnInspectorGUI();
				EditorGUI.indentLevel = indent;

				//elevationObject.config.heightScale = EditorGUILayout.Slider(elevationObject.config.heightScale, 0.0f, 0.01f);
				//obj.heights[i] = EditorGUILayout.Slider(obj.heights[i], 0.0f, 0.01f);
				
			}
			//GUILayout.Label($"Current Wind Texture");
		}
		EditorGUILayout.EndFoldoutHeaderGroup();
	}
}
