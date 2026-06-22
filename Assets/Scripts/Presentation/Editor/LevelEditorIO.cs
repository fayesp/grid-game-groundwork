using UnityEditor;
using UnityEngine;
using System.IO;

public partial class LevelEditor {

    #region 关卡保存与加载

    /// <summary>
    /// 保存关卡到磁盘
    /// </summary>
    void SaveToDisk(string levelName) {

		if(!System.IO.Directory.Exists(levelPath)) {
			System.IO.Directory.CreateDirectory(levelPath);
		}

        string path = levelPath + levelName + ".json";
		StreamWriter writer = new StreamWriter(path, false);
		writer.WriteLine(JsonUtility.ToJson(new SerializedLevel(currentLevelParent)));
		writer.Close();
		AssetDatabase.ImportAsset(path);
		RefreshSavedLevels();
		AssetDatabase.Refresh();

		isDirty = false;
    }

    /// <summary>
    /// 从磁盘加载关卡
    /// </summary>
    void LoadFromDisk(string levelName) {

		if (isLoading || string.IsNullOrWhiteSpace(levelName)) {
			return;
		}

		Vector3 levelPosition = Vector3.zero;
		GameObject existingLevelParent = GameObject.Find(levelName);
		if (existingLevelParent != null && existingLevelParent.CompareTag("Level")) {
			levelPosition = existingLevelParent.transform.position;
			Undo.DestroyObjectImmediate(existingLevelParent);
		}

		LevelManager.currentLevelName = levelName;
        TextAsset textFile = Resources.Load<TextAsset>("Levels/" + levelName);
		if (textFile == null) {
			Debug.LogError("No level found called " + levelName);
			return;
		}

		isLoading = true;

        SerializedLevel serializedLevel = LevelLoader.LoadLevel(levelName);

        foreach (var slo in serializedLevel.LevelObjects) {
			GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath(PathToAsset(slo.prefab), typeof(GameObject));
	        var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
	        go.transform.parent = currentLevelParent.transform;
	        go.transform.localPosition = slo.pos;
	        go.transform.localEulerAngles = slo.angles;
	        Undo.RegisterCreatedObjectUndo (go, "Create object");
        }

		currentLevelParent.transform.position = levelPosition;
		newLevelName = levelName;
		isLoading = false;
		isDirty = false;
    }

    #endregion
}
