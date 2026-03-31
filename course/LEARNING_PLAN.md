# Grid Game Groundwork 学习计划

本课程共15节,带你从零开始掌握这个Unity网格推箱子游戏框架的开发。

---

## 课程导航

| 阶段 | 课程 | 难度 | 预计时间 |
|------|------|------|----------|
| 基础入门 | 第1-4课 | ⭐ | 4小时 |
| 核心机制 | 第5-9课 | ⭐⭐ | 6小时 |
| 工具与系统 | 第10-12课 | ⭐⭐ | 3小时 |
| 数据与编辑器 | 第13-15课 | ⭐⭐⭐ | 4小时 |

---

## 第一阶段:基础入门 (第1-4课)

### 第1课:项目架构与Unity基础

> **难度:** ⭐ | **预计时间:** 45分钟 | **前置要求:** 无

**学习目标:**
- 了解项目整体架构(三层架构:Data/Logic/Presentation)
- 掌握Unity 2019.4.x LTS的基本操作
- 理解DOTween插件的安装与基础用法

**核心内容:**
1. 项目目录结构解析
   - `Assets/Scripts/Data/` - 数据持久化层
   - `Assets/Scripts/Logic/` - 游戏逻辑层
   - `Assets/Scripts/Presentation/` - 表现层
2. Unity场景与GameObject基础
3. DOTween安装与简单动画示例

**实践任务:**
- 打开`LevelScene.unity`场景
- 安装DOTween并创建一个简单的移动动画

**参考资料:**
- [DOTween官方文档](http://dotween.demigiant.com/)

---

### 第2课:核心数据结构 - Tile与Enum

> **难度:** ⭐ | **预计时间:** 30分钟 | **前置要求:** 第1课

**学习目标:**
- 理解`Tile`结构体的设计
- 掌握方向枚举`Direction`的使用

**核心文件:**
- `Logic/Entity/BaseClass/Tile.cs`
- `Logic/Entity/BaseClass/Enum.cs`

**核心内容:**
1. Tile结构体:存储Transform引用和网格位置
2. Direction枚举:六方向移动系统(Up/Down/Left/Right/Forward/Back)
3. 网格坐标系:Z轴为深度(下落方向)

**代码示例:**
```csharp
// Tile结构
public struct Tile {
    public Transform t;  // 子物体Transform
    public Vector3Int pos;  // 网格坐标
}
```

**实践任务:**
- 创建一个自定义的多格物体,理解Tile的收集机制

---

### 第3课:静态对象 - Wall类

> **难度:** ⭐ | **预计时间:** 30分钟 | **前置要求:** 第2课

**学习目标:**
- 理解Wall类的设计模式
- 掌握Unity Tag系统的应用

**核心文件:**
- `Logic/Entity/Wall.cs`

**核心内容:**
1. Wall类:继承MonoBehaviour的静态障碍物
2. "Tile"标签的作用:用于碰撞检测
3. BoxCollider配置要求

**实践任务:**
- 在场景中添加自定义形状的墙体
- 使用Level Editor绘制关卡

---

### 第4课:游戏管理器 - Game单例

> **难度:** ⭐⭐ | **预计时间:** 45分钟 | **前置要求:** 第3课

**学习目标:**
- 掌握Unity单例模式实现
- 理解游戏循环与状态管理

**核心文件:**
- `Logic/Entity/Game.cs`

**核心内容:**
1. 单例模式实现(`Game.instance`)
2. 核心变量:
   - `moveTime`: 移动1单位时间
   - `fallTime`: 下落1单位时间
   - `allowPushMulti`: 多物体推动开关
3. 生命周期方法(Awake/Start/Update)
4. 状态刷新机制(`Refresh()`)

**关键代码:**
```csharp
public static Game instance {
    get {
        if (instanceRef == null)
            instanceRef = FindObjectOfType<Game>();
        return instanceRef;
    }
}
```

---

## 第二阶段:核心机制 (第5-9课)

### 第5课:可移动对象基类 - Mover (上)

> **难度:** ⭐⭐ | **预计时间:** 60分钟 | **前置要求:** 第4课

**学习目标:**
- 理解Mover类的基础结构
- 掌握Tile收集与方向位置计算

**核心文件:**
- `Logic/Entity/Mover.cs`

**核心内容:**
1. 核心变量:
   - `tiles`: 占据的格子列表
   - `plannedMove`: 计划移动向量
   - `isFalling`: 下落状态标记
2. `CreateTiles()`: 自动收集子物体的Tile
3. `UpdateDirPos()`: 计算六方向相邻格子

**实践任务:**
- 分析单格物体与多格物体的方向计算差异

---

### 第6课:可移动对象基类 - Mover (下)

> **难度:** ⭐⭐⭐ | **预计时间:** 75分钟 | **前置要求:** 第5课

**学习目标:**
- 深入理解移动验证与计划系统
- 掌握推动机制的实现

**核心内容:**
1. 移动验证:`CanMoveToward()`
   - 墙体碰撞检测
   - 其他Mover的碰撞检测
   - 无限循环防护(HashSet visited)
2. 移动计划:`PlanMove()`与`PlanPushes()`
3. 移动执行:`ExecuteLogicalMove()`

**关键流程:**
```
TryPlanMove() -> CanMoveToward() -> PlanMove() -> ExecuteLogicalMove()
```

---

### 第7课:重力与下落系统

> **难度:** ⭐⭐ | **预计时间:** 45分钟 | **前置要求:** 第6课

**学习目标:**
- 理解重力系统的实现
- 掌握下落检测逻辑

**核心内容:**
1. `ShouldFall()`: 判断是否应该下落
2. `GroundBelow()`: 地面检测
3. `DoPostMoveEffects()`: 移动后效果处理
4. 下落动画循环机制

**代码分析:**
```csharp
public virtual bool ShouldFall() {
    if (GroundBelow()) return false;
    return true;
}
```

**实践任务:**
- 实现一个悬浮物体,需要特定条件才能下落

---

### 第8课:玩家控制 - Player类

> **难度:** ⭐⭐ | **预计时间:** 45分钟 | **前置要求:** 第7课

**学习目标:**
- 掌握Player类的设计
- 理解输入缓冲系统

**核心文件:**
- `Logic/Entity/Player.cs`

**核心内容:**
1. Player继承自Mover
2. 输入缓冲:`InputBuffer`队列
3. 输入处理与移动触发
4. 翻滚动画:`OnRollPlayer()`

**实践任务:**
- 修改玩家移动速度参数,体验不同手感

---

### 第9课:空间网格 - LogicalGrid

> **难度:** ⭐⭐ | **预计时间:** 45分钟 | **前置要求:** 第8课

**学习目标:**
- 理解空间哈希网格的实现
- 掌握碰撞检测优化

**核心文件:**
- `Logic/Grid/LogicalGrid.cs`

**核心内容:**
1. 空间哈希:`Dictionary<Vector3Int, List<GameObject>>`
2. `SyncContents()`: 同步网格内容
3. 快速位置查询

**优势:**
- O(1)时间复杂度的位置查询
- 支持多物体占据同一格子

---

## 第三阶段:工具与系统 (第10-12课)

### 第10课:工具类系统

> **难度:** ⭐⭐ | **预计时间:** 40分钟 | **前置要求:** 第9课

**学习目标:**
- 掌握工具类的组织方式
- 理解查询与方向计算的分离

**核心文件:**
- `Logic/Utility/Utils.cs`
- `Logic/Utility/GridQuery.cs`
- `Logic/Utility/DirectionUtils.cs`

**核心内容:**
1. `GridQuery`: 位置查询
   - `GetMoverAtPos()`
   - `WallIsAtPos()`
   - `TileIsEmpty()`
2. `DirectionUtils`: 方向计算
   - `CheckDirection()`
   - `IsRound()`
3. `Utils`: 向后兼容的统一接口

**设计原则:**
单一职责,功能分离

---

### 第11课:撤销与重置系统

> **难度:** ⭐⭐ | **预计时间:** 40分钟 | **前置要求:** 第10课

**学习目标:**
- 理解状态快照机制
- 掌握撤销栈的实现

**核心文件:**
- `Logic/Entity/BaseClass/State.cs`

**核心内容:**
1. 状态快照:`MoverState`结构
2. 撤销栈:`undoStack`
3. `AddToUndoStack()`: 记录状态
4. `DoUndo()`: 执行撤销
5. `DoReset()`: 重置到初始状态

**实践任务:**
- 扩展撤销系统,支持更多状态记录

---

### 第12课:事件系统

> **难度:** ⭐⭐ | **预计时间:** 40分钟 | **前置要求:** 第11课

**学习目标:**
- 掌握C#事件模式
- 理解游戏内通信机制

**核心文件:**
- `Logic/Event/EventManager.cs`

**核心内容:**
1. 静态事件定义:
   - `onLevelStarted`
   - `onMoveComplete`
   - `onPush`
   - `onUndo`
   - `onReset`
2. 事件订阅与触发

**使用示例:**
```csharp
EventManager.onMoveComplete += OnMoveDone;
EventManager.onMoveComplete?.Invoke();
```

---

## 第四阶段:数据与编辑器 (第13-15课)

### 第13课:关卡序列化系统

> **难度:** ⭐⭐ | **预计时间:** 45分钟 | **前置要求:** 第12课

**学习目标:**
- 理解JSON序列化机制
- 掌握关卡数据结构

**核心文件:**
- `Data/Level/LevelSerialization.cs`
- `Data/Level/LevelLoader.cs`
- `Data/Level/LevelManager.cs`

**核心内容:**
1. `SerializedLevel`: 关卡数据结构
2. `SerializedLevelObject`: 单个物体数据
3. JSON序列化与反序列化
4. 资源加载路径:`Assets/Resources/Levels/`

---

### 第14课:关卡编辑器

> **难度:** ⭐⭐⭐ | **预计时间:** 60分钟 | **前置要求:** 第13课

**学习目标:**
- 掌握Unity Editor扩展
- 理解自定义工具开发

**核心文件:**
- `Presentation/Editor/LevelEditor.cs`

**核心内容:**
1. EditorWindow基础
2. 网格绘制系统
3. 预制体管理
4. 旋转与高度调整

**使用方法:**
`Window -> Level Editor` 打开编辑器

**实践任务:**
- 创建一个包含10个关卡的关卡包

---

### 第15课:实战案例与扩展

> **难度:** ⭐⭐⭐ | **预计时间:** 90分钟 | **前置要求:** 第1-14课

**学习目标:**
- 分析示例游戏实现
- 掌握自定义游戏对象的开发

**示例项目:**
1. **Sokoban经典推箱子**
   - 路径:`Assets/Examples/Sokoban/`
   - 胜利条件检测实现

2. **PipePushParadise**
   - 路径:`Assets/Examples/PipePushParadise/`
   - 管道连接机制

3. **磁力系统**
   - 路径:`Assets/Examples/magnetField/`
   - Magnet与MagnetField类

**扩展方向:**
- 自定义Mover子类
- 新的胜利条件
- 特殊地形效果
- 多人协作模式

---

## 附录

### 推荐学习路径

```
第1-4课 → 第5-6课 → 第8课 → 第10课 → 第13-14课 → 第15课
    ↓         ↓
第7课      第9课
    ↓         ↓
第11课 ← 第12课
```

### 关键概念速查表

| 概念 | 文件 | 说明 |
|------|------|------|
| Tile | Tile.cs | 格子数据结构 |
| Mover | Mover.cs | 可移动对象基类 |
| Game | Game.cs | 游戏管理单例 |
| LogicalGrid | LogicalGrid.cs | 空间哈希网格 |
| State | State.cs | 撤销/重置系统 |

### 常见问题

**Q: 如何添加新的可推动物体?**
A: 继承Mover类,重写必要的方法。

**Q: 如何修改移动速度?**
A: 调整Game实例的`moveTime`和`fallTime`参数。

**Q: 如何实现自定义胜利条件?**
A: 订阅`EventManager.onMoveComplete`事件,在回调中检测条件。

### 学习建议

1. **按顺序学习**: 课程设计有前后依赖关系
2. **动手实践**: 每课的实践任务务必完成
3. **阅读源码**: 结合课程内容阅读实际代码
4. **记录笔记**: 记录遇到的问题和解决方案
5. **参与社区**: 在GitHub上提问和分享

---

*最后更新: 2026-03-18*
