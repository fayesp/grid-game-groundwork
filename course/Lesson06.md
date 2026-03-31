# 第6课:可移动对象基类 - Mover (下)

> **难度:** ⭐⭐⭐ | **预计时间:** 75分钟 | **前置要求:** [第5课](./Lesson05.md)

---

## 学习目标

- 深入理解移动验证与计划系统
- 掌握推动机制的实现
- 了解无限循环防护

---

## 核心文件

- `Logic/Entity/Mover.cs`

---

## 核心内容

### 1. 移动验证:CanMoveToward()

```csharp
public virtual bool CanMoveToward(ref Vector3 MoveV3, Direction Dir,
    HashSet<Mover> visited = null)
{
    // ========== 初始化visited set ==========
    if (visited == null)
        visited = new HashSet<Mover>();

    // 防止无限递归(循环引用)
    if (visited.Contains(this))
        return true;  // 已检查过此mover
    visited.Add(this);

    // ========== 遍历所有Tile ==========
    foreach (Tile tile in tiles)
    {
        // 更新方向位置
        UpdateDirPos(tile.t.position);

        // 检查移动方向上的每个格子
        List<Vector3Int> CheckPos = GetDirV3(Dir);
        foreach (var pos in CheckPos)
        {
            // ========== 1. 检查墙体 ==========
            if (Utils.WallIsAtPos(pos))
                return false;  // 撞墙,不能移动

            // ========== 2. 检查其他Mover ==========
            List<Mover> movers = Utils.GetMoverAtPos(pos);
            if (movers.All(m => m == null || m == this))
                continue;  // 该位置无其他物体

            // ========== 3. 检查是否允许推动 ==========
            if (!isPlayer && !Game.allowPushMulti)
                return false;  // 非玩家不能推

            // ========== 4. 递归检查被推动的物体 ==========
            foreach (var m in movers)
            {
                if (m != null && m != this &&
                    !m.CanMoveToward(ref MoveV3, Dir, visited))
                    return false;  // 被推的物体不能移动
            }
        }
    }
    return true;  // 可以移动
}
```

**验证流程:**

```
CanMoveToward()
    │
    ├─→ 检查墙体碰撞 → 有墙? → 返回false
    │
    ├─→ 检查其他Mover → 无物体? → 继续
    │       │
    │       └─→ 检查推动权限 → 无权限? → 返回false
    │               │
    │               └─→ 递归检查被推物体 → 不能推? → 返回false
    │
    └─→ 返回true(所有检查通过)
```

**HashSet<Mover> visited的作用:**

```
场景: A推B,B推C,C推A(循环)

没有visited:
A检查B → B检查C → C检查A → A检查B → ... (无限循环)

有visited:
A检查B → B检查C → C检查A(已visited,跳过) → 完成!
```

### 2. 移动计划:PlanMove()

```csharp
public virtual void PlanMove(Vector3 MoveV3, Direction Dir,
    HashSet<Mover> visited = null)
{
    if (visited == null)
        visited = new HashSet<Mover>();

    // 避免重复计划同一个物体
    if (PlannedMove == MoveV3)
        return;

    // 防止无限递归
    if (visited.Contains(this))
        return;
    visited.Add(this);

    // 记录计划移动
    PlannedMove = MoveV3;

    // 计划推动其他物体
    PlanPushes(MoveV3, Dir, visited);
}
```

### 3. 推动机制:PlanPushes()

```csharp
protected void PlanPushes(Vector3 MoveV3, Direction Dir, HashSet<Mover> visited)
{
    foreach (Tile tile in tiles)
    {
        UpdateDirPos(tile.t.position);
        List<Vector3Int> CheckPos = GetDirV3(Dir);

        // ========== 收集需要被推动的物体 ==========
        List<Mover> moversToPush = new List<Mover>();
        foreach (Vector3Int posToCheck in CheckPos)
        {
            List<Mover> movers = Utils.GetMoverAtPos(posToCheck);
            foreach (var m in movers)
            {
                if (m != null && m != this && !moversToPush.Contains(m))
                    moversToPush.Add(m);
            }
        }

        // ========== 处理连接的物体组 ==========
        List<List<Mover>> pushLists = GetAttachMoverList(moversToPush);

        // ========== 为每个物体组计划移动 ==========
        foreach (List<Mover> moverList in pushLists)
        {
            // 计算推动向量
            Vector3 pushVector = CalculatePushVector(moverList, MoveV3);

            // 递归为被推物体计划移动
            foreach (var m in moverList)
            {
                m.PlanMove(pushVector, Dir, visited);
            }
        }
    }
}
```

**推动链示例:**

```
玩家 → 箱子A → 箱子B → 箱子C

1. 玩家.CanMoveToward(Right)
   → 检查箱子A → 检查箱子B → 检查箱子C → 检查墙体
   → 全部通过,返回true

2. 玩家.PlanMove(Right)
   → 玩家.PlannedMove = Right
   → 箱子A.PlanMove(Right)
      → 箱子A.PlannedMove = Right
      → 箱子B.PlanMove(Right)
         → 箱子B.PlannedMove = Right
         → 箱子C.PlanMove(Right)
            → 箱子C.PlannedMove = Right
```

### 4. 移动执行:ExecuteLogicalMove()

```csharp
public bool ExecuteLogicalMove()
{
    // 没有计划移动
    if (PlannedMove == Vector3Int.zero)
        return false;

    // 更新逻辑位置(立即)
    transform.position = Pos() + PlannedMove;

    // 清空计划
    plannedMove = Vector3.zero;

    return true;
}
```

**注意:** 这里只更新逻辑位置,动画由Game类统一执行。

### 5. 入口方法:TryPlanMove()

```csharp
public virtual bool TryPlanMove(Vector3 MoveV3, Direction Dir)
{
    HashSet<Mover> visited = new HashSet<Mover>();

    // ========== 阶段1: 验证移动是否可行 ==========
    if (!CanMoveToward(ref MoveV3, Dir, visited))
        return false;  // 验证失败,不能移动

    visited.Clear();  // 清空visited用于第二阶段

    // ========== 阶段2: 计划移动 ==========
    PlanMove(MoveV3, Dir, visited);

    return true;
}
```

**两阶段设计的原因:**
1. **验证阶段**: 只检查,不修改状态
2. **计划阶段**: 记录所有移动计划

这样可以确保要么全部成功,要么全部失败(原子性)。

---

## 移动流程图

```
TryPlanMove()
    │
    ├── CanMoveToward()  // 验证阶段
    │       │
    │       ├── 检查墙体
    │       │
    │       ├── 检查Mover碰撞
    │       │
    │       └── 递归检查推动链
    │
    └── PlanMove()       // 计划阶段
            │
            ├── 记录PlannedMove
            │
            └── PlanPushes()
                    │
                    ├── 收集被推物体
                    │
                    └── 递归计划被推物体
```

---

## 实践任务

### 任务1:创建两个相邻的Mover

1. 创建两个Box,命名为BoxA和BoxB
2. 位置设置为(0,0,0)和(1,0,0)
3. 测试推动行为

### 任务2:测试allowPushMulti

```csharp
// 在Game Inspector中切换allowPushMulti
// true: 任何Mover都可以推其他Mover
// false: 只有Player可以推

// 测试步骤:
// 1. allowPushMulti = true
//    → BoxA可以推BoxB
// 2. allowPushMulti = false
//    → BoxA不能推BoxB
//    → Player可以推BoxB
```

### 任务3:创建U形物体组测试循环防护

```
布局:
┌───┐
│ A │
└─┬─┘
  │
┌─┴─┐
│ B │
└─┬─┘
  │
┌─┴─┐
│ C │←─推
└───┘

C推B, B推A, A推C?
→ HashSet防止无限递归
```

---

## 思考题

**Q1: 为什么需要两阶段移动(验证+计划)?**
A: 保证原子性,避免部分移动成功导致状态不一致。

**Q2: `HashSet<Mover> visited`解决了什么问题?**
A: 防止循环引用导致的无限递归,如A推B推C推A的情况。

**Q3: 为什么ExecuteLogicalMove只更新位置不播放动画?**
A: 动画由Game类统一管理,确保所有物体同步移动。

---

## 关键要点

- ✅ CanMoveToward()验证移动可行性
- ✅ PlanMove()记录移动计划
- ✅ HashSet防止循环引用无限递归
- ✅ 两阶段设计保证原子性
- ✅ 推动链通过递归处理

---

## 下一课

[第7课:重力与下落系统](./Lesson07.md)
