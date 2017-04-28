using UnityEngine;
using System.Collections;

namespace HighlightingSystem
{
	public class ShaderPropertyID
	{
		// Common
		public readonly int _MainTex;
		public readonly int _Color;

		// HighlightingSystem
		public readonly int _Cutoff;
		public readonly int _Intensity;
		public readonly int _Cull;
		public readonly int _HighlightingBlur1;
		public readonly int _HighlightingBlur2;
		public readonly int _HighlightingBuffer;

		// HighlightingSystem global shader properties. Should be unique!
		public readonly int _HighlightingBlurOffset;

		// Ctor
		public ShaderPropertyID()
		{
			_MainTex = Shader.PropertyToID("_MainTex");
			_Color = Shader.PropertyToID("_Color");

			_Cutoff = Shader.PropertyToID("_Cutoff");
			_Intensity = Shader.PropertyToID("_Intensity");
			_Cull = Shader.PropertyToID("_Cull");
			_HighlightingBlur1 = Shader.PropertyToID("_HighlightingBlur1");
			_HighlightingBlur2 = Shader.PropertyToID("_HighlightingBlur2");
			_HighlightingBuffer = Shader.PropertyToID("_HighlightingBuffer");

			_HighlightingBlurOffset = Shader.PropertyToID("_HighlightingBlurOffset");
		}
	}
}