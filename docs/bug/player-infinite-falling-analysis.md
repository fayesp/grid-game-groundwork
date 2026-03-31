# Player 无限下落 Bug 分析报告

**日期**: 2026-03-31
**状态**: 待修复
**优先级**: 高

---

## 问题描述

玩家在执行翻滚移动时出现无限下落的 bug。

---

## 相关文件

- `Assets/Scripts/Logic/Entity/Player.cs` - 玩家控制类
- `Assets/Scripts/Logic/Entity/Mover.cs` - 移动物体基类
- `Assets/Scripts/Logic/Entity/Game.cs` - 游戏管理类

---

## 根本原因分析

### 1. OnRollPlayer 父级设置错误 (核心问题)

**位置**: `Player.cs:171-176`

```csharp
public void OnRollPlayer(float rollDuration, Ease rotateEase)
{
    transform.SetParent(pivot.transform);                                    // 1. 设为 pivot 子物体
    pivot.transform.DORotate(...).OnComplete(Game.instance.MoveEnd);         // 2. 开始动画
    transform.SetParent(parent.transform);                                   // 3. 立即设回 parent (问题!)
}
```

**问题**: `transform.SetParent(parent.transform)` 在 DOTween 动画开始后**立即执行**，而不是等动画完成。

**后果**:
1. Player 被设为 pivot 的子物体
2. DOTween 开始旋转 pivot
3. **立即**将 Player 设回 parent 的子物体（动画还没完成）
4. Player 不再跟随 pivot 旋转，动画效果完全失效
5. Player 的实际位置与预期位置不同步

---

### 2. 位置不同步导致 GroundBelow 判断错误

**位置**: `Mover.cs:390-425`

```csharp
public bool GroundBelow()
{
    foreach (Tile tile in tiles)
    {
        if (tile.pos.z == 0)
            return true;
        if (GroundBelowTile(tile))
            return true;
    }
    return false;
}
```

**问题**: 当 Player 翻滚动画失效后:
1. 逻辑位置 (`ExecuteLogicalMove` 更新) 和动画位置 (`transform.position`) 不同步
2. `GroundBelow()` 依赖 `tile.pos`（基于子物体的实际位置）
3. 如果 Player 实际位置偏下，`GroundBelow()` 返回 false
4. `ShouldFall()` 返回 true，继续计划下落

---

### 3. CalRollPivot 深度偏移问题

**位置**: `Player.cs:163-169`

```csharp
public void CalRollPivot(Vector3 Dir)
{
    pivot.transform.position = transform.position + Vector3.forward * 0.5f + Dir * 0.5f;
    rotationAxis = Vector3.Cross(Vector3.back, Dir).normalized;
}
```

**问题**: 根据项目说明 *"Z-axis is depth (forward = falling direction)"*

`Vector3.forward * 0.5f` 会在 Z 轴方向偏移 0.5f，导致:
- Pivot 位置在深度方向偏移
- 翻滚后 Player 的 Z 坐标可能变化
- `GroundBelow()` 中的 `tile.pos.z == 0` 判断失效

---

## 代码执行流程

```
1. Player.CheckBufferedInput()
   ↓
2. TryPlanMove() - 计划移动
   ↓
3. CalRollPivot() - 计算旋转参数 (问题开始)
   ↓
4. Game.MoveStart()
   ↓
5. ExecuteLogicalMove() - 更新逻辑位置
   ↓
6. DoPostMoveEffects() - ShouldFall() 判断是否下落
   ↓
7. StartMoveCycle() - 开始动画
   ↓
8. OnRollPlayer() - 父级设置错误 (问题恶化)
   ↓
9. MoveEnd() → StartMoveCycle(true) - 开始下落动画
   ↓
10. 循环回到步骤 6，ShouldFall() 仍返回 true (无限循环)
```

---

## 修复方案

### 修复 1: 修正 OnRollPlayer 的父级设置 (推荐)

```csharp
public void OnRollPlayer(float rollDuration, Ease rotateEase)
{
    transform.SetParent(pivot.transform);
    pivot.transform.DORotate(rotationAxis * 90f, rollDuration, RotateMode.LocalAxisAdd)
        .SetEase(rotateEase)
        .OnComplete(() => {
            transform.SetParent(parent.transform);  // 移到回调中执行
            Game.instance.MoveEnd();
        });
}
```

### 修复 2: 调整 CalRollPivot 的深度偏移

```csharp
public void CalRollPivot(Vector3 Dir)
{
    // 水平移动时不应该在深度方向偏移
    Vector3 depthOffset = (Dir.z != 0) ? Vector3.forward * 0.5f : Vector3.zero;
    pivot.transform.position = transform.position + depthOffset + Dir * 0.5f;
    rotationAxis = Vector3.Cross(Vector3.back, Dir).normalized;
}
```

### 修复 3: 添加防重复下落检查 (可选)

在 `Game.MoveStart()` 中添加检查:

```csharp
public void MoveStart(bool doPostMoveEffects = true)
{
    // 防止已经下落时重复计划
    if (!doPostMoveEffects && movers.Any(m => m.isFalling))
    {
        // 已经在下落中，跳过
        return;
    }
    // ... 原有逻辑
}
```

---

## 测试建议

1. **单元测试**: 测试 `OnRollPlayer` 动画完成后父级是否正确恢复
2. **集成测试**: 测试完整的移动流程，验证 Player 不会无限下落
3. **边界测试**: 测试 Player 在地面边缘、角落位置的翻滚行为

---

## 影响范围

- 仅影响 Player 的翻滚移动逻辑
- 不影响其他 Mover 的行为
- 不影响游戏的输入和撤销系统
