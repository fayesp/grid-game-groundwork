# 第15课:实战案例与扩展

> **难度:** ⭐⭐⭐ | **预计时间:** 90分钟 | **前置要求:** [第1-14课](./Lesson01.md)

---

## 学习目标

- 分析示例游戏实现
- 掌握自定义游戏对象的开发
- 了解项目扩展方向

---

## 示例项目

### 1. Sokoban经典推箱子

**路径:** `Assets/Examples/Sokoban/`

**核心机制:**
- 玩家推动箱子到目标位置
- 箱子只能推,不能拉
- 所有箱子到位即胜利

**胜利条件检测:**

```csharp
public class SokobanWinCondition : MonoBehaviour
{
    public List<Transform> targetPositions;  // 目标位置列表
    public List<Mover> boxes;                // 所有箱子

    void OnEnable()
    {
        EventManager.onMoveComplete += CheckWinCondition;
    }

    void OnDisable()
    {
        EventManager.onMoveComplete -= CheckWinCondition;
    }

    void CheckWinCondition()
    {
        int boxesOnTarget = 0;

        foreach (var box in boxes)
        {
            foreach (var target in targetPositions)
            {
                // 检查箱子是否在目标位置(允许小误差)
                if (Vector3.Distance(box.transform.position, target.position) < 0.1f)
                {
                    boxesOnTarget++;
                    break;  // 每个箱子只计数一次
                }
            }
        }

        // 所有箱子都在目标位置
        if (boxesOnTarget == targetPositions.Count)
        {
            Debug.Log("🎉 关卡完成!");
            EventManager.onLevelComplete?.Invoke(1);
        }
    }
}
```

### 2. PipePushParadise管道推箱子

**路径:** `Assets/Examples/PipePushParadise/`

**核心机制:**
- 推动管道方块
- 连接水源到出口
- 水流通过连通的管道

**管道连接检测:**

```csharp
public class Pipe : Mover
{
    // 四个方向的连接状态
    public bool[] connections = new bool[4]; // 上右下左

    // 是否有指定方向的连接
    public bool IsConnectedTo(Direction dir)
    {
        int index = (int)dir - 1;  // Direction从1开始
        if (index >= 0 && index < 4)
            return connections[index];
        return false;
    }

    public override void DoPostMoveEffects()
    {
        base.DoPostMoveEffects();
        CheckPipeConnections();
    }

    void CheckPipeConnections()
    {
        // 检查四个方向的相邻管道
        CheckConnection(Direction.Up);
        CheckConnection(Direction.Right);
        CheckConnection(Direction.Down);
        CheckConnection(Direction.Left);

        // 更新水流状态
        UpdateWaterFlow();
    }

    void CheckConnection(Direction dir)
    {
        Vector3Int neighborPos = GetNeighborPosition(dir);
        List<Mover> neighbors = Utils.GetMoverAtPos(neighborPos);

        foreach (var neighbor in neighbors)
        {
            Pipe pipe = neighbor as Pipe;
            if (pipe != null && pipe.IsConnectedTo(DirectionUtils.GetOpposite(dir)))
            {
                // 双方都有连接,形成通路
                CreateConnection(dir);
            }
        }
    }
}
```

### 3. 磁力系统

**路径:** `Assets/Examples/magnetField/`

**核心文件:**
- `Logic/Entity/Magnet.cs`
- `Logic/Entity/MagnetField.cs`

**核心机制:**
- 磁铁吸引/排斥其他物体
- 磁场影响范围内的物体移动

**磁力实现:**

```csharp
public class Magnet : Mover
{
    public float strength = 1f;      // 磁力强度
    public bool isAttracting = true; // true=吸引, false=排斥
    public float range = 5f;         // 影响范围

    public override void DoPostMoveEffects()
    {
        base.DoPostMoveEffects();
        ApplyMagneticForce();
    }

    void ApplyMagneticForce()
    {
        // 找到所有磁力场
        MagnetField[] fields = FindObjectsOfType<MagnetField>();

        foreach (var field in fields)
        {
            float distance = Vector3.Distance(transform.position, field.transform.position);

            // 在影响范围内
            if (distance < range && distance > 0.1f)
            {
                // 计算方向
                Vector3 direction = (field.transform.position - transform.position).normalized;

                // 排斥则反向
                if (!isAttracting)
                    direction = -direction;

                // 计算力的大小(距离越近力越大)
                float force = strength / (distance * distance);

                // 应用力到移动计划
                ApplyForce(direction * force);
            }
        }
    }
}
```

---

## 自定义Mover开发

### 创建冰块(滑动)

```csharp
public class IceBlock : Mover
{
    private Vector3 slideDirection;
    private bool isSliding = false;

    public override void DoPostMoveEffects()
    {
        // 冰块会滑动直到碰到障碍物
        if (ShouldSlide())
        {
            PlanMove(slideDirection, DirectionUtils.CheckDirection(Vector3.zero, slideDirection));
        }
        else
        {
            base.DoPostMoveEffects();
            isSliding = false;
        }
    }

    bool ShouldSlide()
    {
        if (!isSliding) return false;

        // 检查前方是否有障碍物
        Direction slideDir = DirectionUtils.CheckDirection(Vector3.zero, slideDirection);
        return CanMoveToward(ref slideDirection, slideDir);
    }

    public void StartSlide(Vector3 direction)
    {
        slideDirection = direction;
        isSliding = true;
    }
}
```

### 创建传送门

```csharp
public class Portal : MonoBehaviour
{
    public Portal linkedPortal;  // 连接的传送门
    public float cooldown = 0.5f;
    private bool canTeleport = true;

    void OnTriggerEnter(Collider other)
    {
        if (!canTeleport || linkedPortal == null) return;

        Mover mover = other.GetComponentInParent<Mover>();
        if (mover != null)
        {
            // 传送到连接的传送门
            Teleport(mover);
        }
    }

    void Teleport(Mover mover)
    {
        // 瞬移
        mover.transform.position = linkedPortal.transform.position;

        // 设置冷却
        canTeleport = false;
        linkedPortal.canTeleport = false;

        // 冷却后恢复
        StartCoroutine(CooldownRoutine());
    }

    IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(cooldown);
        canTeleport = true;
        linkedPortal.canTeleport = true;
    }
}
```

### 创建开关机关

```csharp
public class Switch : MonoBehaviour
{
    public List<Wall> controlledWalls;  // 控制的墙体列表
    public bool isActivated = false;

    void OnTriggerEnter(Collider other)
    {
        if (!isActivated)
        {
            Activate();
        }
    }

    void Activate()
    {
        isActivated = true;

        // 隐藏所有控制的墙体
        foreach (var wall in controlledWalls)
        {
            wall.gameObject.SetActive(false);
        }

        // 播放音效
        AudioManager.PlaySwitchSound();

        Debug.Log("开关已激活!");
    }

    // 可选: 支持重置
    public void Reset()
    {
        isActivated = false;
        foreach (var wall in controlledWalls)
        {
            wall.gameObject.SetActive(true);
        }
    }
}
```

---

## 扩展方向

### 1. 新的胜利条件

```csharp
// 收集所有宝石
public class CollectWinCondition : MonoBehaviour
{
    public List<Mover> gems;
    private int collected = 0;

    void OnEnable()
    {
        EventManager.onMoverMoved += OnGemMoved;
    }

    void OnGemMoved(Mover mover)
    {
        if (gems.Contains(mover))
        {
            // 检查宝石是否到达收集点
            // ...
        }
    }
}

// 限时挑战
public class TimeLimitCondition : MonoBehaviour
{
    public float timeLimit = 60f;
    private float timeRemaining;

    void Start()
    {
        timeRemaining = timeLimit;
        StartCoroutine(TimerRoutine());
    }

    IEnumerator TimerRoutine()
    {
        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateUI();
            yield return null;
        }

        // 时间到,失败
        EventManager.onLevelFailed?.Invoke();
    }
}
```

### 2. 特殊地形

```csharp
// 冰面: 物体会滑动
public class IceSurface : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Mover mover = other.GetComponentInParent<Mover>();
        if (mover != null)
        {
            IceBlock ice = mover as IceBlock;
            if (ice == null)
            {
                // 给普通Mover添加滑动效果
                mover.StartSlide(mover.lastMoveDirection);
            }
        }
    }
}

// 陷阱: 销毁接触的物体
public class Trap : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Mover mover = other.GetComponentInParent<Mover>();
        if (mover != null && !mover.isPlayer)
        {
            Destroy(mover.gameObject);
        }
    }
}

// 弹跳板: 将物体弹飞
public class BouncePad : MonoBehaviour
{
    public float bounceForce = 2f;
    public Direction bounceDirection = Direction.Up;

    void OnTriggerEnter(Collider other)
    {
        Mover mover = other.GetComponentInParent<Mover>();
        if (mover != null)
        {
            Vector3 bounceVector = DirectionUtils.GetVector(bounceDirection) * bounceForce;
            mover.PlanMove(bounceVector, bounceDirection);
            Game.instance.MoveStart();
        }
    }
}
```

### 3. 多人协作

```csharp
public class MultiplayerManager : MonoBehaviour
{
    public List<Player> players;

    void Update()
    {
        // 处理多个玩家的输入
        for (int i = 0; i < players.Count; i++)
        {
            HandlePlayerInput(i);
        }
    }

    void HandlePlayerInput(int playerIndex)
    {
        Player player = players[playerIndex];

        // 玩家1: WASD
        // 玩家2: 方向键
        KeyCode up, down, left, right;

        if (playerIndex == 0)
        {
            up = KeyCode.W; down = KeyCode.S;
            left = KeyCode.A; right = KeyCode.D;
        }
        else
        {
            up = KeyCode.UpArrow; down = KeyCode.DownArrow;
            left = KeyCode.LeftArrow; right = KeyCode.RightArrow;
        }

        if (Input.GetKeyDown(up))
            player.AddToInputBuffer(Direction.Up);
        else if (Input.GetKeyDown(down))
            player.AddToInputBuffer(Direction.Down);
        else if (Input.GetKeyDown(left))
            player.AddToInputBuffer(Direction.Left);
        else if (Input.GetKeyDown(right))
            player.AddToInputBuffer(Direction.Right);
    }
}
```

### 4. AI敌人

```csharp
public class Enemy : Mover
{
    public float moveInterval = 1f;
    public bool chasePlayer = true;

    void Start()
    {
        StartCoroutine(AIMoveRoutine());
    }

    IEnumerator AIMoveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(moveInterval);

            Direction moveDir;

            if (chasePlayer)
            {
                // 追踪玩家
                moveDir = GetDirectionToPlayer();
            }
            else
            {
                // 随机移动
                moveDir = (Direction)Random.Range(1, 7);
            }

            Vector3 moveVector = DirectionUtils.GetVector(moveDir);
            if (TryPlanMove(moveVector, moveDir))
            {
                Game.instance.MoveStart();
            }
        }
    }

    Direction GetDirectionToPlayer()
    {
        Vector3 toPlayer = Player.instance.transform.position - transform.position;

        // 选择主要方向
        if (Mathf.Abs(toPlayer.x) > Mathf.Abs(toPlayer.y))
        {
            return toPlayer.x > 0 ? Direction.Right : Direction.Left;
        }
        else
        {
            return toPlayer.y > 0 ? Direction.Up : Direction.Down;
        }
    }
}
```

---

## 项目架构总结

```
Grid Game Groundwork
│
├── 数据层 (Data/)
│   ├── 关卡序列化 (LevelSerialization.cs)
│   ├── 关卡加载 (LevelLoader.cs)
│   ├── 关卡管理 (LevelManager.cs)
│   └── 存档系统 (SaveData.cs)
│
├── 逻辑层 (Logic/)
│   ├── 实体系统
│   │   ├── Mover.cs (可移动对象基类)
│   │   ├── Player.cs (玩家控制)
│   │   ├── Wall.cs (静态障碍)
│   │   └── BaseClass/ (基础类型)
│   │       ├── Tile.cs
│   │       ├── Enum.cs
│   │       └── State.cs
│   │
│   ├── 网格系统 (LogicalGrid.cs)
│   │
│   ├── 工具类
│   │   ├── Utils.cs (统一入口)
│   │   ├── GridQuery.cs (位置查询)
│   │   └── DirectionUtils.cs (方向计算)
│   │
│   └── 事件系统 (EventManager.cs)
│
└── 表现层 (Presentation/)
    ├── 编辑器工具 (LevelEditor.cs)
    ├── Gizmo可视化
    └── 自定义Shader
```

---

## 学习路径回顾

```
基础入门 → 核心机制 → 工具系统 → 数据持久化 → 实战扩展
  1-4       5-8        9-12        13-14        15
```

---

## 下一步建议

1. **深入源码**: 阅读并理解所有核心类
2. **实践开发**: 创建自己的推箱子变体
3. **性能优化**: 分析并优化性能瓶颈
4. **功能扩展**: 添加新的游戏机制
5. **发布分享**: 打包并分享你的游戏

---

**🎉 恭喜完成全部15课的学习!**

现在你已经掌握了Grid Game Groundwork框架的核心概念和开发技能。

继续探索、实践和创造吧!

---

## 延伸资源

- [Unity官方文档](https://docs.unity3d.com/)
- [DOTween文档](http://dotween.demigiant.com/)
- [C#事件教程](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/events/)
- [Unity Editor扩展](https://docs.unity3d.com/Manual/editor-EditorWindows.html)
