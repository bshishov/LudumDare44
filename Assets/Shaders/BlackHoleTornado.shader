Shader "Custom/BlackHoleTornado"
{
	Properties
	{
		_Color("Color", Color) = (0.26,0.19,0.16,0.0)
		_MainTex ("Texture", 2D) = "white" {}
		_NoiseTex("Texture", 2D) = "white" {}
		_NoiseTex2("Texture", 2D) = "white" {}
		_Speed("Speed", Float) = 1.0
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent+100"}
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha
		//Blend DstColor Zero
		//Blend OneMinusDstColor One
		ZWrite Off

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
			};

			sampler2D _MainTex;
			sampler2D _NoiseTex;
			sampler2D _NoiseTex2;
			fixed4 _Color;
			float4 _MainTex_ST;
			float _Speed;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);				
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{	
				float2 uv = float2(_Time.x * _Speed, 0);
				fixed4 noise = tex2D(_NoiseTex, i.uv + uv);
				fixed4 noise2 = tex2D(_NoiseTex2, i.uv + uv * 0.5);
				fixed4 col = tex2D(_MainTex, i.uv) * _Color * 2;
				col.a = col.r * noise.r;
				col.rgb = col.a * col.a * col.rgb * noise2.r; // +(1 - col.a);
				return col * 4;
				//return saturate(col - noise.r * noise.r);
			}
			ENDCG
		}
	}
}
