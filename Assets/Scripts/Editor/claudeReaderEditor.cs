using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(claudeReader))]
class claudeReaderEditor : Editor
{
	int currentLayer = 0;
	bool isFoldout = false;
	GenericMenu layerMenu;

	const float aspectRatio = 181.0f / 360.0f;
	float width => Screen.width;
	float height => width * aspectRatio;

	private void OnEnable()
	{
		layerMenu = new GenericMenu();
		foreach(var index in (target as claudeReader).GetLayers())
		{
			layerMenu.AddItem(new GUIContent(index.ToSafeString()), false, () => currentLayer = index);
		}
		
	}
	public override void OnInspectorGUI()
	{
		var obj = target as claudeReader;
		base.OnInspectorGUI();

		if(obj.TextureDictionary != null)
		foreach(var (_, temperatureTexture) in obj.TextureDictionary)
		{
			EditorGUILayout.ObjectField(temperatureTexture, typeof(RenderTexture), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
			//GUILayout.Box(temperatureTexture, GUILayout.Width(width), GUILayout.Height(height));
		}

		
		if(EditorGUILayout.DropdownButton(new GUIContent(currentLayer.ToSafeString()), FocusType.Passive))
		{
			layerMenu.ShowAsContext();
		}

		isFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(isFoldout, "Config");
		if (isFoldout)
		{
			GUILayout.Box(obj.WindTextureForLayer(currentLayer), GUILayout.Width(width), GUILayout.Height(height));
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

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