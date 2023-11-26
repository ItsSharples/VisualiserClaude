using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


[CreateAssetMenu(menuName = "Wind Data/Camera Config")]
public class CameraConfig : ScriptableObject
{
    private const float Tau = 2 * Mathf.PI;
    private const float halfPI = Mathf.PI / 2;
    private const float justLessThanHalfPI = halfPI - 0.001f;
	[Range(-justLessThanHalfPI, justLessThanHalfPI)]
    public float latitude;
    [Range(-Tau, 2 * Tau)]
	public float longitude;

    public float mappedLatitude => latitude;
	public float mappedLongitude => longitude % Tau;

	public float altitude;

    private float adjusted_lat => mappedLatitude;
    private float adjusted_lon => mappedLongitude;

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
