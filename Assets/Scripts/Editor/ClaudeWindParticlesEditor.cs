using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(ClaudeWindParticles))]
class ClaudeWindParticlesEditor : Editor
{
	bool foldOutConfigs;
	bool enableAll;
	public override void OnInspectorGUI()
	{
		var obj = target as ClaudeWindParticles;
		base.OnInspectorGUI();

		if (GUILayout.Button("Update Data"))
		{
			obj.rebuildBuffers(true);
		}

		if (obj.elevationDict != null)
		{
			var boxStyle = new GUIStyle(GUI.skin.box);
			boxStyle.normal.background = new Texture2D(1, 1);
			foldOutConfigs = EditorGUILayout.BeginFoldoutHeaderGroup(foldOutConfigs, "Configs");
			if (foldOutConfigs)
			{
				using (new EditorGUI.IndentLevelScope())
				{
					using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
					{
						var allEnabled = obj.elevationDict.All(pair => pair.Value.enableUpdates);
						var anyEnabled = obj.elevationDict.Any(pair => pair.Value.enableUpdates);

						var showMixedValue = EditorGUI.showMixedValue;
						EditorGUI.showMixedValue = anyEnabled && !allEnabled;
						var display = anyEnabled;
						GUILayout.Box("", boxStyle, GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(false));
						var change = EditorGUILayout.Toggle("Enable All:", display, GUILayout.ExpandWidth(false));
						Debug.Log($"Toggle States: {display}, {change}");
						if (display != change)
						{
							if (allEnabled) { obj.DisableAllLayers(); }
							else {
								if(!anyEnabled) { obj.EnableAllLayers(); }
							}
						}
						EditorGUI.showMixedValue = showMixedValue;
					}


					foreach (var (elevation, elevationObject) in obj.elevationDict)
					{
						using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
						{
							var boxColour = elevationObject.config.particleColour;
							boxStyle.normal.background.SetPixel(0,0, boxColour);
							boxStyle.normal.background.Apply();
							GUILayout.Box("", boxStyle, GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(false));
							elevationObject.enableUpdates = EditorGUILayout.Toggle($"Enabled ({elevation}):", elevationObject.enableUpdates, GUILayout.ExpandWidth(false));
							EditorGUILayout.Space();
						}


						/*
						var editor = Editor.CreateEditor(elevationObject);
						var root = editor.CreateInspectorGUI();
						
						using (new EditorGUI.IndentLevelScope())
						{
							editor.OnInspectorGUI();
						}
						*/
					}
				}
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}
	}

}