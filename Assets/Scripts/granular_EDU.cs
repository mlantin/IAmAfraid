using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

public class granular_EDU : MonoBehaviour {

	//public AudioClip _clip; // attach a sample in the editor
	public Hv_slo_Granular_AudioLib granular;
	public AudioSource source;
	public AudioClip clip;
	GvrAudioSource gvrSource = null;

	// Use this for initialization
	void Start () {
		//AudioSource source = GetComponent<AudioSource>();
		//AudioClip clip = source.clip;
		gvrSource = gameObject.AddComponent<GvrAudioSource> ();
		granular = gameObject.AddComponent<Hv_slo_Granular_AudioLib>();
		granular.FillTableWithMonoAudioClip("source_Array", clip);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Source_length, (clip.samples));
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Metro, 0.2f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainden_vari, 5.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindensity, 300.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindur_vari, 5.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainduration, 100.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainpos_vari, 10.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainposition, 0.5f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainrate_vari, 1.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainrate, 1.0f);

		AudioMixer mixer = (Resources.Load("sound0") as AudioMixer);
		if (mixer != null) {
			source.outputAudioMixerGroup = mixer.FindMatchingGroups ("Master") [0];
		} else {
			Debug.Log ("Could not find the mixer");
		}

		Debug.Log ("Clip: " + clip.name);
		Debug.Log ("Samples: " + clip.samples);
	}

	// Update is called once per frame
	void Update () {
		{
//			if(Input.touchCount == 1 && Input.GetTouch(0).phase== TouchPhase.Began)
//			{
//				//AudioSource source = GetComponent<AudioSource>();
//				clip = (AudioClip)Resources.Load("Educational");
//				//AudioClip clip = source.clip;
//				granular = GetComponent<Hv_slo_Granular_AudioLib>();
//				granular.FillTableWithMonoAudioClip("source_Array", clip);
//				granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Source_length, (clip.samples));
//				granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Metro, 0.0f);
//				granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainden_vari, 5.0f);
//				granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindensity, 300.0f);
//				granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindur_vari, 5.0f);
//				granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainduration, 100.0f);
//				granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainpos_vari, 10.0f);
//				granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainposition, 0.5f);
//				granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainrate_vari, 1.0f);
//				granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainrate, 1.0f);
//				Debug.Log ("Clip: " + clip.name);
//				Debug.Log ("Samples: " + clip.samples);
//			}
		}
	}
/*	void OnMouseDown() {
		clip = (AudioClip)Resources.Load("Educational");233
		granular = GetComponent<Hv_slo_Granular_AudioLib>();
		granular.FillTableWithMonoAudioClip("source_Array", clip);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Source_length, (clip.samples));
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Metro, 1.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainden_vari, 5.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindensity, 300.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Graindur_vari, 5.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainduration, 100.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainpos_vari, 10.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainposition, 0.5f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainrate_vari, 0.0f);
		granular.SetFloatParameter(Hv_slo_Granular_AudioLib.Parameter.Grainrate, 1.0f);
		Debug.Log (clip.name);
		Debug.Log (clip.samples);
	} */
}