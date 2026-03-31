# 第14课:关卡编辑器

> **难度:** ⭐⭐⭐ | **预计时间:** 60分钟 | **前置要求:** [第13课](./Lesson13.md)

---

## 学习目标

- 掌握Unity Editor扩展基础
- 理解自定义工具开发
- 学会使用Level Editor创建关卡

---

## 核心文件

- `Presentation/Editor/LevelEditor.cs`

---

## 核心内容

### 1. EditorWindow基础

```csharp
// 位于 Presentation/Editor/LevelEditor.cs
using UnityEditor;
using UnityEngine;

public class LevelEditor : EditorWindow
{
    // ========== 打开编辑器窗口 ==========
    [MenuItem("Window/Level Editor")]
    public static void ShowWindow()
    {
        // 获取或创建窗口
        LevelEditor window = GetWindow<LevelEditor>("Level Editor");
        window.minSize = new Vector2(300, 400);
        window.Show();
    }

    // ========== 绘制窗口内容 ==========
    void OnGUI()
    {
        GUILayout.Label("关卡编辑器", EditorStyles.boldLabel);

        // 绘制UI控件...
    }
}
```

**EditorWindow生命周期:**

```
ShowWindow() → OnEnable() → OnGUI() (循环) → OnDisable() → OnDestroy()
```

### 2. 编辑器状态

```csharp
public class LevelEditor : EditorWindow
{
    // ========== 编辑器状态 ==========
    private int selectedPrefabIndex = 0;   // 当前选中的预制体索引
    private string[] prefabNames;          // 预制体名称数组
    private GameObject[] prefabs;          // 预制体数组

    // ========== 绘制设置 ==========
    private int rotation = 0;              // 旋转角度 (0/90/180/270)
    private float spawnHeight = 0;         // 生成高度
    private bool eraseMode = false;        // 擦除模式

    // ========== 网格设置 ==========
    private float gridSize = 1f;           // 网格大小
    private Vector3 gridOffset = Vector3.zero;

    // ========== 场景引用 ==========
    private GameObject gridPlane;          // 网格平面
}
```

### 3. 预制体管理

```csharp
// 从文件加载预制体列表
void LoadPrefabs()
{
    string path = "Assets/leveleditorprefabs.txt";

    if (File.Exists(path))
    {
        string[] lines = File.ReadAllLines(path);
        prefabNames = lines;
        prefabs = new GameObject[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            // 从Resources加载预制体
            prefabs[i] = Resources.Load<GameObject>(lines[i]);
        }

        Debug.Log($"加载了 {prefabNames.Length} 个预制体");
    }
    else
    {
        Debug.LogError($"预制体列表文件不存在: {path}");
    }
}

// 绘制预制体选择UI
void OnGUI()
{
    GUILayout.Label("选择预制体", EditorStyles.boldLabel);

    // 使用SelectionGrid显示预制体选项
    selectedPrefabIndex = GUILayout.SelectionGrid(
        selectedPrefabIndex,
        prefabNames,
        3,  // 每行3个
        GUILayout.Height(100)
    );
}
```

**leveleditorprefabs.txt格式:**
```
Player
Box
Wall
Target
```

### 4. 场景绘制

```csharp
// 注册场景视图回调
void OnEnable()
{
    SceneView.duringSceneGui += OnSceneGUI;
    LoadPrefabs();
}

void OnDisable()
{
    SceneView.duringSceneGui -= OnSceneGUI;
}

// 在Scene视图中处理输入
void OnSceneGUI(SceneView sceneView)
{
    Event e = Event.current;

    // 只处理左键点击
    if (e.type == EventType.MouseDown && e.button == 0)
    {
        // 射线检测鼠标位置
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 position = hit.point;

            // 对齐到网格
            position = SnapToGrid(position);

            if (eraseMode)
            {
                EraseAtPosition(position);
            }
            else
            {
                SpawnPrefab(position);
            }

            // 使用事件
            e.Use();
        }
    }
}

// 对齐到网格
Vector3 SnapToGrid(Vector3 position)
{
    position.x = Mathf.Round(position.x / gridSize) * gridSize;
    position.y = spawnHeight;  // 固定Y高度
    position.z = Mathf.Round(position.z / gridSize) * gridSize;

    return position;
}
```

### 5. 生成与擦除

```csharp
void SpawnPrefab(Vector3 position)
{
    GameObject prefab = prefabs[selectedPrefabIndex];
    if (prefab == null) return;

    // 检查是否已有物体
    if (ObjectExistsAt(position)) return;

    // 实例化预制体
    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    instance.transform.position = position;
    instance.transform.rotation = Quaternion.Euler(0, rotation, 0);

    // 注册撤销
    Undo.RegisterCreatedObjectUndo(instance, "Spawn " + prefab.name);

    // 标记场景为脏(需要保存)
    EditorUtility.SetDirty(instance);

    Debug.Log($"生成: {prefab.name} @ {position}");
}

void EraseAtPosition(Vector3 position)
{
    // 使用OverlapSphere检测位置附近的物体
    Collider[] colliders = Physics.OverlapSphere(position, 0.4f);

    foreach (var col in colliders)
    {
        GameObject obj = col.transform.root.gameObject;

        // 只删除Mover或Wall
        if (obj.GetComponent<Mover>() || obj.GetComponent<Wall>())
        {
            Undo.DestroyObjectImmediate(obj);
            Debug.Log($"擦除: {obj.name}");
        }
    }
}

bool ObjectExistsAt(Vector3 position)
{
    Collider[] colliders = Physics.OverlapSphere(position, 0.4f);
    return colliders.Length > 0;
}
```

### 6. 保存关卡

```csharp
void SaveLevel()
{
    // 打开保存对话框
    string path = EditorUtility.SaveFilePanel(
        "保存关卡",
        "Assets/Resources/Levels",
        "NewLevel",
        "json"
    );

    if (string.IsNullOrEmpty(path)) return;

    // 创建关卡数据
    SerializedLevel level = new SerializedLevel();
    level.levelName = Path.GetFileNameWithoutExtension(path);

    // 收集所有物体
    Mover[] movers = FindObjectsOfType<Mover>();
    Wall[] walls = FindObjectsOfType<Wall>();

    foreach (var mover in movers)
    {
        level.objects.Add(new SerializedLevelObject(mover.gameObject));
    }
    foreach (var wall in walls)
    {
        level.objects.Add(new SerializedLevelObject(wall.gameObject));
    }

    // 保存到文件
    LevelSerialization.SaveToFile(level, path);

    // 刷新资源数据库
    AssetDatabase.Refresh();

    Debug.Log($"关卡已保存: {path}");
}
```

### 7. 工具栏UI

```csharp
void OnGUI()
{
    // ========== 预制体选择 ==========
    GUILayout.Label("预制体", EditorStyles.boldLabel);
    selectedPrefabIndex = GUILayout.SelectionGrid(
        selectedPrefabIndex,
        prefabNames,
        3
    );

    GUILayout.Space(10);

    // ========== 旋转设置 ==========
    GUILayout.Label("旋转", EditorStyles.boldLabel);
    string[] rotations = { "0°", "90°", "180°", "270°" };
    rotation = GUILayout.SelectionGrid(rotation, rotations, 4);

    GUILayout.Space(10);

    // ========== 高度设置 ==========
    GUILayout.Label("高度", EditorStyles.boldLabel);
    spawnHeight = EditorGUILayout.FloatField("Spawn Height", spawnHeight);

    GUILayout.Space(10);

    // ========== 模式切换 ==========
    Color originalColor = GUI.backgroundColor;
    GUI.backgroundColor = eraseMode ? Color.red : Color.white;
    eraseMode = GUILayout.Toggle(eraseMode, "擦除模式", "Button");
    GUI.backgroundColor = originalColor;

    GUILayout.Space(10);

    // ========== 操作按钮 ==========
    if (GUILayout.Button("保存关卡", GUILayout.Height(30)))
        SaveLevel();

    if (GUILayout.Button("清空场景", GUILayout.Height(30)))
        ClearScene();

    if (GUILayout.Button("刷新网格", GUILayout.Height(30)))
        Game.instance?.EditorRefresh();

    if (GUILayout.Button("加载关卡", GUILayout.Height(30)))
        LoadLevelDialog();
}
```

---

## 使用流程

1. **打开编辑器**: `Window -> Level Editor`
2. **选择预制体**: 在左侧面板点击选择
3. **设置参数**: 旋转、高度等
4. **绘制关卡**: 在Scene视图中点击绘制
5. **擦除物体**: 开启擦除模式后点击删除
6. **保存关卡**: 点击"保存关卡"按钮

---

## 实践任务

### 任务1:创建一个包含10个关卡的关卡包

1. 使用Level Editor创建10个关卡
2. 难度逐渐递增
3. 保存为level1.json ~ level10.json

### 任务2:添加撤销/重做功能

```csharp
private Stack<Action> undoStack = new Stack<Action>();
private Stack<Action> redoStack = new Stack<Action>();

void SpawnPrefab(Vector3 position)
{
    // ...生成逻辑...

    // 记录撤销操作
    undoStack.Push(() => Undo.DestroyObjectImmediate(instance));
    redoStack.Clear();
}

void Undo()
{
    if (undoStack.Count > 0)
    {
        Action action = undoStack.Pop();
        action();
        redoStack.Push(action);
    }
}
```

### 任务3:实现关卡预览功能

```csharp
private SerializedLevel previewLevel;

void OnGUI()
{
    // 关卡预览
    if (previewLevel != null)
    {
        GUILayout.Label($"预览: {previewLevel.levelName}");
        GUILayout.Label($"物体数量: {previewLevel.objects.Count}");

        // 预览窗口...
    }
}
```

---

## 扩展方向

### 多选操作

```csharp
private List<GameObject> selectedObjects = new List<GameObject>();

void MoveSelected(Vector3 delta)
{
    foreach (var obj in selectedObjects)
    {
        obj.transform.position += delta;
    }
}
```

### 复制粘贴

```csharp
private List<SerializedLevelObject> clipboard = new List<SerializedLevelObject>();

void Copy()
{
    clipboard.Clear();
    foreach (var obj in Selection.gameObjects)
    {
        clipboard.Add(new SerializedLevelObject(obj));
    }
}

void Paste(Vector3 position)
{
    foreach (var obj in clipboard)
    {
        SpawnObject(obj, position);
    }
}
```

### 图层系统

```csharp
private int currentLayer = 0;
private string[] layerNames = { "Default", "Background", "Foreground" };

void OnGUI()
{
    currentLayer = GUILayout.Popup(currentLayer, layerNames);
}
```

---

## 常见问题

**Q: 编辑器脚本放在哪里?**
A: 放在`Editor`文件夹下,或使用`#if UNITY_EDITOR`条件编译。

**Q: 如何自定义Gizmo?**
A: 使用`OnDrawGizmos`和`OnDrawGizmosSelected`方法。

**Q: 如何支持撤销?**
A: 使用`Undo`类的静态方法记录操作。

---

## 关键要点

- ✅ 继承EditorWindow创建自定义窗口
- ✅ 使用OnGUI绘制编辑器界面
- ✅ 使用SceneView.duringSceneGui处理场景输入
- ✅ 使用Undo支持撤销操作
- ✅ PrefabUtility正确实例化预制体

---

## 下一课

[第15课:实战案例与扩展](./Lesson15.md)
