# Grid Game Groundwork — 新架构文档

## 日期
2026/04/30

## 架构概览

项目从传统的三层架构（Data / Logic / Presentation）演进为 **MVP + Command Pattern + Service Locator** 混合架构。

### 核心原则

1. **Model 与 Unity 解耦**: 所有游戏逻辑和状态存储在纯 C# 类中，不依赖 `MonoBehaviour`、`Transform`、`GameObject`
2. **View 只负责视觉**: `MonoBehaviour` 仅处理动画、输入收集、Gizmo 绘制
3. **Presenter 协调两者**: `GamePresenter` 接收输入、调用 Model、驱动 View
4. **命令封装状态变更**: 每次移动通过 `ICommand` 执行，支持 Undo/Redo
5. **单点服务定位**: `GameServices` 替代分散的 `Game.instance`、`Player.instance` 等单例

---

## 目录结构

```
Assets/Scripts/
├── Core/                    # 基础设施
│   ├── GameServices.cs      # Service Locator
│   ├── GameEventBus.cs      # 实例化事件总线
│   └── MovementSettings.cs  # 可配置移动参数 (ScriptableObject)
│
├── Model/                   # 纯 C# 模型层（可单元测试）
│   ├── GridEntity.cs        # 实体纯数据
│   ├── SpatialGrid.cs       # 空间哈希网格
│   ├── GameBoard.cs         # 实体注册表 + 查询
│   ├── GameBoardGridQuery.cs# IGridQuery 实现
│   ├── IGridQuery.cs        # 网格查询接口
│   ├── MovePlanner.cs       # 移动规划器
│   ├── MoverModel.cs        # 可移动实体模型
│   ├── PlayerModel.cs       # 玩家模型
│   ├── ICommand.cs          # 命令接口
│   ├── CommandStack.cs      # Undo/Redo 栈
│   ├── MoveCommand.cs       # 移动命令
│   └── SnapshotCommand.cs   # 快照命令
│
├── View/                    # Unity 视觉层
│   ├── MoverView.cs         # Transform 同步 + Gizmo
│   └── PlayerView.cs        # 翻滚动画 (DOTween)
│
├── Controller/              # Presenter / 控制器
│   ├── GamePresenter.cs     # 核心协调器（替代 Game.cs）
│   └── PlayerInputController.cs # 输入收集
│
├── Data/                    # 数据持久化（原有）
├── Logic/                   # 遗留逻辑层（逐步迁移中）
└── Presentation/            # 编辑器工具（原有）
```

---

## 各层职责

### Core 层

| 文件 | 职责 |
|------|------|
| `GameServices` | 场景中单例，持有 `Events`、`Presenter`、`MovementSettings`。测试时可替换为 Mock |
| `GameEventBus` | 实例化的事件总线，替代静态 `EventManager`。生命周期与场景一致，切换场景时自动清理 |
| `MovementSettings` | ScriptableObject，集中 `moveTime`、`fallTime`、`ease` 等参数，支持运行时调整 |

### Model 层

| 文件 | 职责 |
|------|------|
| `GridEntity` | 纯数据：ID、Position(Vector3)、Rotation、TileOffsets、IsPlayer、IsStatic |
| `GameBoard` | 所有实体的注册表，拥有 `SpatialGrid`，提供位置查询、移动、快照 |
| `SpatialGrid` | `Dictionary<Vector3Int, HashSet<string>>`，纯 C# 空间索引，无 GameObject 引用 |
| `IGridQuery` / `GameBoardGridQuery` | 接口 + 实现，解耦移动规划与具体网格存储，便于 Mock 测试 |
| `MovePlanner` | 纯 C# 移动规划：碰撞检测、推动传播、循环推动防止。替代 `Mover.CanMoveToward` |
| `MoverModel` | 包装 `GridEntity`，持有 `PlannedMove`、执行逻辑移动、下落判定 |
| `CommandStack` | Undo/Redo 栈，管理 `ICommand` 列表，支持 Reset |
| `MoveCommand` | 封装一次移动：Execute 应用 deltas，Undo 恢复快照 |
| `SnapshotCommand` | 保存/恢复 `GameBoard` 完整状态 |

### View 层

| 文件 | 职责 |
|------|------|
| `MoverView` | `MonoBehaviour`，持有 `GridEntity` 引用，每帧同步 Transform，支持 DOTween 动画标记 |
| `PlayerView` | 翻滚动画：创建 RollPivot、计算旋转轴、DORotate |

### Controller 层

| 文件 | 职责 |
|------|------|
| `PlayerInputController` | 每帧读取 `Input.GetAxisRaw`，缓冲输入，发射 `OnMoveRequested/OnUndoRequested/OnResetRequested` |
| `GamePresenter` | 接收输入事件 -> `MovePlanner` 规划 -> `CommandStack` 执行 -> 驱动 `MoverView` 动画 -> 检查下落 -> 循环 |

---

## 数据流

### 移动流程

```
PlayerInputController.Update()
  -> OnMoveRequested(Vector3Int dir)
    -> GamePresenter.HandleMoveRequested()
      -> MovePlanner.TryPlanMove() // 纯 C# 规划
        -> 返回 PlannedMoveResult (entityId -> delta)
      -> MoveCommand.Execute() // 更新 GameBoard
      -> MoverView DOTween 动画
        -> OnAnimationComplete()
          -> 检查下落 (ShouldFall)
            -> 如有下落，递归执行下落移动
          -> GameEventBus.onMoveComplete
```

### Undo 流程

```
PlayerInputController.OnUndoRequested
  -> GamePresenter.HandleUndoRequested()
    -> CommandStack.Undo()
      -> SnapshotCommand.Undo() / MoveCommand.Undo()
        -> GameBoard.RestoreSnapshot()
    -> MoverView.SyncToModel() // Transform 同步回 Model 位置
```

---

## 与旧架构对比

| 旧架构 | 新架构 | 收益 |
|--------|--------|------|
| `Game` 上帝对象 (350+ 行) | `GamePresenter` (300 行) + `GameBoard` (200 行) + `MovePlanner` (250 行) | 职责分离，文件 < 400 行 |
| `State` 静态类，直接操作 Transform | `CommandStack` + `SnapshotCommand`，纯 C# | 可测试、可序列化、支持 Redo |
| `Mover` 继承 MonoBehaviour | `MoverModel` 纯 C# + `MoverView` MonoBehaviour | 逻辑可单元测试，无需 Unity 运行器 |
| `Player.Update()` 读取 Input | `PlayerInputController` 发射事件 | 输入可替换为 AI / 网络 / 自动化测试 |
| `Game.instance` 等分散单例 | `GameServices` 单点 Service Locator | 依赖可见，测试可 Mock |
| `EventManager` 静态 Action | `GameEventBus` 实例 | 生命周期可控，切换关卡自动清理 |
| `GridQuery` 静态类依赖 `Game.instance` | `IGridQuery` 接口 + `GameBoardGridQuery` | 可注入 Mock 网格 |

---

## 测试策略

### 可单元测试的组件（纯 C#，无需 Unity）

- `MovePlanner` — 使用 Mock `IGridQuery` 测试各种碰撞和推动场景
- `GameBoard` — 测试实体注册、移动、查询、快照
- `CommandStack` — 测试 Execute/Undo/Redo/Reset 语义
- `SpatialGrid` — 测试单元格占用、更新、清除

### 需要 Unity Test Runner 的组件

- `PlayerInputController` — 输入缓冲逻辑
- `MoverView` / `PlayerView` — 动画同步
- `GamePresenter` — 完整移动循环集成测试
