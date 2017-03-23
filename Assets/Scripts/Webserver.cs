using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

//Utility class to save Wav files to the server
public class Webserver : MonoBehaviour {
	static public Webserver singleton;

	public string m_serverIP;
	public string m_serverPort;

	// Use this for initialization
	void Start () {
		singleton = this;
	}
	
	public  bool Upload(string filename, AudioClip clip) {
		float[] audioData = new float[clip.samples];
		clip.GetData (audioData, 0);
		MemoryStream stream = new MemoryStream();
		BinaryWriter bw = new BinaryWriter(stream);
		ConvertAndWrite (bw, audioData, clip.samples, clip.channels);
		byte[] floatBytes = stream.ToArray();
		//NetworkConnection conn = NetworkManager.singleton.client.connection;
		UnityWebRequest www = UnityWebRequest.Put("http://"+m_serverIP+":"+m_serverPort+"/audio?fn="+filename, floatBytes);
		//yield return www.Send();
		www.Send();

		if(www.isError) {
			Debug.Log(www.error);
		}
		else {
			Debug.Log("Upload complete!");
		}
		return !www.isError;
	}

	public void ConvertAndWrite(BinaryWriter bw, float[] samplesData, int numsamples, int channels)
	{
		float[] samples = new float[numsamples*channels];

		samples = samplesData;

		short intDatum;

		byte[] bytesData = new byte[samples.Length * 2];

		const float rescaleFactor = 32767; //to convert float to Int16

		for (int i = 0; i < samples.Length; i++)
		{
			intDatum = (short)(samples[i] * rescaleFactor);
			bw.Write (intDatum);
			//Debug.Log (samples [i]);
		}
		bw.Flush ();
	}

	public string GenerateFileName(string context)
	{
		return context + "_" + System.DateTime.Now.ToString("yyyyMMddHHmm") + "_" + System.Guid.NewGuid().ToString("N");
	}

	public IEnumerator GetAudioClip(string fileName, System.Action<AudioClip> callback) {
		using(UnityWebRequest www = UnityWebRequest.GetAudioClip("http://"+m_serverIP+":"+m_serverPort+"?fn=" + fileName, AudioType.WAV)) {
			yield return www.Send();

			if(www.isError) {
				Debug.Log("Logging error: " + www.error);
			}
			else {
				AudioClip newClip = DownloadHandlerAudioClip.GetContent(www);
				if (newClip == null) {
					Debug.Log ("The received audio clip is null");
				} else {
					Debug.Log ("Acquisition of audio clip complete");
					callback (newClip);
				}
				yield return null;
			}
		}
	}
}
