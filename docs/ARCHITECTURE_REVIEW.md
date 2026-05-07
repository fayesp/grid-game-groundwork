# 架构变更评审报告

## 元信息

- 评审对象: `docs/ARCHITECTURE.md`(2026/04/30 版)所声明的 MVP + Command Pattern + Service Locator 架构
- 评审日期: 2026/05/06
- 评审范围: `Assets/Scripts/{Core, Model, View, Controller}` 全量代码 + 与遗留 `Logic/` 层的关系
- 评审目的: 验证架构变更是否合理、实现是否到位、是否引入了新风险

---

## 一、总体结论

**方向合理,落地未完成,存在并行双系统的高风险阶段。**

| 维度 | 评分 | 说明 |
|------|------|------|
| 架构思想 | 合理 | MVP + Command + Service Locator 对网格回合制游戏是教科书级方案 |
| 分层纯度 | 部分达成 | Model 层确实做到纯 C#,但部分逻辑仍滞留在 `Mover` MonoBehaviour |
| 迁移完整度 | **未完成** | `Game.cs`/`Mover.cs`/`Player.cs`/`State.cs`/`GridQuery.cs` 仍在运行,新旧并行 |
| 可测试性承诺 | **未兑现** | 全仓搜索 `*Test*.cs` 为零,新架构号称的"可单元测试"没有任何测试 |
| 风险等级 | **中高** | 双系统状态同步、单例冲突、ID 不稳定等问题需在合并前解决 |

---

## 二、合理且兑现的部分

### 2.1 Model 层确实做到了纯 C# 解耦

- `GridEntity.cs:1-82` 仅依赖 `Vector3`/`Vector3Int` 值类型,无 `MonoBehaviour`/`Transform`/`GameObject` 引用,文档承诺兑现。
- `SpatialGrid.cs:8-106` 使用 `Dictionary<Vector3Int, HashSet<string>>` 存储 entity ID,完全无 GameObject 引用,优于遗留 `LogicalGrid.cs:7` 的 `Dictionary<Vector3, HashSet<GameObject>>`。
- `GameBoard.cs:9-201` 是清晰的实体注册表 + 查询 + 快照,职责集中,文件长度可控(约 200 行)。

### 2.2 Command Pattern 设计正确

- `ICommand.cs:5-16` 接口最小化,只暴露 `Execute`/`Undo`,符合命令模式定义。
- `CommandStack.cs:7-93` 实现了完整的 Undo/Redo + 截断 redo 历史,语义清晰。
- `MoveCommand.cs:31-44` 在 `Execute` 时拍快照、`Undo` 时恢复,支持任意复合移动的回退,优于遗留 `State.cs:55-64` 的位置数组方式。

### 2.3 Service Locator 替代散单例

- `GameServices.cs:11-55` 集中了 `Events`/`Presenter`/`MovementSettings`,场景级单例语义合理,`OnDestroy` 中清理 `instanceRef` 避免悬挂引用。
- `GameEventBus.cs:8-38` 实例化事件总线,提供 `Clear()` 便于关卡切换时清理,优于静态 `EventManager` 的全局生命周期。
- `MovementSettings.cs:8-31` 改为 ScriptableObject 是 Unity 最佳实践,允许策划在 Inspector 调参且支持版本控制。

### 2.4 接口隔离

- `IGridQuery.cs:9-22` + `GameBoardGridQuery.cs:8-66` 把网格查询从具体实现抽离,`MovePlanner` 依赖接口而非 `GameBoard`,理论上支持 Mock 测试。

---

## 三、不合理或落地未完成的部分

### 3.1 【严重】新旧架构并行运行,且双方都在写状态

**问题描述:**

- `Game.cs:82-99` 的 `Awake` 仍在初始化 `LogicalGrid`、`movers`、`walls`、`State.Init()`。
- `GamePresenter.cs:48-78` 的 `Awake/Start` 也在创建 `GameBoard`、`CommandStack`、`MovePlanner`,并 `FindObjectsOfType<Mover>` 重新注册实体。
- `Game.cs:110-150` 的 `InitGameBoard` **额外**再做一次同样的 `GameBoard` 同步。

**后果:**

1. 同一场景中如果 `Game` 和 `GamePresenter` 都存在,会出现两个 `GameBoard` 实例,各自被独立同步,UndoStack 双写。
2. `Player.Update()`(`Player.cs:44-56`)和 `PlayerInputController.Update()`(`PlayerInputController.cs:28-56`)同帧都读 `Input.GetAxisRaw`,玩家按一次会被双重消费。
3. `PlayerView` 创建 `RollPivot/PlayerParent`(`PlayerView.cs:22-26`),`Player.Start()` 也创建 `RollPivot/Parent`(`Player.cs:30-33`),场景中会出现 4 个空 GameObject,Inspector 混乱。

**评估:** 文档 `MIGRATION_GUIDE.md:97-103` 把这种状态称为"方式 A: 并行运行(推荐,低风险)",但实际上**双系统同时跑等于高风险**——任何一边的状态漂移都会导致 Undo 出错或视觉位置与逻辑位置不一致。

### 3.2 【严重】MovePlanner 没有真正替换遗留逻辑

**证据:**

- `MovePlanner.cs:8-10` 自己的注释明示:
  > TODO: Full extraction of multi-tile push logic and magnet attach propagation.
- `MovePlanner.cs:159-186` 的 `GetCellsInDirection` 仅返回**单格**邻居,完全不处理 `Mover.UpdateDirPos`(`Mover.cs:459-552`)中"半格位置占据 4 个相邻整数格"的情况。
- 磁力联动 `Magnet.TryPlanMove`(`Magnet.cs:45-63`)中的 `AttachBlock` 联动 + 强迫推动判断,在新 `MovePlanner` 中**完全没有体现**。
- `MovePlanner.cs:85, 119` 在递归过程中临时 `new MoverModel(other)` 做包装,说明 `MovePlanner` 既未维护 model 集合也未与 `MoverModel` 解耦,只是把 `GridEntity` 在外面套层壳——架构意图模糊。

**后果:** 如果场景里包含 `Magnet`、半格 mover、多 tile 块,使用 `GamePresenter` 的新流程会出现碰撞误判、推动丢失。当前只有简单单格盒子可以"假装"工作。

### 3.3 【严重】实体 ID 不稳定

`GamePresenter.cs:102` / `Game.cs:125`:
```csharp
string id = mover.GetInstanceID().ToString();
```

- `GetInstanceID()` 在每次 Unity 运行时(包括同场景重新加载)都会变化,`MoveCommand` 缓存的 `entityDeltas` 字典 key 一旦跨域(序列化、热重载、关卡重启)立刻失效。
- `SnapshotCommand.cs:11` 的 `Dictionary<string, EntityTransformSnapshot>` 同样依赖这个不稳定 ID。
- 关卡编辑器 `LevelSerialization` 将来若想把 Undo 历史持久化(命令模式的常见收益),完全做不到。

**改进方向:** ID 应来自关卡数据(prefab 名 + 关卡内序号)或 `GUID`,在 `GridEntity` 创建时由数据层注入。

### 3.4 双源同步漂移

`Game.cs:473-507` 提供了 `SyncTransformsToBoard` / `SyncBoardToTransforms`,这两个方向的同步说明**当前没有单一事实源**:

- 旧路径:`Mover.transform.position` 是真值,`GameBoard` 是镜像。
- 新路径:`GameBoard.entity.Position` 是真值,`Transform` 由 `MoverView` 镜像。

`MoverView.Update()`(`MoverView.cs:36-45`)每帧无条件 `SyncToModel`,而 `Game.MoveStart`(`Game.cs:367-400`)直接修改 `transform.position`,在并行场景中:

1. 玩家按键 → `Player` 旧逻辑修改 `transform.position`。
2. 同帧 `MoverView.Update` 把刚改完的 transform 又用旧的 `model.Position` 覆盖回去。
3. 玩家看到瞬时位置闪烁。

这是典型的"两个 Update 互相纠正"反模式。

### 3.5 性能问题

| 位置 | 问题 | 建议 |
|------|------|------|
| `MoverView.cs:36-45` | 每帧 `SyncToModel`,即使没有移动 | 改为事件驱动:Model 变化时 push 到 View |
| `GamePresenter.cs:294-302` | `FindViewForEntity` 用 `foreach` 线性查找 | 用 `Dictionary<string, MoverView>` |
| `GamePresenter.cs:271-278` | 每次移动结束都遍历所有 movers 检查 `GroundBelow` | 仅检查刚移动的 mover 及其相邻列 |
| `GameBoard.cs:51` | `GetMovers()` 每次 LINQ where 全表扫 | 用两个分桶集合,Walls 与 Movers 分开存 |
| `SpatialGrid.GetEntityIdsAt` | 每次返回 `new HashSet<string>(...)` 拷贝 | 返回只读视图或 IEnumerable |

### 3.6 文档与实现脱节

| 文档声明(ARCHITECTURE.md) | 实际情况 |
|----------------------------|----------|
| `Game` 已被 `GamePresenter` 替代 | `Game.cs` 仍是 512 行的活跃文件 |
| 单元可测的 `MovePlanner` | 目录下零测试代码 |
| `IGridQuery` 接口便于 Mock | 没有 Mock 实现也没有测试夹具 |
| "替代 EventManager" | `EventManager.cs` 仍存在,`Game`/`Player`/`LevelManager`/`LevelEditor` 都通过它调用事件 |
| 表格"`Game.instance` 等分散单例 → `GameServices`" | 全仓搜索 `Game.instance` 仍 16 处、`Player.instance` 4 处、`State.` 多处 |

---

## 四、设计层面值得商榷的选择

### 4.1 把"下落"硬编码进 Model 层

`GameBoard.GroundBelow`(`GameBoard.cs:99-116`)和 `MoverModel.ShouldFall`(`MoverModel.cs:70-73`)默认 `Vector3Int(0,0,1)` 是"下方",这是 3D + 重力假设。
对**纯 2D Sokoban**(`docs/SOKOBAN_ARCHITECTURE_ANALYSIS.md` 已指出的目标场景)而言,这是过度耦合。建议:

- 把"重力检查"提到独立的 `GravitySystem`/`PostMoveEffect`,Model 不强制感知。
- `Direction.Forward`/`Back` 与"上下"耦合也属同类问题。

### 4.2 `MoverModel` 与 `GridEntity` 的职责重复

`MoverModel.cs:8-74` 只是给 `GridEntity` 包了 `PlannedMove`/`IsFalling`/`AttachedMoverIds`,但:

- `PlannedMove` 在 Command 模式下应该是 `MovePlanner` 的瞬时输出,不需要存到 Model。
- `IsFalling` 是状态标志,放 `GridEntity` 上即可。
- `AttachedMoverIds` 在 `MoverModel` 上但没有任何代码使用——空字段。

**建议:** 删除 `MoverModel`,`MovePlanner` 直接消费 `GridEntity`。`PlayerModel` 也可改为 `GridEntity.IsPlayer` 标志。

### 4.3 GameServices 单例的职责膨胀风险

`GameServices.cs:27-34` 已经持有 `Events`/`Presenter`/`MovementSettings`。Service Locator 模式的常见陷阱是逐渐沦为"全局变量袋",失去依赖可见性。
建议在文档中明确"哪些可以放入 GameServices",至少:

- 只放无状态服务或场景生命周期服务。
- 不要放游戏状态(`Board`、`CommandStack` 应注入到 `GamePresenter`,而非通过 Locator 拿)。

### 4.4 `EventManager` 的退化包装毫无价值

`EventManager.cs:13-111` 把每个事件包成 getter/setter 委托到 `GameServices.Instance.Events`。
但 set 操作 `GameServices.Instance.Events.onUndo = value` 会**整体覆盖**多播委托,任何旧订阅者都会被踢掉。对外看似"向后兼容",实际上语义已破坏。
建议:要么真删,要么改成 `+=`/`-=` 风格的 Subscribe/Unsubscribe API。

---

## 五、合并到主干前的必修项

按优先级排序:

### P0(阻塞合并)

1. **决定单一架构路径**:要么删除 `Game`/`Player`/`Mover` 的旧逻辑,要么明确暂不上线 `GamePresenter`。**不允许两套系统在同一场景共存**。
2. **修复实体 ID**:改用 prefab 名 + 关卡序号或 GUID,在 `GridEntity` 构造时注入。
3. **`Magnet`、多 tile 块在 `MovePlanner` 中的等价实现**,或显式声明这些实体在新架构下"暂不支持",让上层场景拒绝加载。

### P1(合并后下个迭代)

4. 为 `MovePlanner`、`GameBoard`、`CommandStack`、`SpatialGrid` 补单元测试,目标 ≥ 80% 行覆盖。文档已经为此立下承诺,缺测试就是欠债。
5. 删除 `EventManager` 退化包装,所有调用点改成 `GameServices.Instance.Events.xxx`。
6. `MoverView.Update` 改为事件驱动,移除每帧 `SyncToModel`。
7. 删除 `MoverModel`/`PlayerModel`,合并到 `GridEntity` + 标志位。

### P2(架构演进)

8. 把"下落/重力"从 `Model` 抽出,作为可插拔 `PostMoveEffect`,以支持纯 2D Sokoban。
9. `GameServices` 引入注册接口(`Register<T>`/`Resolve<T>`),减少硬编码字段。
10. `MoveCommand` 增加可视化日志/序列化,为关卡录制和回放铺路。

---

## 六、结论与建议

新架构的**意图正确**:解耦、可测、Undo 可序列化是网格游戏框架的合理目标。
但当前提交是**未完成的中间态**——核心规划逻辑(`Mover.TryPlanMove`、`Magnet`)未迁移、ID 不稳、双系统并行、零测试。

**给出的合并策略建议:**

1. 当前分支 `archchange` 不要合入 `master`,先在分支内补齐 P0。
2. 如急需先合,把 `Controller/`、`View/PlayerView.cs`、`Model/MovePlanner.cs` 标记为 `experimental/`,场景中仍由 `Game`/`Player`/`State` 主导,新架构不参与运行,只作为原型保存。
3. 在 `docs/ARCHITECTURE.md` 中明确标注"以下组件正处于迁移阶段,生产场景请勿启用 `GamePresenter`",避免下游开发者误用。

---

## 七、附录:验证用的快速命令

```bash
# 验证双系统耦合点
grep -rn "Game.instance\|Player.instance\|State\." Assets/Scripts | grep -v Logic/

# 检查 MovePlanner 缺失功能
grep -n "TODO\|todo" Assets/Scripts/Model/MovePlanner.cs

# 找到所有写入 GameBoard 的位置
grep -rn "Board\.\|board\." Assets/Scripts

# 寻找潜在 ID 不稳定使用
grep -rn "GetInstanceID" Assets/Scripts
```
