Shader "Unlit/GlobeTemperatureVis"
{
	Properties
	{
		_MainTex("Temperature Texture", 2D) = "white" {}
		_WindMax("Max Wind", Float) = 0.2
		_OverlayTex("Overlay Texture", 2D) = "white" {}
		_OverlayStr("Overlay Strength", Range(0.0, 1.0)) = 0.2
		_ColA ("ColA", Color) = (1,1,1,1)
		_ColB ("ColB", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 pos : TEXCOORD1;
			};

			sampler2D _MainTex;
			float  _WindMax;
			sampler2D _OverlayTex;
			float _OverlayStr;

			float4 _MainTex_ST;
			float4 _OverlayTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.pos = v.vertex;
				return o;
			}

			float4 _ColA, _ColB;

			
			// Scale longitude and latitude (given in radians) to range [0, 1]
			float2 longitudeLatitudeToUV(float2 longLat) {
				float PI = 3.1415;
				float longitude = longLat[0]; // range [-PI, PI]
				float latitude = longLat[1]; // range [-PI/2, PI/2]
				
				float u = (longitude / PI + 1) * 0.5;
				float v = latitude / PI + 0.5;
				return float2(u,v);
			}

			// Convert point on unit sphere to longitude and latitude
			float2 pointToLongitudeLatitude(float3 p) {
				float longitude = atan2(p.x, -p.z);
				float latitude = asin(p.y);
				return float2(longitude, latitude);
			}

			// Convert point on unit sphere to uv texCoord
			float2 pointToUV(float3 p) {
				float2 longLat = pointToLongitudeLatitude(p);
				return longitudeLatitudeToUV(longLat);
			}



			float4 frag (v2f i) : SV_Target
			{
				float2 uv = pointToUV(normalize(i.pos));
				float3 temperatureCol = tex2D(_MainTex, TRANSFORM_TEX(uv, _MainTex)).rgb;
				float3 overlayCol = tex2D(_OverlayTex, TRANSFORM_TEX(uv, _OverlayTex));

				float3 col = lerp(temperatureCol, overlayCol, _OverlayStr);
				return float4(col, 1);
			}
			ENDCG
		}
	}
}
