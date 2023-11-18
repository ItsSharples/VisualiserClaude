using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LayeredWindReader))]
public class LayeredWindReaderEditor : Editor
{


	bool texturesVisible;
	public override void OnInspectorGUI()
	{
		var obj = target as LayeredWindReader;
		base.DrawDefaultInspector();

		if (GUILayout.Button("Select File:"))
		{
			string path = EditorUtility.OpenFilePanel("Load Wind Data", "", "json");
			if (path.Length != 0)
			{
				obj.windFilePath = path;
				obj.LoadFile();
			}
		}
		if (GUILayout.Button("Update Materials"))
		{
			obj.UpdateMaterials();
		}
			if (!Application.isPlaying)
		{
			return;
		}

		if (obj == null) { return; }
		EditorGUILayout.Space();
		texturesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(texturesVisible, "Generated Textures");
		if (texturesVisible)
		{
			foreach (var (elevation, texture) in obj.TextureDictionary)
			{
				GUILayout.Box(texture, GUILayout.Width(360), GUILayout.Height(181));

				if (GUILayout.Button("Set current"))
				{
					obj.SetCurrentElevation(elevation);
				}

				GUILayout.Label($"Elevation: {elevation}.");
			}
			//GUILayout.Label($"Current Wind Texture");
		}
		EditorGUILayout.EndFoldoutHeaderGroup();
	}
}
