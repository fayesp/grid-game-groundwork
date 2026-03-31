# 第4课:游戏管理器 - Game单例

> **难度:** ⭐⭐ | **预计时间:** 45分钟 | **前置要求:** [第3课](./Lesson03.md)

---

## 学习目标

- 掌握Unity单例模式实现
- 理解游戏循环与状态管理
- 了解核心配置参数

---

## 核心文件

- `Logic/Entity/Game.cs`

---

## 核心内容

### 1. 单例模式实现

```csharp
// 位于 Logic/Entity/Game.cs
public class Game : MonoBehaviour
{
    // 私有静态引用
    private static Game instanceRef;

    // 公共静态访问器
    public static Game instance
    {
        get
        {
            if (instanceRef == null)
            {
                // 场景中查找Game对象
                instanceRef = FindObjectOfType<Game>();
            }
            return instanceRef;
        }
    }

    void Awake()
    {
        // 确保单例唯一性
        if (instanceRef == null || instanceRef == this)
        {
            instanceRef = this;
            // 初始化逻辑...
        }
        else
        {
            Debug.LogError("场景中存在多个Game实例!");
            Destroy(gameObject);
        }
    }
}
```

**单例优势:**

| 优势 | 说明 |
|------|------|
| 全局访问 | `Game.instance`随处可调用 |
| 唯一实例 | 确保场景中只有一个Game |
| 方便访问 | 其他脚本无需持有引用 |

**使用示例:**
```csharp
// 在任何脚本中访问Game
if (Game.instance.isMoving)
{
    Debug.Log("游戏正在移动中...");
}

// 访问配置参数
float moveDuration = Game.instance.moveTime;
```

### 2. 核心变量

```csharp
public class Game : MonoBehaviour
{
    // ========== 游戏对象引用 ==========
    public LogicalGrid Grid = new LogicalGrid();  // 逻辑网格
    public static List<Mover> movers;              // 所有可移动对象
    public static List<Wall> walls;                // 所有墙体对象

    // ========== 时间参数 ==========
    public float moveTime = 0.18f;                 // 移动1单位时间(秒)
    public float rotateTime = 0.18f;               // 翻滚90度时间(秒)
    public float fallTime = 0.1f;                  // 下落1单位时间(秒)
    public float moveBufferSpeedupFactor = 0.5f;   // 连续输入加速因子

    // ========== 动画效果 ==========
    public Ease moveEase = Ease.Linear;            // 移动缓动类型
    public Ease rotateEase = Ease.OutCubic;        // 翻滚缓动类型

    // ========== 游戏状态 ==========
    public static bool allowPushMulti = true;      // 是否可推多个箱子
    public bool blockInput = false;                // 是否阻止输入
    public bool isMoving { get { return movingCount > 0; } }

    private int movingCount = 0;                   // 正在移动的物体数量
}
```

**时间参数说明:**

| 参数 | 默认值 | 说明 | 调节建议 |
|------|--------|------|----------|
| moveTime | 0.18s | 移动一格的时间 | 小值=快速响应 |
| fallTime | 0.1s | 下落一格的时间 | 通常比移动快 |
| moveBufferSpeedupFactor | 0.5 | 输入加速倍率 | 大值=加速更明显 |

### 3. 生命周期方法

```csharp
void Awake()
{
    // 1. 初始化单例
    // 2. 设置帧率
    Application.targetFrameRate = 60;
    // 3. 加载存档
    SaveData.Load();
}

void Start()
{
    // 延迟初始化,等待所有对象就绪
    StartCoroutine(InitAfterFrame());
}

IEnumerator InitAfterFrame()
{
    blockInput = true;
    yield return WaitFor.EndOfFrame;  // 等待一帧

    SetReferences();       // 收集场景中的Mover和Wall
    State.Init();          // 初始化状态系统
    State.AddToUndoStack(); // 记录初始状态

    blockInput = false;
    EventManager.onLevelStarted?.Invoke();
}

void Update()
{
    // 处理撤销(Z键)
    if (Input.GetKeyDown(KeyCode.Z))
    {
        DoUndo();
    }
    // 处理重置(R键)
    else if (Input.GetKeyDown(KeyCode.R))
    {
        DoReset();
    }
}
```

### 4. 状态刷新机制

```csharp
// 刷新游戏状态
public void Refresh()
{
    movingCount = 0;
    Player.instance?.ClearInputBuffer();
    PlannedMoves.Clear();

    // 重置所有Mover
    foreach (var mover in movers)
    {
        mover.Stop();
    }

    // 同步网格
    SyncGrid();
}

// 同步逻辑网格
public void SyncGrid()
{
    var tiles = GameObject.FindGameObjectsWithTag("Tile");
    Grid.SyncContents(tiles);
}

// 编辑器刷新(供Level Editor调用)
public void EditorRefresh()
{
    SetReferences();
    SyncGrid();
}
```

---

## 实践任务

### 任务1:修改时间参数

1. 在Hierarchy中选择GameController
2. 在Inspector中找到Game组件
3. 尝试修改以下参数并测试:
   - `moveTime`: 0.1 → 0.3 (观察移动速度变化)
   - `fallTime`: 0.05 → 0.2 (观察下落速度变化)

### 任务2:切换推动模式

1. 在Game组件中找到`allowPushMulti`
2. 取消勾选(false)
3. 测试推动多个物体的行为变化

**allowPushMulti行为对比:**
```
true:  任何Mover都可以推动其他Mover
false: 只有Player可以推动(经典推箱子模式)
```

### 任务3:添加调试输出

```csharp
// 在Game.cs的Update方法中添加
void Update()
{
    // 按D键显示调试信息
    if (Input.GetKeyDown(KeyCode.D))
    {
        Debug.Log($"=== 游戏状态 ===");
        Debug.Log($"移动中: {isMoving}");
        Debug.Log($"Movers数量: {movers.Count}");
        Debug.Log($"Walls数量: {walls.Count}");
    }

    // 原有的撤销/重置逻辑...
}
```

---

## 调试技巧

### 在运行时检查状态

```csharp
// 在任何脚本中添加调试代码
void DebugGameState()
{
    Debug.Log($"IsMoving: {Game.instance.isMoving}");
    Debug.Log($"Movers count: {Game.movers.Count}");
    Debug.Log($"Block input: {Game.instance.blockInput}");
}
```

### 使用Inspector调试

1. 运行场景
2. 在Hierarchy中选择GameController
3. 观察Inspector中的实时变量值

---

## 常见问题

**Q: 为什么使用延迟初始化?**
A: 等待所有对象的Awake/Start执行完毕,确保引用正确。

**Q: 单例会被销毁吗?**
A: 本项目单例随场景销毁。如需跨场景保留,可添加`DontDestroyOnLoad`。

**Q: 如何在多个场景中使用Game?**
A: 每个场景需要一个GameController预制体实例。

---

## 关键要点

- ✅ Game是全局单例,通过`Game.instance`访问
- ✅ 时间参数控制游戏节奏
- ✅ `allowPushMulti`控制推动模式
- ✅ Refresh()用于重置游戏状态

---

## 下一课

[第5课:可移动对象基类 - Mover (上)](./Lesson05.md)
