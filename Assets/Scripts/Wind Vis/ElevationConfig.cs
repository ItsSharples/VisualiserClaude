using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "ElevationCfg", menuName = "Wind Data/Elevation Config")]
public class ElevationConfig : ScriptableObject
{
	public float heightScale = 1.0f;
	public float particleScale = 2f;
	internal float stretch = 0;
	public Color particleColour = Color.white;
}