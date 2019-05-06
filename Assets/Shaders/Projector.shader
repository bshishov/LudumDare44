Shader "Custom/Projector_BlobShadow" 
{
	Properties
	{
		_Color("Color", Color) = (0.26,0.19,0.16,0.0)
		_ShadowTex("Cookie", 2D) = "gray" {}
		_FalloffTex("FallOff", 2D) = "white" {}
		_ClipStart("Clip Start", Range(0.0, 1.0)) = 0.2
		_ClipEnd("Clip End", Range(0.0, 1.0)) = 0.8		

	}
	Subshader{
		Tags {"Queue" = "Transparent"}
		Pass {
			ZWrite Off
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha
			//Blend DstColor Zero
			//Offset - 1, -1

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

			struct vertex_out {
				float4 uvShadow : TEXCOORD0;
				float4 uvFalloff : TEXCOORD1;
				UNITY_FOG_COORDS(2) // TEXCOORD2
				float4 pos : SV_POSITION;
				float intensity : TEXCOORD3; // additional intensity, based on normal orientation
			};

			float4x4 unity_Projector;
			float4x4 unity_ProjectorClip;
			fixed _ClipStart;
			fixed _ClipEnd;

			vertex_out vert(float4 vertex : POSITION, float3 normal : NORMAL)
			{
				vertex_out o;
				o.intensity = sign(dot(float3(0.0, 1.0, 0.0), UnityObjectToWorldNormal(normal))); // 1.0 if pointing UP
				o.pos = UnityObjectToClipPos(vertex);
				o.uvShadow = mul(unity_Projector, vertex);
				o.uvFalloff = mul(unity_ProjectorClip, vertex);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}

			fixed4 _Color;
			sampler2D _ShadowTex;
			sampler2D _FalloffTex;

			fixed4 frag(vertex_out i) : SV_Target
			{
				fixed4 texS = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uvShadow));
				//texS.rgb = 1 - texS.rgb;
				texS.a = 1.0 - texS.a;
				//texS.rgb *= _Color.rgb;

				fixed4 texF = tex2Dproj(_FalloffTex, UNITY_PROJ_COORD(i.uvFalloff));
				fixed4 res = lerp(fixed4(1,1,1,0), texS, texF.a * i.intensity);
				//clip(0.5 - saturate(UNITY_PROJ_COORD(i.uvFalloff).z));
				fixed p = saturate(UNITY_PROJ_COORD(i.uvFalloff).z);
				clip(p - _ClipStart);
				clip(_ClipEnd - p);
				//return saturate(UNITY_PROJ_COORD(i.uvFalloff).z);

				UNITY_APPLY_FOG_COLOR(i.fogCoord, res, fixed4(1,1,1,1));

				return fixed4(texS.rgb * texS.rgb * _Color.rgb * 4, texS.r * _Color.a * 4);
				
				return res;
			}
			ENDCG
		}
	}
}