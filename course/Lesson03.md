# 第3课:静态对象 - Wall类

> **难度:** ⭐ | **预计时间:** 30分钟 | **前置要求:** [第2课](./Lesson02.md)

---

## 学习目标

- 理解Wall类的设计模式
- 掌握Unity Tag系统的应用
- 学会创建自定义墙体

---

## 核心文件

- `Logic/Entity/Wall.cs`

---

## 核心内容

### 1. Wall类结构

Wall是场景中的静态障碍物:

```csharp
// 位于 Logic/Entity/Wall.cs
public class Wall : MonoBehaviour
{
    // 墙体是静态的,不需要复杂的逻辑
    // 主要依赖物理碰撞器和Tag系统
}
```

**设计特点:**

| 特点 | 说明 |
|------|------|
| 继承MonoBehaviour | 作为Unity组件挂载 |
| 逻辑简单 | 静态对象无需复杂行为 |
| 依赖物理系统 | 使用BoxCollider进行碰撞检测 |
| 通过Tag标识 | "Tile"标签用于空间划分 |

**为什么Wall类这么简单?**
- 墙体是静态的,不需要移动逻辑
- 碰撞检测由LogicalGrid和物理系统处理
- 保持简洁,遵循最小化原则

### 2. Tag系统

**"Tile"标签的作用:**

1. **空间划分**: 用于LogicalGrid的空间哈希
2. **Mover收集**: Mover类通过Tag收集子物体
3. **碰撞检测**: Game类通过Tag查找所有可碰撞物体

**配置步骤:**

1. 选择Wall的子物体(如Cube)
2. 在Inspector顶部点击"Tag"下拉菜单
3. 选择"Tile"(如果没有,需要先添加标签)
4. 确保子物体有BoxCollider组件

**添加新标签:**
1. `Edit > Project Settings > Tags and Layers`
2. 在Tags列表中点击"+"
3. 输入"Tile"

### 3. BoxCollider配置

```
Wall GameObject
├── Cube1 (带"Tile"标签, BoxCollider)
├── Cube2 (带"Tile"标签, BoxCollider)
└── Cube3 (带"Tile"标签, BoxCollider)
```

**配置要求:**

| 组件 | 要求 | 说明 |
|------|------|------|
| BoxCollider | 必须 | 用于物理碰撞检测 |
| "Tile"标签 | 必须 | 用于LogicalGrid识别 |
| MeshRenderer | 可选 | 用于可视化 |

**BoxCollider设置:**
```
Size: (1, 1, 1)  // 标准格子大小
Center: (0, 0, 0) // 居中
Is Trigger: false // 非触发器
```

### 4. 使用Level Editor绘制墙体

通过`Window -> Level Editor`打开编辑器:

**基本操作:**
1. 在预制体列表中选择Wall
2. 在Scene视图中点击绘制
3. 使用擦除模式删除
4. 调整旋转(0°/90°/180°/270°)
5. 调整生成高度

---

## 实践任务

### 任务1:创建一个L形的墙体

1. 创建空GameObject,命名为"Wall_L"
2. 添加Wall组件
3. 创建3个Cube作为子物体:
   - Cube1: 位置(0,0,0)
   - Cube2: 位置(1,0,0)
   - Cube3: 位置(0,1,0)
4. 所有Cube添加"Tile"标签和BoxCollider

```
L形墙体示意:
■■
■
```

### 任务2:验证墙体配置

使用代码检查墙体是否正确配置:

```csharp
// 在Game.cs的Start方法中添加
void VerifyWalls()
{
    Wall[] walls = FindObjectsOfType<Wall>();
    foreach (var wall in walls)
    {
        int tileCount = 0;
        foreach (Transform child in wall.transform)
        {
            if (child.CompareTag("Tile"))
                tileCount++;
        }
        Debug.Log($"{wall.name}: {tileCount}个Tile");
    }
}
```

### 任务3:使用Level Editor绘制关卡

1. 打开Level Editor窗口
2. 选择Wall预制体
3. 绘制一个简单的迷宫
4. 测试玩家是否能正确碰撞墙体

---

## 代码示例

### 检查墙体配置

```csharp
// 在LogicalGrid中检查墙体
if (Utils.WallIsAtPos(positionToCheck))
{
    // 该位置有墙体,阻止移动
    return false;
}
```

### 获取所有墙体

```csharp
// 获取场景中所有墙体
Wall[] allWalls = FindObjectsOfType<Wall>();
foreach (var wall in allWalls)
{
    Debug.Log($"墙体位置: {wall.transform.position}");
}
```

---

## 常见问题

**Q: Wall和Mover的区别?**
A: Wall是静态的,不会移动;Mover可以移动和下落。Wall更轻量级。

**Q: 为什么Wall需要子物体?**
A: 子物体用于标记占据的格子,支持不规则形状的墙体。

**Q: Wall可以是触发器吗?**
A: 可以,但本项目使用LogicalGrid进行碰撞检测,不依赖物理触发器。

---

## 关键要点

- ✅ Wall类是轻量级的静态障碍物
- ✅ "Tile"标签用于标识碰撞格子
- ✅ 每个格子需要一个带BoxCollider的子物体
- ✅ Level Editor可以快速绘制墙体

---

## 下一课

[第4课:游戏管理器 - Game单例](./Lesson04.md)
