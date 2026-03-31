# 第11课:撤销与重置系统

> **难度:** ⭐⭐ | **预计时间:** 40分钟 | **前置要求:** [第10课](./Lesson10.md)

---

## 学习目标

- 理解状态快照机制
- 掌握撤销栈的实现
- 了解状态管理最佳实践

---

## 核心文件

- `Logic/Entity/BaseClass/State.cs`

---

## 核心内容

### 1. 状态数据结构

```csharp
// 单个Mover的状态快照
public struct MoverState
{
    public Mover mover;         // Mover引用
    public Vector3 position;    // 位置快照

    public MoverState(Mover m)
    {
        mover = m;
        position = m.transform.position;
    }

    // 恢复状态
    public void Restore()
    {
        if (mover != null)
        {
            mover.transform.position = position;
        }
    }
}

// 游戏完整状态快照
public struct GameState
{
    public List<MoverState> moverStates;  // 所有Mover的状态
    public int stateIndex;                 // 状态索引

    public GameState(int index)
    {
        moverStates = new List<MoverState>();
        stateIndex = index;
    }
}
```

**为什么使用struct?**
- 值类型,避免GC压力
- 自动复制,无需手动深拷贝
- 适合小型数据结构

### 2. State静态类

```csharp
public static class State
{
    // ========== 撤销栈 ==========
    private static List<GameState> undoStack = new List<GameState>();
    public static int undoIndex = -1;  // 当前位置(-1表示空)

    // ========== 所有Mover引用 ==========
    private static List<Mover> allMovers = new List<Mover>();

    // ========== 最大撤销深度 ==========
    private static int maxUndoDepth = 100;
}
```

**撤销栈结构:**

```
undoStack:
[0] State0 (初始状态) ◄── DoReset()回到这里
[1] State1 (第1次移动后)
[2] State2 (第2次移动后)
[3] State3 (第3次移动后) ◄── undoIndex=3 (当前位置)

DoUndo() → undoIndex-- → 恢复State2
```

### 3. 初始化与注册

```csharp
// 初始化状态系统
public static void Init()
{
    undoStack.Clear();
    undoIndex = -1;
    allMovers.Clear();
}

// 注册Mover
public static void AddMover(Mover mover)
{
    if (!allMovers.Contains(mover))
    {
        allMovers.Add(mover);
    }
}

// 移除Mover
public static void RemoveMover(Mover mover)
{
    allMovers.Remove(mover);
}
```

### 4. 记录状态快照

```csharp
public static void AddToUndoStack()
{
    // ========== 截断分支 ==========
    // 如果当前不在栈顶,移除后面的状态
    if (undoIndex < undoStack.Count - 1)
    {
        int removeCount = undoStack.Count - undoIndex - 1;
        undoStack.RemoveRange(undoIndex + 1, removeCount);
        Debug.Log($"移除了{removeCount}个未来状态");
    }

    // ========== 限制栈深度 ==========
    if (undoStack.Count >= maxUndoDepth)
    {
        undoStack.RemoveAt(0);  // 移除最旧的状态
        undoIndex--;
    }

    // ========== 创建新快照 ==========
    GameState state = new GameState(undoStack.Count);

    foreach (var mover in allMovers)
    {
        if (mover != null)
        {
            state.moverStates.Add(new MoverState(mover));
        }
    }

    // ========== 添加到栈 ==========
    undoStack.Add(state);
    undoIndex = undoStack.Count - 1;

    Debug.Log($"状态快照 #{state.stateIndex} 已保存");
}
```

**分支截断示例:**

```
撤销前:
[0] → [1] → [2] → [3] (当前)
              ↓
           DoUndo()

撤销后:
[0] → [1] → [2] (当前) → [3] (未来,待删除)

新移动:
[0] → [1] → [2] → [4] (新的当前)
              [3] 被删除
```

### 5. 执行撤销

```csharp
public static void DoUndo()
{
    // 检查是否可以撤销
    if (undoIndex <= 0)
    {
        Debug.Log("无法撤销: 已经是最早状态");
        return;
    }

    // 移动索引
    undoIndex--;

    // 恢复状态
    RestoreState(undoStack[undoIndex]);

    Debug.Log($"撤销到状态 #{undoIndex}");
}

private static void RestoreState(GameState state)
{
    foreach (var moverState in state.moverStates)
    {
        if (moverState.mover != null)
        {
            // 恢复位置
            moverState.mover.transform.position = moverState.position;

            // 停止动画
            moverState.mover.Stop();
        }
    }
}
```

### 6. 执行重置

```csharp
public static void DoReset()
{
    if (undoStack.Count == 0)
    {
        Debug.Log("无法重置: 没有保存的状态");
        return;
    }

    // 回到初始状态(索引0)
    undoIndex = 0;
    RestoreState(undoStack[0]);

    Debug.Log("重置到初始状态");
}
```

### 7. 可选:重做功能

```csharp
public static void DoRedo()
{
    if (undoIndex >= undoStack.Count - 1)
    {
        Debug.Log("无法重做: 已经是最新状态");
        return;
    }

    undoIndex++;
    RestoreState(undoStack[undoIndex]);

    Debug.Log($"重做到状态 #{undoIndex}");
}
```

---

## 状态流程图

```
初始状态
    │
    └── AddToUndoStack() ──► [State 0] ◄── DoReset()
            │                     ▲
            │                     │
    玩家移动1                      │
            │                     │
            └── AddToUndoStack() ─┤
                    │             │
                    ▼             │
                [State 1]         │
                    │             │
            玩家移动2              │
                    │             │
                    └── AddToUndoStack()
                            │
                            ▼
                        [State 2] ◄── 当前位置
                            │
                    DoUndo() │
                            ▼
                        [State 1] ◄── 撤销后
                            │
                    DoRedo() │
                            ▼
                        [State 2] ◄── 重做后
```

---

## Game类中的集成

```csharp
// 位于 Game.cs
void Update()
{
    // ========== 撤销(Z键) ==========
    if (Input.GetKeyDown(KeyCode.Z))
    {
        DoUndo();
    }
    // ========== 重置(R键) ==========
    else if (Input.GetKeyDown(KeyCode.R))
    {
        DoReset();
    }
}

void DoUndo()
{
    // 检查条件
    if (State.undoIndex <= 0)
        return;

    // 停止所有动画
    DOTween.KillAll();

    // 如果正在移动,先完成
    if (isMoving)
        CompleteMove();

    // 执行撤销
    State.DoUndo();

    // 刷新游戏状态
    Refresh();

    // 触发事件
    EventManager.onUndo?.Invoke();
}

void DoReset()
{
    // 停止所有动画
    DOTween.KillAll();

    // 执行重置
    State.DoReset();

    // 刷新游戏状态
    Refresh();

    // 触发事件
    EventManager.onReset?.Invoke();
}
```

---

## 实践任务

### 任务1:添加重做(Redo)功能

1. 在State类中添加DoRedo方法
2. 在Game类中添加Y键触发重做
3. 测试撤销→重做→撤销的循环

### 任务2:限制撤销栈深度

```csharp
// 在State类中修改
private static int maxUndoDepth = 50;  // 从100改为50

// 测试: 连续移动51次,观察第1个状态是否被移除
```

### 任务3:记录更多状态

```csharp
// 扩展MoverState
public struct MoverState
{
    public Mover mover;
    public Vector3 position;
    public Quaternion rotation;  // 新增: 旋转
    public bool isActive;         // 新增: 激活状态

    public MoverState(Mover m)
    {
        mover = m;
        position = m.transform.position;
        rotation = m.transform.rotation;
        isActive = m.gameObject.activeSelf;
    }

    public void Restore()
    {
        if (mover != null)
        {
            mover.transform.position = position;
            mover.transform.rotation = rotation;
            mover.gameObject.SetActive(isActive);
        }
    }
}
```

---

## 扩展思考

**Q: 如何优化大量物体的状态存储?**
A: 使用增量快照,只记录变化的部分:
```csharp
public struct DeltaState
{
    public int moverIndex;
    public Vector3 deltaPosition;  // 位置变化量
}
```

**Q: 如何实现跨关卡的状态持久化?**
A: 序列化为JSON保存到文件:
```csharp
public static void SaveToFile(string path)
{
    string json = JsonUtility.ToJson(currentState);
    File.WriteAllText(path, json);
}
```

**Q: 如何支持网络同步?**
A: 使用确定性状态同步,每次状态包含完整的游戏信息。

---

## 常见问题

**Q: 撤销后新移动,旧状态去哪了?**
A: 被移除了(分支截断),撤销栈变成线性结构。

**Q: 为什么撤销栈用List而不是Stack?**
A: List支持索引访问,方便实现重做和重置功能。

**Q: 如何防止内存溢出?**
A: 设置maxUndoDepth限制栈深度,定期清理旧状态。

---

## 关键要点

- ✅ MoverState存储单个物体的状态
- ✅ GameState存储所有物体的完整快照
- ✅ 撤销栈使用List实现
- ✅ 新移动会截断撤销分支
- ✅ 可设置最大撤销深度防止内存溢出

---

## 下一课

[第12课:事件系统](./Lesson12.md)
