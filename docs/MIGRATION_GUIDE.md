# 架构迁移指南

## 日期
2026/04/30

## 迁移阶段

重构采用**增量迁移**策略，旧系统与新系统可并行运行。每个阶段独立可提交。

---

## Phase 1: Core 基础设施（已完成）

### 变更
- 新建 `Core/` 目录：`GameEventBus.cs`、`MovementSettings.cs`、`GameServices.cs`
- `EventManager` 改为委托到 `GameEventBus` 实例
- `Game.cs` 的 `moveTime`、`fallTime`、`ease`、`allowPushMulti` 改为从 `MovementSettings` ScriptableObject 读取

### 对现有代码的影响
**无影响**。所有变更均为向后兼容：
- `EventManager.onMoveComplete` 等事件仍可正常使用
- `Game.moveTime` 等属性保持原有访问方式（内部委托到 `MovementSettings`）

### 如何启用新功能
1. 在场景中放置 `GameServices` MonoBehaviour（`Game.cs.Awake` 会自动创建）
2. 创建 `MovementSettings` Asset：`Assets > Create > Game > Movement Settings`
3. 将 Asset 拖到 `GameServices.MovementSettings` 字段

---

## Phase 2: 纯 C# Model 层（已完成）

### 变更
- 新建 `Model/` 目录：`GridEntity.cs`、`SpatialGrid.cs`、`GameBoard.cs`
- 新建 `View/` 目录：`MoverView.cs`
- `Game.cs` 新增 `Board` 属性，在 `SetReferences()` 时自动同步场景中所有 `Mover`/`Wall`

### 对现有代码的影响
**无影响**。`GameBoard` 仅同步数据，不参与游戏逻辑。`Mover` 仍通过 `transform.position` 移动。

### 开发者注意事项
- 新代码应优先使用 `Game.instance.Board` 查询实体，而非 `GridQuery` 或 `Utils.GetMoverAtPos`
- `MoverView` 可附加到现有 Mover Prefab 上，为未来动画迁移做准备

---

## Phase 3: 移动规划提取（已完成）

### 变更
- 新建 `Model/MovePlanner.cs`、`IGridQuery.cs`、`GameBoardGridQuery.cs`
- 新建 `Model/MoverModel.cs`、`PlayerModel.cs`
- `Mover.cs` 新增 `Model` 属性（`MoverModel`）

### 对现有代码的影响
**无影响**。`MovePlanner` 作为独立组件存在，`Mover.TryPlanMove` 仍使用原有逻辑。

### 如何使用 MovePlanner
```csharp
var gridQuery = new GameBoardGridQuery(Game.instance.Board);
var planner = new MovePlanner(gridQuery, allowPushMulti: true);
var playerModel = new PlayerModel(playerEntity);

if (planner.TryPlanMove(playerModel, direction, dir, out var result))
{
    // result.EntityDeltas: Dictionary<string, Vector3>
}
```

---

## Phase 4: Command 模式（已完成）

### 变更
- 新建 `Model/CommandStack.cs`、`MoveCommand.cs`、`SnapshotCommand.cs`
- `Game.cs` 新增 `CommandStack` 属性
- `Game.CompleteMove()` 自动记录 `SnapshotCommand`
- `Game.DoUndo()` / `DoReset()` 优先使用 `CommandStack`，回退到 `State`

### 对现有代码的影响
**极小**。Undo/Reset 行为不变。`CommandStack` 在后台运行，记录 `GameBoard` 快照。

### 验证方式
- 运行游戏，执行移动和 Undo
- 检查 `Game.instance.CommandStack.UndoCount` 是否正常递增

---

## Phase 5: 输入与动画解耦（已完成）

### 变更
- 新建 `Controller/PlayerInputController.cs`
- 新建 `Controller/GamePresenter.cs`
- 新建 `View/PlayerView.cs`

### 如何切换到新架构

#### 方式 A: 并行运行（推荐，低风险）
保持 `GameController` prefab 不变。在场景中新增 `GamePresenter` GameObject：
1. 创建空 GameObject，命名为 `GamePresenter`
2. 附加 `GamePresenter` 组件
3. 附加 `PlayerInputController` 组件（到同一物体或玩家物体）
4. 将 `PlayerInputController` 拖到 `GamePresenter.InputController` 字段
5. `GamePresenter` 会自动发现场景中的 `Mover`/`Wall` 并接管

#### 方式 B: 完全替换（需验证后使用）
1. 从场景中移除旧的 `Game` 对象
2. 确保 `GamePresenter` 已配置 `PlayerInputController`
3. `LevelEditor` 已通过 `GameServices.Instance.Presenter` 支持刷新

---

## Phase 6: Level Editor 与清理（已完成）

### 变更
- `LevelEditor.Refresh()` 优先调用 `GamePresenter.SyncFromScene()`，回退到 `Game.instance.EditorRefresh()`
- `GridQuery` 添加注释，提示使用 `IGridQuery`

---

## 遗留代码清单

以下代码仍使用旧架构，将在后续迭代中逐步替换：

| 文件 | 遗留内容 | 替代方案 |
|------|----------|----------|
| `Logic/Entity/Game.cs` | 上帝对象，管理动画、输入、网格同步 | `GamePresenter` |
| `Logic/Entity/Mover.cs` | `TryPlanMove`、`CanMoveToward` 仍使用旧逻辑 | `MovePlanner` |
| `Logic/Entity/Player.cs` | `Update()` 中读取 Input，管理 RollPivot | `PlayerInputController` + `PlayerView` |
| `Logic/Entity/BaseClass/State.cs` | 静态类，直接操作 Transform | `CommandStack` + `SnapshotCommand` |
| `Logic/Utility/GridQuery.cs` | 静态类，依赖 `Game.instance.Grid` | `IGridQuery` + `GameBoardGridQuery` |
| `Logic/Grid/LogicalGrid.cs` | 存储 `GameObject` 引用 | `SpatialGrid`（存储 entity ID） |

---

## 编写新代码的规范

### 查询位置
```csharp
// 旧方式（遗留）
var movers = Utils.GetMoverAtPos(pos);

// 新方式
var movers = Game.instance.Board.GetMoversAtPos(pos);
```

### 监听事件
```csharp
// 旧方式（仍可工作）
EventManager.onMoveComplete += OnMoveComplete;

// 新方式（推荐）
GameServices.Instance.Events.onMoveComplete += OnMoveComplete;
```

### 移动实体
```csharp
// 旧方式（遗留）
mover.transform.position += delta;

// 新方式
Game.instance.Board.MoveEntity(entityId, delta);
// 然后由 MoverView 同步到 Transform
```

---

## 常见问题

### Q: `Game.instance` 还能用吗？
可以。`Game` 单例仍然正常运行，旧代码无需修改。`GamePresenter` 是可选的替代方案。

### Q: 为什么 `MovePlanner` 没有完全替换 `Mover.TryPlanMove`？
`Mover` 中的多 tile 碰撞、磁力附着、非整数位置处理等逻辑非常复杂。`MovePlanner` 已提取核心框架，特殊场景可在后续迭代中逐步迁移。

### Q: `CommandStack` 的 Undo 为什么没有动画？
`CommandStack` 恢复 `GameBoard` 模型位置后，需要调用 `MoverView.SyncToModel()` 立即同步 Transform。如需动画效果，可在 `GamePresenter.HandleUndoRequested()` 中添加 DOTween。

### Q: 如何为 `MovePlanner` 编写单元测试？
```csharp
[Test]
public void MovePlanner_WallBlocksMove_ReturnsFalse()
{
    var mockGrid = new MockGridQuery();
    mockGrid.AddWall(new Vector3Int(1, 0, 0));

    var planner = new MovePlanner(mockGrid, allowPushMulti: true);
    var entity = new GridEntity("player", "Player");
    var model = new MoverModel(entity);

    bool canMove = planner.TryPlanMove(model, Vector3.right, Direction.Right, out var result);

    Assert.IsFalse(canMove);
}
```
