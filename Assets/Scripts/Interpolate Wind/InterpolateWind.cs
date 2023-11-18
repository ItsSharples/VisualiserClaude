using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class InterpolateTexture : MonoBehaviour
{
	[SerializeField]
	ComputeShader interpolateCompute;

	/// <summary>
	/// Compare Two Textures to make sure interpolation will go okay
	/// </summary>
	/// <returns>If interpolation will go okay.</returns>
	static bool canTexturesInterpolate(RenderTextureDescriptor textureA, RenderTextureDescriptor textureB)
	{

		var textureFormat = textureA.graphicsFormat;
		var bFormat = textureB.graphicsFormat;
		if (textureFormat != bFormat)
		{
			Debug.LogError("Textures do not share a graphics format");
			return false;
		}
		if (textureFormat != GraphicsFormat.R32G32_SFloat)
		{
			Debug.LogError($"Unexpected Format.\nExpected \"{GraphicsFormat.R32G32_SFloat}\", got \"{textureFormat}\"");
			return false;
		}

		var textureSize = new Vector2Int(textureA.width, textureA.height);
		var bSize = new Vector2Int(textureB.width, textureB.height);
		if (textureSize != bSize)
		{
			Debug.LogError($"Different Texture Sizes. ({textureSize} vs {bSize})");
			return false;
		}

		Debug.Log($"Selected Format: {textureFormat}\nSelected Dimensions: {textureSize}");
		return true;
	}

	void createInterpolatedTexture(RenderTexture textureFrom, RenderTexture textureTo, ref RenderTexture interpolatedResult, float scale = 0.5f)
	{
		if (!canTexturesInterpolate(textureFrom.descriptor, textureTo.descriptor)) { return; }

		var kernelID = interpolateCompute.FindKernel("CSMain");

		interpolateCompute.SetTexture(kernelID, "TextureA", textureFrom);
		interpolateCompute.SetTexture(kernelID, "TextureB", textureTo);

		interpolateCompute.SetTexture(kernelID, "TextureOut", interpolatedResult);

		interpolateCompute.SetFloat("mixStr", scale);
		interpolateCompute.SetFloat("width", textureFrom.descriptor.width);
		interpolateCompute.SetFloat("height", textureFrom.descriptor.height);
		ComputeHelper.Dispatch(interpolateCompute, interpolatedResult);
	}

	public RenderTexture InterpolateTextures(RenderTexture textureFrom, RenderTexture textureTo, float scale = 0.5f)
	{
		RenderTexture output = ComputeHelper.CreateRenderTexture(textureFrom.width, textureFrom.height, FilterMode.Bilinear, textureFrom.graphicsFormat, "Interpolated Texture");
		createInterpolatedTexture(textureFrom, textureTo, ref output, scale);
		return output;
	}
}
