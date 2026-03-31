# 第9课:空间网格 - LogicalGrid

> **难度:** ⭐⭐ | **预计时间:** 45分钟 | **前置要求:** [第8课](./Lesson08.md)

---

## 学习目标

- 理解空间哈希网格的实现
- 掌握碰撞检测优化原理
- 了解快速位置查询机制

---

## 核心文件

- `Logic/Grid/LogicalGrid.cs`

---

## 核心内容

### 1. 空间哈希原理

使用字典将3D坐标映射到游戏对象列表:

```csharp
// 位于 Logic/Grid/LogicalGrid.cs
public class LogicalGrid
{
    // 核心数据结构: 坐标 -> 物体列表
    private Dictionary<Vector3Int, List<GameObject>> grid =
        new Dictionary<Vector3Int, List<GameObject>>();
}
```

**为什么使用空间哈希?**

| 数据结构 | 查询时间 | 空间复杂度 | 适用场景 |
|----------|----------|------------|----------|
| 遍历所有物体 | O(n) | O(1) | 小规模场景 |
| 3D数组 | O(1) | O(x*y*z) | 固定大小网格 |
| 空间哈希 | O(1) | O(k) | 稀疏网格(推荐) |

其中:
- n = 物体数量
- x*y*z = 网格总大小
- k = 实际占用格子数

**本项目特点:**
- 关卡通常稀疏(大部分格子为空)
- 需要频繁的位置查询
- 空间哈希是最优选择

### 2. 同步网格内容

```csharp
public void SyncContents(GameObject[] tiles)
{
    // 清空旧数据
    grid.Clear();

    // 遍历所有Tile
    foreach (var tile in tiles)
    {
        // 计算网格坐标
        Vector3Int pos = new Vector3Int(
            Mathf.RoundToInt(tile.transform.position.x),
            Mathf.RoundToInt(tile.transform.position.y),
            Mathf.RoundToInt(tile.transform.position.z)
        );

        // 添加到字典
        if (!grid.ContainsKey(pos))
        {
            grid[pos] = new List<GameObject>();
        }
        grid[pos].Add(tile);
    }
}
```

**调用时机:**
- 游戏初始化时
- 每次移动完成后
- 物体位置发生变化时

**多物体同一格子:**
```csharp
// 空间哈希支持多物体占据同一格子
grid[(0, 0, 0)] = [Wall_Tile1, Box_Tile1]
grid[(1, 0, 0)] = [Box_Tile2]
```

### 3. 位置查询方法

```csharp
// 获取指定位置的所有物体
public List<GameObject> GetObjectsAt(Vector3Int pos)
{
    if (grid.ContainsKey(pos))
        return grid[pos];
    return new List<GameObject>();  // 空列表
}

// 检查位置是否为空
public bool IsEmpty(Vector3Int pos)
{
    return !grid.ContainsKey(pos) || grid[pos].Count == 0;
}

// 检查位置是否有特定类型的物体
public bool HasObjectOfType<T>(Vector3Int pos) where T : Component
{
    if (!grid.ContainsKey(pos))
        return false;

    foreach (var obj in grid[pos])
    {
        if (obj.GetComponent<T>() != null)
            return true;
    }
    return false;
}

// 获取指定类型的所有物体
public List<T> GetObjectsOfType<T>(Vector3Int pos) where T : Component
{
    List<T> result = new List<T>();
    if (!grid.ContainsKey(pos))
        return result;

    foreach (var obj in grid[pos])
    {
        T component = obj.GetComponentInParent<T>();
        if (component != null && !result.Contains(component))
            result.Add(component);
    }
    return result;
}
```

### 4. 与工具类的集成

GridQuery类封装了对LogicalGrid的查询:

```csharp
// 位于 Logic/Utility/GridQuery.cs
public static class GridQuery
{
    // 获取指定位置的Mover
    public static List<Mover> GetMoverAtPos(Vector3Int pos)
    {
        var result = new List<Mover>();
        var objects = Game.instance.Grid.GetObjectsAt(pos);

        foreach (var obj in objects)
        {
            // 向上查找Mover组件
            Mover mover = obj.GetComponentInParent<Mover>();
            if (mover != null && !result.Contains(mover))
                result.Add(mover);
        }
        return result;
    }

    // 检查是否有墙体
    public static bool WallIsAtPos(Vector3Int pos)
    {
        var objects = Game.instance.Grid.GetObjectsAt(pos);
        foreach (var obj in objects)
        {
            if (obj.GetComponentInParent<Wall>() != null)
                return true;
        }
        return false;
    }

    // 检查格子是否为空(无墙无Mover)
    public static bool TileIsEmpty(Vector3Int pos)
    {
        return !WallIsAtPos(pos) && GetMoverAtPos(pos).Count == 0;
    }
}
```

---

## 性能对比

### 查询性能测试

```csharp
// 测试代码
void TestPerformance()
{
    int iterations = 10000;
    var stopwatch = new System.Diagnostics.Stopwatch();

    // ========== 方法1: 遍历所有物体 ==========
    stopwatch.Start();
    for (int i = 0; i < iterations; i++)
    {
        foreach (var mover in Game.movers)
        {
            if (mover.transform.position == targetPos)
                break;
        }
    }
    stopwatch.Stop();
    Debug.Log($"遍历查询: {stopwatch.ElapsedMilliseconds}ms");

    // ========== 方法2: 空间哈希 ==========
    stopwatch.Restart();
    for (int i = 0; i < iterations; i++)
    {
        Game.instance.Grid.GetObjectsAt(targetPos);
    }
    stopwatch.Stop();
    Debug.Log($"空间哈希: {stopwatch.ElapsedMilliseconds}ms");
}
```

**典型结果:**
```
遍历查询: 150ms
空间哈希: 2ms

性能提升: 75倍!
```

---

## 网格可视化

```csharp
// 在Scene视图中绘制网格
void OnDrawGizmos()
{
    if (grid == null) return;

    Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    foreach (var kvp in grid)
    {
        // 绘制线框立方体
        Gizmos.DrawWireCube(kvp.Key, Vector3.one);

        // 显示物体数量
        if (kvp.Value.Count > 1)
        {
            // 多物体格子用不同颜色标记
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(kvp.Key, Vector3.one * 0.3f);
        }
    }
}
```

---

## 实践任务

### 任务1:添加网格可视化

1. 在LogicalGrid类中添加OnDrawGizmos方法
2. 运行场景,观察Scene视图中的网格

### 任务2:实现范围查询

```csharp
// 添加到LogicalGrid类
public List<GameObject> GetObjectsInRadius(Vector3Int center, int radius)
{
    List<GameObject> result = new List<GameObject>();

    for (int x = -radius; x <= radius; x++)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                Vector3Int pos = center + new Vector3Int(x, y, z);
                if (grid.ContainsKey(pos))
                {
                    result.AddRange(grid[pos]);
                }
            }
        }
    }
    return result;
}
```

### 任务3:性能对比测试

使用上方的测试代码,比较不同查询方法的性能。

---

## 扩展思考

**Q: 如何处理高速移动的物体?**
A: 可能需要使用连续碰撞检测(CCD)或射线检测,而非离散的格子查询。

**Q: 如何支持动态网格大小?**
A: 空间哈希天然支持任意坐标,无需预分配,已经支持动态大小。

**Q: 如何优化大量物体的同步?**
A: 可以使用增量更新,只同步位置变化的物体。

---

## 常见问题

**Q: 为什么使用GetComponentInParent而不是GetComponent?**
A: Tile是子物体,Mover/Wall组件在父对象上。

**Q: SyncContents每帧调用会怎样?**
A: 性能问题!只在必要时调用(移动完成后)。

**Q: 如何调试网格内容?**
A: 使用OnDrawGizmos可视化,或添加调试日志输出。

---

## 关键要点

- ✅ 空间哈希使用Dictionary<Vector3Int, List<GameObject>>
- ✅ O(1)时间复杂度的位置查询
- ✅ 支持多物体占据同一格子
- ✅ SyncContents()同步场景状态到网格
- ✅ GridQuery类提供便捷的查询接口

---

## 下一课

[第10课:工具类系统](./Lesson10.md)
