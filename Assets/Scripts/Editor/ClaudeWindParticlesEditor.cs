using Codice.Client.GameUI.Checkin;
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


	bool tryToggleAll = false;
	bool setAllToOn = true;
	bool dontUpdate = false;

	public override void OnInspectorGUI()
	{
		var obj = target as ClaudeWindParticles;
		base.OnInspectorGUI();

		// Visualise the Global Config
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Global Config", EditorStyles.boldLabel);
		var editor = Editor.CreateEditor(obj.globalConfig);
		var root = editor.CreateInspectorGUI();
		editor.OnInspectorGUI();

		EditorGUILayout.Space();
		// Enable/Disable Elevations
		if (obj.elevationDict != null)
		{
			var boxStyle = new GUIStyle(GUI.skin.box);
			boxStyle.normal.background = new Texture2D(1, 1);
			
			

			foldOutConfigs = EditorGUILayout.BeginFoldoutHeaderGroup(foldOutConfigs, "Enabled Elevations");
			if (foldOutConfigs)
			{
				using (new EditorGUI.IndentLevelScope())
				{
					using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
					{
						var allEnabled = obj.elevationDict.All(pair => pair.Value.enableUpdates);
						var anyEnabled = obj.elevationDict.Any(pair => pair.Value.enableUpdates);
						var enabledCount = obj.elevationDict.Count(pair => pair.Value.enableUpdates);

						// If all enabled, want to set all to off. Else want to set all to on
						setAllToOn = !allEnabled;

						var showMixedValue = EditorGUI.showMixedValue;
						GUILayout.Box("", boxStyle, GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(false));
						//var display = anyEnabled;
						//EditorGUI.showMixedValue = anyEnabled && !allEnabled;
						//var change = EditorGUILayout.Toggle("Enable All:", display, GUILayout.ExpandWidth(false));
						//EditorGUI.showMixedValue = showMixedValue;
						//Debug.Log($"Toggle States: {display}, {change}");
						if (GUILayout.Button("Enable All")) { obj.EnableAllLayers(); }
						if (GUILayout.Button("Disable All")) { obj.DisableAllLayers(); }
						//tryToggleAll = display != change;

					}

					
					foreach (var (elevation, elevationObject) in obj.elevationDict)
					{
						using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
						{
							var boxColour = elevationObject.config.particleColour;
							boxStyle.normal.background.SetPixel(0, 0, boxColour);
							boxStyle.normal.background.Apply();
							GUILayout.Box("", boxStyle, GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(false));

							bool oldState = elevationObject.enableUpdates;
							elevationObject.enableUpdates = EditorGUILayout.Toggle($"Enabled ({elevation}):", elevationObject.enableUpdates, GUILayout.ExpandWidth(false));
							//if ((oldState != elevationObject.enableUpdates))
							//{
							//	Debug.Log("Don't Update!");
							//	dontUpdate = true;
							//}
							EditorGUILayout.Space();
						}
					}
				}
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

			if (tryToggleAll)
			{
				Debug.Log($"Try Toggle All! {tryToggleAll}, {setAllToOn}, {dontUpdate}");
				//if (setAllToOn) { obj.EnableAllLayers(); }
				//else { obj.DisableAllLayers();}
			}
			tryToggleAll = false;
			dontUpdate = false;
		}

		if (GUILayout.Button("Reload Data"))
		{
			obj.rebuildBuffers(false);
		}
	}

}