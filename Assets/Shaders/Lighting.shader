Shader "Unlit/Lighitng"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		 [HDR] _Color("Color", Color) = (1, 1, 1, 1)
		_MaxOffset("MaxOffset", Range(0, 2)) = 0.5
		_Frequency("Frequency", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
			Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			half _MaxOffset;
			float4 _Color;
			half _Frequency;

            v2f vert (appdata v)
            {
                v2f o;

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				half k = 2 * (o.uv.x - 0.5);
				half displacementImpactCap = _MaxOffset * (1 - k * k);
				v.vertex.x += displacementImpactCap * sin(_Time.w * _Frequency + o.uv.x * _Frequency);
				//v.vertex.y += displacementImpactCap * sin(_Time.w * 100 + o.uv.x * 100);
				//v.vertex.z += displacementImpactCap * sin(_Time.w * 100 + o.uv.x * 100);

                o.vertex = UnityObjectToClipPos(v.vertex);
                
				
				

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {                
                fixed4 col = tex2D(_MainTex, i.uv);                
                UNITY_APPLY_FOG(i.fogCoord, col);
				
				half k = 2 * (i.uv.x - 0.5);


                return col * _Color;
            }
            ENDCG
        }
    }
}
