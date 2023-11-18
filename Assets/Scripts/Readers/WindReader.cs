using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class WindReader : MonoBehaviour
{

	public TextAsset windFile;
	public ComputeShader compute;
	public MeshRenderer globe;

	public RenderTexture windTexture { get; private set; }
	public RenderTexture upscaledWindTexture { get; private set; }


	void Awake()
	{

		// Read
		JsonTextReader reader = new JsonTextReader(new System.IO.StringReader(windFile.text));

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
		windTexture = ComputeHelper.CreateRenderTexture(360, 181, FilterMode.Bilinear, format, "Wind Vector Map");
		windTexture.wrapMode = TextureWrapMode.Repeat;

		compute.SetTexture(0, "WindTexture", windTexture);
		compute.SetBuffer(0, "WindVectors", windBuffer);
		ComputeHelper.Dispatch(compute, windTexture);
		windBuffer.Release();

		upscaledWindTexture = ComputeHelper.BicubicUpscale(windTexture, 8);

		globe.material.mainTexture = upscaledWindTexture;
	}


	void OnDestroy()
	{
		ComputeHelper.Release(windTexture, upscaledWindTexture);
	}

}
