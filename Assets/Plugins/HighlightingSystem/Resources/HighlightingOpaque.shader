Shader "Hidden/Highlighted/Opaque"
{
	Properties
	{
		[HideInInspector] _Color ("", Color) = (1, 1, 1, 1)
	}
	
	SubShader
	{
		Lighting Off
		Fog { Mode Off }
		ZWrite Off			// Manual depth test
		ZTest Always		// Manual depth test

		Pass
		{
			Stencil
			{
				Ref 1
				Comp Always
				Pass Replace
				ZFail Keep
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 2.0
			#pragma multi_compile __ SEE_THROUGH
			#include "UnityCG.cginc"

			uniform fixed4 _Color;

			#ifndef SEE_THROUGH
			uniform sampler2D_float _CameraDepthTexture;
			#endif
			
			struct vs_input
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct ps_input
			{
				float4 pos : SV_POSITION;

				#ifndef SEE_THROUGH
				float4 screen : TEXCOORD0;
				#endif
			};
			
			ps_input vert(vs_input v)
			{
				ps_input o;

				UNITY_SETUP_INSTANCE_ID(v);
				o.pos = UnityObjectToClipPos(v.vertex);

				#ifndef SEE_THROUGH
				o.screen = ComputeScreenPos(o.pos);
				COMPUTE_EYEDEPTH(o.screen.z);
				#endif

				return o;
			}

			fixed4 frag(ps_input i) : SV_Target
			{
				#ifndef SEE_THROUGH
				float d = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screen));
				d = LinearEyeDepth(d);
				clip(d - i.screen.z + 0.01);
				#endif

				return _Color;
			}
			ENDCG
		}
	}
	Fallback Off
}