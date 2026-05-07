# 为什么本项目用 MVP 而不是 MVVM

## 元信息

- 文档目的: 解释 MVP 与 MVVM 的本质差异,以及在 Unity + 网格回合制游戏场景下为何选 MVP
- 配套阅读: `docs/MVP_PATTERN_GUIDE.md`(MVP 实现指南)

---

## 一、一句话回答

**MVVM 的灵魂是数据绑定基础设施,Unity 原生没有,且本项目的游戏逻辑是离散回合事件——为绑定建一套基础设施的成本远超收益,所以选 MVP。**

下面展开。

---

## 二、MVP 与 MVVM 的本质差异

### 2.1 表面看,二者很像

| | MVP | MVVM |
|---|-----|------|
| Model 知道 View 吗? | 不 | 不 |
| View 知道 Model 吗? | 不 | 不 |
| 中间层名 | Presenter | ViewModel |
| 谁监听用户输入? | View 抛事件 → Presenter 接 | View 抛事件 → ViewModel 接,或绑定到 Command |

**目标都一样:解耦 Model 与 View。** 关键差异不在结构图,在"View 怎么得知 Model 变了"。

### 2.2 本质差异:更新机制

#### MVP — Presenter 主动推

```csharp
void HandleMoveRequested(Vector3Int dir) {
    if (movePlanner.TryPlanMove(...)) {
        commandStack.Execute(command);     // Model 改了
        view.PlayAnimation(delta);          // ← Presenter 显式调用 View
        view.RefreshHealth(model.Health);   // ← 一项一项推
    }
}
```

Presenter **逐字段、逐时机**告诉 View 该刷新什么。

#### MVVM — View 自动拉

```csharp
public class PlayerViewModel {
    public ReactiveProperty<int> Health { get; } = new(100);
    public ReactiveProperty<Vector3> Position { get; } = new();
}

// View 端(伪代码,需要绑定库)
healthBar.Value.BindTo(viewModel.Health);
playerTransform.Position.BindTo(viewModel.Position);
```

ViewModel 改 `Health.Value = 50`,**所有订阅者自动收到**——View 完全不需要被 Presenter 显式触发。这就是数据绑定。

### 2.3 数据绑定要靠什么

MVVM 工作的前提是**有一套反应式基础设施**,通常包含:

1. **可观察属性**:`ReactiveProperty<T>`、`Observable<T>`、`INotifyPropertyChanged`
2. **绑定引擎**:把"属性变化"自动映射到 UI 控件值的中间层
3. **生命周期管理**:订阅、解订、避免内存泄漏(`CompositeDisposable` 等)
4. **绑定语法/标记**:WPF 的 `{Binding Health}`、SwiftUI 的 `$health`、Vue 的 `v-bind`

**这一整套,叫"binding infrastructure"。** 桌面/Web 框架(WPF、Xamarin、SwiftUI、Vue、React)默认带,Unity **不带**。

---

## 三、Unity 的绑定支持现状(2026)

### 3.1 Unity 原生情况

| 子系统 | 绑定支持 |
|--------|----------|
| Transform / GameObject | ❌ 完全没有 |
| 旧 uGUI(`Canvas` + `Image`/`Text`) | ❌ 没有,只有 `UnityEvent` |
| UI Toolkit(UIElements) | ⚠️ 2021+ 起有 SerializedObject 绑定,仅限 Editor;运行时绑定 2023+ 才稳定 |
| Visual Scripting | ⚠️ 节点图绑定,不适合代码项目 |

### 3.2 本项目的具体情况

`CLAUDE.md` 第 7 行明确:
> Built with Unity 2019.4.10f1 LTS.

**Unity 2019.4 没有 UI Toolkit 运行时绑定。** 这意味着:

- 想做 MVVM 必须自带或引入第三方反应式库
- 主流选项:UniRx(老牌、维护放缓)、R3(新一代、需要 .NET Standard 2.1)、MessagePipe + 自写绑定
- Unity 2019.4 是 .NET Standard 2.0,R3 不直接可用,UniRx 还行但有 GC 压力

引入这些库的代价:

1. 团队学习曲线(Rx 操作符 50+ 个)
2. 包体增加 1-3MB
3. 调试困难(异步流的栈追踪比同步差)
4. GC 压力(大量临时对象)

---

## 四、本项目为什么不值得为 MVVM 建基础设施

### 4.1 游戏逻辑是"离散事件"而非"连续状态"

观察 `GamePresenter.cs:148-164`:

```csharp
void HandleMoveRequested(Vector3Int direction) {
    if (movePlanner.TryPlanMove(...)) {
        ExecutePlannedMove(plannedMove, false);
    }
}
```

游戏每帧并没有"100 个属性持续变化",而是**每次玩家按键产生一个明确的事件**:

- 移动前:静止
- 移动中:动画期间锁输入,只有 Transform 在 DOTween 插值
- 移动后:重新静止

这种节奏下,Presenter 主动调 `view.PlayAnimation(delta)` **比 View 订阅 `position.Changed` 更直观**:

| 反应式订阅(MVVM) | 命令式调用(MVP) |
|--------------------|--------------------|
| 订阅了 position 变化,但不知道是"瞬移"还是"应该播翻滚" | Presenter 知道是玩家移动,显式调 `playerView.AnimateRoll(...)` |
| 必须额外发 "MoveType" 信号区分,失去绑定的简洁 | 一行代码搞定 |

**反应式绑定擅长"持续映射",但游戏逻辑要求"语义化触发"。** MVP 在这里更贴合需求。

### 4.2 已有 Command Pattern 解决了"状态变化的可追溯"

MVVM 流派常用 reactive stream 来**记录状态变化轨迹**(便于回放/调试)。
但本项目已经用 `CommandStack`(`Assets/Scripts/Model/CommandStack.cs:7-93`)+ `MoveCommand`(`MoveCommand.cs:8-58`)解决了同一问题:

- 每次移动 → 一个 `ICommand` 入栈
- Undo → 从栈弹出 + 调用 `Undo()`
- Redo → 重新 `Execute()`

这是 Command Pattern 的天然能力,**不需要 ReactiveStream 二次实现**。MVVM 在这点上对本项目是冗余。

### 4.3 视觉同步是"一对一",绑定开销不划算

MVVM 真正发光的场景:

- HUD 上 10 个数字字段全部反应玩家状态
- 表单 50 个输入项双向绑定到设置数据
- 列表虚拟化 + 数据驱动渲染

本项目的视觉:

- 每个 `MoverView` 同步**一个** `Vector3 Position` + 一个 `Vector3 Rotation`
- 没有列表、表单、复杂 HUD
- 同步代码就两行(`MoverView.cs:51-57`):

```csharp
transform.position = model.Position;
transform.eulerAngles = model.Rotation;
```

为这两行建一套绑定基础设施,ROI 极低。

### 4.4 性能:绑定会引入隐藏成本

数据绑定的潜在开销:

- **订阅链遍历**:每次属性变化要遍历观察者列表
- **闭包分配**:Lambda 订阅会触发 GC
- **脏检查**(部分绑定库):每帧扫描属性变化
- **冗余刷新**:绑定容易让一次操作触发多个观察者

Unity 主线程对 GC 极敏感,大量小对象分配会导致 GC spike → 卡帧。
MVP 的命令式调用是**确定性、零额外分配**(忽略 string ID),更适合实时游戏。

### 4.5 团队/工具链熟悉度

- MVP 的"事件 + 方法调用"是 C# 工程师默认技能
- MVVM 需要熟悉 Rx 操作符、订阅生命周期、`Subject<T>`/`BehaviorSubject<T>` 区别
- 调试时,MVP 的栈追踪是同步直链,MVVM 的异步流追踪经常断在 `Subscribe` 里

对 5-10 人小团队,MVP 维护成本更低。

---

## 五、MVVM 真的值得用的场景

不是说 MVVM 错,只是不适合**本项目当前阶段**。下列情况建议引入 MVVM:

| 场景 | 原因 |
|------|------|
| 大量复杂 UI 表单(角色养成、装备系统、商店) | 双向绑定能省 70% 模板代码 |
| HUD 字段数 > 20 且联动复杂(连击数→颜色→动画) | Reactive 链式映射更易维护 |
| 多端共享 ViewModel(Editor 工具 + 运行时 + 测试 mock) | 数据驱动让 View 替换免改逻辑 |
| 已经引入 UniRx/R3 做异步流 | 边际成本接近零,顺手用上 |

---

## 六、混合方案:游戏世界用 MVP,UI 用 MVVM

很多商业项目的做法是**双轨制**:

```
┌─────────────────────────────────────────────────┐
│                  Model 层                       │
│  GameBoard, GridEntity, CommandStack ...       │
└──────────┬──────────────────────────┬───────────┘
           │                          │
           ▼                          ▼
   ┌──────────────┐          ┌────────────────┐
   │ GamePresenter│          │   UIViewModel  │
   │   (MVP)      │          │   (MVVM)       │
   └──────┬───────┘          └────────┬───────┘
          │                            │
          ▼                            ▼
   ┌──────────────┐          ┌────────────────┐
   │  MoverView   │          │  HUD/Settings  │
   │  PlayerView  │          │   UI Toolkit   │
   └──────────────┘          └────────────────┘
```

- **游戏世界**(回合、移动、碰撞):MVP + Command Pattern
- **UI 层**(菜单、HUD、设置):MVVM + Reactive Binding

本项目当前 UI 极简(没有完整 HUD/菜单),所以连 UI 部分都没必要先引入 MVVM。等 UI 复杂度上来再说。

---

## 七、决策记录(ADR-style)

```
状态:    已采纳
日期:    2026-04-30(架构变更日)
决策者:  liuwenjie

背景:
  项目从三层架构迁移,需要选择新的应用层模式。候选 MVP / MVVM。

决定:
  采用 MVP + Command Pattern。

理由:
  1. Unity 2019.4 LTS 无原生绑定支持,引入 UniRx/R3 增加包体和复杂度
  2. 游戏逻辑是离散回合事件,Command Pattern 已天然支持 Undo/Redo
  3. 视觉同步极简(两个 Vector3 字段),绑定 ROI 低
  4. 团队 C# 同步代码经验充足,Rx 学习成本高

后果:
  优点:
    - 上手快,栈追踪直观
    - 性能可控,无隐藏 GC
    - Command 已覆盖回放/撤销需求
  代价:
    - 大量字段 UI 出现时(后期)需要手写刷新代码
    - 未来若引入复杂菜单系统,可能需要补 MVVM 支撑

回顾时机:
  - 引入 UI Toolkit(需先升级到 Unity 2021+ LTS)
  - HUD 字段数 > 15
  - UI 联动复杂度显著上升
```

---

## 八、给团队的实操建议

1. **现在不要引入 UniRx/R3。** 当前架构已能解决问题。
2. **Model 写干净,这是为未来切到任何模式留后路。** 哪怕将来上 MVVM,Model 这一层(`GameBoard`/`GridEntity`/`MovePlanner`)是不变资产。
3. **如果 UI 复杂度上升,优先升级 Unity 到 2022 LTS + UI Toolkit**,再考虑 MVVM,而非给 Unity 2019 打补丁。
4. **绑定基础设施评估**:真要引入时,做小型 spike(2-3 天)验证 GC、调试体验、构建产物大小,再决定。

---

## 九、一句话总结

> **MVP 适合事件驱动的游戏世界,MVVM 适合数据驱动的复杂 UI。本项目当前两者都不复杂,先选成本低的 MVP;未来 UI 膨胀再补 MVVM,模型层不会浪费。**
