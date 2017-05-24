using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class GranularUploadHandler : MonoBehaviour {
	[DllImport("AudioPluginDemo")]
	private static extern bool Granulator_UploadSample(int index, float[] data, int numsamples, int numchannels, int samplerate, [MarshalAs(UnmanagedType.LPStr)] string name);

	static public GranularUploadHandler singleton;

	static public int MaxSamples = 64;

	static float lowcut = 0.0f;
	static float highcut = 24000.0f;
	static int order = 3;

	static bool[] slotFilled = new bool[MaxSamples];

	// Use this for initialization
	void Start () {
		singleton = this;

		for (int i = 0; i < slotFilled.Length; i++) {
			slotFilled[i] = false;
		}
	}

	public void setSlotToEmpty(int slot) {
		slotFilled[slot] = false;
	}

	public int uploadSample(AudioClip s) {
		// Get the first unfilled slot
		int currindex;
		for (currindex = 0; currindex < MaxSamples; currindex++) {
			if (!slotFilled [currindex])
				break;
		}
		if (s != null && s.loadState == AudioDataLoadState.Loaded) {
			Debug.Log ("Uploading sample " + s.name + " to slot " + currindex);

			int numsamples = s.samples;
			int numchannels = s.channels;
			float[] data = new float[numsamples * numchannels];
			s.GetData (data, 0);
			for (int c = 0; c < numchannels; c++) {
				bool modified = false;
				float sr = (float)s.frequency, bw = 0.707f;
				for (int k = 0; k < order; k++) {
					if (lowcut > 0.0f) {
						float lpf = 0.0f, bpf = 0.0f, cutoff = 2.0f * Mathf.Sin (0.25f * Mathf.Min (lowcut / sr, 0.5f));
						for (int n = 0; n < numsamples; n++) {
							lpf += bpf * cutoff;
							float hpf = bw * data [n * numchannels + c] - lpf - bpf * bw;
							bpf += hpf * cutoff;
							lpf += bpf * cutoff;
							hpf = bw * data [n * numchannels + c] - lpf - bpf * bw;
							bpf += hpf * cutoff;
							data [n * numchannels + c] = hpf;
						}
						modified = true;
					}
					if (highcut < sr * 0.5f) {
						float lpf = 0.0f, bpf = 0.0f, cutoff = 2.0f * Mathf.Sin (0.25f * Mathf.Min (highcut / sr, 0.5f));
						for (int n = 0; n < numsamples; n++) {
							lpf += bpf * cutoff;
							float hpf = bw * data [n * numchannels + c] - lpf - bpf * bw;
							bpf += hpf * cutoff;
							lpf += bpf * cutoff;
							hpf = bw * data [n * numchannels + c] - lpf - bpf * bw;
							bpf += hpf * cutoff;
							data [n * numchannels + c] = lpf;
						}
						modified = true;
					}
					if (k == order - 1 && modified) {
						float peak = 0.0f;
						for (int n = 0; n < numsamples; n++) {
							float a = Mathf.Abs (data [n * numchannels + c]);
							if (a > peak)
								peak = a;
						}
						float scale = 1.0f / peak;
						for (int n = 0; n < numsamples; n++)
							data [n * numchannels + c] *= scale;
					}
				}
			}
			Debug.Log ("uploading " + numsamples + " at " + currindex);
			Granulator_UploadSample (currindex, data, numsamples, numchannels, s.frequency, s.name);
			slotFilled [currindex] = true;
			return currindex;
		} else {
			return -1;
		}
	}
}
