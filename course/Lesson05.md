# 第5课:可移动对象基类 - Mover (上)

> **难度:** ⭐⭐ | **预计时间:** 60分钟 | **前置要求:** [第4课](./Lesson04.md)

---

## 学习目标

- 理解Mover类的基础结构
- 掌握Tile收集与方向位置计算
- 了解移动计划系统

---

## 核心文件

- `Logic/Entity/Mover.cs`

---

## 核心内容

### 1. 核心变量

```csharp
public class Mover : MonoBehaviour
{
    // ========== Tile系统 ==========
    public List<Tile> tiles = new List<Tile>();  // 占据的格子列表

    // ========== 状态标记 ==========
    [HideInInspector] public bool isFalling = false;   // 是否正在下落
    [HideInInspector] public bool isNotRound = false;  // 是否非整数位置

    // ========== 身份识别 ==========
    public bool isPlayer { get { return CompareTag("Player"); } }

    // ========== 移动计划(线程安全) ==========
    private object setValue = new object();
    private Vector3 plannedMove;
    public Vector3 PlannedMove
    {
        get => plannedMove;
        set
        {
            lock (setValue)  // 线程锁保护
            {
                plannedMove = value + plannedMove;
            }
        }
    }

    // ========== 六方向相邻格子缓存 ==========
    public List<Vector3Int> rightDir = new List<Vector3Int>();
    public List<Vector3Int> leftDir = new List<Vector3Int>();
    public List<Vector3Int> upDir = new List<Vector3Int>();
    public List<Vector3Int> downDir = new List<Vector3Int>();
    public List<Vector3Int> forwardDir = new List<Vector3Int>();
    public List<Vector3Int> backDir = new List<Vector3Int>();
}
```

**变量说明:**

| 变量 | 类型 | 用途 |
|------|------|------|
| tiles | List<Tile> | 物体占用的所有格子 |
| isFalling | bool | 标记是否在下落状态 |
| plannedMove | Vector3 | 计划的移动向量 |
| *Dir | List<Vector3Int> | 各方向的相邻格子列表 |

### 2. Tile收集机制

```csharp
void Start()
{
    CreateTiles();
}

void CreateTiles()
{
    tiles.Clear();

    // 遍历所有子物体
    foreach (Transform child in transform)
    {
        // 只收集带"Tile"标签的子物体
        if (child.gameObject.CompareTag("Tile"))
        {
            Tile tile = new Tile();
            tile.t = child;
            tiles.Add(tile);
        }
    }
}
```

**工作原理:**

```
Mover GameObject
├── Cube1 (Tile标签) → Tile { pos: (0,0,0), t: Cube1 }
├── Cube2 (Tile标签) → Tile { pos: (1,0,0), t: Cube2 }
└── OtherChild (无Tile标签) → 被忽略

tiles = [Tile1, Tile2]
```

**支持多格物体:**
- 1x1方块: 1个Tile
- 2x1方块: 2个Tile
- 不规则形状: 任意数量的Tile

### 3. 方向位置计算

`UpdateDirPos()`方法计算物体在六个方向的相邻格子:

```csharp
public void UpdateDirPos(Vector3 Position)
{
    // ========== 处理非整数坐标 ==========
    // 物体可能跨越两个格子(如位置0.5)

    List<int> xPositions = new List<int>();
    List<int> yPositions = new List<int>();
    List<int> zPositions = new List<int>();

    // X轴: 0.5 → 占据0和1两个格子
    if (Position.x % 1 != 0)
    {
        xPositions.Add(Mathf.FloorToInt(Position.x));  // 0
        xPositions.Add(Mathf.CeilToInt(Position.x));   // 1
    }
    else
    {
        xPositions.Add((int)Position.x);
    }

    // Y轴和Z轴类似处理...
    // ...

    // ========== 计算各方向的相邻格子 ==========
    rightDir.Clear();
    leftDir.Clear();
    upDir.Clear();
    downDir.Clear();
    forwardDir.Clear();
    backDir.Clear();

    foreach (int x in xPositions)
    {
        foreach (int y in yPositions)
        {
            foreach (int z in zPositions)
            {
                // 右方向: X+1
                rightDir.Add(new Vector3Int(x + 1, y, z));
                // 左方向: X-1
                leftDir.Add(new Vector3Int(x - 1, y, z));
                // 上方向: Y+1
                upDir.Add(new Vector3Int(x, y + 1, z));
                // ... 其他方向
            }
        }
    }
}
```

**为什么需要处理非整数坐标?**

```
位置0.0: 占据格子0
位置0.5: 占据格子0和1(跨越)
位置1.0: 占据格子1

这在动画过程中很重要!
```

### 4. 获取方向格子

```csharp
public List<Vector3Int> GetDirV3(Direction dir)
{
    switch (dir)
    {
        case Direction.Up: return upDir;
        case Direction.Down: return downDir;
        case Direction.Left: return leftDir;
        case Direction.Right: return rightDir;
        case Direction.Forward: return forwardDir;
        case Direction.Back: return backDir;
        default: return new List<Vector3Int>();
    }
}
```

**使用示例:**
```csharp
// 获取物体右侧的所有相邻格子
List<Vector3Int> rightPositions = mover.GetDirV3(Direction.Right);

// 检查这些位置是否有障碍物
foreach (var pos in rightPositions)
{
    if (Utils.WallIsAtPos(pos))
    {
        Debug.Log($"右侧位置{pos}有墙");
    }
}
```

---

## 可视化调试

Mover类已包含Gizmos绘制:

```csharp
void OnDrawGizmosSelected()
{
    // 编辑器模式下也收集Tile
    if (!Application.isPlaying)
    {
        CreateTiles();
    }

    // 绘制每个Tile的边界
    Gizmos.color = Color.blue;
    foreach (Tile tile in tiles)
    {
        Gizmos.DrawWireCube(tile.pos, Vector3.one);
    }
}
```

**在Scene视图中查看:**
1. 选择一个Mover对象
2. 观察蓝色线框显示的Tile边界

---

## 实践任务

### 任务1:创建一个2x2的方块Mover

1. 创建空GameObject,命名为"BigBox"
2. 添加Mover组件
3. 创建4个Cube作为子物体,位置:
   - Cube1: (0,0,0)
   - Cube2: (1,0,0)
   - Cube3: (0,1,0)
   - Cube4: (1,1,0)
4. 所有Cube添加"Tile"标签

### 任务2:可视化六个方向的相邻格子

修改OnDrawGizmosSelected:

```csharp
void OnDrawGizmosSelected()
{
    if (!Application.isPlaying)
    {
        CreateTiles();
    }

    // 绘制Tile(蓝色)
    Gizmos.color = Color.blue;
    foreach (Tile tile in tiles)
    {
        Gizmos.DrawWireCube(tile.pos, Vector3.one);
    }

    // 绘制方向格子(不同颜色)
    if (tiles.Count > 0)
    {
        UpdateDirPos(tiles[0].t.position);

        Gizmos.color = Color.red;  // 右
        foreach (var pos in rightDir)
            Gizmos.DrawWireCube(pos, Vector3.one * 0.8f);

        Gizmos.color = Color.green;  // 上
        foreach (var pos in upDir)
            Gizmos.DrawWireCube(pos, Vector3.one * 0.8f);
    }
}
```

### 任务3:分析方向计算差异

在控制台输出方向信息:

```csharp
void Start()
{
    CreateTiles();

    if (tiles.Count > 0)
    {
        UpdateDirPos(tiles[0].t.position);

        Debug.Log($"右侧相邻格子数: {rightDir.Count}");
        Debug.Log($"上侧相邻格子数: {upDir.Count}");

        // 多格物体的方向格子数会更多
    }
}
```

---

## 常见问题

**Q: 为什么PlannedMove使用线程锁?**
A: 防止多线程同时修改导致的数据竞争。

**Q: 什么时候调用UpdateDirPos?**
A: 在碰撞检测和移动验证时调用,确保使用最新位置。

**Q: 多格物体的方向格子为什么更多?**
A: 每个Tile都会产生相邻格子,2x2方块每个方向有2个相邻格子。

---

## 关键要点

- ✅ Mover通过Tile列表管理占用的格子
- ✅ CreateTiles()自动收集带"Tile"标签的子物体
- ✅ UpdateDirPos()计算六方向的相邻格子
- ✅ 支持非整数位置的物体(动画过渡中)

---

## 下一课

[第6课:可移动对象基类 - Mover (下)](./Lesson06.md)
