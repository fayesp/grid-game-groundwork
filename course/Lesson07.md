# 第7课:重力与下落系统

> **难度:** ⭐⭐ | **预计时间:** 45分钟 | **前置要求:** [第6课](./Lesson06.md)

---

## 学习目标

- 理解重力系统的实现
- 掌握下落检测逻辑
- 了解移动后效果处理

---

## 核心内容

### 1. 下落判断:ShouldFall()

```csharp
// 位于 Mover.cs
public virtual bool ShouldFall()
{
    // 有地面支撑就不下落
    if (GroundBelow())
        return false;
    return true;
}
```

**设计要点:**
- `virtual`方法,子类可重写
- 简单委托给GroundBelow()
- 可扩展为更复杂的条件

### 2. 地面检测:GroundBelow()

```csharp
public bool GroundBelow()
{
    // 遍历所有Tile
    foreach (Tile tile in tiles)
    {
        // ========== 检查1: 地面层(Z=0) ==========
        if (tile.pos.z == 0)
            return true;  // 在地面层,有支撑

        // ========== 检查2: 下方有支撑物 ==========
        if (GroundBelowTile(tile))
            return true;
    }
    return false;  // 所有Tile都无支撑
}

bool GroundBelowTile(Tile tile)
{
    // 检查下方一格(Z+1,因为Z+是下落方向)
    Vector3Int posToCheck = tile.pos + Utils.forward;

    // ========== 检查墙体支撑 ==========
    if (Utils.WallIsAtPos(posToCheck))
        return true;

    // ========== 检查其他Mover支撑 ==========
    List<Mover> movers = Utils.GetMoverAtPos(posToCheck);
    if (movers.Any(m => m != null && m != this && !m.isFalling))
        return true;  // 下方有静止的Mover

    return false;
}
```

**支撑条件:**

| 条件 | 说明 |
|------|------|
| Z=0 | 物体在地面层 |
| 下方有墙 | Z+1位置有Wall |
| 下方有静止Mover | Z+1位置有非下落的Mover |

**重要:** `!m.isFalling`确保正在下落的物体不能互相支撑。

### 3. 移动后效果:DoPostMoveEffects()

```csharp
public virtual void DoPostMoveEffects()
{
    // 移动完成后检查是否需要下落
    if (ShouldFall())
    {
        // 计划下落移动
        PlanMove(Utils.forward, Direction.Forward);
    }
}
```

**触发时机:**
- 每次移动完成后由Game类调用
- 检查是否需要下落
- 自动计划下落移动

### 4. 下落动画循环

在Game类中,下落使用单独的动画循环:

```csharp
// 位于 Game.cs
private void StartMoveCycle(bool falling)
{
    // ========== 下落特殊处理 ==========
    if (falling)
    {
        // 下落时清空输入缓冲
        Player.instance?.ClearInputBuffer();

        // 标记所有下落的Mover
        foreach (var mover in movers)
        {
            if (mover.PlannedMove == Utils.forward)
                mover.isFalling = true;
        }
    }

    // ========== 计算动画时间 ==========
    // 下落使用更短的时间(更快)
    float dur = falling ? fallTime : moveTime / ...;

    // ========== 执行动画 ==========
    // ...
}

public void MoveEnd()
{
    movingCount--;

    if (movingCount == 0)
    {
        // 所有普通移动完成

        if (HasFallingMovers())
        {
            // 有物体需要下落,开始下落动画循环
            StartMoveCycle(true);
        }
        else
        {
            // 无下落,移动完成
            EventManager.onMoveComplete?.Invoke();
        }
    }
}
```

**下落流程:**

```
普通移动完成
    │
    ├── DoPostMoveEffects()
    │       │
    │       └── ShouldFall() → true
    │               │
    │               └── PlanMove(forward)
    │
    └── MoveEnd() → 有下落?
            │
            ├── 是 → StartMoveCycle(falling=true)
            │           │
            │           └── 下落动画
            │                   │
            │                   └── MoveEnd() → 还有下落?
            │                           │
            │                           └── 循环...
            │
            └── 否 → onMoveComplete触发
```

### 5. 下落状态标记

```csharp
[HideInInspector] public bool isFalling = false;
```

**用途:**
1. **防止互相支撑**: 下落中的物体不能支撑其他物体
2. **状态判断**: 用于游戏逻辑判断

**重置时机:**
- 动画完成后重置为false
- 撤销/重置时重置

---

## 下落流程图

```
移动完成
    │
    └── DoPostMoveEffects()
            │
            ├── ShouldFall()
            │       │
            │       └── GroundBelow()
            │               ├── 检查Z=0
            │               ├── 检查墙体
            │               └── 检查其他Mover
            │
            └── 如果需要下落
                    │
                    └── PlanMove(forward)
                            │
                            └── Game.MoveStart()
                                    │
                                    └── StartMoveCycle(falling=true)
                                            │
                                            └── 动画完成
                                                    │
                                                    └── 检查是否继续下落
```

---

## 实践任务

### 任务1:创建悬空方块观察下落

1. 创建一个Box,位置(0,0,2) - Z=2表示悬空2格
2. 运行场景
3. 观察方块下落行为

### 任务2:创建堆叠方块测试互相支撑

1. 创建Box1,位置(0,0,1)
2. 创建Box2,位置(0,0,2)
3. 运行场景
4. 观察两个方块的行为

**预期结果:**
- Box1有支撑(Z=1,下方Z=0是地面)
- Box2由Box1支撑(下方有静止的Box1)
- 两方块都不下落

### 任务3:实现悬浮物体

```csharp
// 创建自定义Mover: 悬浮方块
public class FloatingBlock : Mover
{
    public float floatDuration = 2f;  // 悬浮时间
    private float floatTimer = 0f;
    private bool isFloating = true;

    public override bool ShouldFall()
    {
        // 悬浮期间不下落
        if (isFloating)
        {
            floatTimer += Time.deltaTime;
            if (floatTimer >= floatDuration)
            {
                isFloating = false;
                Debug.Log("悬浮结束,开始下落!");
            }
            return false;
        }

        // 悬浮结束后正常下落
        return base.ShouldFall();
    }
}
```

---

## 扩展思路

### 自定义下落条件

```csharp
// 例: 只有在特定条件下才下落
public class ConditionalFallBlock : Mover
{
    public bool activated = false;

    public override bool ShouldFall()
    {
        if (!activated)
            return false;  // 未激活时悬浮

        return base.ShouldFall();
    }

    // 被玩家触碰时激活
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            activated = true;
        }
    }
}
```

### 延迟下落

```csharp
public class DelayedFallBlock : Mover
{
    public float delay = 0.5f;
    private bool triggered = false;
    private float triggerTime;

    public override bool ShouldFall()
    {
        if (!triggered)
        {
            triggered = true;
            triggerTime = Time.time;
            return false;
        }

        if (Time.time - triggerTime < delay)
            return false;  // 延迟未到

        return base.ShouldFall();
    }
}
```

---

## 常见问题

**Q: 为什么下落时清空输入缓冲?**
A: 下落过程中玩家不能操作,防止输入堆积。

**Q: 下落的物体能互相支撑吗?**
A: 不能,`!m.isFalling`条件确保了这一点。

**Q: 如何实现"踩上去后延迟下落"的机关?**
A: 参考上方DelayedFallBlock示例。

---

## 关键要点

- ✅ ShouldFall()判断是否需要下落
- ✅ GroundBelow()检测支撑物
- ✅ 下落物体不能互相支撑(isFalling标记)
- ✅ DoPostMoveEffects()在移动后触发下落检查
- ✅ 可重写ShouldFall()实现自定义下落逻辑

---

## 下一课

[第8课:玩家控制 - Player类](./Lesson08.md)
