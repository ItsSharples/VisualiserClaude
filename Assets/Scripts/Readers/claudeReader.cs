using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using UnityEngine.Analytics;
using UnityEngine.Assertions;

struct claudePixel
{
	public int index;
	public float longitude;
	public float latitude;

	public float ground_temp;
	public float[] air_temp;

	static public claudePixel Default { get { var New = new claudePixel(); New.index = -1; return New; } }
}

struct claudeBoundary
{
	public int index;
	public float longitude;
	public float latitude;

	public float[] u;
	public float[] v;

	
}

struct boundaryData
{
	public uint index;
	public uint elevationIndex;
	public float longitude;
	public float latitude;

	public float u;
	public float v;
};

/*
struct Boundary
{
	public int index;
	public int elevationIndex;
	public Vector2 longlat;
	public Vector2 uv;
};
*/
internal struct WindData
{
	public float elevationFromGround;
	public float[] uData;
	public float[] vData;
};

[ExecuteAlways]
public class claudeReader : MonoBehaviour
{
	public ComputeShader windCompute;
	public RenderTexture texture;
	public MeshRenderer globe;

    public string filePath;


	public Vector2 extremityGroundTemp;
	public Dictionary<float, ComputeBuffer> BufferDictionary { get; private set; }
	public Dictionary<float, RenderTexture> TextureDictionary { get; private set; }
	public List<float> elevationLookup;
	public uint numBoundaries;
	public int elevationCount;

	public float currentElevation { get; private set; }
	public RenderTexture groundTexture;

	public int TextureCount => TextureDictionary.Count;
	public bool isLoaded => (TextureDictionary != null);

	public RenderTexture WindTextureForLayer(int layer)
	{
		// If the layer is in the valid range
		if (0 < layer && layer < elevationLookup.Count) { return WindTextureForElevation(elevationLookup[layer]); }
		return null;
	}
	public ComputeBuffer GetBoundariesForElevation(float elevation)
	{
		if (BufferDictionary.TryGetValue(elevation, out var buffer)) { return buffer; }
		Debug.LogWarning($"Couldn't Fetch Boundary for: {elevation}");
		return null;

	}
	public RenderTexture WindTextureForElevation(float elevation)
	{
		if (TextureDictionary.TryGetValue(elevation, out var texture)) { return texture; }
		return null;
	}

	private void Start()
	{
		LoadFile();
		UpdateMaterials();
	}

	public void LoadFile() { UpdateData(); }
	public void UpdateData()
	{
		var jsonText = System.IO.File.ReadAllText(filePath.ToString());
		Debug.Log(jsonText);
		//JsonTextReader reader = new JsonTextReader(new System.IO.StringReader(jsonText));

		var objects = JsonConvert.DeserializeObject<JObject>(jsonText);

		var pixels = new Dictionary<int, claudePixel>();
		var boundaries = new Dictionary<int, claudeBoundary>();
		this.elevationCount = 0;

		foreach (var child in objects.Children())
		{
			var property = child as JProperty;
			var value = property.Value;
			var value_type = property.Value.Type;
			//Debug.Log($"{child.Path}, Values: {child.Values().Count()}");
			float closestToZero = -1;
			if (value_type.Equals(JTokenType.Array))
			{
				if (value[0].Type.Equals(JTokenType.Array))
				{
					if (value[0][0] is IComparable)
					{
						//Debug.Log($"Comparing: {property.Path}");
						var orderedBySmallest = value[0].OrderBy((item) =>
						{
							return Math.Abs((float)item);
						});
						closestToZero = (float)orderedBySmallest.First();
						var verySmalls = orderedBySmallest.TakeWhile(token => (float)token < 1e-4);
						Debug.Log($"{property.Path} has {verySmalls.Count()} very small values");
					}
				}
			}

			Debug.Log(	$"{property.Path}, {value_type}" +
						$"{(value_type.Equals(JTokenType.Array) ? $", {value.Count()}, {value[0].Count()}" : "")}" +
						$"{(value is IComparable ? $", Min: {value.Min()}, Max: {value.Max()}" : "")}" +
						$"{((value_type.Equals(JTokenType.Array)) ? (value[0] is IComparable ? $", Min[0]: {value.Min()}, Max[0]: {value.Max()}" : "") : "")}" +
						$"{((value_type.Equals(JTokenType.Array)) ? (value[0].Type.Equals(JTokenType.Array) ? (value[0][0] is IComparable ? $", Min[0][0]: {value[0].Min()}, Max[0][0]: {value[0].Max()}" : "") : "") : "")}" +
						$", Smallest Value: {closestToZero}" +
						$", {value}");



			switch (property.Path)
			{
				/// Air Temp is 2D
				case "air_temp":
					elevationCount = value.Count();
					// Test: Expect values count = 11
					// Test: Expect pixel_index < 150
					for (int pixel_index = 0; pixel_index < value[0].Count(); pixel_index++)
					{
						var temperatures = new List<float>();
						for (int elevation_index = 0; elevation_index < value.Count(); elevation_index++)
						{
							temperatures.Add((float)value[elevation_index][pixel_index]);
						}

						var pixel = pixels.GetValueOrDefault(pixel_index, claudePixel.Default);
						pixel.air_temp = temperatures.ToArray();
						pixels[pixel_index] = pixel;
					}
					break;
				case "ground_temp":
					var maxGroundTemp = (float)value.Max();
					var minGroundTemp = (float)value.Min();
					extremityGroundTemp = new Vector2((float)maxGroundTemp, (float)minGroundTemp);
					for (int pixel_index = 0; pixel_index < value.Count(); pixel_index++)
					{
						var pixel = pixels.GetValueOrDefault(pixel_index, claudePixel.Default);
						pixel.ground_temp = (float)value[pixel_index];
						pixels[pixel_index] = pixel;
					}
					break;
				/// Wind Speeds
				case "u":
					for (int boundary_index = 0; boundary_index < value[0].Count(); boundary_index++)
					{
						var u_speeds = new List<float>();
						for (int elevation_index = 0; elevation_index < value.Count(); elevation_index++)
						{
							u_speeds.Add((float)value[elevation_index][boundary_index]);
						}

						var boundary = boundaries.GetValueOrDefault(boundary_index, new claudeBoundary());
						boundary.u = u_speeds.ToArray();
						boundaries[boundary_index] = boundary;
					}
					break;
				case "v":
					for (int boundary_index = 0; boundary_index < value[0].Count(); boundary_index++)
					{
						var u_speeds = new List<float>();
						for (int elevation_index = 0; elevation_index < value.Count(); elevation_index++)
						{
							u_speeds.Add((float)value[elevation_index][boundary_index]);
						}

						var boundary = boundaries.GetValueOrDefault(boundary_index, new claudeBoundary());
						boundary.v = u_speeds.ToArray();
						boundaries[boundary_index] = boundary;
					}
					break;
				/// Pixel Location
				case "pixel_latitudes":
					for (int pixel_index = 0; pixel_index < value.Count(); pixel_index++)
					{
						var pixel = pixels.GetValueOrDefault(pixel_index, claudePixel.Default);
						pixel.latitude = (float)value[pixel_index];
						if(pixel.index == -1) { pixel.index = pixel_index; }
						Debug.Assert(pixel.index == pixel_index, $"{pixel.index}, {pixel_index}, not equal");
						pixels[pixel_index] = pixel;
					}
					break;
				case "pixel_longitudes":
					for (int pixel_index = 0; pixel_index < value.Count(); pixel_index++)
					{
						var pixel = pixels.GetValueOrDefault(pixel_index, claudePixel.Default);
						pixel.longitude = (float)value[pixel_index];
						pixels[pixel_index] = pixel;
					}
					break;
				/// TODO: Boundaries affect rendering
				case "boundary_latitudes":
					for (int boundary_index = 0; boundary_index < value.Count(); boundary_index++)
					{
						var boundary = boundaries.GetValueOrDefault(boundary_index, new claudeBoundary());
						boundary.latitude = (float)value[boundary_index];
						boundaries[boundary_index] = boundary;
					}
					break;
				case "boundary_longitudes":
					for (int boundary_index = 0; boundary_index < value.Count(); boundary_index++)
					{
						var boundary = boundaries.GetValueOrDefault(boundary_index, new claudeBoundary());
						boundary.longitude = (float)value[boundary_index];
						boundaries[boundary_index] = boundary;
					}
					break;
				default:
					break;
			}
		}

		for (int pixel_index = 0; pixel_index < pixels.Count; pixel_index++)
		{
			var pixel = pixels[pixel_index];
			Debug.Assert(pixel.index == pixel_index, $"{pixel.index}, {pixel_index}, not equal");
			pixel.index = pixel_index;
			pixels[pixel_index] = pixel;
		}

		Debug.Log($"Pixel[0] - Latitude: {pixels[0].latitude}, Longitude: {pixels[0].longitude}");

		for (int i = 0; i < boundaries.Count; i++)
		{
			var temp = boundaries[i];
			temp.index = i;
			boundaries[i] = temp;
		}

		Debug.Log($"Boundary Count: {boundaries.Count}\nPixel Count: {pixels.Count}");
		foreach (var (index, pixel) in pixels)
		{
			//Debug.Log($"{index} : {pixel.index}");
		}

		var pixelPoints = new List<Vector4>();

		foreach (var (index, pixel) in pixels)
		{
			pixelPoints.Add(new Vector4(pixel.latitude, pixel.longitude, pixel.index, pixel.ground_temp));

			//pixelPoints.Add(new Vector4(pixel.latitude, pixel.longitude + 2 * Mathf.PI, pixel.index, pixel.ground_temp));
			//pixelPoints.Add(new Vector4(pixel.latitude, pixel.longitude - 2 * Mathf.PI, pixel.index, pixel.ground_temp));
			//pixelPoints.Add(new Vector4(pixel.latitude + Mathf.PI, pixel.longitude, pixel.index, pixel.ground_temp));
			//pixelPoints.Add(new Vector4(pixel.latitude - Mathf.PI, pixel.longitude, pixel.index, pixel.ground_temp));

			//pixelPoints.Add(new Vector4(pixel.latitude + Mathf.PI, pixel.longitude - 2 * Mathf.PI, pixel.index, pixel.ground_temp));
			//pixelPoints.Add(new Vector4(pixel.latitude - Mathf.PI, pixel.longitude - 2 * Mathf.PI, pixel.index, pixel.ground_temp));
			//pixelPoints.Add(new Vector4(pixel.latitude + Mathf.PI, pixel.longitude + 2 * Mathf.PI, pixel.index, pixel.ground_temp));
			//pixelPoints.Add(new Vector4(pixel.latitude - Mathf.PI, pixel.longitude + 2 * Mathf.PI, pixel.index, pixel.ground_temp));
		}


		var boundaryPoints = new List<boundaryData>();
		foreach (var (index, boundary) in boundaries)
		{
			var data = new boundaryData();
			data.index = (uint)boundary.index;
			data.elevationIndex = (uint)0;
			Debug.Log($"{index}");
			data.u = boundary.u[data.elevationIndex];
			data.v = boundary.v[data.elevationIndex];

			data.latitude = boundary.latitude;
			data.longitude = boundary.longitude;
			boundaryPoints.Add(data);
		}

		int scale = 8;
		int width = 360 * scale;
		int height = 181 * scale;

		ComputeBuffer pixelBuffer = ComputeHelper.CreateStructuredBuffer(pixelPoints.ToArray());
		ComputeBuffer boundaryBuffer = ComputeHelper.CreateStructuredBuffer(boundaryPoints.ToArray());
		var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;

		RenderTexture outTexture;
		outTexture = ComputeHelper.CreateRenderTexture(width, height, FilterMode.Bilinear, format, "SDF Map");
		outTexture.wrapMode = TextureWrapMode.Repeat;


		Debug.Log("Rendering");
		/*
		var blankKernel = windCompute.FindKernel("CSBlankTexture");
		windCompute.SetTexture(blankKernel, "OutTexture", outTexture);
		ComputeHelper.Dispatch(windCompute, outTexture, kernelIndex: blankKernel); // Blank Texture
		*/
		var mainKernel = windCompute.FindKernel("CSMain");

		windCompute.SetInt("width", width);
		windCompute.SetInt("height", height);

		windCompute.SetInt("pixel_count", pixelPoints.Count);
		windCompute.SetInt("boundary_count", boundaryPoints.Count);
		windCompute.SetTexture(mainKernel, "OutTexture", outTexture);
		windCompute.SetBuffer(mainKernel, "Pixels", pixelBuffer);
		windCompute.SetBuffer(mainKernel, "Boundaries", boundaryBuffer);

		
		ComputeHelper.Dispatch(windCompute, width, height, pixelBuffer.count, kernelIndex: mainKernel); // Set Data
		pixelBuffer.Release();
		boundaryBuffer.Release();
		Debug.Log("Rendered!");

		texture = outTexture;

		elevationLookup = new List<float>();
		TextureDictionary = new Dictionary<float, RenderTexture>();
		BufferDictionary = new Dictionary<float, ComputeBuffer>();

		TextureDictionary[1] = texture;

		for (int elevation = 0; elevation < elevationCount; elevation++)
		{
			var bufferBoundaries = new List<boundaryData>();
			foreach (var (index, boundary) in boundaries) {
				var bound = new boundaryData();

				bound.index = (uint)boundary.index;
				bound.elevationIndex = (uint)elevation;
				bound.u = boundary.u[bound.elevationIndex];
				bound.v = boundary.v[bound.elevationIndex];

				bound.latitude = boundary.latitude;
				bound.longitude = boundary.longitude;

				bufferBoundaries.Add(bound);
			}
			var boundariesBuffer = ComputeHelper.CreateStructuredBuffer(bufferBoundaries.ToArray());

			BufferDictionary[elevation] = boundariesBuffer;
		}
		numBoundaries = (uint)BufferDictionary[1].count;

		foreach (var (elevation, buffer) in BufferDictionary)
		{
			elevationLookup.Add(elevation);
		}
	}

    public void UpdateMaterials()
    {
		if (texture)
		{
			globe.sharedMaterial.mainTexture = texture;
		}
	}

	void OnDestroy()
	{
		if (TextureDictionary != null)
		{
			foreach(var (elevation, texture) in TextureDictionary)
			{
				texture.Release();
			}
		}
		if (BufferDictionary != null)
		{
			foreach(var (elevation, buffer) in BufferDictionary)
			{
				buffer.Release();
			}
		}
		if (texture) texture.Release();
		if(groundTexture) groundTexture.Release();
	}
}