# 第2课:核心数据结构 - Tile与Enum

> **难度:** ⭐ | **预计时间:** 30分钟 | **前置要求:** [第1课](./Lesson01.md)

---

## 学习目标

- 理解`Tile`结构体的设计
- 掌握方向枚举`Direction`的使用
- 了解网格坐标系统

---

## 核心文件

- `Logic/Entity/BaseClass/Tile.cs`
- `Logic/Entity/BaseClass/Enum.cs`

---

## 核心内容

### 1. Tile结构体

Tile是存储格子信息的基础数据结构:

```csharp
// 位于 Logic/Entity/BaseClass/Tile.cs
public struct Tile
{
    public Transform t;      // 子物体的Transform引用
    public Vector3Int pos;   // 网格坐标(整数)

    public Tile(Transform transform)
    {
        t = transform;
        // 将浮点坐标转换为整数网格坐标
        pos = new Vector3Int(
            Mathf.RoundToInt(t.position.x),
            Mathf.RoundToInt(t.position.y),
            Mathf.RoundToInt(t.position.z)
        );
    }
}
```

**设计要点:**

| 特性 | 说明 | 原因 |
|------|------|------|
| `struct` | 值类型 | 避免GC压力,提高性能 |
| `Vector3Int` | 整数坐标 | 确保网格对齐,避免浮点误差 |
| Transform引用 | 存储子物体 | 用于动画和渲染更新 |

**使用示例:**
```csharp
// 创建Tile
Tile tile = new Tile(childTransform);

// 访问网格位置
Vector3Int gridPos = tile.pos;  // 如 (2, 1, 0)

// 访问实际位置
Vector3 worldPos = tile.t.position;  // 如 (2.0, 1.0, 0.0)
```

### 2. Direction枚举

六方向移动系统:

```csharp
// 位于 Logic/Entity/BaseClass/Enum.cs
public enum Direction
{
    None,      // 0 - 无方向
    Up,        // 1 - Y+1
    Down,      // 2 - Y-1
    Left,      // 3 - X-1
    Right,     // 4 - X+1
    Forward,   // 5 - Z+1 (下落方向)
    Back       // 6 - Z-1
}
```

**坐标系说明:**

```
        Y (Up)
        ↑
        |
        +------→ X (Right)
       /
      ↙
    Z (Forward/下落方向)
```

| 方向 | 坐标变化 | 说明 |
|------|----------|------|
| Up | Y+1 | 向上移动 |
| Down | Y-1 | 向下移动 |
| Left | X-1 | 向左移动 |
| Right | X+1 | 向右移动 |
| Forward | Z+1 | 向前(下落方向) |
| Back | Z-1 | 向后 |

**重要:** Z轴是深度方向,Z+为下落方向,地面位于Z=0。

### 3. Tile收集机制

Mover类中的Tile收集:

```csharp
// 位于 Mover.cs
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

**关键点:**
- 自动收集所有带"Tile"标签的子物体
- 支持多格物体(如2x1的方块)
- 在Start()和OnDrawGizmosSelected()中调用

**多格物体示例:**
```
2x1方块结构:
Parent (Mover)
├── Cube1 (Tile标签) - 占据(0,0,0)
└── Cube2 (Tile标签) - 占据(1,0,0)

tiles列表将包含2个Tile
```

---

## 实践任务

### 任务1:创建一个2x1的多格物体

1. 创建空GameObject,命名为"Box2x1"
2. 添加Mover组件
3. 创建两个Cube作为子物体
4. 两个Cube都添加"Tile"标签
5. 位置分别设置为(0,0,0)和(1,0,0)

### 任务2:观察Tile列表

1. 在Mover类中添加调试代码:

```csharp
void Start()
{
    CreateTiles();
    Debug.Log($"收集到 {tiles.Count} 个Tile");
    foreach (var tile in tiles)
    {
        Debug.Log($"Tile位置: {tile.pos}");
    }
}
```

2. 运行场景,查看Console输出

### 任务3:可视化Tile

使用Gizmos绘制Tile边界:

```csharp
void OnDrawGizmosSelected()
{
    if (!Application.isPlaying)
    {
        CreateTiles();  // 编辑器模式下也收集
    }

    Gizmos.color = Color.blue;
    foreach (Tile tile in tiles)
    {
        // 绘制线框立方体
        Gizmos.DrawWireCube(tile.pos, Vector3.one);
    }
}
```

---

## 常见问题

**Q: 为什么使用`Vector3Int`而不是`Vector3`?**
A: 网格游戏需要精确的整数坐标,Vector3的浮点数可能导致碰撞检测误差。

**Q: 如何支持不规则形状的物体?**
A: 只需添加更多带"Tile"标签的子物体,Tile收集机制会自动处理任意形状。

**Q: Tile结构体为什么不存储更多信息?**
A: 保持简单,遵循单一职责原则。额外信息可以从Transform或其他组件获取。

---

## 关键要点

- ✅ Tile是值类型结构体,包含Transform和网格位置
- ✅ Direction枚举定义了6个移动方向
- ✅ Z轴是下落方向,地面在Z=0
- ✅ "Tile"标签用于标识物体占用的格子

---

## 下一课

[第3课:静态对象 - Wall类](./Lesson03.md)

---

## 扩展阅读

- [Unity Vector3Int文档](https://docs.unity3d.com/ScriptReference/Vector3Int.html)
- [C# struct vs class](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/choosing-between-class-and-struct)
