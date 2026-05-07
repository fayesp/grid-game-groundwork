# MVP 架构模式:结构解析与实现指南

## 元信息

- 文档目的: 解释 MVP(Model-View-Presenter)模式的结构,并用本仓库代码演示如何落地
- 阅读对象: 本项目开发者,假设读者熟悉 Unity / C# / MonoBehaviour
- 配套阅读: `docs/ARCHITECTURE.md`(架构现状)、`docs/ARCHITECTURE_REVIEW.md`(评审)

---

## 一、MVP 是什么

### 1.1 三个角色

| 角色 | 职责 | 典型禁忌 |
|------|------|----------|
| **Model**(模型) | 持有业务状态 + 业务规则,纯数据/纯逻辑 | 不能引用 View、不能依赖 Unity 类型(`MonoBehaviour`/`Transform`/`GameObject`) |
| **View**(视图) | 把 Model 状态画到屏幕,把用户操作当作事件抛出 | 不能直接修改 Model、不能写业务规则 |
| **Presenter**(主持人) | 监听 View 事件 → 调 Model 改状态 → 把结果推回 View | 不能持有 UI 控件细节、不能直接绑定 Unity 输入 API |

### 1.2 信息流

```
        ┌──────────────┐  事件   ┌────────────────┐
        │     View     │────────▶│   Presenter    │
        │ (MonoBeh.)   │         │   (纯 C#       │
        │              │◀────────│   或 Mono)     │
        └──────────────┘  指令    └────────────────┘
                                          │ 读/写
                                          ▼
                                  ┌────────────────┐
                                  │     Model      │
                                  │   (纯 C#)      │
                                  └────────────────┘
```

**关键约束:View ↔ Model 之间没有箭头。** 这是 MVP 与 MVC 最大的区别——MVC 中 View 可以直接观察 Model,MVP 中所有交互都经过 Presenter。

### 1.3 与 MVC、MVVM 的差异

| 模式 | View 是否知道 Model | 中间层名 | 中间层与 View 的耦合 |
|------|---------------------|----------|----------------------|
| MVC | 知道 | Controller | Controller 选择 View |
| MVP | **不知道** | Presenter | Presenter 持有 View 接口引用 |
| MVVM | 不知道 | ViewModel | View 通过数据绑定订阅 ViewModel |

### 1.4 为什么 Unity 项目特别需要 MVP

`MonoBehaviour` 天生混淆了"视图"与"控制":一个 `Player` 脚本既绑了 Transform,又监听键盘,又调用网格判定,又跑 DOTween 动画。这种"上帝组件"导致:

- 单元测试必须开 Unity Test Runner,慢且依赖场景
- 一份 `Player.cs` 改一行可能同时引发逻辑 bug + 动画 bug + 输入 bug
- 切换关卡时单例污染、动画悬挂、事件订阅泄漏

MVP 把这些职责拆开,让 Unity 部分尽可能"哑",纯 C# 部分尽可能"密"。

---

## 二、本项目的 MVP 结构对照

### 2.1 文件分布

```
Assets/Scripts/
├── Model/                # 纯 C#,无 Unity 依赖(Vector3 除外)
│   ├── GridEntity.cs     # 实体数据(ID、位置、旋转、占据格)
│   ├── GameBoard.cs      # 全实体注册表 + 查询 + 快照
│   ├── SpatialGrid.cs    # 空间哈希索引
│   ├── MovePlanner.cs    # 移动规划算法(碰撞、推动)
│   ├── MoverModel.cs     # 移动者包装(临时态:PlannedMove)
│   ├── CommandStack.cs   # Undo/Redo 栈
│   ├── MoveCommand.cs    # 一次移动的封装(Execute/Undo)
│   ├── SnapshotCommand.cs# 状态快照命令
│   └── IGridQuery.cs     # 查询接口(便于 Mock 测试)
│
├── View/                 # MonoBehaviour,只做视觉
│   ├── MoverView.cs      # Transform 同步
│   └── PlayerView.cs     # 翻滚动画
│
└── Controller/           # Presenter 层
    ├── GamePresenter.cs       # 核心协调器
    └── PlayerInputController.cs # 输入采集
```

### 2.2 角色对照(逐行索引)

#### Model 层

`GridEntity.cs:9-82` —— 纯数据载体:

```csharp
public class GridEntity {
    public string Id;
    public Vector3 Position;
    public Vector3 Rotation;
    public List<Vector3Int> TileOffsets;
    public bool IsPlayer;
    public bool IsStatic;

    public IEnumerable<Vector3Int> OccupiedCells { get { /* ... */ } }
    public void MoveBy(Vector3 delta) { Position += delta; }
}
```

注意整个类**没有继承 `MonoBehaviour`**,可以在普通 NUnit 测试里实例化。

`GameBoard.cs:9-201` —— 实体注册表 + 业务查询:

```csharp
public class GameBoard {
    public void RegisterEntity(GridEntity entity);
    public bool WallIsAtPos(Vector3Int cell);
    public List<GridEntity> GetMoversAtPos(Vector3Int cell);
    public bool GroundBelow(GridEntity entity);
    public Dictionary<string, EntityTransformSnapshot> CreateSnapshot();
    public void RestoreSnapshot(Dictionary<string, EntityTransformSnapshot> snap);
}
```

`MovePlanner.cs:11-189` —— 业务规则:能否移动、连锁推动谁:

```csharp
public bool TryPlanMove(MoverModel mover, Vector3 moveDelta, Direction dir,
                        out PlannedMoveResult result)
```

`MovePlanner` 接受 `IGridQuery` 而不是具体 `GameBoard`(`MovePlanner.cs:13-20`),这是依赖倒置——使得测试时能注入 `MockGridQuery`。

#### View 层

`MoverView.cs:9-76` —— 唯一职责是把 Model 位置同步到 Transform:

```csharp
public class MoverView : MonoBehaviour {
    private GridEntity model;
    public void SyncToModel() {
        transform.position = model.Position;
        transform.eulerAngles = model.Rotation;
    }
}
```

注意它**没有任何**移动判定、网格查询、输入逻辑。

`PlayerView.cs:9-70` —— 翻滚动画专用:

```csharp
public void AnimateRoll(Vector3 direction, float duration, Ease ease,
                        System.Action onComplete);
```

接受外部传入的 `direction`/`duration`/`ease`,不自己决定何时播。

#### Presenter 层

`GamePresenter.cs:18-342` —— 把上面三层串起来。核心循环在 `HandleMoveRequested`(`GamePresenter.cs:148-164`):

```csharp
void HandleMoveRequested(Vector3Int direction) {
    if (blockInput || isMoving) return;

    var playerEntity = board.GetPlayer();
    var playerModel = new PlayerModel(playerEntity);
    Direction dir = DirectionUtils.CheckDirection(direction);

    if (movePlanner.TryPlanMove(playerModel, direction, dir, out var plannedMove)) {
        ExecutePlannedMove(plannedMove, false);  // 创建 Command + 动画
    }
}
```

`PlayerInputController.cs:10-126` —— 输入采集器,只发事件不动状态:

```csharp
public System.Action<Vector3Int> OnMoveRequested;
public System.Action OnUndoRequested;
public System.Action OnResetRequested;
```

Presenter 通过 `+=` 订阅这些事件(`GamePresenter.cs:62-67`),完全不关心键位是 WASD 还是手柄。

### 2.3 一次移动的完整数据流

```
[1] 玩家按方向键
    └─▶ PlayerInputController.Update() 读取 Input.GetAxisRaw
        └─▶ inputBuffer.Add(dir)
            └─▶ OnMoveRequested?.Invoke(dir)   ← View 抛事件

[2] GamePresenter.HandleMoveRequested(dir)
    └─▶ movePlanner.TryPlanMove(...)   ← Presenter 调 Model 算逻辑
        └─▶ 返回 PlannedMoveResult { entityId → delta }

[3] new MoveCommand(board, plannedMove)
    └─▶ commandStack.Execute(command)
        └─▶ command.Execute()
            └─▶ board.CreateSnapshot()       ← Model 自记快照
            └─▶ board.MoveEntity(...)         ← Model 改状态

[4] foreach (entityId, delta) in plannedMove:
    └─▶ view = FindViewForEntity(entityId)
    └─▶ view.transform.DOMove(...)            ← Presenter 驱动 View 动画

[5] OnAnimationComplete (callback)
    └─▶ animatingCount-- → 0
        └─▶ OnMoveCycleComplete()
            └─▶ 检查下落 → 递归 ExecutePlannedMove
            └─▶ events.onMoveComplete?.Invoke()
```

注意第 [3] 步:**View 完全没有参与决策**,只在第 [4] 步被动地播动画。这就是"Passive View"形态。

---

## 三、如何实现一个 MVP 模块(实操步骤)

下面以"实现一个新的可推动盒子"为例,演示从零落地 MVP 的完整步骤。

### Step 1:先写 Model(零 Unity 依赖)

```csharp
// Assets/Scripts/Model/CrateEntity.cs
public class CrateEntity : GridEntity {
    public bool IsOnTarget;

    public CrateEntity(string id) : base(id, "Crate") {
        IsStatic = false;
        IsPlayer = false;
        IsOnTarget = false;
    }
}
```

**自检:** 这个文件能不能脱离 Unity 编译?如果 `using UnityEngine` 之外引入了 `MonoBehaviour`、`GameObject`、`Transform`、`Time`、`Input`,就走偏了。

### Step 2:扩展 Model 的业务规则

在 `GameBoard` 或单独的服务类里加规则。**不要把规则塞到 `CrateEntity` 内部**——保持实体是数据。

```csharp
// Assets/Scripts/Model/GameBoard.cs (扩展)
public bool AllCratesOnTargets() {
    return entities.Values
        .OfType<CrateEntity>()
        .All(c => c.IsOnTarget);
}
```

### Step 3:写测试(此时不需要 Unity)

```csharp
// Tests/CrateRulesTests.cs
[Test]
public void AllCratesOnTargets_ReturnsTrue_WhenAllCovered() {
    var board = new GameBoard();
    var crate = new CrateEntity("c1") { IsOnTarget = true };
    board.RegisterEntity(crate);

    Assert.IsTrue(board.AllCratesOnTargets());
}
```

如果上面的测试能跑过,说明 Model 真的纯 C#。如果链接器报错说要 `UnityEngine.dll`,Model 就有问题。

### Step 4:写 View(只画不算)

```csharp
// Assets/Scripts/View/CrateView.cs
public class CrateView : MonoBehaviour {
    [SerializeField] private Renderer rend;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color onTargetColor;

    private CrateEntity model;

    public void Bind(CrateEntity entity) { model = entity; }

    public void RefreshVisual() {
        if (model == null) return;
        transform.position = model.Position;
        rend.material.color = model.IsOnTarget ? onTargetColor : normalColor;
    }
}
```

**自检:** View 里出现 `if (canMove)`、`if (hasWall)` 这种判断,就走偏了——视觉刷新不需要知道"为什么变了"。

### Step 5:在 Presenter 里串联

```csharp
// 扩展 GamePresenter.cs
private List<CrateView> crateViews = new List<CrateView>();

void OnMoveCycleComplete() {
    // ...原有代码

    UpdateCrateTargetStates();   // 检查并更新箱子状态
    RefreshAllCrateViews();      // 刷新视图

    if (board.AllCratesOnTargets()) {
        events.onLevelComplete?.Invoke(currentLevelName);
    }
}

void UpdateCrateTargetStates() {
    foreach (var crate in board.GetEntitiesOfType<CrateEntity>()) {
        crate.IsOnTarget = board.HasTargetAt(Vector3Int.RoundToInt(crate.Position));
    }
}

void RefreshAllCrateViews() {
    foreach (var view in crateViews) view.RefreshVisual();
}
```

### Step 6:用事件解耦输入(若有)

如果新功能涉及输入(比如按键 R 标记目标),**不要**在 Presenter 里写 `Input.GetKeyDown`——加到 `PlayerInputController`:

```csharp
// PlayerInputController.cs
public System.Action OnMarkTargetRequested;

void Update() {
    // ...
    if (Input.GetKeyDown(KeyCode.M)) OnMarkTargetRequested?.Invoke();
}
```

```csharp
// GamePresenter.cs - Start()
inputController.OnMarkTargetRequested += HandleMarkTarget;
```

---

## 四、五条落地铁律

### 4.1 Model 永远不引用 View

```csharp
// 错误示范
public class GridEntity {
    public Transform visualRef;   // ← 把 View 引用塞进 Model,死路
}

// 正确做法
public class GridEntity {
    public string Id;             // ← View 拿 Id 来找自己
}
```

### 4.2 View 永远不写规则

```csharp
// 错误示范(在 MoverView 里)
void Update() {
    if (Input.GetKeyDown(KeyCode.Space) && !IsBlockedByWall()) {
        Move(Vector3.right);
    }
}

// 正确做法
void Update() {
    SyncToModel();  // 仅此而已
}
```

### 4.3 Presenter 不持 UI 控件细节

```csharp
// 错误示范
public class GamePresenter : MonoBehaviour {
    [SerializeField] Button undoButton;   // ← Presenter 不该知道有按钮

    void Start() {
        undoButton.onClick.AddListener(DoUndo);
    }
}

// 正确做法 — 用接口隔离
public interface IUndoView {
    event System.Action UndoClicked;
}

public class GamePresenter : MonoBehaviour {
    private IUndoView undoView;
    void Start() { undoView.UndoClicked += DoUndo; }
}
```

### 4.4 用事件代替直接调用,降低 Presenter 与 View 的耦合方向

| 方向 | 推荐方式 |
|------|----------|
| View → Presenter | 事件(`Action`、`UnityEvent`) |
| Presenter → View | 直接方法调用(`view.PlayAnimation()`) |
| Presenter → Model | 直接方法调用 |
| Model → Presenter | 事件或返回值,**不要回调 View** |

### 4.5 一个场景只能有一个事实源

每个状态字段(位置、生命值、道具数)只能有一个权威来源:

- 位置:`GridEntity.Position` 是真值,`Transform.position` 只是它的视觉副本
- 不要在 Presenter 里再缓存一份 `lastPosition`——需要时去 Model 拿

本项目当前违背这一条(`Game.cs:473-507` 同时维护 transform 和 board,见 `ARCHITECTURE_REVIEW.md` §3.4),是隐患的根源。

---

## 五、常见反模式与排查

### 5.1 "Presenter 越长越像 Game"

症状:`GamePresenter.cs` 超过 800 行,既管移动又管 UI 又管音效又管存档。

破法:按子领域拆 Presenter。本项目可考虑:

- `MovementPresenter`:负责移动循环
- `UIPresenter`:负责 HUD、菜单
- `AudioPresenter`:负责事件→音效映射

每个子 Presenter 订阅 `GameServices.Instance.Events` 的子集即可,互不感知。

### 5.2 "View 偷偷调 Model"

症状:`MoverView.Update()` 里写了 `GameBoard.instance.MoveEntity(...)`。

破法:加个 lint/搜索规则——`Assets/Scripts/View/` 下任何文件不允许出现 `GameBoard`、`MovePlanner`、`CommandStack`。

### 5.3 "Model 里冒出 Unity 协程"

症状:`GameBoard` 里写了 `IEnumerator MoveAfterDelay(...)`、`yield return new WaitForSeconds(0.5f)`。

破法:协程是 View 层的玩意,把"延迟"作为 Presenter 用 DOTween 或 `IEnumerator` 调度的细节,不让 Model 感知时间。

### 5.4 "Presenter 直接 new MonoBehaviour"

症状:`new MoverView()` 或 `gameObject.AddComponent<MoverView>()` 出现在 Presenter。

破法:用工厂或对象池。比如 `IMoverViewFactory.Create(GridEntity)`,Presenter 通过接口拿 View,生产环境的工厂从 Prefab 实例化,测试环境的工厂返回 Mock。

---

## 六、本项目落地建议(基于评审结论)

按照 §1-5 的标准,本项目当前 MVP 结构:

| 维度 | 现状 | 建议 |
|------|------|------|
| Model 纯净度 | ✅ 已达成 | 删除 `MoverModel`(职责重复),逻辑合并到 `GridEntity` + `MovePlanner` |
| View 被动性 | ⚠️ `MoverView.Update` 每帧同步 | 改为事件驱动,Presenter 在动画结束后主动调 `SyncToModel` |
| Presenter 单一性 | ⚠️ `GamePresenter` 承担过多 | 拆分:Movement、Fall、Animation、Input 四块 |
| 单一事实源 | ❌ Transform 与 Board.Position 双写 | 决定 `GameBoard` 为唯一真值,移除 `Game.cs` 中的 `SyncTransformsToBoard` |
| Mock 可测性 | ⚠️ 有接口但无测试 | 至少为 `MovePlanner` 写 5 个用例:墙阻挡、推单/双箱、循环推、磁力联动 |

---

## 七、参考代码索引

| 学习目标 | 推荐阅读文件 |
|----------|--------------|
| 看一个标准 Model | `Assets/Scripts/Model/GridEntity.cs` |
| 看 Model 层的服务 | `Assets/Scripts/Model/GameBoard.cs` |
| 看接口隔离 | `Assets/Scripts/Model/IGridQuery.cs` + `GameBoardGridQuery.cs` |
| 看 Command 模式 | `Assets/Scripts/Model/CommandStack.cs` + `MoveCommand.cs` |
| 看一个被动 View | `Assets/Scripts/View/MoverView.cs` |
| 看 Presenter 串联 | `Assets/Scripts/Controller/GamePresenter.cs` |
| 看输入解耦 | `Assets/Scripts/Controller/PlayerInputController.cs` |
| 看 Service Locator | `Assets/Scripts/Core/GameServices.cs` |

---

## 八、一句话记忆

> **Model 不知道屏幕,View 不知道规则,Presenter 把它们粘起来——并且 Presenter 自己也不直接读输入,输入由 Input 子系统抛事件给 Presenter。**

这一句话覆盖了 MVP 在游戏中的全部要害。
