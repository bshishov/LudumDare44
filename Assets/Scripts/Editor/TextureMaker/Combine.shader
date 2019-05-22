Shader "TextureMaker/Combine"
{
	Properties
	{
		_TexA("TexA", 2D) = "white" {}
		_TexB("TexB", 2D) = "white" {}
		[HDR]_Color("Color", Color) = (1,1,1,1)	
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM			
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"					
			
			sampler2D _TexA;
			sampler2D _TexB;
			int _rgbSource;
			int _alphaSource;
			half4 _Color;

			inline half grayscale(half3 c) 
			{
				return 0.3 * c.r + 0.59 * c.g + 0.11 * c.b;
			}

			fixed4 frag (v2f_img i) : SV_Target
			{					
				half4 a = tex2D(_TexA, i.uv);
				half4 b = tex2D(_TexB, i.uv);

				half4 res;

				if(_rgbSource == 0)
					res.rgb = a.rgb;				
				if(_rgbSource == 1)
					res.rgb = a.a;

				if(_alphaSource == 0)
					res.a = grayscale(b.rgb);
				if(_alphaSource == 1)
					res.a = b.a;

				return _Color * res;
			}
			ENDCG
		}
	}
}
