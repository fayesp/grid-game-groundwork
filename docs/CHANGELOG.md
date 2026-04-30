# 架构重构变更日志

## 2026/04/30 — MVP + Command Pattern + Service Locator 架构引入

### 新增

#### Core 基础设施
- `Assets/Scripts/Core/GameEventBus.cs` — 实例化事件总线，替代静态 `EventManager`
- `Assets/Scripts/Core/MovementSettings.cs` — ScriptableObject，集中管理移动/动画参数
- `Assets/Scripts/Core/GameServices.cs` — Service Locator，单点依赖容器

#### Model 层（纯 C#，可单元测试）
- `Assets/Scripts/Model/GridEntity.cs` — 游戏实体纯数据表示
- `Assets/Scripts/Model/SpatialGrid.cs` — 纯 C# 空间哈希网格
- `Assets/Scripts/Model/GameBoard.cs` — 实体注册表、位置查询、快照/恢复
- `Assets/Scripts/Model/GameBoardGridQuery.cs` — `IGridQuery` 的 GameBoard 实现
- `Assets/Scripts/Model/IGridQuery.cs` — 网格查询接口，支持 Mock 测试
- `Assets/Scripts/Model/MovePlanner.cs` — 移动规划器（提取自 `Mover`）
- `Assets/Scripts/Model/MoverModel.cs` — 可移动实体模型
- `Assets/Scripts/Model/PlayerModel.cs` — 玩家实体模型
- `Assets/Scripts/Model/ICommand.cs` — 命令接口
- `Assets/Scripts/Model/CommandStack.cs` — Undo/Redo 命令栈
- `Assets/Scripts/Model/MoveCommand.cs` — 封装一次移动的命令
- `Assets/Scripts/Model/SnapshotCommand.cs` — 快照保存/恢复命令

#### View 层
- `Assets/Scripts/View/MoverView.cs` — Transform 同步、DOTween 动画标记、Gizmo
- `Assets/Scripts/View/PlayerView.cs` — 翻滚动画（RollPivot + DORotate）

#### Controller 层
- `Assets/Scripts/Controller/GamePresenter.cs` — 核心协调器，替代 `Game.cs`
- `Assets/Scripts/Controller/PlayerInputController.cs` — 输入收集与缓冲

#### 文档
- `docs/ARCHITECTURE.md` — 新架构概述
- `docs/MIGRATION_GUIDE.md` — 迁移指南与规范
- `docs/CHANGELOG.md` — 本文件

### 修改

- `Assets/Scripts/Logic/Event/EventManager.cs` — 委托到 `GameEventBus` 实例，保持向后兼容
- `Assets/Scripts/Logic/Entity/Game.cs` — 添加 `GameBoard`、`CommandStack`、MovementSettings 委托、Board/Transform 同步辅助方法
- `Assets/Scripts/Logic/Entity/Mover.cs` — 添加 `MoverModel` 属性引用
- `Assets/Scripts/Presentation/Editor/LevelEditor.cs` — 支持 `GamePresenter.SyncFromScene()`
- `Assets/Scripts/Logic/Utility/GridQuery.cs` — 添加注释，提示使用 `IGridQuery`
- `Assets/Scripts/Core/GameServices.cs` — 添加 `Presenter` 字段引用

### 架构改进

| 改进项 | 说明 |
|--------|------|
| Model-View 分离 | 逻辑位置存储在纯 C# Model，Transform 同步由 View 负责 |
| 命令模式 Undo/Redo | `CommandStack` 替代静态 `State`，支持 Redo，可序列化 |
| Service Locator | `GameServices` 替代 `Game.instance`、`Player.instance` 等分散单例 |
| 可测试性 | `MovePlanner`、`GameBoard`、`CommandStack` 均为纯 C#，可 NUnit 测试 |
| 输入解耦 | `PlayerInputController` 发射事件，可被 AI/网络/测试替代 |
| 动画解耦 | DOTween 逻辑集中在 `PlayerView` / `MoverView`，不在 `Game` 中 |
| 配置集中 | `MovementSettings` ScriptableObject 替代硬编码动画参数 |

### 已知限制

- `MovePlanner` 尚未完全覆盖 `Mover` 中的多 tile 精确碰撞、磁力附着传播、非整数位置处理等复杂逻辑。这些场景仍回退到 `Mover.TryPlanMove`。
- `GamePresenter` 已完整实现，但默认场景仍使用 `Game.cs` 运行。切换需要在场景中手动配置 `GamePresenter`。
- `CommandStack` 的 Undo 恢复模型位置后，Transform 同步是瞬时的（无动画）。
