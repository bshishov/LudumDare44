Shader "FX/Decal"
{
    Properties
    {
        _MainTex ("Mask (RGBA)", 2D) = "white" {}
		_Tex1("Tex1 (RGBA)", 2D) = "white" {}
		_Tex2("Tex2 (RGBA)", 2D) = "white" {}
		_Tex3("Tex3 (RGBA)", 2D) = "white" {}
		[HDR]_Color("Color (RGBA)", Color) = (1, 1, 1, 1)
		_Speed1("Speed1", Vector) = (0, 0, 0, 0)
		_Speed2("Speed2", Vector) = (0, 0, 0, 0)
		_Speed3("Speed3", Vector) = (0, 0, 0, 0)
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

				// For decal
				float4 screenPos : TEXCOORD3; 
				float3 viewRay : TEXCOORD4;
            };

			sampler2D _CameraDepthTexture;
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

				// Decal
				o.screenPos = ComputeScreenPos(o.vertex);
				o.viewRay = UnityObjectToViewPos(v.vertex).xyz * float3(-1,-1,1);			

				// v.uv is equal to 0 when we are drawing 3D light shapes and
				// contains a ray pointing from the camera to one of near plane's
				// corners in camera space when we are drawing a full screen quad.
				o.viewRay = lerp(o.viewRay, v.uv, v.uv.z != 0);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {   
				// Decal geometry computation
				i.viewRay = i.viewRay * (_ProjectionParams.z / i.viewRay.z);

				// Get depth in the current pixel		    			
				float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.screenPos.xy / i.screenPos.w));			

				// Get new projection coordinates. It is almost like originat o.position, 
				// except that Z axis is using depth information. Such taht we are ignoring our projected object, Z values			
				float4 prjPos = float4(i.viewRay * depth,1);
				float3 worldPos = mul(unity_CameraToWorld, prjPos).xyz;
				float4 objPos = mul(unity_WorldToObject, float4(worldPos, 1));

				// Clip decal to be inside object
				clip(float3(0.5, 0.5, 0.5) - abs(objPos.xyz));

				// Calculate new (projected) uv coordinaties
				half2 uv = objPos.xz + 0.5;

				// Sample stuff				
                half4 mask = tex2D(_MainTex, uv * _MainTex_ST.xy + _MainTex_ST.zw);
				half4 tex1 = tex2D(_Tex1,	 uv * _Tex1_ST.xy + _Tex1_ST.zw + _Speed1.xy * _Time.x);
				half4 tex2 = tex2D(_Tex2,	 uv * _Tex2_ST.xy + _Tex2_ST.zw + _Speed2.xy * _Time.x);
				half4 tex3 = tex2D(_Tex3,	 uv * _Tex3_ST.xy + _Tex3_ST.zw + _Speed3.xy * _Time.x);

				// Main color and alpha computation				
				float4 res = ((tex1 * tex2 * 2) * tex3 * 2) * mask * _Color;				

				// Premultiply alpha for Diablo-like "Blend-add"
				res.rgb *= res.a;
				res.a = saturate(res.a);

                return res;
            }
            ENDCG
        }
    }
}
