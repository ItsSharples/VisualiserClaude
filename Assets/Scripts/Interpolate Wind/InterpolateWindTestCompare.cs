using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


public class InterpolateWindTest : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
		var text02 = System.IO.File.ReadAllText("Assets/Data/WindData/windspeed_2023090200.json");
		var text03 = System.IO.File.ReadAllText("Assets/Data/WindData/windspeed_2023090300.json");
		var text04 = System.IO.File.ReadAllText("Assets/Data/WindData/windspeed_2023090212.json");
		RenderHelper.WriteToRenderTextureFromJSON(text02, ref textureA, ref generateCompute);
		RenderHelper.WriteToRenderTextureFromJSON(text03, ref textureB, ref generateCompute);
		RenderHelper.WriteToRenderTextureFromJSON(text04, ref compareTexture, ref generateCompute);

		InterpretAttributes();
		InterpolateTextures();
		GenerateCompare();

	}
	[SerializeField]
	ComputeShader generateCompute;
	[SerializeField]
	ComputeShader interpolateCompute;
	[SerializeField]
	ComputeShader compareCompute;

	[SerializeField]
	RenderTexture textureA, textureB, compareTexture;
	[SerializeField]
	RenderTexture outTexture, diffTexture;

	GraphicsFormat textureFormat;
	Vector2Int textureSize;

	void InterpretAttributes()
	{
		textureFormat = textureA.graphicsFormat;
		var bFormat = textureB.graphicsFormat;
		if (textureFormat != bFormat) { Debug.LogError("Textures do not share a graphics format"); }
		if (textureFormat != GraphicsFormat.R32G32_SFloat) { Debug.LogError($"Unexpected Format.\nExpected \"{GraphicsFormat.R32G32_SFloat}\", got \"{textureFormat}\""); }

		textureSize = new Vector2Int(textureA.width, textureA.height);
		var bSize = new Vector2Int(textureB.width, textureB.height);
		if (textureSize != bSize) { Debug.LogError($"Different Texture Sizes. ({textureSize} vs {bSize})"); }

		print($"Selected Format: {textureFormat}\nSelected Dimensions: {textureSize}");
	}

	void InterpolateTextures()
	{
		var kernelID = interpolateCompute.FindKernel("CSMain");

		interpolateCompute.SetTexture(kernelID, "TextureA", textureA);
		interpolateCompute.SetTexture(kernelID, "TextureB", textureB);

		interpolateCompute.SetTexture(kernelID, "TextureOut", outTexture);

		interpolateCompute.SetFloat("mixStr", 0.5f);
		interpolateCompute.SetFloat("width", textureSize.x);
		interpolateCompute.SetFloat("height", textureSize.y);
		ComputeHelper.Dispatch(interpolateCompute, outTexture);
	}

	void GenerateCompare()
	{
		var kernelID = compareCompute.FindKernel("CSMain");

		compareCompute.SetTexture(kernelID, "TextureA", outTexture);
		compareCompute.SetTexture(kernelID, "TextureB", compareTexture);

		compareCompute.SetTexture(kernelID, "TextureOut", diffTexture);

		compareCompute.SetFloat("width", textureSize.x);
		compareCompute.SetFloat("height", textureSize.y);
		ComputeHelper.Dispatch(compareCompute, diffTexture);
	}

	// Update is called once per frame
	void Update()
	{

	}
}
