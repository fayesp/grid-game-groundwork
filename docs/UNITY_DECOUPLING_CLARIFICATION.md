# "与 Unity 解耦"的澄清:到底能不能在 GameObject 上挂脚本?

## 元信息

- 文档目的: 消除"解耦 = 不用 MonoBehaviour"的误解,明确各层与 Unity 的合法关系
- 配套阅读: `docs/MVP_PATTERN_GUIDE.md`

---

## 一、一句话结论

**可以挂,但要挂对脚本——Model 不挂,View 必须挂,Presenter 选择性挂。**

"与 Unity 解耦"说的是**游戏逻辑(规则、状态)不依赖 Unity 的类型和生命周期**,不是说**视觉表现也不许用 Unity**。

---

## 二、三层与 Unity 的关系对照表

| 层级 | 是否可以是 MonoBehaviour | 是否可以用 Transform | 是否可以用 GameObject | 是否可以用 Input/Time | 示例 |
|------|------------------------|--------------------|---------------------|---------------------|------|
| **Model** | ❌ 绝对不行 | ❌ | ❌ | ❌ | `GridEntity`、`GameBoard`、`MovePlanner` |
| **View** | ✅ 必须是 | ✅ 核心职责 | ✅ 挂载宿主 | ❌(只读输入发给 Presenter) | `MoverView`、`PlayerView` |
| **Presenter** | ✅ 通常是(为了场景生命周期) | ⚠️ 仅用于发现场景对象 | ⚠️ 仅用于创建/查找 GameObject | ❌ | `GamePresenter` |

---

## 三、正确做法图解

### 3.1 一个 Mover 在场景中的真实结构

```
场景中的 GameObject (玩家方块)
├── Transform (位置、旋转)
├── MeshRenderer + BoxCollider (渲染、碰撞)
├── MoverView.cs   ← View 层,MonoBehaviour ✅
│   └── 字段: GridEntity model
│   └── 行为: 每帧把 model.Position 同步到 Transform
│
├── PlayerView.cs  ← View 层,MonoBehaviour ✅
│   └── 行为: DOTween 翻滚动画
│
└── (遗留的) Mover.cs ← ❌ 错误: 这里不该有逻辑
```

**注意:** `MoverView` 是挂在 GameObject 上的,但它**不决定**自己该往哪走,它只**执行**别人告诉它的位置。

### 3.2 Model 在哪里?

```csharp
// 这个对象不在场景中!
// 它由 GamePresenter 在内存里 new 出来
GridEntity playerEntity = new GridEntity("player_1", "Player") {
    Position = new Vector3(0, 0, 0),
    IsPlayer = true
};
```

`GridEntity` 不是 MonoBehaviour,没有 `transform`,没有 `gameObject`,不在 Hierarchy 面板里。它就是内存里的一个 C# 对象。

### 3.3 Presenter 如何配对

```csharp
// GamePresenter.cs:98-127 ( DiscoverSceneEntities )
void DiscoverSceneEntities() {
    var movers = FindObjectsOfType<Mover>();
    foreach (var mover in movers) {
        // 1. 从场景 Mover 的位置创建纯 C# Model
        string id = mover.GetInstanceID().ToString();
        var entity = new GridEntity(id, "Mover") {
            Position = mover.transform.position,   // ← 只读一次,做初始化
            IsPlayer = mover.isPlayer
        };
        board.RegisterEntity(entity);

        // 2. 找到/创建 View 组件
        var view = mover.GetComponent<MoverView>();
        if (view == null)
            view = mover.gameObject.AddComponent<MoverView>();

        // 3. 把 Model 交给 View
        view.Model = entity;
    }
}
```

流程:
1. Presenter 在场景里找到一个视觉对象(MonoBehaviour)
2. Presenter 从它的位置**克隆**出一份纯 C# 数据(Model)
3. Presenter 把 Model 塞给 View
4. 从此**逻辑只操作 Model,视觉只读 Model**

---

## 四、"解耦"具体解的是什么?

很多人把"解耦"理解成"不用 Unity",这是错的。正确理解:

### 4.1 解耦的是"规则",不是"显示"

```csharp
// ❌ 错误: 规则写在了 View(MonoBehaviour) 里
public class Mover : MonoBehaviour {
    void Update() {
        if (Input.GetKeyDown(KeyCode.W)) {
            if (!WallInFront()) {        // ← 碰撞判定!
                transform.position += Vector3.forward;  // ← 状态修改!
            }
        }
    }
}
```

这个 `Mover` 既管输入,又管碰撞判定,又改 Transform。**三个职责混在一起。**

```csharp
// ✅ 正确: View 只做同步
public class MoverView : MonoBehaviour {
    void Update() {
        if (model != null && !isAnimating) {
            transform.position = model.Position;   // ← 只读 Model,不改规则
        }
    }
}
```

```csharp
// ✅ 正确: 规则在纯 C# Model/Presenter 里
public class MovePlanner {
    public bool TryPlanMove(MoverModel mover, Vector3 delta, Direction dir, out PlannedMoveResult result) {
        // 纯 C#,没有 MonoBehaviour,没有 Transform
        // 判定能不能走、推谁、会不会循环推
    }
}
```

**"解耦" = 把"能不能走"从 MonoBehaviour 里抽出来,变成纯 C# 类。**

### 4.2 解耦的是"生命周期",不是"存在"

MonoBehaviour 有 `Awake`/`Start`/`Update`/`OnDestroy`,这些生命周期钩子会绑架你的设计:

```csharp
// ❌ 被生命周期绑架
public class Game : MonoBehaviour {
    void Awake() { instance = this; }     // ← 单例在生命周期里初始化
    void Update() { CheckInput(); }        // ← 每帧自动跑,无法控制
    void OnDestroy() { SaveState(); }      // ← 销毁时自动存,难以测试
}
```

```csharp
// ✅ 生命周期只给 View/Presenter,Model 无生命周期
public class GameBoard {        // 纯 C#,没有 Awake/Update
    public void RegisterEntity(GridEntity entity) { ... }
    public bool WallIsAtPos(Vector3Int pos) { ... }
}
```

**"解耦" = Model 没有生命周期,谁创建它、何时调用它,完全由 Presenter 控制。**

### 4.3 解耦的是"引擎依赖",不是"引擎使用"

| 层面 | 是否解耦 | 说明 |
|------|--------|------|
| 不用 `Transform` | 是 | Model 不持有 Transform 引用 |
| 不用 `Vector3` | **否** | `Vector3` 是纯值类型,不依赖引擎生命周期,可以用 |
| 不用 `GameObject.Find` | 是 | Model 不搜索场景 |
| 不用 `Input.GetKey` | 是 | 输入在 InputController 层,Model 不知道键位 |
| 不用 `Time.deltaTime` | 是 | Model 不知道帧率 |
| 不用 `MonoBehaviour` | 是 | Model 不继承 MonoBehaviour |

**注意:** `Vector3`/`Quaternion`/`Color` 虽然在 `UnityEngine` 命名空间里,但它们是**纯 struct 值类型**,不依赖引擎运行时。把它们当 C# 标准库用没问题。

---

## 五、常见反模式:"为了解耦而解耦"

### 5.1 反模式 A: 把所有 MonoBehaviour 都删了,用反射绑定

```csharp
// ❌ 过度设计
public class MoverView {
    // 不用 MonoBehaviour!
    // 用反射每帧查找场景中对应的 Transform...
}
```

**问题:** Unity 的场景系统就是围绕 GameObject + MonoBehaviour 设计的。硬要绕过它,代码复杂度暴增,性能更差。

### 5.2 反模式 B: Model 里存 GameObject 引用"只读"

```csharp
// ❌ 伪装的耦合
public class GridEntity {
    public GameObject VisualRef;   // ← "我只读,不改"
}
```

**问题:**
- 测试时仍需构造 GameObject,不能纯 C# 跑
- 一旦有人忍不住写 `VisualRef.transform.position = ...`,防线就破了
- 序列化时 GameObject 引用无法持久化

### 5.3 反模式 C: Presenter 不做 MonoBehaviour,导致场景管理困难

```csharp
// ❌ 给自己挖坑
public class GamePresenter {   // 纯 C# 类
    // 谁负责调用它的 Init?谁负责在场景切换时清理?
    // 没有 OnDestroy,订阅泄漏了都不知道
}
```

**问题:** Presenter 需要知道场景生命周期(初始化、清理),做成 MonoBehaviour 是合理的。不要矫枉过正。

---

## 六、回到本项目的具体落地

### 6.1 当前正确示范

```csharp
// ✅ Model: 纯 C#,不在场景中
// Assets/Scripts/Model/GridEntity.cs
public class GridEntity {
    public string Id;
    public Vector3 Position;   // Vector3 是值类型,可用
    public bool IsPlayer;
}
```

```csharp
// ✅ View: MonoBehaviour,挂在 GameObject 上
// Assets/Scripts/View/MoverView.cs
public class MoverView : MonoBehaviour {
    [SerializeField] private string entityId;
    private GridEntity model;

    void Update() {
        if (model == null || isAnimating) return;
        transform.position = model.Position;   // 只读 Model
    }
}
```

```csharp
// ✅ Presenter: MonoBehaviour,为了场景生命周期
// Assets/Scripts/Controller/GamePresenter.cs
public class GamePresenter : MonoBehaviour {
    private GameBoard board;   // 纯 C# Model
    private List<MoverView> moverViews;  // View 引用

    void Awake() {
        board = new GameBoard();   // new 出 Model
        DiscoverSceneEntities();    // 配对 View 和 Model
    }
}
```

### 6.2 当前错误示范(遗留代码)

```csharp
// ❌ Model + View + Controller 全混在一起
// Assets/Scripts/Logic/Entity/Mover.cs
public class Mover : MonoBehaviour {
    public List<Tile> tiles;           // 数据
    private Vector3 plannedMove;       // 状态

    public bool TryPlanMove(Vector3 MoveV3, Direction Dir) {
        // 碰撞判定(规则)
        // 推动传播(规则)
        // 直接改 transform.position(视图)
    }

    public bool ExecuteLogicalMove() {
        transform.position = Pos() + PlannedMove;   // 视图 + 状态混在一起
    }
}
```

这个 `Mover` 同时是:
- 数据容器(`tiles`、`plannedMove`)
- 规则引擎(`TryPlanMove`、`CanMoveToward`)
- 视觉对象(直接改 `transform.position`)
- 输入接收器(从 `Player` 接收移动指令)

**这就是 MVP 要拆的东西。** 拆完后:
- `GridEntity` 取代 `Mover` 的数据部分
- `MovePlanner` 取代 `Mover` 的规则部分
- `MoverView` 取代 `Mover` 的视觉部分
- `PlayerInputController` + `GamePresenter` 取代输入流

---

## 七、实施检查清单

当你写一个类时,问自己三个问题:

### 问题 1: 这个类需要出现在 Unity 场景里吗?

- **需要** → 做成 MonoBehaviour(View 或 Presenter)
- **不需要** → 纯 C# 类(Model)

### 问题 2: 这个类包含游戏胜负、碰撞、推动、计分等规则吗?

- **包含** → 必须是纯 C# Model,不能是 MonoBehaviour
- **不包含,只画东西** → 可以是 MonoBehaviour View

### 问题 3: 这个类测试时需要开 Unity 吗?

- **需要** → 大概率是 View 或 Presenter,保持薄层
- **不需要** → 大概率是 Model,保持厚层(核心业务)

---

## 八、总结

| 误解 | 真相 |
|------|------|
| "解耦 = 不用 MonoBehaviour" | View 必须是 MonoBehaviour,Presenter 通常也是 |
| "解耦 = 不能用 UnityEngine 命名空间" | `Vector3`/`Vector3Int` 等值类型可以用 |
| "解耦 = 不能挂在 GameObject 上" | View 就是挂在 GameObject 上的,但它只负责同步 |
| "解耦 = 所有代码都要纯 C#" | 只要求 Model 纯 C#,View/Presenter 可以用 Unity |

**正确理解:**

> **Model 是大脑,活在内存里,不挂场景。**
> **View 是手脚,挂在 GameObject 上,只执行不动脑。**
> **Presenter 是神经中枢,挂在场景里协调,但不替 Model 想规则。**

这就是本项目的解耦目标。
