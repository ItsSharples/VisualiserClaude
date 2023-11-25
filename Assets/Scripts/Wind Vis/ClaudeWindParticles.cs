using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;





[ExecuteInEditMode]
public class ClaudeWindParticles : MonoBehaviour
{

	const int initKernel = 0;
	const int updateKernel = 1;
	[Header("Settings")]
	public int numParticles = 100000;

	//int numLayers => FindObjectOfType<LayeredWindReader>().TextureCount;
	//bool move;
	//public float size = 0.1f;
	public float duration;
	//float stretch = 0;
	public float timeScale = 1;

	//public float[] heights;

	public List<ClaudeElevation> elevationDict;
	//Dictionary<float, ComputeBuffer> dictionaryParticleBuffers;

	[Header("References")]
	public Mesh mesh;
	public Shader instanceShader;

	public ComputeShader compute;

	//Material[] materials;
	//ComputeBuffer particleBuffer;
	ComputeBuffer argsBuffer;
	Bounds bounds;

	//ComputeBuffer[] layeredParticleBuffers;
	
	//public int currLayer;
	int numBuffers;
	int numLayers;

	void Start() { FreshStart(); }
	public void FreshStart()
	{
		elevationDict = new List<ClaudeElevation>();
		//dictionaryParticleBuffers = new Dictionary<float, ComputeBuffer>();

		foreach (var elevation in GetComponentsInParent<ClaudeElevation>())
		{
			elevationDict.Add(elevation);
		}

		rebuildBuffers();

		bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

		// Create args buffer
		argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, numParticles);

		foreach(var elevation in elevationDict)
		{
			InitElevation(elevation);
		}


	}

	public void rebuildBuffers(bool ForceRebuild = false)
	{
		var reader = FindObjectOfType<claudeReader>();
		if (reader == null) { return; }
		if (!reader.isLoaded) { reader.LoadFile(); }

		if (ForceRebuild)
		{

			foreach (var elevation in GetComponentsInParent<ClaudeElevation>())
			{
				DestroyImmediate(elevation);
			}

			elevationDict = new List<ClaudeElevation>();
			ClaudeElevation elevationObject;
			foreach (var elevation in reader.GetElevations())
			{
				elevationObject = gameObject.AddComponent<ClaudeElevation>();
				elevationObject.Create(elevation, ref instanceShader, (uint)numParticles);
				elevationObject.config.heightScale = elevation;
				elevationDict.Add(elevationObject);

				elevationObject.SetData(reader.GetBoundariesForElevation(elevation), reader.WindTextureForElevation(elevation));
			}
		}
		else
		{
			foreach (var elevation in reader.GetElevations())
			{
				//var buffer = ;
				//Debug.Log(elevationDict.Keys.Count);
				ClaudeElevation elevationObject;

				elevationObject = elevationDict.Find(x => x.elevation == elevation);
				if (elevationObject != null)
				{
					elevationObject.Rebuild(ref instanceShader, (uint)numParticles);
					
				}
				else
				{
					elevationObject = gameObject.AddComponent<ClaudeElevation>();
					elevationObject.Create(elevation, ref instanceShader, (uint)numParticles);
					elevationObject.config.heightScale = elevation;
					elevationDict.Add(elevationObject);
				}

				//if (reader.IsValidElevation(elevation))
				{
					var boundaries = reader.GetBoundariesForElevation(elevation);
					var texture = reader.WindTextureForElevation(elevation);
					Debug.Log($"{boundaries},{texture}");
					elevationObject.SetData(boundaries, texture);
				}
				//else
				//{
				//	Debug.Log($"AAAAAA: {elevation}");
				//	continue;
				//}
			}
		}

		foreach (var elevation in elevationDict)
		{
			InitElevation(elevation);
		}
	}

	/*
	void OldUpdate()
	{
		Assign();
		material.SetBuffer("Particles", particleBuffer);
		material.SetFloat("size", size * 0.001f);
		material.SetFloat("stretch", stretch);

		compute.SetFloat("deltaTime", Time.deltaTime);
		compute.SetFloat("speedScale", timeScale * 0.001f);
		compute.SetFloat("lifeSpan", duration);
		ComputeHelper.Dispatch(compute, numParticles, kernelIndex: updateKernel);

		Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
	}

	void OldAssign()
	{
		ComputeHelper.AssignBuffer(compute, particleBuffer, "Particles", initKernel, updateKernel);
		ComputeHelper.AssignTexture(compute, FindObjectOfType<WindFolderReader>().currentWindTexture, "WindMap", initKernel, updateKernel);
		compute.SetInt("numParticles", numParticles);
	}
	*/
	bool IsInvalidLayer(int layer) => (layer < 0 || layer >= numLayers);

	private void Update()
	{
		//if (numBuffers != numLayers)
		//{
		//	rebuildBuffers();
		//}
		if(argsBuffer == null)
		{
			argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, numParticles);
		}

		if (Application.isPlaying)
		{
			foreach (var elevationObject in elevationDict)
			{
				if(elevationObject.enableUpdates)
				{
					UpdateElevation(elevationObject);// Update(elevation: elevation);
					Graphics.DrawMeshInstancedIndirect(mesh, 0, elevationObject.particleMaterial, bounds, argsBuffer);
				}
			}
		}

		//Graphics.DrawMeshInstancedIndirect(mesh, 0, materials[currLayer], bounds, argsBuffer);
		//Update(layer: currLayer);
		//currLayer = (currLayer + 1) % numLayers;
	}
	void InitElevation(ClaudeElevation elevation)
	{
		//elevation.ActivateMaterial();
		ComputeHelper.AssignBuffer(compute, elevation.particleBuffer, "Particles", initKernel, updateKernel);
		ComputeHelper.AssignBuffer(compute, elevation.boundaryBuffer, "Boundaries", initKernel, updateKernel);
		ComputeHelper.AssignTexture(compute, elevation.texture, "WindDataTexture", initKernel, updateKernel);
		ComputeHelper.Dispatch(compute, numParticles, kernelIndex: initKernel);
	}
	void UpdateElevation(ClaudeElevation elevation)
	{
		elevation.ActivateMaterial();
		
		ComputeHelper.AssignBuffer(compute, elevation.particleBuffer, "Particles", initKernel, updateKernel);
		ComputeHelper.AssignBuffer(compute, elevation.boundaryBuffer, "Boundaries", initKernel, updateKernel);
		ComputeHelper.AssignTexture(compute, elevation.texture, "WindDataTexture", initKernel, updateKernel);

		compute.SetInt("numParticles", numParticles);
		compute.SetInt("numBoundaries", numParticles);

		compute.SetFloat("deltaTime", Time.deltaTime);
		compute.SetFloat("speedScale", timeScale * 0.001f);
		compute.SetFloat("lifeSpan", duration);
		ComputeHelper.Dispatch(compute, numParticles, kernelIndex: updateKernel);
	}
	/*
	void Update(float elevation)
	{
		//if(IsInvalidLayer(layer))
		//{
		//	Debug.LogWarning($"Invalid Layer: {layer}");
		//	return;
		//}

		///AssignLayer(layer);

		//var elevation = heights[layer];
		if (!elevationDict.TryGetValue(elevation, out var level))
		{
			return;
		}
		level.UpdateMaterial();
		//var level = elevationDict[elevation];

		//level.UpdateBuffer(ref compute);
		//var reader = FindObjectOfType<LayeredWindReader>();
		//var texture = reader.WindTextureForElevation(elevation);

		ComputeHelper.AssignBuffer(compute, level.particleBuffer, "Particles", initKernel, updateKernel);
		ComputeHelper.AssignTexture(compute, level.texture, "WindMap", initKernel, updateKernel);

		compute.SetInt("numParticles", numParticles);

		compute.SetFloat("deltaTime", Time.deltaTime);
		compute.SetFloat("speedScale", timeScale * 0.001f);
		compute.SetFloat("lifeSpan", duration);
		ComputeHelper.Dispatch(compute, numParticles, kernelIndex: updateKernel);

		
	}
	*/
	//void Assign()
	//{
	//	for (int i = 0; i < numLayers; i++)
	//	{
	//		AssignLayer(layer: i);
	//	}

	//}
	//void AssignLayer(int layer)
	//{
	//	if (IsInvalidLayer(layer))
	//	{
	//		return;
	//	}
	//	var reader = FindObjectOfType<LayeredWindReader>();
	//	var elevation = reader.elevationLookup[layer];
	//	var texture = reader.WindTextureForLayer(layer);

	//	ComputeHelper.AssignBuffer(compute, layeredParticleBuffers[layer], "Particles", initKernel, updateKernel);
	//	ComputeHelper.AssignTexture(compute, texture, "WindMap", initKernel, updateKernel);
	//	compute.SetInt("numParticles", numParticles);


	//	materials[layer].SetFloat("scale", 1 + heights[layer] * (elevation) * 0.01f);
	//}


	void OnDestroy()
	{
		//ComputeHelper.Release(layeredParticleBuffers);
		if (argsBuffer != null)
		{
			argsBuffer.Release();
		}
		//if (dictionaryParticleBuffers != null)
		//{
		//	foreach (var (elevation, buffer) in dictionaryParticleBuffers)
		//	{
		//		buffer.Release();
		//	}
		//}
		//ComputeHelper.Release(argsBuffer);
	}

	public struct Particle
	{
		public Vector3 position;
		public Vector3 velocity;
		public float lifeT;
	}

	public void DisableAllLayers()
	{
		foreach(var objec in elevationDict)
		{
			objec.enableUpdates = false;
		}
	}

	public void EnableAllLayers()
	{
		foreach (var objec in elevationDict)
		{
			objec.enableUpdates = true;
		}
	}
}