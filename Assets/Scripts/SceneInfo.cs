using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class SceneInfoList {

	public List<SceneInfo> scenes;
	public SceneInfoList(List<SceneInfo> _sceneInfoList) {
		scenes = _sceneInfoList;
	}

	public static SceneInfoList CreateFromJSON(string jsonText) {
		// string json = File.ReadAllText(DATA_FILE_NAME);
		return JsonUtility.FromJson<SceneInfoList> (jsonText);
	}
		
	// Workaround for JsonUtility refusing to deserialize a root-level json array.
	// We read the single-property object into an instance of WordInfoList, and then just take our desired list from it
	[System.Serializable]
	public class SceneInfo {
		// public string name;
		public SceneInfo(string _name, string _title) {
			title = _title;
			name = _name;
		}

		public string title;
		public string name;

	}

}
