Shader "Unlit/GlobeSDFVisTemp"
{
	Properties
	{
		_MainTex("SDF Texture", 2D) = "white" {}
		_OverlayTex("Overlay Texture", 2D) = "white" {}
		_OverlayStr("Overlay Strength", Range(0.0, 1.0)) = 0.0
		_TextureStr("Texture Strength", Range(0.0, 1.0)) = 1.0
		_MaxWind("Max Wind", Range(0.0, 1.0)) = 0.0

		_MinTemp("Min Temp", Range(-1.0, 1.0)) = 0.0
		_MaxTemp("Max Temp", Range(-1.0, 1.0)) = 0.5
		_Black("Black", Color) = (0,0,0,1)
		_White("White", Color) = (1,1,1,1)
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
			#include "Assets/Shaders/GetColourHotToCold.hlsl"


			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 pos : TEXCOORD1;
				float3 normal : TEXCOORD2;
			};

			sampler2D _MainTex;
			sampler2D _OverlayTex;
			
			float _OverlayStr;
			float _TextureStr;

			float  _MaxWind;
			float _MinTemp;
			float _MaxTemp;

			float4 _MainTex_ST;
			float4 _OverlayTex_ST;

			fixed4 _White;
			fixed4 _Black;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.pos = v.vertex;
				o.normal = v.normal;
				return o;
			}
						
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


			float4 betterFrag(v2f i) {
				const float PI = 3.14159265359;
				// Puff out the direction to compensate for interpolation.
				float3 direction = normalize(i.normal);

				// Get a longitude wrapping eastward from x-, in the range 0-1.
				float longitude = 0.5 - atan2(direction.z, direction.x) / (2.0f * PI);
				// Get a latitude wrapping northward from y-, in the range 0-1.
				float latitude = 0.5 + asin(direction.y) / PI;

				// Combine these into our own sampling coordinate pair.
				float2 customUV = float2(longitude, latitude);

				// Use the Scaling in the Material Settings 
				customUV = TRANSFORM_TEX(customUV, _MainTex);
				// Use them to sample our texture(s), instead of the defaults.
				fixed4 wind = tex2D(_MainTex, customUV);

				return wind;
			}

			float length(float2 vec) {
				return sqrt(pow(vec.x,2) + pow(vec.y, 2));
			}
			float remap(float minOld, float maxOld, float minNew, float maxNew, float val) {
				return saturate(minNew + (val - minOld) * (maxNew - minNew) / (maxOld - minOld));
			}

			float4 frag(v2f i) : SV_Target
			{
				float2 longLat = pointToLongitudeLatitude(normalize(i.pos));
				float2 uv = longitudeLatitudeToUV(longLat);
				float4 sdfValue = tex2D(_MainTex, TRANSFORM_TEX(uv, _MainTex)).rgba;
				float3 colour = float3(
					remap(_MinTemp, _MaxTemp, 0.0, 1.0, sdfValue.x),
					remap(_MinTemp, _MaxTemp, 0.0, 1.0, sdfValue.y),
					0
				);
					//GetColourHotToCold(sdfValue.g, _MinTemp, _MaxTemp);

				float3 overlayCol = tex2D(_OverlayTex, TRANSFORM_TEX(uv, _OverlayTex)).rgb;
				float3 lerpedColour = lerp(colour, overlayCol, _OverlayStr);

				float3 lerpedStrength = lerp(_White, lerpedColour, _TextureStr);

				return float4(lerpedStrength, 1);
			}
			/*

				//float2 longLat = pointToLongitudeLatitude(normalize(i.pos));
				//float2 uv = longitudeLatitudeToUV(longLat);
				float4 pixel = tex2D(_MainTex, TRANSFORM_TEX(uv, _MainTex)).rgba;


				return float4(GetColourHotToCold(pixel.g, _MinTemp, _MaxTemp), 1);



				return float4(GetColourHotToCold(betterFrag(i).r, _MinTemp, _MaxTemp), 1);
				//return float4(i.pos,1);
				//float2 longLat = pointToLongitudeLatitude(normalize(i.pos));
				//longLat = longLat * 2;

				const float PI = 3.14159265359;
				static const float halfPI = PI / 2.0f;
				//longLat.y += halfPI;

				//float2 uv = longitudeLatitudeToUV(longLat);
				//float2 uv = pointToUV(normalize(i.pos));
				float4 temperatureCol = tex2D(_MainTex, TRANSFORM_TEX(uv, _MainTex)).rgba;
				float3 colour = GetColourHotToCold(temperatureCol.r, _MinTemp, _MaxTemp);
				
				float3 overlayCol = tex2D(_OverlayTex, TRANSFORM_TEX(uv, _OverlayTex)).rgb;

				//float m = length(temperatureCol.g) / _MaxWind;
				//float3 adjCol = lerp(_Black, _White, saturate(m));
				//float3 adjCol = float3(length(temperatureCol.rg) * 10.0f,0, 0);// temperatureCol.b / 4.0f + temperatureCol.a);
				
				float3 lerpedColour = lerp(colour, overlayCol, _OverlayStr);

				return float4(lerpedColour, 1);
			}
			*/
			ENDCG
		}
	}
}
