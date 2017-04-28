Shader "Hidden/Highlighted/Transparent"
{
	Properties
	{
		[HideInInspector] _MainTex ("", 2D) = "" {}
		[HideInInspector] _Color ("", Color) = (1, 1, 1, 1)
		[HideInInspector] _Cutoff ("", Float) = 0.5
		[HideInInspector] _Cull ("", Int) = 2		// UnityEngine.Rendering.CullMode.Back
	}
	
	SubShader
	{
		Lighting Off
		Fog { Mode Off }
		ZWrite Off		// Manual depth test
		ZTest Always	// Manual depth test
		Cull [_Cull]	// For rendering both sides of the Sprite

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
			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform fixed _Cutoff;

			#ifndef SEE_THROUGH
			uniform sampler2D_float _CameraDepthTexture;
			#endif

			struct vs_input
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				fixed4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct ps_input
			{
				float4 pos : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				fixed alpha : TEXCOORD1;

				#ifndef SEE_THROUGH
				float4 screen : TEXCOORD2;
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

				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.alpha = v.color.a;
				return o;
			}

			fixed4 frag(ps_input i) : SV_Target
			{
				fixed a = tex2D(_MainTex, i.texcoord).a;
				clip(a - _Cutoff);

				#ifndef SEE_THROUGH
				float d = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screen));
				d = LinearEyeDepth(d);
				clip(d - i.screen.z + 0.01);
				#endif

				fixed4 c = _Color;
				c.a *= i.alpha;

				return c;
			}
			ENDCG
		}
	}
	
	Fallback Off
}