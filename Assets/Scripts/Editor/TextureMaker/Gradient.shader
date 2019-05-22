Shader "Custom/Gradient"
{
	Properties
	{
		_AX("A Position X", float) = 0
		_AY("B Position Y", float) = 0
		_BX("A Position X", float) = 1
		_BY("B Position Y", float) = 1
		_Radial("Radial", Range(0, 1)) = 0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#define MAX_KEYS 100
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"
						
			float _AX;
			float _BX;
			float _AY;
			float _BY;
			float _Radial;

			int _ColorKeysLength;
			fixed4 _Colors[MAX_KEYS];
			float _ColorKeys[MAX_KEYS];

			int _AlphaKeysLength;
			float _Alpha[MAX_KEYS];
			float _AlphaKeys[MAX_KEYS];

			fixed4 frag (v2f_img i) : SV_Target
			{	
				float2 aPos = float2(_AX, _AY);
				float2 bPos = float2(_BX, _BY);

				float2 va = i.uv - aPos;
				float2 vb = bPos - aPos;
				
				// LINEAR				
				float2 aOnb = dot(va, normalize(vb)) * normalize(vb);
				float tLinear = length(aOnb) / length(vb);		

				// RADIAL
				float tRadial = length(va) / length(vb);


				// COMBINE T
				float t = clamp(lerp(tLinear, tRadial, _Radial), 0, 1);
				

				// COLOR
				int ib = 0;
				for (ib = 0; ib < _ColorKeysLength; ib++)
				{
					if (_ColorKeys[ib] > t)
						break;					
				}

				float cta = _ColorKeys[ib - 1];
				float ctb = _ColorKeys[ib];

				fixed4 ca = _Colors[ib - 1];
				fixed4 cb = _Colors[ib];


				fixed4 color = lerp(ca, cb, (t-cta) / (ctb - cta));


				// ALPHA
				for (ib = 0; ib < _AlphaKeysLength; ib++)
				{
					if (_AlphaKeys[ib] > t)
						break;
				}

				float ata = _AlphaKeys[ib - 1];
				float atb = _AlphaKeys[ib];

				float aa = _Alpha[ib - 1];
				float ab = _Alpha[ib];

				float alpha = lerp(aa, ab, (t - ata) / (atb - ata));

				return fixed4(color.rgb, alpha);
			}
			ENDCG
		}
	}
}
