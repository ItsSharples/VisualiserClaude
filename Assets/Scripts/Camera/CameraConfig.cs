using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


[CreateAssetMenu(menuName = "Wind Data/Camera Config")]
public class CameraConfig : ScriptableObject
{
    private const float Tau = 2 * Mathf.PI;
    private const float halfPI = Mathf.PI / 2;
	[Range(-halfPI, halfPI)]
    public float latitude;
    [Range(0f, Tau)]
	public float longitude;
    public float altitude;

    private float adjusted_lat => latitude;
    private float adjusted_lon => longitude - Mathf.PI;

	private float cos(float x) => Mathf.Cos(x);
	private float sin(float x) => Mathf.Sin(x);

	float x => altitude * cos(adjusted_lat) * cos(adjusted_lon);
    float y => altitude * cos(adjusted_lat) * sin(adjusted_lon);
    float z => altitude * sin(adjusted_lat);

    /// <summary>
    /// Adjust the Position so that it is in Unity Coords
    /// </summary>
    public Vector3 Position => new Vector3(y, z, x);

}
