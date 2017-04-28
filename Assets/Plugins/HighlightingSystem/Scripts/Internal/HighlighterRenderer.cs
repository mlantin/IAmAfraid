using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

namespace HighlightingSystem
{
	[DisallowMultipleComponent]
	[AddComponentMenu("")]  // Hide in 'Add Component' menu
	public class HighlighterRenderer : MonoBehaviour
	{
		private struct Data
		{
			public Material material;
			public int submeshIndex;
			public bool transparent;
		}

		#region Constants
		// Default transparency cutoff value (used for shaders without _Cutoff property)
		static private float transparentCutoff = 0.5f;

		// Flags to hide and don't save this component in editor
		private const HideFlags flags = HideFlags.HideInInspector | HideFlags.DontSaveInEditor | HideFlags.NotEditable | HideFlags.DontSaveInBuild;
		
		// Cull Off
		private const int cullOff = (int)CullMode.Off;

		// 
		static private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
		#endregion

		// Shader property ID cached constants
		static private ShaderPropertyID shaderPropertyID
		{
			get { return HighlightingBase.shaderPropertyID; }
		}

		static private readonly string sRenderType = "RenderType";
		static private readonly string sOpaque = "Opaque";
		static private readonly string sTransparent = "Transparent";
		static private readonly string sTransparentCutout = "TransparentCutout";
		static private readonly string sMainTex = "_MainTex";

		private Renderer r;
		private List<Data> data;
		private Camera lastCamera = null;
		private bool isAlive;
		private Coroutine endOfFrame;

		#region MonoBehaviour
		// 
		void OnEnable()
		{
			endOfFrame = StartCoroutine(EndOfFrame());
		}

		// 
		void OnDisable()
		{
			lastCamera = null;
			if (endOfFrame != null)
			{
				StopCoroutine(endOfFrame);
			}
		}

		// Called once (before OnPreRender) for each camera if the object is visible
		void OnWillRenderObject()
		{
			Camera cam = Camera.current;
			// Another camera may intercept rendering and send it's own OnWillRenderObject events (i.e. water rendering), 
			// so we're caching currently rendering camera only if it has HighlighterBase component
			if (HighlightingBase.IsHighlightingCamera(cam))
			{
				// VR Camera renders twice per frame (once for each eye), but OnWillRenderObject is called once so we have to cache reference to the camera
				lastCamera = cam;
			}
		}

		// 
		void OnDestroy()
		{
			// Data will be null if Undo / Redo was performed in Editor to delete / restore object with this component
			if (data == null) { return; }

			for (int i = 0, imax = data.Count; i < imax; i++)
			{
				Data d = data[i];
				// Unity never garbage-collects unreferenced materials, so it is our responsibility to destroy them
				if (d.transparent)
				{
					Destroy(d.material);
				}
			}
		}
		#endregion

		#region Private Methods
		// 
		IEnumerator EndOfFrame()
		{
			while (true)
			{
				yield return waitForEndOfFrame;

				lastCamera = null;

				if (!isAlive)
				{
					Destroy(this);
				}
			}
		}
		#endregion

		#region Public Methods
		// 
		public void Initialize(Material sharedOpaqueMaterial, Shader transparentShader, bool seeThrough)
		{
			data = new List<Data>();

			r = GetComponent<Renderer>();
			this.hideFlags = flags;
			Material[] materials = r.sharedMaterials;

			if (materials != null)
			{
				for (int i = 0; i < materials.Length; i++)
				{
					Material sourceMat = materials[i];
					
					if (sourceMat == null) { continue; }
					
					Data d = new Data();
					
					string tag = sourceMat.GetTag(sRenderType, true, sOpaque);
					if (tag == sTransparent || tag == sTransparentCutout)
					{
						Material replacementMat = new Material(transparentShader);

						// To render both sides of the Sprite
						if (r is SpriteRenderer) { replacementMat.SetInt(shaderPropertyID._Cull, cullOff); }

						// Make sure that shader will have proper default value
						if (seeThrough) { replacementMat.EnableKeyword(Highlighter.keywordSeeThrough); }
						else { replacementMat.DisableKeyword(Highlighter.keywordSeeThrough); }

						if (sourceMat.HasProperty(shaderPropertyID._MainTex))
						{
							replacementMat.SetTexture(shaderPropertyID._MainTex, sourceMat.mainTexture);
							replacementMat.SetTextureOffset(sMainTex, sourceMat.mainTextureOffset);
							replacementMat.SetTextureScale(sMainTex, sourceMat.mainTextureScale);
						}
						
						int cutoff = shaderPropertyID._Cutoff;
						replacementMat.SetFloat(cutoff, sourceMat.HasProperty(cutoff) ? sourceMat.GetFloat(cutoff) : transparentCutoff);
						
						d.material = replacementMat;
						d.transparent = true;
					}
					else
					{
						d.material = sharedOpaqueMaterial;
						d.transparent = false;
					}
					
					d.submeshIndex = i;
					data.Add(d);
				}
			}
		}

		// 
		public bool FillBuffer(CommandBuffer buffer, bool forceRender)
		{
			if (r == null) { return false; }

			if (lastCamera == Camera.current || forceRender)
			{
				for (int i = 0, imax = data.Count; i < imax; i++)
				{
					Data d = data[i];
					buffer.DrawRenderer(r, d.material, d.submeshIndex);
				}
			}
			return true;
		}

		// Sets given color as highlighting color on all transparent materials of this renderer
		public void SetColorForTransparent(Color clr)
		{
			for (int i = 0, imax = data.Count; i < imax; i++)
			{
				Data d = data[i];
				if (d.transparent)
				{
					d.material.SetColor(shaderPropertyID._Color, clr);
				}
			}
		}
		
		// Sets ZTest parameter on all transparent materials of this renderer
		public void SetSeeThroughForTransparent(bool seeThrough)
		{
			for (int i = 0, imax = data.Count; i < imax; i++)
			{
				Data d = data[i];
				if (d.transparent)
				{
					if (seeThrough) { d.material.EnableKeyword(Highlighter.keywordSeeThrough); }
					else { d.material.DisableKeyword(Highlighter.keywordSeeThrough); }
				}
			}
		}
		
		// 
		public void SetState(bool alive)
		{
			isAlive = alive;
		}
		#endregion
	}
}