using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using OpenCover.Framework.Model;
using System.IO;
using UnityEditor;
using UnityEngine.Windows;

public class WindFolderReader : MonoBehaviour
{
	public string windFilesPath;

	public ComputeShader compute;
	public MeshRenderer globe;

	//public RenderTexture windTexture { get; private set; }
	//public RenderTexture upscaledWindTexture { get; private set; }

	public List<RenderTexture> GeneratedTextures { get; private set; }
	public RenderTexture currentWindTexture => GeneratedTextures[currentTextureIndex];
	public int textureCount => GeneratedTextures.Count;

	[SerializeField]
	bool interpolateBetweenFrames = false;
	[SerializeField]
	int interpolateNum = 4;
	[SerializeField]
	InterpolateTexture interpolator;

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

	void Awake()
	{
		GeneratedTextures = new List<RenderTexture>();
		
		foreach (var file in System.IO.Directory.EnumerateFiles(windFilesPath.ToString())) {
			if (System.IO.Path.GetExtension(file) == ".json")
			{
				var text = System.IO.File.ReadAllText(file);
				GeneratedTextures.Add(RenderHelper.GetRenderTextureFromJSONText(text, ref compute));
			}
		}

		if (interpolateBetweenFrames)
		{
			List<RenderTexture> interpolatedList = new List<RenderTexture>();

			for (int texture_i = 1; texture_i < textureCount; texture_i++)
			{
				var texture = GeneratedTextures[texture_i - 1];
				var next_texture = GeneratedTextures[texture_i];

				interpolatedList.Add(texture);
				for (int i = 0; i < interpolateNum; i++)
				{
					float interpolateStep = 1.0f / interpolateNum;

					var newTexture = interpolator.InterpolateTextures(texture, next_texture, interpolateStep * i);

					interpolatedList.Add(newTexture);
				}
			}
			interpolatedList.Add(GeneratedTextures[GeneratedTextures.Count - 1]);

			GeneratedTextures.Clear();
			GeneratedTextures = interpolatedList;
		}

		globe.material.mainTexture = GeneratedTextures[currentTextureIndex];
	}

	[SerializeField]
	[Range(0.0f, 10.0f)]
	float timeBetweenTextures = 1.0f;
	int internalTextureIndex => Mathf.FloorToInt(Time.realtimeSinceStartup / timeBetweenTextures);
	int desiredTextureIndex => internalTextureIndex % textureCount;
	int currentTextureIndex = 0;
	private void FixedUpdate()
	{
		if (desiredTextureIndex != currentTextureIndex)
		{
			globe.material.mainTexture = GeneratedTextures[desiredTextureIndex];
			currentTextureIndex = desiredTextureIndex;
		}
	}

	void OnDestroy()
	{
		ComputeHelper.Release(GeneratedTextures.ToArray());
	}

}
