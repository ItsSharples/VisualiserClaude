using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(claudeReader))]
class claudeReaderEditor : Editor
{

	public override void OnInspectorGUI()
	{
		var obj = target as claudeReader;

		
		if (!obj.ShowDebugValues)
		{
			var computeProperty = serializedObject.FindProperty(nameof(obj.generationCompute));
			var textureProperty = serializedObject.FindProperty(nameof(obj.texture));
			var globeProperty = serializedObject.FindProperty(nameof(obj.globe));
			var textProperty = serializedObject.FindProperty(nameof(obj.jsonAsset));
			var debugProperty = serializedObject.FindProperty(nameof(obj.ShowDebugValues));

			
			EditorGUILayout.PropertyField(textProperty);
			GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);
			GUILayout.Label("Properties", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(computeProperty);
			EditorGUILayout.PropertyField(textureProperty);
			EditorGUILayout.PropertyField(globeProperty);	

			EditorGUILayout.PropertyField(debugProperty);

			var Tau = 2 * Mathf.PI;
			var currentOffset = obj.globe.sharedMaterial.mainTextureOffset * Tau;
			var newOffsetX = EditorGUILayout.Slider("Ground Texture Rotation", currentOffset.x, -Tau, Tau);
			currentOffset.x = newOffsetX / Tau;
			obj.globe.sharedMaterial.mainTextureOffset = currentOffset;


			if (serializedObject.hasModifiedProperties)
			{
				serializedObject.ApplyModifiedProperties();
			}
		}
		else
		{
			base.OnInspectorGUI();
		}

		

		

		if (GUILayout.Button("Load Data"))
		{
			obj.UpdateData();
		}
		if (GUILayout.Button("Update Materials"))
		{
			obj.UpdateMaterials();
		}
	}

}