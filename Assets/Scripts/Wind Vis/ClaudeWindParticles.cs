using System;
using System.Collections.Generic;
using System.Dynamic;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;





//[ExecuteInEditMode]
public class ClaudeWindParticles : MonoBehaviour
{

	const int initKernel = 0;
	const int updateKernel = 1;
	[Header("Settings")]
	public int numParticles = 100000;

	[HideInInspector]
	public ElevationConfig globalConfig;
	public float duration;
	public float timeScale = 1;


	[Header("References")]
	public Mesh mesh;
	public Shader instanceShader;
	public ComputeShader compute;

	ComputeBuffer argsBuffer;
	public Dictionary<float, ClaudeElevation> elevationDict;
	Bounds bounds;


	void fetchElevationComponents()
	{
		elevationDict = new Dictionary<float, ClaudeElevation>();
		foreach (var elevation in GetComponentsInParent<ClaudeElevation>())
		{
			elevationDict.Add(elevation.elevation, elevation);
		}
	}
	private void Awake()
	{
		if (globalConfig == null)
		{
			globalConfig = ScriptableObject.CreateInstance<ElevationConfig>();
		}
	}
	void Start()
	{
		

		fetchElevationComponents();

		rebuildBuffers();

		bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

		// Create args buffer
		argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, numParticles);

		foreach(var (height, elevation) in elevationDict)
		{
			InitElevation(elevation);
		}

		rebuildBuffers();
	}

	public void rebuildBuffers(bool ForceRebuild = false)
	{

		//if (layeredParticleBuffers != null)
		//{
		//	ComputeHelper.Release(layeredParticleBuffers);
		//}
		var reader = FindObjectOfType<claudeReader>();
		if (reader == null) { return; }
		if (!reader.isLoaded) { reader.LoadFile(); }

		if (ForceRebuild)
		{

			foreach (var elevation in GetComponentsInParent<ClaudeElevation>())
			{
				DestroyImmediate(elevation);
			}

			elevationDict = new Dictionary<float, ClaudeElevation>();
			ClaudeElevation elevationObject;
			foreach (var elevation in reader.elevationLookup)
			{
				elevationObject = gameObject.AddComponent<ClaudeElevation>();
				elevationObject.Create(elevation, ref instanceShader, (uint)numParticles, reader.GetBoundariesForElevation(elevation));
				elevationObject.config.heightScale = elevation;
				elevationDict.Add(elevation, elevationObject);
			}
		}
		else
		{
			foreach (var elevation in reader.elevationLookup)
			{
				ClaudeElevation elevationObject;

				if (elevationDict == null) { fetchElevationComponents(); }

				if (elevationDict.TryGetValue(elevation, out elevationObject))
				{
					elevationObject.Rebuild(ref instanceShader, (uint)numParticles, reader.GetBoundariesForElevation(elevation));
				}
				else
				{
					elevationObject = gameObject.AddComponent<ClaudeElevation>();
					elevationObject.Create(elevation, ref instanceShader, (uint)numParticles, reader.GetBoundariesForElevation(elevation));
					elevationObject.config.heightScale = elevation;
					elevationDict.Add(elevation, elevationObject);
				}
			}
		}

		foreach (var (height, elevation) in elevationDict)
		{
			InitElevation(elevation);
		}
	}

	private void Update()
	{
		if(argsBuffer == null)
		{
			argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, numParticles);
		}


		if (Application.isPlaying)
		{
			foreach (var (elevation, elevationObject) in elevationDict)
			{
				if(elevationObject.enableUpdates)
				{
					UpdateElevation(elevationObject);// Update(elevation: elevation);

					Graphics.DrawMeshInstancedIndirect(mesh, 0, elevationObject.particleMaterial, bounds, argsBuffer);
				}
			}
		}
	}
	void InitElevation(ClaudeElevation elevation)
	{
		ComputeHelper.AssignBuffer(compute, elevation.particleBuffer, "Particles", initKernel, updateKernel);
		ComputeHelper.AssignBuffer(compute, elevation.boundaryBuffer, "Boundaries", initKernel, updateKernel);
		ComputeHelper.Dispatch(compute, numParticles, kernelIndex: initKernel);
	}
	void UpdateElevation(ClaudeElevation elevation)
	{
		elevation.ActivateMaterial(globalConfig);
		
		ComputeHelper.AssignBuffer(compute, elevation.particleBuffer, "Particles", initKernel, updateKernel);
		ComputeHelper.AssignBuffer(compute, elevation.boundaryBuffer, "Boundaries", initKernel, updateKernel);

		compute.SetInt("numParticles", numParticles);
		compute.SetInt("numBoundaries", numParticles);

		compute.SetFloat("deltaTime", Time.deltaTime);
		compute.SetFloat("speedScale", timeScale * 0.001f);
		compute.SetFloat("lifeSpan", duration);
		ComputeHelper.Dispatch(compute, numParticles, kernelIndex: updateKernel);
	}

	void OnDestroy()
	{
		if (argsBuffer != null)
		{
			argsBuffer.Release();
		}
	}

	public struct Particle
	{
		public Vector3 position;
		public Vector3 velocity;
		public float lifeT;
	}

	public void DisableAllLayers()
	{
		foreach(var (elevation, objec) in elevationDict)
		{
			objec.enableUpdates = false;
		}
	}

	public void EnableAllLayers()
	{
		foreach (var (elevation, objec) in elevationDict)
		{
			objec.enableUpdates = true;
		}
	}
}