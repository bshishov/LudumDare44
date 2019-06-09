Shader "FX/BlendAdd"
{
    Properties
    {
        _MainTex ("Texture (RGBA)", 2D) = "white" {}		
		[HDR]_Color("Color (RGBA)", Color) = (1, 1, 1, 1)		
    }
    SubShader
    {
        Tags 
		{ 		
			"RenderType"="Transparent" 
			"Queue"="Transparent" 
			"PreviewType"="Plane"
			"IgnoreProjector"="True"
			"ForceNoShadowCasting"="True"
		}
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
                half4 vertex : POSITION;
                half4 uv : TEXCOORD0;
				half4 color: COLOR;
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
				half2 uv : TEXCOORD0;				
				half4 color: TEXCOORD2;
            };

            sampler2D _MainTex;
			float4 _MainTex_ST;
			half4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

				// Additional offset (from particles vertex stream)
				half2 offset = v.uv.zw;
                
				// Mask UV
				o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw + offset;
				o.color = v.color;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {   
				// Sample stuff
                half4 tex = tex2D(_MainTex, i.uv.xy);				

				// Main color and alpha computation				
				float4 res = tex * i.color * _Color;				

				// Premultiply alpha for Diablo-like "Blend-add"
				res.rgb *= res.a;
				res.a = saturate(res.a);

                return res;
            }
            ENDCG
        }
    }
}
