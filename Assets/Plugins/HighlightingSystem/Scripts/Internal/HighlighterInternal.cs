using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

namespace HighlightingSystem
{
	[DisallowMultipleComponent]
	public partial class Highlighter : MonoBehaviour
	{
		private enum Mode : int
		{
			None, 
			Highlighter, 
			HighlighterSeeThrough, 
			OccluderSeeThrough, 
		}

		// Constants (don't touch this!)
		#region Constants
		// 
		public const string keywordSeeThrough = "SEE_THROUGH";

		// 2 * PI constant for sine flashing
		private const float doublePI = 2f * Mathf.PI;

		// Occlusion color
		private readonly Color occluderColor = new Color(0f, 0f, 0f, 0f);

		// Highlighting modes rendered in that order
		static private readonly Mode[] renderingOrder = new Mode[] { Mode.Highlighter, Mode.HighlighterSeeThrough, Mode.OccluderSeeThrough };
		#endregion

		#region Static Fields
		// List of all highlighters in scene
		static private HashSet<Highlighter> highlighters = new HashSet<Highlighter>();

		// Shader property ID cached constants
		static private ShaderPropertyID shaderPropertyID
		{
			get { return HighlightingBase.shaderPropertyID; }
		}
		#endregion

		#region Public Fields
		/// <summary>
		/// Controls see-through mode for highlighters or occluders. When set to true - highlighter in this mode will not be occluded by anything (except for see-through occluders). Occluder in this mode will overlap any highlighting.
		/// </summary>
		[HideInInspector]
		public bool seeThrough;

		/// <summary>
		/// Controls occluder mode. Note that non-see-through highlighting occluders will be enabled only when frame depth buffer is not available!
		/// </summary>
		[HideInInspector]
		public bool occluder;

		/// <summary>
		/// Force-render highlighting of this Highlighter. No culling is performed in this case (neither frustum nor occlusion culling) and renderers from all LOD levels will be always rendered. 
		/// Please be considerate in enabling this mode, or you may experience performance degradation. 
		/// </summary>
		[HideInInspector]
		public bool forceRender = false;
		#endregion

		#region Private Fields
		// Cached transform component reference
		private Transform tr;

		// Cached Renderers
		private List<HighlighterRenderer> highlightableRenderers = new List<HighlighterRenderer>();

		// Renderers reinitialization is required flag
		private bool renderersDirty;

		// Static list to prevent unnecessary memory allocations when grabbing renderer components
		static private List<Component> sComponents = new List<Component>(4);

		// Highlighting mode
		private Mode mode;
		
		// Cached seeThrough value
		private bool cachedSeeThrough;

		// One-frame highlighting flag
		private int cachedOnce = -1;
		private bool once
		{
			get { return cachedOnce == Time.frameCount; }
			set { cachedOnce = value ? Time.frameCount : -1; }
		}

		// Flashing enabled flag
		private bool flashing;

		// Current highlighting color
		private Color currentColor;
		
		// Current transition value
		private float transitionValue;

		// Current Transition target
		private float transitionTarget;

		// Transition duration
		private float transitionTime;

		// One-frame highlighting color
		private Color onceColor;
		
		// Flashing frequency (times per second)
		private float flashingFreq;
		
		// Flashing from color
		private Color flashingColorMin;
		
		// Flashing to color
		private Color flashingColorMax;

		// Constant highlighting color
		private Color constantColor;

		// Opaque shader cached reference
		static private Shader _opaqueShader;
		static public Shader opaqueShader
		{
			get
			{
				if (_opaqueShader == null)
				{
					_opaqueShader = Shader.Find("Hidden/Highlighted/Opaque");
				}
				return _opaqueShader;
			}
		}
		
		// Transparent shader cached reference
		static private Shader _transparentShader;
		static public Shader transparentShader
		{
			get
			{
				if (_transparentShader == null)
				{
					_transparentShader = Shader.Find("Hidden/Highlighted/Transparent");
				}
				return _transparentShader;
			}
		}
		
		// Shared (for this component) replacement material for opaque geometry highlighting
		private Material _opaqueMaterial;
		private Material opaqueMaterial
		{
			get
			{
				if (_opaqueMaterial == null)
				{
					_opaqueMaterial = new Material(opaqueShader);

					// Make sure that shader will have proper default value
					if (cachedSeeThrough) { _opaqueMaterial.EnableKeyword(keywordSeeThrough); }
					else { _opaqueMaterial.DisableKeyword(keywordSeeThrough); }
				}
				return _opaqueMaterial;
			}
		}
		#endregion

		#region MonoBehaviour
		// 
		void Awake()
		{
			tr = GetComponent<Transform>();

			renderersDirty = true;
			cachedSeeThrough = seeThrough = true;
			mode = Mode.None;

			// Initial highlighting state
			once = false;
			flashing = false;
			occluder = false;
			transitionValue = transitionTarget = 0f;
			onceColor = Color.red;
			flashingFreq = 2f;
			flashingColorMin = new Color(0f, 1f, 1f, 0f);
			flashingColorMax = new Color(0f, 1f, 1f, 1f);
			constantColor = Color.yellow;
		}
		
		// 
		void OnEnable()
		{
			highlighters.Add(this);
		}

		// 
		void OnDisable()
		{
			highlighters.Remove(this);
			
			ClearRenderers();

			// Reset highlighting parameters to default values
			renderersDirty = true;
			once = false;
			flashing = false;
			transitionValue = transitionTarget = 0f;

			/* 
			// Reset custom parameters of the highlighting
			occluder = false;
			seeThrough = false;
			onceColor = Color.red;
			flashingFreq = 2f;
			flashingColorMin = new Color(0f, 1f, 1f, 0f);
			flashingColorMax = new Color(0f, 1f, 1f, 1f);
			constantColor = Color.yellow;
			transitionTime = 0f;
			*/
		}

		// 
		void Update()
		{
			UpdateTransition();
		}

		// 
		void OnDestroy()
		{
			// Unity never garbage-collects unreferenced materials, so it is our responsibility to destroy them
			if (_opaqueMaterial != null)
			{
				Destroy(_opaqueMaterial);
			}
		}
		#endregion

		#region Private Methods
		// Clear cached renderers
		private void ClearRenderers()
		{
			for (int i = highlightableRenderers.Count - 1; i >= 0; i--)
			{
				HighlighterRenderer renderer = highlightableRenderers[i];
				renderer.SetState(false);
			}
			highlightableRenderers.Clear();
		}

		// This method defines the way in which renderers are initialized
		private void UpdateRenderers()
		{
			if (renderersDirty)
			{
				ClearRenderers();

				// Find all renderers which should be controlled with this Highlighter component
				List<Renderer> renderers = new List<Renderer>();
				GrabRenderers(tr, renderers);

				// Cache found renderers
				for (int i = 0, imax = renderers.Count; i < imax; i++)
				{
					GameObject rg = renderers[i].gameObject;
					HighlighterRenderer renderer = rg.GetComponent<HighlighterRenderer>();
					if (renderer == null) { renderer = rg.AddComponent<HighlighterRenderer>(); }
					renderer.SetState(true);

					renderer.Initialize(opaqueMaterial, transparentShader, cachedSeeThrough);
					highlightableRenderers.Add(renderer);
				}
				
				renderersDirty = false;
			}
		}

		// Recursively follows hierarchy of objects from t, searches for Renderers and adds them to the list. 
		// Breaks if HighlighterBlocker or another Highlighter component found
		private void GrabRenderers(Transform t, List<Renderer> renderers)
		{
			GameObject g = t.gameObject;

			// Find all Renderers of all types on current GameObject g and add them to the renderers list
			for (int i = 0, imax = types.Count; i < imax; i++)
			{
				g.GetComponents(types[i], sComponents);
				for (int j = 0, jmax = sComponents.Count; j < jmax; j++)
				{
					renderers.Add(sComponents[j] as Renderer);
				}
			}
			sComponents.Clear();
			
			// Return if transform t doesn't have any children
			int childCount = t.childCount;
			if (childCount == 0) { return; }
			
			// Recursively cache renderers on all child GameObjects
			for (int i = 0; i < childCount; i++)
			{
				Transform childTransform = t.GetChild(i);

				// Do not cache Renderers of this childTransform in case it has it's own Highlighter component
				Highlighter h = childTransform.GetComponent<Highlighter>();
				if (h != null) { continue; }

				// Do not cache Renderers of this childTransform in case HighlighterBlocker found
				HighlighterBlocker hb = childTransform.GetComponent<HighlighterBlocker>();
				if (hb != null) { continue; }
				
				GrabRenderers(childTransform, renderers);
			}
		}

		// Updates highlighting color
		private void UpdateColors()
		{
			if (once)
			{
				currentColor = onceColor;
			}
			else if (flashing)
			{
				// Flashing frequency is not affected by Time.timeScale
				currentColor = Color.Lerp(flashingColorMin, flashingColorMax, 0.5f * Mathf.Sin(Time.realtimeSinceStartup * flashingFreq * doublePI) + 0.5f);
			}
			else if (transitionValue > 0f)
			{
				currentColor = constantColor;
				currentColor.a *= transitionValue;
			}
			else if (occluder)
			{
				currentColor = occluderColor;
			}
			else
			{
				return;
			}

			// Apply color
			opaqueMaterial.SetColor(shaderPropertyID._Color, currentColor);
			for (int i = 0; i < highlightableRenderers.Count; i++)
			{
				highlightableRenderers[i].SetColorForTransparent(currentColor);
			}
		}

		// Update transition value
		private void UpdateTransition()
		{
			if (transitionValue != transitionTarget)
			{
				if (transitionTime <= 0f)
				{
					transitionValue = transitionTarget;
				}
				else
				{
					float dir = (transitionTarget > 0f ? 1f : -1f);
					transitionValue = Mathf.Clamp01(transitionValue + (dir * Time.unscaledDeltaTime) / transitionTime);
				}
			}
		}

		// 
		private void FillBufferInternal(CommandBuffer buffer, Mode m)
		{
			UpdateRenderers();

			// Update mode
			mode = Mode.None;
			// Highlighter
			if (once || flashing || (transitionValue > 0f))
			{
				mode = seeThrough ? Mode.HighlighterSeeThrough : Mode.Highlighter;
			}
			// Occluder
			else if (occluder && seeThrough)
			{
				mode = Mode.OccluderSeeThrough;
			}

			if (mode == Mode.None || mode != m) { return; }

			// Update shader property if changed
			if (cachedSeeThrough != seeThrough)
			{
				cachedSeeThrough = seeThrough;

				if (cachedSeeThrough) { _opaqueMaterial.EnableKeyword(keywordSeeThrough); }
				else { _opaqueMaterial.DisableKeyword(keywordSeeThrough); }

				for (int i = 0; i < highlightableRenderers.Count; i++) 
				{
					highlightableRenderers[i].SetSeeThroughForTransparent(seeThrough);
				}
			}

			UpdateColors();

			// Fill CommandBuffer with this highlighter rendering commands
			for (int i = highlightableRenderers.Count - 1; i >= 0; i--)
			{
				// To avoid null-reference exceptions when cached renderer has been removed but ReinitMaterials wasn't been called
				HighlighterRenderer renderer = highlightableRenderers[i];
				if (renderer == null)
				{
					highlightableRenderers.RemoveAt(i);
				}
				// Try to fill buffer
				else if (!renderer.FillBuffer(buffer, forceRender))
				{
					highlightableRenderers.RemoveAt(i);
					renderer.SetState(false);
				}
			}
		}
		#endregion

		#region Static Methods
		// Fill CommandBuffer with highlighters rendering commands
		static public void FillBuffer(CommandBuffer buffer)
		{
			for (int i = 0; i < renderingOrder.Length; i++)
			{
				Mode mode = renderingOrder[i];

				var e = highlighters.GetEnumerator();
				while (e.MoveNext())
				{
					Highlighter highlighter = e.Current;
					highlighter.FillBufferInternal(buffer, mode);
				}
			}
		}
		#endregion
	}
}