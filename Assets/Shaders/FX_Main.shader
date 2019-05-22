Shader "FX/Main"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Tex1("Tex1", 2D) = "white" {}
		_Tex2("Tex2", 2D) = "white" {}
		_Tex3("Tex3", 2D) = "white" {}
		[HDR]_Color("Color", Color) = (1, 1, 1, 1)
		_Speed1("Speed1", Vector) = (0, 0, 0, 0)
		_Speed2("Speed2", Vector) = (0, 0, 0, 0)
		_Speed3("Speed3", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		Lighting Off
        Fog{ Mode Off }
		ZWrite Off
		Cull Off
        LOD 100

        Pass
        {			
			Blend One OneMinusSrcAlpha // Premultiplied transparency

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
				half4 color: COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float4 uv : TEXCOORD0;                                
				half4 color: TEXCOORD1;
            };

            sampler2D _MainTex;
			sampler2D _Tex1;
			sampler2D _Tex2;
			sampler2D _Tex3;

			float4 _MainTex_ST;
			float4 _Tex1_ST;
			float4 _Tex2_ST;
			float4 _Tex3_ST;

			half4 _Color;
			half2 _Speed1;
			half2 _Speed2;
			half2 _Speed3;

			half2 _Custom1;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
				o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {                
                half4 mask = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);
				half4 tex1 = tex2D(_Tex1, i.uv * _Tex1_ST.xy + _Tex1_ST.zw + _Speed1.xy * _Time.x + i.uv.zw);
				half4 tex2 = tex2D(_Tex2, i.uv * _Tex2_ST.xy + _Tex2_ST.zw + _Speed2.xy * _Time.x + i.uv.zw);
				half4 tex3 = tex2D(_Tex3, i.uv * _Tex3_ST.xy + _Tex3_ST.zw + _Speed3.xy * _Time.x + i.uv.zw);

				// Main color and alpha computation
				float4 res = ((tex1 * tex2 * 2) * tex3 * 2) * mask * i.color * _Color;

				// Premultiply alpha for Diablo-like "Blend-add"
				res.rgb *= res.a;
				res.a = saturate(res.a);

                return res;
            }
            ENDCG
        }
    }
}
