using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveData {

	public static bool initialized { get; private set; } = false;
	private static int gameNumber = 1;
	private static PlayerData playerData = new PlayerData();

	private static string Path(int i) {
		return Application.persistentDataPath + "/" + i + ".json";
	}

	public static bool GameExists(int i) {
		return File.Exists(Path(i));
	}

	static PlayerData FileData(int i) {
		string json = File.ReadAllText(Path(i));
		return JsonUtility.FromJson<PlayerData>(json);
	}

	public static void LoadGame(int i) {
		initialized = true;
		gameNumber = i;

		PlayerData data = new PlayerData();
		if (GameExists(gameNumber)) {
			data = FileData(gameNumber);
        }
		playerData = data;
	}

	public static void DeleteGame(int i) {
		if (File.Exists(Path(i))) {
			File.Delete(Path(i));
		}
	}

	public static void SaveGame() {
		string json = JsonUtility.ToJson(playerData, true);
		File.WriteAllText(Path(gameNumber), json);
	}

	public static void BeatLevel(string level) {
		if (!playerData.levelsBeaten.Contains(level)) {
			playerData.levelsBeaten.Add(level);
		}
	}
}

[Serializable]
public class PlayerData {
	public List<string> levelsBeaten = new List<string>();
}
