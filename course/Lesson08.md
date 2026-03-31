# 第8课:玩家控制 - Player类

> **难度:** ⭐⭐ | **预计时间:** 45分钟 | **前置要求:** [第7课](./Lesson07.md)

---

## 学习目标

- 掌握Player类的设计
- 理解输入缓冲系统
- 了解翻滚动画实现

---

## 核心文件

- `Logic/Entity/Player.cs`

---

## 核心内容

### 1. Player类结构

Player继承自Mover,增加输入处理功能:

```csharp
public class Player : Mover
{
    // ========== 单例 ==========
    public static Player instance;

    // ========== 输入缓冲 ==========
    private Queue<Direction> inputBuffer = new Queue<Direction>();
    private int maxBufferSize = 3;  // 最大缓冲数量

    // ========== 动画组件 ==========
    private RollCube rollCube;  // 翻滚动画控制器

    // ========== 生命周期 ==========
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        base.Start();  // 调用Mover的初始化
        rollCube = GetComponentInChildren<RollCube>();
    }
}
```

**类继承关系:**
```
MonoBehaviour
    └── Mover (可移动对象基类)
            └── Player (玩家控制)
```

### 2. 输入缓冲系统

**目的:**
- 在动画播放时缓存玩家输入
- 实现流畅的连续移动
- 支持输入加速

```csharp
// 添加输入到缓冲区
public void AddToInputBuffer(Direction dir)
{
    if (inputBuffer.Count < maxBufferSize)
    {
        inputBuffer.Enqueue(dir);
        Debug.Log($"输入缓冲: {inputBuffer.Count}个待处理");
    }
}

// 清空缓冲区
public void ClearInputBuffer()
{
    inputBuffer.Clear();
}

// 处理缓冲区中的输入
void ProcessInputBuffer()
{
    // 只在非移动状态处理
    if (inputBuffer.Count > 0 && !Game.instance.isMoving)
    {
        Direction dir = inputBuffer.Dequeue();
        TryMove(dir);
    }
}
```

**输入缓冲流程:**

```
玩家按下方向键
    │
    └── AddToInputBuffer(dir)
            │
            ├── 缓冲区未满? → 存入队列
            │
            └── 缓冲区已满? → 丢弃

每帧检查
    │
    └── ProcessInputBuffer()
            │
            ├── 队列为空? → 跳过
            │
            ├── 正在移动? → 跳过
            │
            └── 可以移动? → TryMove()
```

### 3. 输入处理

```csharp
void Update()
{
    // ========== 检测方向键输入 ==========
    if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        AddToInputBuffer(Direction.Up);
    else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        AddToInputBuffer(Direction.Down);
    else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        AddToInputBuffer(Direction.Left);
    else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        AddToInputBuffer(Direction.Right);

    // ========== 处理缓冲区 ==========
    ProcessInputBuffer();
}
```

**输入映射:**

| 按键 | 方向 | 说明 |
|------|------|------|
| W / ↑ | Up | 向上移动 |
| S / ↓ | Down | 向下移动 |
| A / ← | Left | 向左移动 |
| D / → | Right | 向右移动 |
| Z | - | 撤销(Game处理) |
| R | - | 重置(Game处理) |

### 4. 移动触发

```csharp
void TryMove(Direction dir)
{
    // ========== 检查是否允许移动 ==========
    if (Game.instance.blockInput)
    {
        Debug.Log("输入被阻止");
        return;
    }

    if (Game.instance.isMoving)
    {
        Debug.Log("正在移动中,请等待");
        return;
    }

    // ========== 获取移动向量 ==========
    Vector3 moveVector = DirectionUtils.GetVector(dir);

    // ========== 尝试计划移动 ==========
    if (TryPlanMove(moveVector, dir))
    {
        // 移动成功!

        // 1. 记录状态用于撤销
        State.AddToUndoStack();

        // 2. 开始移动动画
        Game.instance.MoveStart();

        // 3. 触发翻滚动画
        OnRollPlayer();
    }
}
```

### 5. 翻滚动画

```csharp
public void OnRollPlayer(float duration, Ease ease)
{
    if (rollCube != null)
    {
        rollCube.Roll(direction, duration, ease);
    }
}

// 无参数版本,使用默认值
public void OnRollPlayer()
{
    OnRollPlayer(Game.instance.rotateTime, Game.instance.rotateEase);
}
```

**RollCube组件:**
- 处理玩家的翻滚视觉效果
- 根据移动方向计算旋转轴
- 使用DOTween实现平滑动画

**翻滚原理:**
```
向右移动:
1. 以右下角为轴
2. 旋转90度
3. 落到新位置

向上移动:
1. 以后下角为轴
2. 旋转90度
3. 落到新位置
```

---

## 完整输入流程

```
按键按下
    │
    └── AddToInputBuffer(dir)
            │
            └── 存入队列
                    │
                    └── ProcessInputBuffer()
                            │
                            ├── 检查是否可移动
                            │
                            └── TryMove(dir)
                                    │
                                    ├── TryPlanMove()
                                    │       │
                                    │       └── 验证+计划
                                    │
                                    ├── State.AddToUndoStack()
                                    │
                                    ├── Game.instance.MoveStart()
                                    │
                                    └── OnRollPlayer()
```

---

## 输入加速机制

```csharp
// 位于 Game.cs
float CalculateDuration()
{
    // 缓冲区越多,速度越快
    int bufferCount = Player.instance.GetBufferCount();

    // 公式: 基础时间 / (缓冲数 * 加速因子 + 1)
    // bufferCount=0: duration = moveTime
    // bufferCount=1: duration = moveTime / 1.5
    // bufferCount=2: duration = moveTime / 2
    float duration = moveTime / (bufferCount * moveBufferSpeedupFactor + 1);

    return duration;
}
```

**加速效果:**

| 缓冲数 | 加速倍率 | 效果 |
|--------|----------|------|
| 0 | 1.0x | 正常速度 |
| 1 | 1.5x | 快50% |
| 2 | 2.0x | 快100% |
| 3 | 2.5x | 快150% |

---

## 实践任务

### 任务1:修改加速参数

1. 在Game Inspector中找到`moveBufferSpeedupFactor`
2. 尝试不同的值: 0.3, 0.5, 0.8
3. 体验不同的加速手感

### 任务2:调整输入缓冲区大小

```csharp
// 在Player类中修改
private int maxBufferSize = 5;  // 从3改为5

// 测试连续按5次方向键的行为
```

### 任务3:禁用翻滚动画

```csharp
// 在TryMove方法中注释掉翻滚调用
// OnRollPlayer();

// 现在玩家只平移,不翻滚
```

---

## 配置参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| moveTime | 0.18s | 移动1单位时间 |
| rotateTime | 0.18s | 翻滚90度时间 |
| fallTime | 0.1s | 下落1单位时间 |
| moveBufferSpeedupFactor | 0.5 | 缓冲加速因子 |
| maxBufferSize | 3 | 最大输入缓冲 |

---

## 常见问题

**Q: 为什么需要输入缓冲?**
A: 防止快速输入丢失,提升操作手感。

**Q: 如何添加新的控制键?**
A: 在Update()中添加Input.GetKeyDown检测,调用AddToInputBuffer。

**Q: 翻滚动画和移动动画如何同步?**
A: 使用相同的duration参数,确保动画同步完成。

---

## 关键要点

- ✅ Player继承自Mover,增加输入处理
- ✅ 输入缓冲系统存储待处理的移动
- ✅ 缓冲数量越多,移动速度越快
- ✅ 翻滚动画由RollCube组件处理
- ✅ 移动前记录状态用于撤销

---

## 下一课

[第9课:空间网格 - LogicalGrid](./Lesson09.md)
