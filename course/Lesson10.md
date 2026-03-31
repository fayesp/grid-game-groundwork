# 第10课:工具类系统

> **难度:** ⭐⭐ | **预计时间:** 40分钟 | **前置要求:** [第9课](./Lesson09.md)

---

## 学习目标

- 掌握工具类的组织方式
- 理解查询与方向计算的分离
- 学会使用和扩展工具类

---

## 核心文件

- `Logic/Utility/Utils.cs`
- `Logic/Utility/GridQuery.cs`
- `Logic/Utility/DirectionUtils.cs`

---

## 核心内容

### 1. 架构设计

```
Utils.cs (统一入口,向后兼容)
    │
    ├── GridQuery.cs (位置查询)
    │       ├── GetMoverAtPos()
    │       ├── WallIsAtPos()
    │       └── TileIsEmpty()
    │
    └── DirectionUtils.cs (方向计算)
            ├── CheckDirection()
            ├── IsRound()
            └── GetVector()
```

**设计原则:**

| 原则 | 说明 |
|------|------|
| 单一职责 | 每个类只负责一类功能 |
| 静态方法 | 无需实例化,直接调用 |
| 向后兼容 | Utils类保持旧代码可运行 |
| 性能优化 | 缓存常用对象 |

### 2. GridQuery类

专门处理位置相关查询:

```csharp
// 位于 Logic/Utility/GridQuery.cs
public static class GridQuery
{
    // ========== 获取指定位置的Mover ==========
    public static List<Mover> GetMoverAtPos(Vector3Int pos)
    {
        var result = new List<Mover>();
        var objects = Game.instance.Grid.GetObjectsAt(pos);

        foreach (var obj in objects)
        {
            Mover mover = obj.GetComponentInParent<Mover>();
            if (mover != null && !result.Contains(mover))
                result.Add(mover);
        }
        return result;
    }

    // ========== 检查是否有墙体 ==========
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

    // ========== 检查格子是否为空 ==========
    public static bool TileIsEmpty(Vector3Int pos)
    {
        return !WallIsAtPos(pos) && GetMoverAtPos(pos).Count == 0;
    }

    // ========== 获取指定位置的Tile ==========
    public static List<GameObject> GetTilesAtPos(Vector3Int pos)
    {
        return Game.instance.Grid.GetObjectsAt(pos);
    }

    // ========== 检查是否有指定组件 ==========
    public static bool HasComponentAtPos<T>(Vector3Int pos) where T : Component
    {
        var objects = Game.instance.Grid.GetObjectsAt(pos);
        foreach (var obj in objects)
        {
            if (obj.GetComponentInParent<T>() != null)
                return true;
        }
        return false;
    }
}
```

### 3. DirectionUtils类

专门处理方向相关计算:

```csharp
// 位于 Logic/Utility/DirectionUtils.cs
public static class DirectionUtils
{
    // ========== 方向向量常量 ==========
    public static readonly Vector3Int Up = new Vector3Int(0, 1, 0);
    public static readonly Vector3Int Down = new Vector3Int(0, -1, 0);
    public static readonly Vector3Int Left = new Vector3Int(-1, 0, 0);
    public static readonly Vector3Int Right = new Vector3Int(1, 0, 0);
    public static readonly Vector3Int Forward = new Vector3Int(0, 0, 1);
    public static readonly Vector3Int Back = new Vector3Int(0, 0, -1);

    // ========== 根据向量判断方向 ==========
    public static Direction CheckDirection(Vector3 from, Vector3 to)
    {
        Vector3 diff = to - from;

        // 取绝对值最大的轴
        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y) &&
            Mathf.Abs(diff.x) >= Mathf.Abs(diff.z))
        {
            return diff.x > 0 ? Direction.Right : Direction.Left;
        }
        else if (Mathf.Abs(diff.y) >= Mathf.Abs(diff.z))
        {
            return diff.y > 0 ? Direction.Up : Direction.Down;
        }
        else
        {
            return diff.z > 0 ? Direction.Forward : Direction.Back;
        }
    }

    // ========== 检查位置是否为整数(在网格上) ==========
    public static bool IsRound(Vector3 pos, Direction dir)
    {
        switch (dir)
        {
            case Direction.Left:
            case Direction.Right:
                return pos.x % 1 == 0;
            case Direction.Up:
            case Direction.Down:
                return pos.y % 1 == 0;
            case Direction.Forward:
            case Direction.Back:
                return pos.z % 1 == 0;
            default:
                return true;
        }
    }

    // ========== 获取方向的向量 ==========
    public static Vector3 GetVector(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Up;
            case Direction.Down: return Down;
            case Direction.Left: return Left;
            case Direction.Right: return Right;
            case Direction.Forward: return Forward;
            case Direction.Back: return Back;
            default: return Vector3.zero;
        }
    }

    // ========== 获取相反方向 ==========
    public static Direction GetOpposite(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Direction.Down;
            case Direction.Down: return Direction.Up;
            case Direction.Left: return Direction.Right;
            case Direction.Right: return Direction.Left;
            case Direction.Forward: return Direction.Back;
            case Direction.Back: return Direction.Forward;
            default: return Direction.None;
        }
    }
}
```

### 4. Utils类(统一入口)

提供向后兼容的统一接口:

```csharp
// 位于 Logic/Utility/Utils.cs
public static class Utils
{
    // ========== 委托给GridQuery ==========
    public static List<Mover> GetMoverAtPos(Vector3Int pos)
        => GridQuery.GetMoverAtPos(pos);

    public static bool WallIsAtPos(Vector3Int pos)
        => GridQuery.WallIsAtPos(pos);

    public static bool TileIsEmpty(Vector3Int pos)
        => GridQuery.TileIsEmpty(pos);

    // ========== 委托给DirectionUtils ==========
    public static bool IsRound(Vector3 pos, Direction dir)
        => DirectionUtils.IsRound(pos, dir);

    public static Direction CheckDirection(Vector3 from, Vector3 to)
        => DirectionUtils.CheckDirection(from, to);

    // ========== 常用常量 ==========
    public static readonly Vector3Int forward = DirectionUtils.Forward;
    public static readonly Vector3Int back = DirectionUtils.Back;
    public static readonly Vector3Int up = DirectionUtils.Up;
    public static readonly Vector3Int down = DirectionUtils.Down;
    public static readonly Vector3Int left = DirectionUtils.Left;
    public static readonly Vector3Int right = DirectionUtils.Right;
}
```

### 5. WaitFor工具类

缓存Unity协程等待对象,避免GC:

```csharp
// 位于 Logic/Utility/WaitFor.cs
public static class WaitFor
{
    // ========== 缓存常用等待对象 ==========
    public static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();
    public static readonly WaitForFixedUpdate FixedUpdate = new WaitForFixedUpdate();

    // ========== 缓存WaitForSeconds ==========
    private static Dictionary<float, WaitForSeconds> waitCache =
        new Dictionary<float, WaitForSeconds>();

    public static WaitForSeconds Seconds(float seconds)
    {
        if (!waitCache.ContainsKey(seconds))
        {
            waitCache[seconds] = new WaitForSeconds(seconds);
        }
        return waitCache[seconds];
    }
}
```

**使用对比:**

```csharp
// ❌ 每次调用都创建新对象(有GC)
yield return new WaitForSeconds(1f);

// ✅ 使用缓存(无GC)
yield return WaitFor.Seconds(1f);
```

---

## 使用示例

```csharp
// ========== 新代码推荐:直接使用专用类 ==========
if (GridQuery.TileIsEmpty(targetPos))
{
    // 可以移动
}

Vector3 moveDir = DirectionUtils.GetVector(Direction.Right);

// ========== 旧代码兼容:使用Utils ==========
if (Utils.WallIsAtPos(targetPos))
{
    // 阻止移动
}
```

---

## 实践任务

### 任务1:添加范围查询方法

```csharp
// 在GridQuery类中添加
public static List<Mover> GetAllMoversInRadius(Vector3Int center, int radius)
{
    List<Mover> result = new List<Mover>();

    for (int x = -radius; x <= radius; x++)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                Vector3Int pos = center + new Vector3Int(x, y, z);
                result.AddRange(GetMoverAtPos(pos));
            }
        }
    }
    return result.Distinct().ToList();
}
```

### 任务2:添加方向扩展方法

```csharp
// 在DirectionUtils类中添加
public static bool IsHorizontal(this Direction dir)
{
    return dir == Direction.Left || dir == Direction.Right;
}

public static bool IsVertical(this Direction dir)
{
    return dir == Direction.Up || dir == Direction.Down;
}

// 使用
if (Direction.Right.IsHorizontal())
{
    Debug.Log("水平方向");
}
```

### 任务3:为Utils类添加单元测试

```csharp
// 创建测试类
[TestFixture]
public class UtilsTests
{
    [Test]
    public void TestGetOpposite()
    {
        Assert.AreEqual(Direction.Down, DirectionUtils.GetOpposite(Direction.Up));
        Assert.AreEqual(Direction.Left, DirectionUtils.GetOpposite(Direction.Right));
    }

    [Test]
    public void TestDirectionVectors()
    {
        Assert.AreEqual(new Vector3Int(1, 0, 0), DirectionUtils.Right);
        Assert.AreEqual(new Vector3Int(0, 1, 0), DirectionUtils.Up);
    }
}
```

---

## 扩展方向

### PathQuery: 路径查询

```csharp
public static class PathQuery
{
    // A*寻路
    public static List<Vector3Int> FindPath(Vector3Int start, Vector3Int end)
    {
        // 实现A*算法...
    }
}
```

### AreaQuery: 区域查询

```csharp
public static class AreaQuery
{
    // 矩形范围查询
    public static List<GameObject> GetObjectsInRect(Vector3Int min, Vector3Int max)
    {
        // ...
    }

    // 圆形范围查询
    public static List<GameObject> GetObjectsInCircle(Vector3Int center, float radius)
    {
        // ...
    }
}
```

---

## 常见问题

**Q: 为什么要分离GridQuery和DirectionUtils?**
A: 单一职责原则,便于维护和测试。

**Q: 什么时候用Utils,什么时候用专用类?**
A: 新代码用专用类,旧代码保持兼容用Utils。

**Q: WaitFor缓存有什么限制?**
A: 只缓存常用的秒数,不常用的时间值仍会创建新对象。

---

## 关键要点

- ✅ Utils是统一入口,委托给专用类
- ✅ GridQuery处理位置查询
- ✅ DirectionUtils处理方向计算
- ✅ WaitFor缓存协程等待对象
- ✅ 单一职责,便于维护

---

## 下一课

[第11课:撤销与重置系统](./Lesson11.md)
