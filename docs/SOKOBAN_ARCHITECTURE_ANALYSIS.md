# 架构适配性分析：Sokoban 推箱子游戏

## 日期
2026/04/30

## 总体评价

当前 MVP + Command Pattern 架构作为"多游戏 groundwork"是**合格的**，但对**经典 Sokoban** 携带了过多 3D/重力/实时同步的包袱，且缺少胜利条件、目标点、步数统计等 Sokoban 核心语义。

---

## 契合良好的部分

### 1. Command + Snapshot 撤销机制
`CommandStack` + `MoveCommand` + `SnapshotCommand` 非常适合 Sokoban。**撤销/重做是 Sokoban 的核心玩法**，基于快照的 `Undo`（`RestoreSnapshot`）比逐帧反推更可靠，能正确处理连锁推动后的回退。

### 2. 连锁推动防护
`MovePlanner` 的 `visited` HashSet 有效防止了循环推动导致的死循环，对"玩家推箱子A、箱子A推箱子B"的连锁推动是必要保障。

### 3. 纯 C# Model 层
`GameBoard`、`SpatialGrid`、`GridEntity` 完全脱离 Unity，便于单元测试。`IGridQuery` 接口解耦了规划器和具体网格实现，符合"groundwork"支持多游戏类型的定位。

### 4. Presenter 的回合制流程
`GamePresenter.HandleMoveRequested` 中的 `if (blockInput || isMoving) return` 明确实现了回合制锁，防止动画期间重复输入，符合 Sokoban 的"输入-执行-等待"节奏。

---

## 过度设计的部分

### 1. MoverView 的每帧同步
`MoverView.Update()` 中每帧调用 `SyncToModel()` 对 Sokoban 是浪费。Sokoban 移动是离散的（一次一格），只有在移动动画期间才需要视觉插值，动画结束后一次性对齐即可。

### 2. 3D 空间与下落机制
`GroundBelow` 检查 Z 轴深度、`Forward/Back` 方向、`IsFalling` 状态 —— 这些都是为立体/重力游戏准备的。经典 Sokoban 是纯 2D 网格，没有下落概念。当前 `MovePlanner` 和 `GameBoard` 把 3D 重力作为默认假设，增加了不必要的复杂性。

### 3. PlayerView 的滚动动画
`AnimateRoll` 使用 DOTween 做滚动效果，对 Sokoban 过于花哨。Sokoban 通常需要瞬时或极短（<100ms）的平移，滚动动画会拖慢节奏。

### 4. Service Locator 的复杂度
`GameServices` 单例持有 `MovementSettings` ScriptableObject，对于 Sokoban 这种规则固定的游戏，直接用常量或简单配置即可，Service Locator 增加了隐式依赖。

---

## 缺失的关键部分

### 1. 目标点（Target/Goal）模型
`GridEntity` 和 `GameBoard` 完全没有"目标点"概念。Sokoban 的胜利条件是所有箱子覆盖目标点，当前架构只区分 `IsStatic`（墙）和 `!IsStatic`（推动者），缺少 `IsTarget`、`IsCrate` 等语义。

### 2. 箱子/玩家语义分离
当前所有可动物体都是 `MoverModel`，仅靠 `IsPlayer` bool 区分。Sokoban 需要明确的"玩家只能推动、箱子只能被推动"的语义，而当前 `allowPushMulti` 只是开关式控制，没有类型级约束。

### 3. 移动步数统计
Sokoban 核心反馈是"步数"和"推动次数"，`CommandStack` 和 `GamePresenter` 均未提供步数追踪。

### 4. 2D 网格简化路径
当前 `Vector3` / `Vector3Int` 全 3D 坐标对 Sokoban 是过度表达，缺少一个简化的 2D 网格抽象（X, Y 平面）。

---

## 具体改进建议

| 优先级 | 改进项 | 说明 |
|--------|--------|------|
| 高 | **为 GridEntity 增加游戏类型标签** | 添加 `EntityRole` 枚举：`Player`、`Crate`、`Wall`、`Target`，替代模糊的 `IsStatic` + `IsPlayer` |
| 高 | **增加 LevelGoal 模块** | 新增 `LevelGoal` 纯 C# 类，管理目标点坐标集合，`GamePresenter` 在 `OnMoveCycleComplete` 后调用检测 |
| 中 | **移除或降级下落系统** | 将 `GroundBelow`、`IsFalling` 从核心模型移到扩展模块。Sokoban groundwork 应以 2D 为默认，3D 重力为可选扩展 |
| 中 | **优化 View 同步策略** | `MoverView` 去掉 `Update()` 中的每帧同步，改为动画结束后由 `GamePresenter` 显式调用 `SyncViewsToBoard()` |
| 中 | **Command 增加元数据** | `MoveCommand` 增加 `StepCount`、`PushCount` 属性，便于 UI 展示和关卡评分 |
| 低 | **增加 2D 网格抽象** | 在 `SpatialGrid` 之上封装 `Grid2D` 类，使用 `Vector2Int` 简化 Sokoban 场景的坐标操作 |

---

## 结论

当前架构的**分层思路、Command 撤销机制、Presenter 协调模式**对 Sokoban 是正确方向。问题在于：

1. **承载了过多 3D/重力假设**（下落、Z轴、翻滚动画）
2. **缺少 Sokoban 核心语义**（目标点、胜利条件、步数统计）
3. **View 同步频率过高**（回合制不需要每帧同步）

建议以当前框架为基础，通过增加 `EntityRole`、提取 2D 抽象、降级 3D 特性来更好地适配 Sokoban，同时保留扩展性以支持其他网格游戏类型。
