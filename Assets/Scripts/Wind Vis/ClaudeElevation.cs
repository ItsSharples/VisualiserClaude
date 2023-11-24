using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ClaudeElevation : MonoBehaviour
{
	public float elevation;
	public bool enableUpdates = true;

	[HideInInspector]
	public ElevationConfig config;
	public Material particleMaterial;
	public ComputeBuffer particleBuffer;
	public uint particleCount;
	public ComputeBuffer boundaryBuffer;
	public uint boundaryCount;

	//public RenderTexture texture;

	public void Create(float elevation, ref Shader instanceShader, uint particleCount, ComputeBuffer buffer)
	{
		config = ScriptableObject.CreateInstance<ElevationConfig>();
		this.elevation = elevation;

		Rebuild(ref instanceShader, particleCount, buffer);
	}

	public void Rebuild(ref Shader instanceShader, uint particleCount, ComputeBuffer buffer)
	{
		if(config == null)
		{
			Debug.LogWarning("Can't Find Config, Recreating");
			config = ScriptableObject.CreateInstance<ElevationConfig>();
		}

		particleMaterial = new Material(instanceShader);
		particleMaterial.enableInstancing = true;
		particleMaterial.SetColor("_particleColour", config.particleColour);

		if (particleBuffer != null)
		{
			particleBuffer.Release();
		}
		particleBuffer = ComputeHelper.CreateStructuredBuffer<Particle>((int)particleCount);
		boundaryBuffer = buffer;

		this.particleCount = particleCount;
		this.boundaryCount = (uint)boundaryBuffer.count;
	}

	//public void UpdateBuffer(ref ComputeShader compute)
	//{
	//	var reader = FindObjectOfType<LayeredWindReader>();
	//	var texture = reader.WindTextureForElevation(elevation);

	//	ComputeHelper.AssignBuffer(compute, particleBuffer, "Particles", initKernel, updateKernel);
	//	ComputeHelper.AssignTexture(compute, texture, "WindMap", initKernel, updateKernel);
	//}
	public void ActivateMaterial()
	{
		particleMaterial.SetBuffer("Particles", particleBuffer);
		particleMaterial.SetBuffer("Boundaries", boundaryBuffer);
		particleMaterial.SetFloat("size", config.particleScale);
		particleMaterial.SetFloat("stretch", config.stretch);

		particleMaterial.SetColor("_particleColour", config.particleColour);
		particleMaterial.SetFloat("scale", 1 + config.heightScale * 0.01f);

	}

	public void OnDestroy()
	{
		particleBuffer.Release();
		Destroy(particleMaterial);
	}

}



[CustomEditor(typeof(ClaudeElevation))]
public class ClaudeElevationEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var obj = target as ClaudeElevation;
		base.OnInspectorGUI();
		var editor = Editor.CreateEditor(obj.config);
		var root = editor.CreateInspectorGUI();
		editor.OnInspectorGUI();
	}

}