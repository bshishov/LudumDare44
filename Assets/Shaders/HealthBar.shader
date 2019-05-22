Shader "UI/HealthBar" 
{
    Properties {        
		[PerRendererData] _MainTex ("Texture", 2D) = "white" {}		
		_DivisorTex ("Divisor", 2D) = "white" {}		

		_ColorBase ("Color Base", Color) = (0,0,0,1)
		_ColorDmg ("Color Dmg", Color) = (1,1,1,1)
		_ColorHp ("Color Hp", Color) = (1, 0, 0, 1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

        Pass {
			Name "Default"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

			#include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

			sampler2D _MainTex;
			sampler2D _DivisorTex;
            fixed4 _ColorBase;
			fixed4 _ColorDmg;
			fixed4 _ColorHp;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
			uniform float4 _MainTex_TexelSize;

            v2f vert (appdata_t  v) 
			{
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);				

				#if UNITY_COLORSPACE_GAMMA
				OUT.color = v.color;				
				#else
				OUT.color = float4(LinearToGammaSpace(v.color.rgb), v.color.a);
				#endif
                
                return OUT;
            }

			inline fixed4 alphaBlend(fixed4 dst, fixed4 src) {
				fixed4 o;
				o.a = src.a + dst.a * (1 - src.a);
				o.rgb = src.rgb + dst.rgb * (1 - src.a);
				return o;
			}

            fixed4 frag (v2f i) : SV_Target 
			{     
				half fill = i.color.r;
				half fillW = i.color.g;
				half tickDivisor = i.color.b;
				
				fixed x = i.texcoord.x;				
				fixed4 tex = tex2D(_MainTex, i.texcoord);
				fixed4 base = step(fillW, x) * _ColorBase;
				fixed4 dmg = step(fill, x) * step(x, fillW) * _ColorDmg;
				fixed4 hp = step(x, fill) * _ColorHp;				
				float isTick = step( fmod(x, tickDivisor), 0.01f);				
				fixed4 tick = fixed4(0, 0, 0, isTick * 0.5);								

                fixed4 color = saturate(tex * alphaBlend(base + hp + dmg, tick));				

				#ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

				return color;
            }
            ENDCG
        }
    }
}