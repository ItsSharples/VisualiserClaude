using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class RenderHelperLayered
{
	internal struct WindData
	{
		public float elevationFromGround;
		public float[] uData;
		public float[] vData;
	};
	internal struct RenderTextures
	{
		public Dictionary<float, RenderTexture> WindTextures;
		public Dictionary<float, RenderTexture> TemperatureTextures;
	}

	internal static RenderTextures GetRenderTexturesFromLayeredJSONText(string jsonText, ref ComputeShader windCompute, ref ComputeShader temperatureCompute, bool upscale = true)
	{
		// Read
		JsonTextReader reader = new JsonTextReader(new System.IO.StringReader(jsonText));

		var objects = JsonConvert.DeserializeObject<IList<JObject>>(jsonText);

		var WindTextures = new Dictionary<float, RenderTexture>();
		var TemperatureTextures = new Dictionary<float, RenderTexture>();
		// Elevation, Temperature List
		var Temperatures = new Dictionary<float, float[]>();
		var DataValues = new Dictionary<float, WindData>();

		foreach (JObject obj in objects)
		{
			var jheader = obj.GetValue("header");
			var numx = (int)jheader["nx"];
			var numy = (int)jheader["ny"];
			var nump = (int)jheader["numberPoints"];
			var catName = (string)jheader["parameterCategoryName"];
			var dimName = (string)jheader["parameterNumberName"];
			var typeName = (string)jheader["surface1TypeName"];
			var typeValue = (float)jheader["surface1Value"];

			//Debug.Log($"({catName}) {typeName}: {typeValue}");
			//
			if (catName == "Temperature")
			{
				var jdata = obj.GetValue("data");
				var data = new List<float>();
				foreach (var datum in jdata)
				{
					data.Add((float)datum);
				}

				/// Update new value
				Temperatures[typeValue] = data.ToArray();
			}
			// Either U or V wind
			if (catName == "Momentum")
			{
				var isVDim = dimName[0] == 'V';
				//Debug.Log($"{isVDim}, {dimName}");
				//if (typeName != "Specified height level above ground")
				//{
				//	continue;
				//}
				//Debug.Log(typeValue);
				WindData windData = DataValues.GetValueOrDefault(typeValue, new WindData());

				var jdata = obj.GetValue("data");
				var data = new List<float>();
				foreach (var datum in jdata)
				{
					data.Add((float)datum);
				}

				if (isVDim)
				{
					windData.vData = data.ToArray();
				}
				else
				{
					windData.uData = data.ToArray();
				}

				/// Update new value
				DataValues[typeValue] = windData;
			}
		}

		
		foreach (var (elevation, windData) in DataValues)
		{
			var windX = windData.uData;
			var windY = windData.vData;
			// Create wind direction buffer
			Vector2[] windVectors = new Vector2[windX.Count()];
			for (int i = 0; i < windVectors.Length; i++)
			{
				windVectors[i] = new Vector2(windX[i], windY[i]);
			}


			ComputeBuffer windBuffer = ComputeHelper.CreateStructuredBuffer(windVectors);
			var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SFloat;
			RenderTexture windTexture;
			windTexture = ComputeHelper.CreateRenderTexture(360, 181, FilterMode.Bilinear, format, "Wind Vector Map");
			windTexture.wrapMode = TextureWrapMode.Repeat;

			windCompute.SetTexture(0, "WindTexture", windTexture);
			windCompute.SetBuffer(0, "WindVectors", windBuffer);
			ComputeHelper.Dispatch(windCompute, windTexture);
			windBuffer.Release();

			RenderTexture texture;
			if (upscale)
			{
				var upscaledWindTexture = ComputeHelper.BicubicUpscale(windTexture, 8);
				windTexture.Release();
				texture = upscaledWindTexture;
			}
			else
			{
				texture = windTexture;
			}

			WindTextures[elevation] = texture;
		}
		foreach (var (elevation, temperatureData) in Temperatures) {
			ComputeBuffer temperatureBuffer = ComputeHelper.CreateStructuredBuffer(temperatureData);
			var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
			RenderTexture temperatureTexture;
			temperatureTexture = ComputeHelper.CreateRenderTexture(360, 181, FilterMode.Bilinear, format, "Temperature Map");
			temperatureTexture.wrapMode = TextureWrapMode.Repeat;

			temperatureCompute.SetTexture(0, "TemperatureTexture", temperatureTexture);
			temperatureCompute.SetBuffer(0, "Temperatures", temperatureBuffer);
			ComputeHelper.Dispatch(temperatureCompute, temperatureTexture);
			temperatureBuffer.Release();

			RenderTexture texture;
			if (upscale)
			{
				var upscaledTempTexture = ComputeHelper.BicubicUpscale(temperatureTexture, 8);
				temperatureTexture.Release();
				texture = upscaledTempTexture;
			}
			else
			{
				texture = temperatureTexture;
			}

			TemperatureTextures[elevation] = texture;
		}

		var outStruct = new RenderTextures();
		outStruct.WindTextures = WindTextures;
		outStruct.TemperatureTextures = TemperatureTextures;
		return outStruct;
	}

}
