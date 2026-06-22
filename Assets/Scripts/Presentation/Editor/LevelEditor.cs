using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public partial class LevelEditor : EditorWindow {

    #region 字段定义

    // 选择的预制体索引
    int selGridInt = 0;
    // 预制体名称数组
    string[] selectStrings;

    // 旋转角度索引
    int rotateInt = 0;
    // 旋转角度字符串数组
    string[] rotateStrings = new string[] {
        "0", "90", "180", "270"
    };

    // 生成高度
    int spawnHeight = 0;
    // 当前关卡名
    string currentLevel;
    // 新关卡名
    string newLevelName = "";
    // 关卡文件保存路径
    string levelPath => Application.dataPath + "/Resources/Levels/";
    // 是否覆盖关卡
    bool overwriteLevel = true;

    // 可用的预制体数组
    public GameObject[] prefabs;

    // 编辑器状态相关
    bool isHoldingAlt;
    bool mouseButtonDown;
    bool in2DMode;
    Vector3 drawPos;
    static GameObject newGameObject;
    static bool playModeActive;
    Event e;
    bool titleIsSet;

    // 预制体配置文件路径
    static string textFilePath => Application.dataPath + "/leveleditorprefabs.txt";
    // 已保存关卡列表
    List<string> savedLevels => Utils.allLevels;
    // 已保存关卡索引
    int savedLevelIndex = 0;
    // 场景关卡索引
    int sceneLevelIndex;
    // 是否吸附到网格
    bool snapToGrid = true;
    // 是否正在加载
    bool isLoading;
    // 是否有未保存更改
    bool isDirty;
    // 上一次物体位置
    Vector3 prevPosition;
    // 滚动视图位置
    Vector2 scrollPos;
    // Gizmo颜色
    Color gizmoColor = Color.white;
    // 鼠标点击时的位置
    Vector2 mousePosOnClick = new Vector2();
    // 是否刷新预制体
    bool refreshPrefabs = true;

    // GUI样式缓存
    GUIStyle wrapperRef;
    // GUI样式
    GUIStyle wrapper {
        get {
            if (wrapperRef == null) {
                wrapperRef = new GUIStyle();
                wrapperRef.padding = new RectOffset(20,20,20,20);
                float n = 0.16f;
                wrapperRef.normal.background = Utils.MakeTex(1, 1, new Color(n, n, n, 1f));
            }
            return wrapperRef;
        }
    }

    // 关卡管理器引用
    GameObject levelRef = null;
    GameObject levelManagerGameObject {
        get {
            if (levelRef == null) {
                LevelManager levelManager = FindObjectOfType<LevelManager>();
                if (levelManager != null) {
                    levelRef = levelManager.gameObject;
                } else {
                    levelRef = new GameObject();
                    levelRef.AddComponent<LevelManager>();
                    levelRef.transform.name = "LevelManager";
                }
            }
            return levelRef;
        }
    }

    // 当前关卡父物体引用
    GameObject currentLevelParentRef;
    GameObject currentLevelParent {
        get {
            if (currentLevelParentRef == null) {
                GameObject existingLevelParent = GameObject.Find(currentLevel);
                if (existingLevelParent != null && existingLevelParent.CompareTag("Level")) {
                    currentLevelParentRef = existingLevelParent;
                } else {
                    currentLevelParentRef = new GameObject();
                    currentLevelParentRef.transform.name = currentLevel;
                    currentLevelParentRef.transform.parent = levelManagerGameObject.transform;
                }
            }
            return currentLevelParentRef;
        }
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 绘制水平线
    /// </summary>
    void HorizontalLine() => EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

    /// <summary>
    /// 获取所有关卡列表
    /// </summary>
    List<string> allLevels => Utils.allLevels;

    /// <summary>
    /// 添加菜单项，显示关卡编辑器窗口
    /// </summary>
    [MenuItem("Window/Level Editor")]
    public static void ShowWindow() {
        EditorWindow.GetWindow(typeof(LevelEditor));
    }

    /// <summary>
    /// 编辑器窗口启用时回调
    /// </summary>
    void OnEnable() {
        SceneView.duringSceneGui += SceneGUI;
        EditorApplication.playModeStateChanged += ChangedPlayModeState;
        Undo.undoRedoPerformed += Refresh;
        PopulateList();
    }

    /// <summary>
    /// 编辑器窗口禁用时回调
    /// </summary>
    void OnDisable() {
        SceneView.duringSceneGui -= SceneGUI;
        EditorApplication.playModeStateChanged -= ChangedPlayModeState;
        Undo.undoRedoPerformed -= Refresh;
    }

    /// <summary>
    /// 监听播放模式切换
    /// </summary>
    void ChangedPlayModeState(PlayModeStateChange state) {
        switch (state) {
            case PlayModeStateChange.EnteredPlayMode:
                playModeActive = true;
                break;
            case PlayModeStateChange.EnteredEditMode:
                playModeActive = false;
                GetPlayModeJobs();
                break;
        }
    }

    /// <summary>
    /// 检查和初始化
    /// </summary>
    void OnValidate() {
        if (Utils.isMetaScene) return;
        EnsureTagsExist();
        Reset();
        Refresh();
        refreshPrefabs = true;
    }

    /// <summary>
    /// 重置部分状态
    /// </summary>
    void Reset() {
        mouseButtonDown = false;
        CreateGizmoObject();
    }

    /// <summary>
    /// 刷新关卡和预制体
    /// </summary>
    void Refresh() {
        if (Utils.isMetaScene) return;

        // Prefer new GamePresenter, fall back to legacy Game
        var presenter = GameServices.Instance?.Presenter;
        if (presenter != null) {
            presenter.SyncFromScene();
        } else {
            Game.instance?.EditorRefresh();
        }

        RefreshSavedLevels();
    }

    /// <summary>
    /// 创建Gizmo对象
    /// </summary>
    void CreateGizmoObject() {
        LevelGizmo levelGizmo = FindObjectOfType<LevelGizmo>();
        if (levelGizmo == null) {
            new GameObject("LevelGizmo").AddComponent<LevelGizmo>();
        }
    }

    /// <summary>
    /// 读取预制体列表
    /// </summary>
    void PopulateList() {

        if (File.Exists(textFilePath)) {
            List<GameObject> newPrefabs = new List<GameObject>();
            string[] prefabNames = File.ReadAllLines(textFilePath);
            foreach (string prefabName in prefabNames) {
                GameObject go = (GameObject)AssetDatabase.LoadAssetAtPath(PathToAsset(prefabName), typeof(GameObject));
                if (go != null) {
                    newPrefabs.Add(go);
                }
            }
            prefabs = newPrefabs.ToArray();
        }
    }

    /// <summary>
    /// 通过名称查找预制体资源路径
    /// </summary>
    string PathToAsset(string s) {
        string[] guids = AssetDatabase.FindAssets(s, null);
        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains(".prefab")) {
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName == s) {
                    return path;
                }
            }
        }
        Debug.LogError("Couldnt find a prefab named " + s);
        return string.Empty;
    }

    /// <summary>
    /// 刷新已保存关卡列表
    /// </summary>
    void RefreshSavedLevels() {
        Utils.RefreshLevels();
    }

    /// <summary>
    /// 确保Tag存在
    /// </summary>
    void EnsureTagsExist() {
        TagHelper.AddTag("Level");
        TagHelper.AddTag("Tile");
    }

    #endregion

    #region 主GUI绘制

    /// <summary>
    /// 主窗口GUI
    /// </summary>
    void OnGUI() {

        string previousLevel = currentLevel;
        if (!titleIsSet) {
            titleIsSet = true;
            var texture = Resources.Load<Texture2D>("ggg");
            titleContent = new GUIContent("Level Editor", texture);
        }

        GUILayout.BeginVertical(wrapper);

            scrollPos = GUILayout.BeginScrollView(scrollPos);
                DrawingWindow();
                RefreshSavedLevels();
                SaveLoadWindow();
            EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();

        if (previousLevel != currentLevel) {
            Selection.activeGameObject = currentLevelParent;
        }
    }

    /// <summary>
    /// 绘制关卡编辑区
    /// </summary>
    void DrawingWindow() {

        GUILayout.Label ("DRAWING", EditorStyles.centeredGreyMiniLabel);
        HorizontalLine();

        if (string.IsNullOrWhiteSpace(currentLevel)) {
            GameObject level = GameObject.FindGameObjectWithTag("Level");
            if (level != null) {
                currentLevel = level.name;
            }
        }

        // FOR MULTIPLE SCENES AT ONCE

        GUILayout.Label ("Currently Editing: ", EditorStyles.boldLabel);

        sceneLevelIndex = 0;
        for (int i = 0; i < allLevels.Count; i++) {
            if (allLevels[i] == currentLevel) {
                sceneLevelIndex = i;
            }
        }
        sceneLevelIndex = EditorGUILayout.Popup(sceneLevelIndex, allLevels.ToArray());
        currentLevel = allLevels[sceneLevelIndex];

        if (currentLevel == null) {
            return;
        }

        EditorGUILayout.Space();

        if (prefabs == null || prefabs.Length == 0) {
            PopulateList();
        }

        if (prefabs != null && refreshPrefabs) {
            List<string> selectStringsTmp = new List<string>();
            selectStringsTmp.Add("None");
            selectStringsTmp.Add("Erase");
            foreach (GameObject prefab in prefabs) {
                if (prefab != null) {
                    selectStringsTmp.Add(prefab.transform.name);
                }
            }
            selectStrings = selectStringsTmp.ToArray();
            refreshPrefabs = false;
        }

        GUILayout.Label ("Selected GameObject:", EditorStyles.boldLabel);
        selGridInt = GUILayout.SelectionGrid(selGridInt, selectStrings, 3, GUILayout.Width(370));

        BigSpace();

        GUILayout.Label ("GameObject Rotation:", EditorStyles.boldLabel);
        rotateInt = GUILayout.SelectionGrid(rotateInt, rotateStrings, 4, GUILayout.Width(330));

        BigSpace();

        gizmoColor = EditorGUILayout.ColorField("Gizmo Color:", gizmoColor);

        ///////////////// SPAWN //////////////////

        spawnHeight = EditorGUILayout.IntSlider("Spawn at height:", spawnHeight, 0, 20);

        snapToGrid = EditorGUILayout.Toggle("Snap to grid:", snapToGrid);

        BigSpace();

        ///////////////// ROTATION //////////////////

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label ("Rotate Level:", EditorStyles.boldLabel);
        if (GUILayout.Button("90° CW", GUILayout.Width(80))) {
            RotateLevel(90);
        }
        if (GUILayout.Button("90° CCW", GUILayout.Width(80))) {
            RotateLevel(-90);
        }
        if (GUILayout.Button("180°", GUILayout.Width(80))) {
            RotateLevel(180);
        }
        EditorGUILayout.EndHorizontal();

        BigSpace();

        ///////////////// INVERSION //////////////////

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label ("Invert Level:", EditorStyles.boldLabel);
        if (GUILayout.Button("X axis", GUILayout.Width(80))) {
            InvertLevel("x");
        }
        if (GUILayout.Button("Y axis", GUILayout.Width(80))) {
            InvertLevel("y");
        }
        EditorGUILayout.EndHorizontal();

        BigSpace();
    }

    /// <summary>
    /// 绘制保存与加载区
    /// </summary>
    void SaveLoadWindow() {

        GUILayout.Label ("SAVING AND LOADING", EditorStyles.centeredGreyMiniLabel);
        HorizontalLine();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (string.IsNullOrWhiteSpace(newLevelName)) {
            if (GameObject.FindGameObjectWithTag("Level") == null) {
                GUILayout.Label ("To create a new level, give it a name: ");
            } else {
                newLevelName = currentLevelParent.name;
            }
        }

        if (!string.IsNullOrWhiteSpace(newLevelName) && GUILayout.Button("Save Level As", GUILayout.Width(150))) {

            currentLevelParent.transform.name = currentLevel = newLevelName;
            newLevelName = RemoveInvalidChars(newLevelName);
        	string path = "Assets/Resources/Levels/" + newLevelName + ".txt";

			if (File.Exists(path)) {
				if (EditorUtility.DisplayDialog("Overwrite Level?", "Are you sure you want to overwrite '" + newLevelName + "'?", "Yes", "No")) {
					SaveToDisk(newLevelName);
				}
			} else {
				SaveToDisk(newLevelName);
			}
        }
		newLevelName = EditorGUILayout.TextField(newLevelName);
        EditorGUILayout.EndHorizontal();

		BigSpace();


		if (savedLevels.Count > 0) {

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label ("Overwrite level(s) in scene ");
				overwriteLevel = EditorGUILayout.Toggle(overwriteLevel);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Load Level", GUILayout.Width(150))) {
					if (!isDirty || !overwriteLevel || EditorUtility.DisplayDialog("Load " + savedLevels[savedLevelIndex] + "?", "Load " + savedLevels[savedLevelIndex] + "? Any unsaved changes to " + currentLevel + " will be lost.", "Confirm", "Cancel")) {
						if (overwriteLevel) {

							Transform level = FindObjectOfType<LevelManager>().transform;
							for (int i = level.childCount - 1; i >= 0; i--) {
								Undo.DestroyObjectImmediate(level.GetChild(i).gameObject);
							}
						}
						currentLevel = savedLevels[savedLevelIndex];
						LoadFromDisk(currentLevel);
						Refresh();
					}
				}
				savedLevelIndex = EditorGUILayout.Popup(savedLevelIndex, savedLevels.ToArray());
				EditorGUILayout.EndHorizontal();

				BigSpace();

				ScriptableObject scriptableObj = this;
				SerializedObject serialObj = new SerializedObject (scriptableObj);
				SerializedProperty serialProp = serialObj.FindProperty ("prefabs");
				EditorGUILayout.PropertyField (serialProp, true);
				serialObj.ApplyModifiedProperties ();

				BigSpace();
			}
		}

    /// <summary>
    /// 增加空白间隔
    /// </summary>
    void BigSpace() {
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
	}

    /// <summary>
    /// 移除非法文件名字符
    /// </summary>
    public string RemoveInvalidChars(string filename) {
        return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
    }

    #endregion

    #region 编辑器事件与辅助

    /// <summary>
    /// 编辑器Update事件
    /// </summary>
    void Update() {
        if (!EditorApplication.isPlaying && Selection.transforms.Length > 0 && Selection.transforms[0].position != prevPosition) {
			foreach (Transform t in Selection.transforms) {
				if (t.CompareTag("Level")) {
					currentLevel = t.name;
				}
				if (snapToGrid) {
					if (t.CompareTag("Level") || (t.parent != null && t.parent.CompareTag("Level"))) {
						Utils.RoundPosition(t);
						prevPosition = t.position;
					}
				}
			}
		}
    }

    /// <summary>
    /// 播放模式下的关卡操作
    /// </summary>
    void GetPlayModeJobs() {
		LevelPlayModePersistence.Job[] jobs = LevelPlayModePersistence.GetJobs();
		foreach (LevelPlayModePersistence.Job job in jobs) {
			if (job.name == "clear") {
				ClearObjectsAtPosition(Utils.Vec3ToInt(job.position));
			} else {
				PlayModeCreateObject(job.name, job.position, job.eulerAngles);
			}
		}
	}

    /// <summary>
    /// 播放模式下创建对象
    /// </summary>
    void PlayModeCreateObject(string objName, Vector3 position, Vector3 eulerAngles) {
		for (int i = 0; i < prefabs.Length; i++) {
			if (prefabs[i].transform.name == objName) {
				selGridInt = i + 2;
			}
		}
		CreateObject(position);
		newGameObject.transform.eulerAngles = eulerAngles;
	}

    #endregion
}
