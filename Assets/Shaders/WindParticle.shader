Shader "Instanced/Particle" {
	Properties {
		_particleColour("Colour of the Particle", Color) = (1,1,1,1)
	}
	SubShader {

		Pass {
			Tags
			{
				"LightMode" = "ForwardBase"
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				//"Queue" = "Opaque"
			}
			Lighting Off
			ZWrite On
			Blend SrcAlpha OneMinusSrcAlpha
			Fog { Mode Off }

			CGPROGRAM
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members col)
//#pragma exclude_renderers d3d11

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5

			#include "UnityCG.cginc"
			
			struct Particle {
				float3 position;
				float3 velocity;
				float lifeT;
			};

			StructuredBuffer<Particle> Particles;
			float size;
			float stretch;
			float scale = 1.0f;
			fixed4 _particleColour;


			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 normal : NORMAL;
			};

			v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
				Particle particle = Particles[instanceID];

				float3 forward = normalize(particle.velocity);
				float3 up = normalize(particle.position);
				float3 right = normalize(cross(forward, up));
				forward = -normalize(cross(up, right));
			
				float4x4 mat = float4x4(
					float4(right.x, up.x, forward.x, 0),
					float4(right.y, up.y, forward.y, 0),
					float4(right.z, up.z, forward.z, 0),
					float4(0,		0,	  0,		 1)
				);
				float lifeScale = 1 - 256 * pow(particle.lifeT-0.5, 8);
				float3 localPosition = v.vertex.xyz * size * lifeScale;

				float3 worldPosition = (particle.position + mul(mat, float4(localPosition, 1))) * scale;

				v2f o;
				o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
				o.normal = v.normal;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return _particleColour;
			}

			ENDCG
		}
	}
}