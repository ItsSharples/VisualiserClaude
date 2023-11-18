using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WindFolderReader))]
public class WindFolderReaderEditor : Editor
{
	

	bool texturesVisible;
	public override void OnInspectorGUI()
	{
		var obj = target as WindFolderReader;
		if (!Application.isPlaying)
		{
			base.DrawDefaultInspector();

			return;
		}

		
		if (obj == null) { return; }
		EditorGUILayout.Space();
		texturesVisible = EditorGUILayout.BeginFoldoutHeaderGroup(texturesVisible, "Generated Textures");
		if (texturesVisible)
		{
			foreach (var texture in obj.GeneratedTextures)
			{
				GUILayout.Box(texture, GUILayout.Width(360), GUILayout.Height(181));
			}

			GUILayout.Label($"Current Wind Texture");
		}
		EditorGUILayout.EndFoldoutHeaderGroup();
	}
}
