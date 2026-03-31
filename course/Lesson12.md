# 第12课:事件系统

> **难度:** ⭐⭐ | **预计时间:** 40分钟 | **前置要求:** [第11课](./Lesson11.md)

---

## 学习目标

- 掌握C#事件模式
- 理解游戏内通信机制
- 学会使用事件解耦代码

---

## 核心文件

- `Logic/Event/EventManager.cs`

---

## 核心内容

### 1. 事件定义

```csharp
// 位于 Logic/Event/EventManager.cs
public static class EventManager
{
    // ========== 游戏生命周期事件 ==========

    // 关卡开始事件
    public static event Action onLevelStarted;

    // 移动完成事件
    public static event Action onMoveComplete;

    // ========== 玩家交互事件 ==========

    // 推动物体事件
    public static event Action onPush;

    // 撤销事件
    public static event Action onUndo;

    // 重置事件
    public static event Action onReset;

    // ========== 带参数的事件 ==========

    // 玩家移动事件(带方向参数)
    public static event Action<Direction> onPlayerMove;

    // 物体移动事件(带Mover参数)
    public static event Action<Mover> onMoverMoved;

    // 关卡完成事件(带关卡索引)
    public static event Action<int> onLevelComplete;
}
```

**事件类型:**

| 类型 | 语法 | 适用场景 |
|------|------|----------|
| 无参数 | `event Action` | 简单通知 |
| 单参数 | `event Action<T>` | 传递数据 |
| 多参数 | `event Action<T1,T2>` | 传递多个数据 |
| 自定义参数 | `event Action<EventArgs>` | 复杂数据结构 |

### 2. 事件触发

```csharp
// ========== 在Game.cs中触发 ==========

// 关卡加载完成时
public void OnLevelLoaded()
{
    EventManager.onLevelStarted?.Invoke();
}

// 移动完成时
public void CompleteMove()
{
    State.OnMoveComplete();
    EventManager.onMoveComplete?.Invoke();
}

// 执行撤销时
void DoUndo()
{
    // ...撤销逻辑...
    EventManager.onUndo?.Invoke();
}

// 执行重置时
void DoReset()
{
    // ...重置逻辑...
    EventManager.onReset?.Invoke();
}

// ========== 在移动逻辑中触发 ==========

// 推动物体时
if (isPushing)
{
    EventManager.onPush?.Invoke();
}

// 玩家移动时
EventManager.onPlayerMove?.Invoke(Direction.Right);
```

**`?.Invoke()`的作用:**
- `?.` 是空条件运算符
- 如果事件没有订阅者,跳过调用
- 避免NullReferenceException

### 3. 事件订阅

```csharp
public class UIManager : MonoBehaviour
{
    void OnEnable()
    {
        // ========== 订阅事件 ==========
        EventManager.onLevelStarted += OnLevelStarted;
        EventManager.onMoveComplete += OnMoveDone;
        EventManager.onPush += OnObjectPushed;
        EventManager.onReset += OnLevelReset;
    }

    void OnDisable()
    {
        // ========== 取消订阅 ==========
        EventManager.onLevelStarted -= OnLevelStarted;
        EventManager.onMoveComplete -= OnMoveDone;
        EventManager.onPush -= OnObjectPushed;
        EventManager.onReset -= OnLevelReset;
    }

    // ========== 事件处理方法 ==========

    void OnLevelStarted()
    {
        Debug.Log("关卡开始!");
        // 初始化UI...
    }

    void OnMoveDone()
    {
        // 更新移动计数UI
        moveCount++;
        UpdateUI();
    }

    void OnObjectPushed()
    {
        // 播放推动音效
        AudioManager.PlayPushSound();
    }

    void OnLevelReset()
    {
        // 重置UI状态
        moveCount = 0;
        UpdateUI();
    }
}
```

**订阅/取消订阅规则:**
- ✅ OnEnable中订阅,OnDisable中取消
- ✅ 使用`+=`订阅,使用`-=`取消
- ❌ 不要在Start中订阅(可能错过早期事件)
- ❌ 不要忘记取消订阅(内存泄漏)

### 4. 带参数的事件

```csharp
// ========== 定义带参数的事件 ==========
public static event Action<Direction> onPlayerMove;

// ========== 触发时传递参数 ==========
EventManager.onPlayerMove?.Invoke(Direction.Up);

// ========== 订阅时接收参数 ==========
void OnPlayerMove(Direction dir)
{
    Debug.Log($"玩家向{dir}移动");
}

EventManager.onPlayerMove += OnPlayerMove;
```

### 5. 自定义事件参数

```csharp
// ========== 定义事件参数类 ==========
public class MoveEventArgs
{
    public Mover mover;          // 移动的物体
    public Vector3 from;         // 起始位置
    public Vector3 to;           // 目标位置
    public Direction direction;  // 移动方向
    public bool isPush;          // 是否是推动

    public MoveEventArgs(Mover m, Vector3 f, Vector3 t, Direction d, bool push)
    {
        mover = m;
        from = f;
        to = t;
        direction = d;
        isPush = push;
    }
}

// ========== 事件定义 ==========
public static event Action<MoveEventArgs> onMoverMoved;

// ========== 触发 ==========
var args = new MoveEventArgs(this, oldPos, newPos, moveDir, isPushing);
EventManager.onMoverMoved?.Invoke(args);

// ========== 订阅 ==========
void OnMoverMoved(MoveEventArgs args)
{
    Debug.Log($"{args.mover.name}从{args.from}移动到{args.to}");
}
```

---

## 事件流程图

```
Game.cs                    EventManager                 其他脚本
   │                           │                           │
   │ MoveStart()               │                           │
   │ ──────────────────────►   │                           │
   │                           │                           │
   │ CompleteMove()            │                           │
   │ ──────────────────────►   │ onMoveComplete?.Invoke()  │
   │                           │ ──────────────────────►   │
   │                           │                           │ OnMoveDone()
   │                           │                           │
   │ DoUndo()                  │                           │
   │ ──────────────────────►   │ onUndo?.Invoke()          │
   │                           │ ──────────────────────►   │
   │                           │                           │ OnUndo()
```

---

## 设计优势

| 优势 | 说明 |
|------|------|
| 解耦 | 发布者不需要知道订阅者 |
| 灵活 | 可动态添加/移除监听器 |
| 扩展 | 新功能只需订阅事件 |
| 简洁 | 减少直接引用 |

**解耦示例:**

```csharp
// ❌ 没有事件: 强耦合
public class Game
{
    public UIManager uiManager;
    public AudioManager audioManager;

    void CompleteMove()
    {
        uiManager.UpdateMoveCount();  // 必须知道UIManager
        audioManager.PlayMoveSound(); // 必须知道AudioManager
    }
}

// ✅ 使用事件: 松耦合
public class Game
{
    void CompleteMove()
    {
        EventManager.onMoveComplete?.Invoke();  // 不需要知道谁在监听
    }
}
```

---

## 最佳实践

### 正确的空检查

```csharp
// ✅ 正确: 使用?.避免空引用
EventManager.onMoveComplete?.Invoke();

// ❌ 错误: 可能抛出NullReferenceException
EventManager.onMoveComplete();
```

### 正确的订阅管理

```csharp
// ✅ 正确: OnDisable中取消订阅
void OnDisable()
{
    EventManager.onMoveComplete -= OnMoveDone;
}

// ❌ 错误: 忘记取消订阅会导致内存泄漏
// 当对象被销毁时,事件仍然持有引用
```

### 避免在事件处理中触发同一事件

```csharp
// ❌ 危险: 可能导致无限循环
void OnMoveComplete()
{
    // 这会再次触发onMoveComplete
    EventManager.onMoveComplete?.Invoke();
}

// ✅ 安全: 使用标志防止递归
private bool isHandlingEvent = false;

void OnMoveComplete()
{
    if (isHandlingEvent) return;
    isHandlingEvent = true;

    // 处理逻辑...

    isHandlingEvent = false;
}
```

---

## 实践任务

### 任务1:添加新事件

```csharp
// 在EventManager中添加关卡完成事件
public static event Action<int> onLevelComplete;

// 在胜利条件检测中触发
void CheckWinCondition()
{
    if (AllBoxesOnTarget())
    {
        EventManager.onLevelComplete?.Invoke(currentLevelIndex);
    }
}
```

### 任务2:创建音效管理器

```csharp
public class AudioManager : MonoBehaviour
{
    public AudioClip moveSound;
    public AudioClip pushSound;
    public AudioClip winSound;

    private AudioSource audioSource;

    void OnEnable()
    {
        EventManager.onMoveComplete += PlayMoveSound;
        EventManager.onPush += PlayPushSound;
        EventManager.onLevelComplete += PlayWinSound;
    }

    void OnDisable()
    {
        EventManager.onMoveComplete -= PlayMoveSound;
        EventManager.onPush -= PlayPushSound;
        EventManager.onLevelComplete -= PlayWinSound;
    }

    void PlayMoveSound() => audioSource.PlayOneShot(moveSound);
    void PlayPushSound() => audioSource.PlayOneShot(pushSound);
    void PlayWinSound(int level) => audioSource.PlayOneShot(winSound);
}
```

### 任务3:实现移动计数器

```csharp
public class MoveCounter : MonoBehaviour
{
    public Text moveCountText;
    private int moveCount = 0;

    void OnEnable()
    {
        EventManager.onMoveComplete += IncrementCount;
        EventManager.onReset += ResetCount;
        EventManager.onUndo += DecrementCount;
    }

    void OnDisable()
    {
        EventManager.onMoveComplete -= IncrementCount;
        EventManager.onReset -= ResetCount;
        EventManager.onUndo -= DecrementCount;
    }

    void IncrementCount()
    {
        moveCount++;
        UpdateUI();
    }

    void DecrementCount()
    {
        moveCount--;
        UpdateUI();
    }

    void ResetCount()
    {
        moveCount = 0;
        UpdateUI();
    }

    void UpdateUI()
    {
        moveCountText.text = $"移动: {moveCount}";
    }
}
```

---

## 常见事件列表

| 事件 | 触发时机 | 典型用途 |
|------|----------|----------|
| onLevelStarted | 关卡加载完成 | 初始化UI、音效 |
| onMoveComplete | 移动动画完成 | 更新计数、检测胜利 |
| onPush | 推动物体 | 播放音效、特效 |
| onUndo | 执行撤销 | 更新UI |
| onReset | 重置关卡 | 重置UI、特效 |
| onPlayerMove | 玩家移动 | 记录输入、教程 |
| onLevelComplete | 关卡完成 | 播放胜利动画 |

---

## 常见问题

**Q: 事件和委托有什么区别?**
A: 事件是委托的封装,只能在定义类内部触发,外部只能订阅/取消订阅。

**Q: 静态事件会导致内存泄漏吗?**
A: 如果不取消订阅会!确保在OnDisable中取消订阅。

**Q: 多个订阅者的执行顺序?**
A: 不保证顺序!如果需要顺序,考虑使用有序列表或责任链模式。

---

## 关键要点

- ✅ 使用`event Action`定义事件
- ✅ 使用`?.Invoke()`安全触发
- ✅ OnEnable订阅,OnDisable取消订阅
- ✅ 事件解耦发布者和订阅者
- ✅ 带参数事件使用`Action<T>`

---

## 下一课

[第13课:关卡序列化系统](./Lesson13.md)
