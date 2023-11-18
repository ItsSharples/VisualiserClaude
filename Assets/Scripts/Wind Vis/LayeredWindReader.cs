using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using OpenCover.Framework.Model;
using System.IO;
using UnityEditor;
using UnityEngine.Windows;
using static System.Net.WebRequestMethods;
using System.Linq;
using Unity.VisualScripting;
using System.ComponentModel;
using static LayeredWindParticles;
using System;
using UnityEngine.Windows.Speech;

[ExecuteInEditMode]
public class LayeredWindReader : MonoBehaviour
{
	public string windFilePath;

	public ComputeShader windCompute;
	public ComputeShader temperatureCompute;
	public MeshRenderer globe;

	//public RenderTexture windTexture { get; private set; }
	//public RenderTexture upscaledWindTexture { get; private set; }

	//public List<RenderTexture> GeneratedTextures { get; private set; }
	public Dictionary<float, RenderTexture> TextureDictionary { get; private set; }
	public List<float> elevationLookup;
	public RenderTexture WindTextureForLayer(int layer)
	{
		// If the layer is in the valid range
		if (0 < layer && layer < elevationLookup.Count) { return WindTextureForElevation(elevationLookup[layer]); }
		return null;
	}
	public RenderTexture WindTextureForElevation(float elevation)
	{
		if (TextureDictionary.TryGetValue(elevation, out var texture)) { return texture; }
		return null;
	}
	public int TextureCount => TextureDictionary.Count;

	public float currentElevation { get; private set; }
	public RenderTexture groundTexture;
	public RenderTexture currentWindTexture => TextureDictionary[currentElevation];

	public bool isLoaded => (TextureDictionary != null);
	/*
	static RenderTexture GetRenderTexture(string windFile, ref ComputeShader compute) { 
		// Read
		JsonTextReader reader = new JsonTextReader(new System.IO.StringReader(windFile));

		List<float> windX = new List<float>();
		List<float> windY = new List<float>();
		var currentWindList = windX;

		while (reader.Read())
		{
			if (reader.TokenType == JsonToken.PropertyName)
			{
				string propertyName = (string)reader.Value;
				if (propertyName == "data")
				{
					reader.Read(); // Start array
					reader.Read(); // First value
					while (reader.TokenType != JsonToken.EndArray)
					{
						double val = (double)reader.Value;
						currentWindList.Add((float)val);
						reader.Read();
					}
					currentWindList = windY;
				}
			}
		}

		// Create wind direction buffer
		Vector2[] windVectors = new Vector2[windX.Count];
		for (int i = 0; i < windVectors.Length; i++)
		{
			windVectors[i] = new Vector2(windX[i], windY[i]);
		}

		ComputeBuffer windBuffer = ComputeHelper.CreateStructuredBuffer(windVectors);
		var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SFloat;
		RenderTexture windTexture;
		windTexture = ComputeHelper.CreateRenderTexture(360, 181, FilterMode.Bilinear, format, "Wind Vector Map");
		windTexture.wrapMode = TextureWrapMode.Repeat;

		compute.SetTexture(0, "WindTexture", windTexture);
		compute.SetBuffer(0, "WindVectors", windBuffer);
		ComputeHelper.Dispatch(compute, windTexture);
		windBuffer.Release();

		var upscaledWindTexture = ComputeHelper.BicubicUpscale(windTexture, 8);
		windTexture.Release();
		return upscaledWindTexture;
	}
	*/


	public void SetCurrentElevation(float elevation)
	{
		currentElevation = elevation;
		//globe.material.mainTexture = currentWindTexture;
	}
	private void Awake()
	{
		LoadFile();
		if (Application.isPlaying)
		{
			UpdateMaterials();
		}
	}
	public void UpdateMaterials()
	{

		if (groundTexture != null)
		{
			globe.sharedMaterial.mainTexture = groundTexture;
		}
		if (TextureDictionary != null)
		{
			foreach (var (elevation, texture) in TextureDictionary)
			{
				int index = elevationLookup.FindIndex((x) => (x == elevation));
				globe.sharedMaterial.SetTexture(index, texture);
			}
		}
	}
	public void LoadFile()
	{
		//GeneratedTextures = new List<RenderTexture>();
		elevationLookup = new List<float>();
		TextureDictionary = new Dictionary<float, RenderTexture>();

		var text = System.IO.File.ReadAllText(windFilePath.ToString());
		var dictionaries = RenderHelperLayered.GetRenderTexturesFromLayeredJSONText(text, ref windCompute, ref temperatureCompute);
		TextureDictionary = dictionaries.WindTextures;
		//Debug.Log($"{TextureDictionary.Keys.ToCommaSeparatedString()}");
		//Debug.Log($"{dictionaries.TemperatureTextures.Keys.ToCommaSeparatedString()}");

		//GeneratedTextures.Add(RenderHelper.GetRenderTextureFromJSONText(text, ref compute));

		//globe.material.mainTexture = GeneratedTextures[currentTextureIndex];
		foreach (var (elevation, texture) in TextureDictionary) {
			elevationLookup.Add(elevation);
			//int index = elevationLookup.FindIndex((x) => (x == elevation));
			//globe.sharedMaterial.SetTexture(index, texture);
		}

		dictionaries.TemperatureTextures.TryGetValue(0, out groundTexture);

		// This should sort ascending
		elevationLookup.OrderBy((x) => (x));
	}
	//private void FixedUpdate()
	//{
	//	if (desiredTextureIndex != currentTextureIndex)
	//	{
	//		for (int i = 0; i < textureCount; i++)
	//		{

	//		}

	//		globe.material.mainTexture = GeneratedTextures[desiredTextureIndex];
	//		currentTextureIndex = desiredTextureIndex;
	//	}
	//}

	//private void FixedUpdate()
	//{
		
	//}

	void OnDestroy()
	{
		if (TextureDictionary != null)
		{
			ComputeHelper.Release(TextureDictionary.Values.ToArray());
		}
		groundTexture.Release();
	}

}