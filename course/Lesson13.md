# 第13课:关卡序列化系统

> **难度:** ⭐⭐ | **预计时间:** 45分钟 | **前置要求:** [第12课](./Lesson12.md)

---

## 学习目标

- 理解JSON序列化机制
- 掌握关卡数据结构
- 学会加载和保存关卡

---

## 核心文件

- `Data/Level/LevelSerialization.cs`
- `Data/Level/LevelLoader.cs`
- `Data/Level/LevelManager.cs`

---

## 核心内容

### 1. 序列化数据结构

```csharp
// ========== 单个关卡物体 ==========
// 位于 Data/Level/LevelSerialization.cs
[System.Serializable]
public class SerializedLevelObject
{
    public string prefabName;      // 预制体名称
    public float x, y, z;          // 位置坐标
    public float rotX, rotY, rotZ; // 旋转角度

    // 从GameObject创建
    public SerializedLevelObject(GameObject obj)
    {
        prefabName = obj.name.Replace("(Clone)", "");
        x = obj.transform.position.x;
        y = obj.transform.position.y;
        z = obj.transform.position.z;
        rotX = obj.transform.eulerAngles.x;
        rotY = obj.transform.eulerAngles.y;
        rotZ = obj.transform.eulerAngles.z;
    }

    // 获取位置
    public Vector3 GetPosition()
    {
        return new Vector3(x, y, z);
    }

    // 获取旋转
    public Quaternion GetRotation()
    {
        return Quaternion.Euler(rotX, rotY, rotZ);
    }
}

// ========== 完整关卡 ==========
[System.Serializable]
public class SerializedLevel
{
    public string levelName;       // 关卡名称
    public int levelIndex;         // 关卡索引
    public List<SerializedLevelObject> objects; // 所有物体

    public SerializedLevel()
    {
        objects = new List<SerializedLevelObject>();
    }

    // 添加物体
    public void AddObject(GameObject obj)
    {
        objects.Add(new SerializedLevelObject(obj));
    }
}
```

**数据结构关系:**

```
SerializedLevel
├── levelName: "Level 1"
├── levelIndex: 1
└── objects: List<SerializedLevelObject>
        ├── [0] { prefabName: "Player", x:1, y:1, z:0, ... }
        ├── [1] { prefabName: "Box", x:3, y:1, z:0, ... }
        └── [2] { prefabName: "Wall", x:5, y:1, z:0, ... }
```

### 2. JSON序列化

```csharp
public static class LevelSerialization
{
    // ========== 序列化关卡到JSON ==========
    public static string ToJson(SerializedLevel level)
    {
        // prettyPrint=true 格式化输出
        return JsonUtility.ToJson(level, true);
    }

    // ========== 从JSON反序列化 ==========
    public static SerializedLevel FromJson(string json)
    {
        return JsonUtility.FromJson<SerializedLevel>(json);
    }

    // ========== 保存到文件 ==========
    public static void SaveToFile(SerializedLevel level, string path)
    {
        string json = ToJson(level);
        File.WriteAllText(path, json);
        Debug.Log($"关卡已保存到: {path}");
    }

    // ========== 从文件加载 ==========
    public static SerializedLevel LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"文件不存在: {path}");
            return null;
        }

        string json = File.ReadAllText(path);
        return FromJson(json);
    }
}
```

### 3. JSON格式示例

```json
{
    "levelName": "Level 1",
    "levelIndex": 1,
    "objects": [
        {
            "prefabName": "Player",
            "x": 1.0,
            "y": 1.0,
            "z": 0.0,
            "rotX": 0.0,
            "rotY": 0.0,
            "rotZ": 0.0
        },
        {
            "prefabName": "Box",
            "x": 3.0,
            "y": 1.0,
            "z": 0.0,
            "rotX": 0.0,
            "rotY": 0.0,
            "rotZ": 0.0
        },
        {
            "prefabName": "Wall",
            "x": 5.0,
            "y": 1.0,
            "z": 0.0,
            "rotX": 0.0,
            "rotY": 90.0,
            "rotZ": 0.0
        }
    ]
}
```

### 4. LevelLoader

```csharp
// 位于 Data/Level/LevelLoader.cs
public static class LevelLoader
{
    private const string LEVEL_PATH = "Levels/";

    // ========== 从Resources加载关卡 ==========
    public static SerializedLevel LoadLevel(string levelName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(LEVEL_PATH + levelName);

        if (jsonFile == null)
        {
            Debug.LogError($"关卡文件未找到: {levelName}");
            return null;
        }

        return JsonUtility.FromJson<SerializedLevel>(jsonFile.text);
    }

    // ========== 加载预制体 ==========
    public static GameObject LoadPrefab(string prefabName)
    {
        // 从Resources加载预制体
        GameObject prefab = Resources.Load<GameObject>(prefabName);

        if (prefab == null)
        {
            Debug.LogError($"预制体未找到: {prefabName}");
        }

        return prefab;
    }

    // ========== 获取所有关卡名称 ==========
    public static string[] GetAllLevelNames()
    {
        TextAsset[] files = Resources.LoadAll<TextAsset>(LEVEL_PATH);
        string[] names = new string[files.Length];

        for (int i = 0; i < files.Length; i++)
        {
            names[i] = files[i].name;
        }

        return names;
    }
}
```

### 5. LevelManager

```csharp
// 位于 Data/Level/LevelManager.cs
public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    private SerializedLevel currentLevel;
    private List<GameObject> spawnedObjects = new List<GameObject>();

    void Awake()
    {
        instance = this;
    }

    // ========== 加载关卡 ==========
    public void LoadLevel(string levelName)
    {
        // 1. 清理当前关卡
        UnloadCurrentLevel();

        // 2. 加载关卡数据
        currentLevel = LevelLoader.LoadLevel(levelName);
        if (currentLevel == null) return;

        // 3. 实例化所有物体
        foreach (var obj in currentLevel.objects)
        {
            SpawnObject(obj);
        }

        // 4. 刷新游戏状态
        Game.instance.EditorRefresh();

        // 5. 触发事件
        EventManager.onLevelStarted?.Invoke();

        Debug.Log($"关卡加载完成: {levelName}");
    }

    // ========== 生成单个物体 ==========
    private void SpawnObject(SerializedLevelObject obj)
    {
        GameObject prefab = LevelLoader.LoadPrefab(obj.prefabName);
        if (prefab == null) return;

        // 实例化
        GameObject instance = Instantiate(
            prefab,
            obj.GetPosition(),
            obj.GetRotation()
        );

        // 记录以便清理
        spawnedObjects.Add(instance);
    }

    // ========== 卸载当前关卡 ==========
    public void UnloadCurrentLevel()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        currentLevel = null;
    }

    // ========== 加载下一关 ==========
    public void LoadNextLevel()
    {
        int nextIndex = (currentLevel?.levelIndex ?? 0) + 1;
        LoadLevel($"level{nextIndex}");
    }

    // ========== 重新加载当前关卡 ==========
    public void ReloadCurrentLevel()
    {
        if (currentLevel != null)
        {
            LoadLevel(currentLevel.levelName);
        }
    }
}
```

### 6. 资源路径

```
Assets/Resources/
├── Levels/
│   ├── level1.json
│   ├── level2.json
│   ├── level3.json
│   └── ...
├── Player.prefab
├── Box.prefab
└── Wall.prefab
```

**重要:** JSON文件必须放在`Resources`文件夹下才能使用`Resources.Load`加载。

---

## 实践任务

### 任务1:手动创建JSON关卡文件

1. 在`Assets/Resources/Levels/`创建`test.json`
2. 编写JSON内容:

```json
{
    "levelName": "Test Level",
    "levelIndex": 0,
    "objects": [
        {
            "prefabName": "Player",
            "x": 0.0, "y": 0.0, "z": 0.0,
            "rotX": 0.0, "rotY": 0.0, "rotZ": 0.0
        },
        {
            "prefabName": "Wall",
            "x": 2.0, "y": 0.0, "z": 0.0,
            "rotX": 0.0, "rotY": 0.0, "rotZ": 0.0
        }
    ]
}
```

3. 测试加载

### 任务2:实现关卡保存功能

```csharp
// 从场景导出到JSON
public void SaveCurrentSceneToLevel(string levelName)
{
    SerializedLevel level = new SerializedLevel();
    level.levelName = levelName;
    level.levelIndex = GetNextLevelIndex();

    // 收集所有物体
    Mover[] movers = FindObjectsOfType<Mover>();
    Wall[] walls = FindObjectsOfType<Wall>();

    foreach (var mover in movers)
    {
        level.AddObject(mover.gameObject);
    }
    foreach (var wall in walls)
    {
        level.AddObject(wall.gameObject);
    }

    // 保存
    string path = $"Assets/Resources/Levels/{levelName}.json";
    LevelSerialization.SaveToFile(level, path);

    // 刷新资源数据库
    UnityEditor.AssetDatabase.Refresh();
}
```

### 任务3:添加关卡元数据

```csharp
// 扩展SerializedLevel
[System.Serializable]
public class SerializedLevel
{
    public string levelName;
    public int levelIndex;

    // 新增元数据
    public string author;        // 作者
    public int difficulty;       // 难度(1-5)
    public int parMoves;         // 标准移动次数
    public string description;   // 描述
    public List<string> tags;    // 标签

    public List<SerializedLevelObject> objects;
}
```

---

## 扩展方向

### 压缩存储

```csharp
using System.IO.Compression;

public static byte[] CompressLevel(SerializedLevel level)
{
    string json = JsonUtility.ToJson(level);
    byte[] bytes = Encoding.UTF8.GetBytes(json);

    using (var output = new MemoryStream())
    {
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }
}
```

### 版本控制

```csharp
[System.Serializable]
public class SerializedLevel
{
    public int version = 1;  // 版本号

    // 版本迁移
    public void Migrate()
    {
        if (version < 2)
        {
            // 从版本1迁移到版本2
            MigrateV1ToV2();
        }
    }
}
```

### 热更新

```csharp
public IEnumerator DownloadLevel(string url)
{
    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            SerializedLevel level = JsonUtility.FromJson<SerializedLevel>(json);
            LoadLevel(level);
        }
    }
}
```

---

## 常见问题

**Q: 为什么使用Unity的JsonUtility而不是Newtonsoft.Json?**
A: JsonUtility是Unity内置的,无需额外依赖,适合简单数据结构。

**Q: 如何处理预制体重命名?**
A: 使用GUID而非名称,或维护一个名称映射表。

**Q: 关卡文件可以放在StreamingAssets吗?**
A: 可以,但需要使用不同的加载方式(File.ReadAllBytes)。

---

## 关键要点

- ✅ SerializedLevel存储关卡完整数据
- ✅ SerializedLevelObject存储单个物体
- ✅ 使用Unity的JsonUtility序列化
- ✅ 关卡文件放在Resources文件夹
- ✅ LevelManager管理关卡加载/卸载

---

## 下一课

[第14课:关卡编辑器](./Lesson14.md)
