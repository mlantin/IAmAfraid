using UnityEngine;
using System.Collections.Generic;
using System.IO;

// Workaround for JsonUtility refusing to deserialize a root-level json array.
// We read the single-property object into an instance of WordInfoList, and then just take our desired list from it
[System.Serializable]
public class IAAScene {
	public string title;
	public string name;
	public List<WordInfo> wordInfoList;

	public IAAScene(string _title, string _name, List<WordInfo> _wordInfoList) {
		wordInfoList = _wordInfoList;
		name = _name;
		title = _title;
	}

	public IAAScene() {
		wordInfoList = null;
		title = null;
	}

	public string getJSON() {
		return JsonUtility.ToJson (this, true);
	}

	public static IAAScene fromJSON(string json) {
		return JsonUtility.FromJson<IAAScene>(json);
	}
}



[System.Serializable]
public class WordInfo {

	public static string DATA_FILE_NAME = "sampleData.json";

	public string word;
	public string clipfn;
	public float scale;
	public Vector3 pos;
	public Quaternion rot;
	public List<int> playTriggers;
	public List<Vector3> path;

	public WordInfo(string word, string clipfn, float scale, Vector3 pos, Quaternion rot,
		bool looping, List<int> playTriggers, List<Vector3> path) {
		this.word = word;
		this.clipfn = clipfn;
		this.scale = scale;
		this.pos = pos;
		this.rot = rot;
		this.playTriggers = playTriggers;
		this.path = path;
	}
	/*
	// Main Getter utility method - reads json into list of word info objects
	public static List<WordInfo> newWordInfoListFromFile() {
		string json = File.ReadAllText(DATA_FILE_NAME);
		WordInfoList list = JsonUtility.FromJson<WordInfoList>(json);

		return list.wordInfoList;
	}

	public static List<WordInfo> newWordInfoListFromString(string jsonText) {
		WordInfoList list = JsonUtility.FromJson<WordInfoList>(jsonText);
		return list.wordInfoList;
	}

	// Makes a new Json list entry in the file, alongside the pre-existing ones
	public static void saveNewWordInfo(WordInfo wordInfo) {
		List<WordInfo> loadedInfoList = newWordInfoListFromFile();

		loadedInfoList.Add (wordInfo);
		string newJson = JsonUtility.ToJson(new WordInfoList(loadedInfoList), true);

		File.WriteAllText (DATA_FILE_NAME, newJson);
	}

	// Makes multiple new Json list entries in the file, alongside the pre-existing ones
	public static void saveWordInfoList(List<WordInfo> wordInfoList) {
		List<WordInfo> loadedInfoList = newWordInfoListFromFile();

		loadedInfoList.AddRange (wordInfoList);
		string newJson = JsonUtility.ToJson(new WordInfoList(loadedInfoList), true);

		File.WriteAllText (DATA_FILE_NAME, newJson);
	}

	// Warning: this will overwrite all current file contents with the new word info list
	public static void overwriteFileWithNewWordInfoList(List<WordInfo> wordInfoList) {
		string newJson = JsonUtility.ToJson(new WordInfoList(wordInfoList), true);

		File.WriteAllText (DATA_FILE_NAME, newJson);
	}
		
	public static void writeFileWithNewWordInfoList(string filename, List<WordInfo> wordInfoList) {
		string newJson = JsonUtility.ToJson(new WordInfoList(wordInfoList), true);

		File.WriteAllText (filename, newJson);
	}

	// Removes a word info object from the list and saves the changes
	public static void removeWordInfoFromFile(WordInfo wordInfoToRemove) {
		List<WordInfo> loadedInfoList = newWordInfoListFromFile();

		loadedInfoList.Remove (wordInfoToRemove);
		string newJson = JsonUtility.ToJson(new WordInfoList(loadedInfoList), true);

		File.WriteAllText(DATA_FILE_NAME, newJson);
	}

	public override bool Equals(object obj) {
		if (this == obj) {
			return true;
		}
		if (obj == null) {
			return false;
		}
		if (obj == null || GetType() != obj.GetType()) 
			return false;

		WordInfo other = (WordInfo) obj;
		if (word == null) {
			if (other.word != null) {
				return false;
			}
		} else if (word != other.word) {
			return false;
		}
		if (clipfn == null) {
			if (other.clipfn != null) {
				return false;
			}
		} else if (clipfn != other.clipfn) {
			return false;
		}
		if (scale == null) {
			if (other.scale != null) {
				return false;
			}
		} else if (scale != other.scale) {
			return false;
		}
		if (pos == null) {
			if (other.pos != null) {
				return false;
			}
		} else if (!pos.Equals(other.pos)) {
			return false;
		}
		if (rot == null) {
			if (other.rot != null) {
				return false;
			}
		} else if (!rot.Equals(other.rot)) {
			return false;
		}
		return true;
	}
	*/
}

