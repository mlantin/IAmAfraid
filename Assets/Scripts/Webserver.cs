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

	public void setServerIP(string ip){
		m_serverIP = ip;
		PlayerPrefs.SetString ("SoundServerIP", ip);
	}

	public void setServerPort(string port) {
		m_serverPort = port;
	}

	public  IEnumerator Upload(string filename, AudioClip clip, DownloadHandler handler) {
		float[] audioData = new float[clip.samples];
		clip.GetData (audioData, 0);
		MemoryStream stream = new MemoryStream();
		BinaryWriter bw = new BinaryWriter(stream);
		ConvertAndWrite (bw, audioData, clip.samples, clip.channels);
		byte[] floatBytes = stream.ToArray();
		//NetworkConnection conn = NetworkManager.singleton.client.connection;
		UnityWebRequest www = UnityWebRequest.Put("http://"+m_serverIP+":"+m_serverPort+"/audio?fn="+filename, floatBytes);
		if (handler != null)
			www.downloadHandler = handler;
		yield return www.Send();
		//www.Send();

		if(www.isError) {
			Debug.Log("There was an error uploading: "+www.error);
		}
		else {
			Debug.Log("Upload complete!");
		}
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

	static public string GenerateFileName(string context)
	{
		return context + "_" + System.DateTime.Now.ToString("yyyyMMddHHmm") + "_" + System.Guid.NewGuid().ToString("N");
	}

	public IEnumerator GetAudioClip(string fileName, System.Action<AudioClip> callback) {
		using(UnityWebRequest www = UnityWebRequest.GetAudioClip("http://"+m_serverIP+":"+m_serverPort+"?fn=" + fileName, AudioType.WAV)) {
			yield return www.Send();
			if (!www.downloadHandler.isDone)
				yield return new WaitUntil(() => www.downloadHandler.isDone == true);

			if(www.isError) {
				Debug.Log("Download error: " + www.error);
			}
			else {
				if (DownloadHandlerAudioClip.GetContent(www) == null) {
					Debug.Log ("The received audio clip is null");
				} else {
					// This next yield appears to be necessary to have the correct number of samples in the AudioClip.
					yield return null;
					Debug.Log ("Acquisition of audio clip complete");
					callback (DownloadHandlerAudioClip.GetContent(www));
				}
				yield return null;
			}
		}
	}

	public IEnumerator DeleteAudioClip(string fileName) {
		Debug.Log ("Deleting an audio clip: " + fileName);
		UnityWebRequest www = UnityWebRequest.Delete ("http://" + m_serverIP + ":" + m_serverPort + "?fn=" + fileName);
		www.downloadHandler = new DownloadHandlerBuffer();
		yield return www.Send ();
		if (www.isError) {
			Debug.Log (www.error);
		} else {
			string result = DownloadHandlerBuffer.GetContent (www);
			if (result == null) {
				Debug.Log ("Deletion confirmation is null");
			} else {
				Debug.Log(result.ToString());
			}
		}
	}

	public void DeleteAudioClipNoCheck(string fileName) {
		Debug.Log ("Deleting an audio clip: " + fileName);
		UnityWebRequest www = UnityWebRequest.Delete ("http://" + m_serverIP + ":" + m_serverPort + "?fn=" + fileName);
		www.downloadHandler = new DownloadHandlerBuffer();
		www.Send ();
	}

}
