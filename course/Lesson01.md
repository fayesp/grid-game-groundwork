# 第1课:项目架构与Unity基础

> **难度:** ⭐ | **预计时间:** 45分钟 | **前置要求:** 无

---

## 学习目标

- 了解项目整体架构(三层架构:Data/Logic/Presentation)
- 掌握Unity 2019.4.x LTS的基本操作
- 理解DOTween插件的安装与基础用法

---

## 核心内容

### 1. 项目目录结构解析

```
Assets/Scripts/
├── Data/                    # 数据层 - 持久化与序列化
│   ├── DataSave/           # 玩家存档数据 (JSON)
│   └── Level/              # 关卡加载和序列化
├── Logic/                   # 逻辑层 - 游戏规则与实体
│   ├── Entity/             # 游戏对象 (Mover, Player, Wall等)
│   │   └── BaseClass/      # 基础类型 (Enum, State, Tile)
│   ├── Event/              # 事件管理
│   ├── Grid/               # 碰撞检测空间网格
│   ├── Log/                # 日志工具
│   └── Utility/            # 辅助工具
└── Presentation/            # 表现层 - UI、编辑器、视觉效果
    ├── Editor/             # Unity编辑器工具
    ├── Gizmo/              # 场景Gizmo
    └── Shaders/            # 自定义着色器
```

**三层架构说明:**

| 层级 | 职责 | 示例文件 |
|------|------|----------|
| Data | 数据持久化、序列化 | LevelSerialization.cs |
| Logic | 游戏规则、实体行为 | Mover.cs, Game.cs |
| Presentation | UI、编辑器、视觉效果 | LevelEditor.cs |

### 2. Unity场景与GameObject基础

- **场景文件**: `Assets/Scenes/LevelScene.unity`
- **GameController预制体**: 添加到新场景的核心组件
- **Tag系统**: "Tile"标签用于碰撞检测

**场景层级结构:**
```
LevelScene
├── GameController (Game单例)
│   ├── Main Camera
│   └── EventSystem
├── Walls (墙体容器)
│   └── Wall_001
│       └── Cube (Tile标签)
└── Movers (可移动物体容器)
    └── Player
        └── Cube (Tile标签)
```

### 3. DOTween安装与基础用法

DOTween是Unity中流行的动画插件,用于平滑移动和过渡效果。

**安装步骤:**
1. 从Asset Store下载DOTween (免费版)
2. 导入到项目
3. 执行 `Tools > Demigiant > DOTween Utility Panel > Setup DOTween`
4. 点击"Apply"激活所需模块

**基础API:**

```csharp
using DG.Tweening;  // 引入命名空间

// ========== 移动动画 ==========
// 移动物体到目标位置,耗时1秒
transform.DOMove(targetPos, 1f);

// 从当前位置移动指定偏移量
transform.DOMoveX(5f, 1f);  // X轴移动到5
transform.DOMoveY(3f, 0.5f); // Y轴移动到3

// ========== 旋转动画 ==========
// 旋转到指定角度
transform.DORotate(new Vector3(0, 90, 0), 1f);

// ========== 缩放动画 ==========
// 缩放到指定大小
transform.DOScale(1.5f, 0.5f);

// ========== 缓动函数 ==========
// 使用缓动函数改变动画曲线
transform.DOMove(targetPos, 1f)
    .SetEase(Ease.OutCubic);  // 先快后慢

// 常用缓动类型:
// - Linear: 匀速
// - OutCubic: 先快后慢
// - InCubic: 先慢后快
// - InOutCubic: 两头慢中间快

// ========== 动画回调 ==========
transform.DOMove(targetPos, 1f)
    .OnStart(() => {
        Debug.Log("动画开始");
    })
    .OnComplete(() => {
        Debug.Log("动画完成");
    })
    .OnKill(() => {
        Debug.Log("动画被销毁");
    });

// ========== 序列动画 ==========
Sequence seq = DOTween.Sequence();
seq.Append(transform.DOMoveX(5, 1f));      // 第1步
seq.Append(transform.DORotateY(90, 0.5f)); // 第2步
seq.Append(transform.DOMoveZ(3, 1f));      // 第3步
```

---

## 实践任务

### 任务1:打开项目场景

1. 启动Unity Hub
2. 打开本项目(Unity 2019.4.10f1 LTS)
3. 导航到 `Assets/Scenes/LevelScene.unity`
4. 双击打开场景

### 任务2:检查场景结构

1. 在Hierarchy面板中展开所有节点
2. 找到GameController对象
3. 检查Inspector中的组件配置

### 任务3:安装DOTween并测试

1. 安装DOTween (参考上方步骤)
2. 创建一个测试脚本 `DotweenTest.cs`:

```csharp
using UnityEngine;
using DG.Tweening;

public class DotweenTest : MonoBehaviour
{
    void Start()
    {
        // 简单的往返移动动画
        transform.DOMoveX(5f, 1f)
            .SetEase(Ease.OutCubic)
            .SetLoops(-1, LoopType.Yoyo);  // 无限循环,往返
    }
}
```

3. 将脚本挂载到任意物体上
4. 运行场景观察效果

---

## 常见问题

**Q: 为什么使用Unity 2019.4.x LTS?**
A: LTS(Long Term Support)版本稳定,适合生产环境。本项目基于此版本开发。

**Q: DOTween和Unity内置动画有什么区别?**
A: DOTween更适合代码驱动的程序化动画,内置动画适合美术制作的预设动画。

**Q: 三层架构有什么好处?**
A: 分离关注点,便于维护和测试。数据、逻辑、表现各司其职。

---

## 关键要点

- ✅ 项目采用三层架构:Data/Logic/Presentation
- ✅ DOTween是动画系统的核心依赖
- ✅ "Tile"标签用于标识可碰撞的格子
- ✅ GameController是场景的核心管理器

---

## 下一课

[第2课:核心数据结构 - Tile与Enum](./Lesson02.md)

---

## 参考资料

- [DOTween官方文档](http://dotween.demigiant.com/)
- [DOTween缓动函数可视化](http://easings.net/)
- [Unity 2019.4 LTS文档](https://docs.unity3d.com/2019.4/Documentation/Manual/)
