using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static WindParticles;

public struct Particle
{
	public Vector3 position;
	public Vector3 velocity;
	public float lifeT;
}
public class Elevation : MonoBehaviour
{
	public float elevation;

	public ElevationConfig config;
	public Material particleMaterial;
	public ComputeBuffer particleBuffer;
	public RenderTexture texture;

	public void Create(float elevation, ref Shader instanceShader, int particleCount)
	{
		config = new ElevationConfig();
		this.elevation = elevation;

		Rebuild(ref instanceShader, particleCount);
	}

	public void Rebuild(ref Shader instanceShader, int particleCount)
	{
		particleMaterial = new Material(instanceShader);
		particleMaterial.enableInstancing = true;
		particleMaterial.SetColor("_particleColour", config.particleColour);

		if (particleBuffer != null)
		{
			particleBuffer.Release();
		}
		particleBuffer = ComputeHelper.CreateStructuredBuffer<Particle>(particleCount);
	}

	public void ActivateMaterial()
	{
		particleMaterial.SetBuffer("Particles", particleBuffer);
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

[CustomEditor(typeof(Elevation))]
public class ElevationEditor : Editor
{
public override void OnInspectorGUI()
	{
		var obj = target as Elevation;
		base.OnInspectorGUI();
		var editor = Editor.CreateEditor(obj.config);
		var root = editor.CreateInspectorGUI();
		editor.OnInspectorGUI();
	}

}