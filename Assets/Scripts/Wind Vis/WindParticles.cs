using UnityEngine;

public class WindParticles : MonoBehaviour
{

	const int initKernel = 0;
	const int updateKernel = 1;
	[Header("Settings")]
	public int numParticles = 100000;

	bool move;
	public float size = 0.1f;
	public float duration;
	float stretch = 0;
	public float timeScale = 1;

	[Header("References")]
	public Mesh mesh;
	public Shader instanceShader;

	public ComputeShader compute;

	Material material;
	ComputeBuffer particleBuffer;
	ComputeBuffer argsBuffer;
	Bounds bounds;

	void Start()
	{

		material = new Material(instanceShader);
		material.enableInstancing = true;
		bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

		// Create args buffer
		argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, numParticles);

		particleBuffer = ComputeHelper.CreateStructuredBuffer<Particle>(numParticles);
		Assign();

		ComputeHelper.Dispatch(compute, numParticles, kernelIndex: initKernel);

	}


	void Update()
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

	void Assign()
	{
		ComputeHelper.AssignBuffer(compute, particleBuffer, "Particles", initKernel, updateKernel);
		ComputeHelper.AssignTexture(compute, FindObjectOfType<WindFolderReader>().currentWindTexture, "WindMap", initKernel, updateKernel);
		compute.SetInt("numParticles", numParticles);
	}


	void OnDestroy()
	{
		ComputeHelper.Release(particleBuffer, argsBuffer);
	}

	public struct Particle
	{
		public Vector3 position;
		public Vector3 velocity;
		public float lifeT;
	}
}