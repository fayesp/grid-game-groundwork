# 项目待办事项

> 生成日期：2026-05-07
> 基于 `archchange` 分支的代码分析

---

## 概述

项目正处于从旧三层架构（Data / Logic / Presentation）向 MVP 架构（Model / View / Controller / Core）迁移的中期阶段。新旧代码共存，存在 24 个 TODO 分布在 9 个文件中。

---

## 高优先级

### TASK-001: 完成 MagnetField 磁力系统核心实现

**状态:** 待处理
**涉及文件:** `Assets/Scripts/Logic/Entity/MagnetField.cs`
**TODO 数量:** 10

**具体内容:**

- [ ] 完成刚进入磁场时添加到 `moversInField` 的逻辑
- [ ] 完成 X 轴磁力平衡计算（当前标记为"待定"）
- [ ] 完成 Y 轴磁力平衡计算
- [ ] 完成 Z 轴磁力平衡计算
- [ ] 完成复合磁力方向下的运动合成
- [ ] 补充磁场边界条件处理

---

### TASK-002: 解决 Mover/Game 移动系统 TODO

**状态:** 待处理
**涉及文件:**
- `Assets/Scripts/Logic/Entity/Mover.cs` (5 个 TODO)
- `Assets/Scripts/Logic/Entity/Game.cs` (3 个 TODO)

**Mover.cs 具体内容:**

- [ ] 移动验证逻辑优化（约第 100 行）
- [ ] 下落高度计算完善（约第 376 行）
- [ ] 返回值优化（约第 588 行）

**Game.cs 具体内容:**

- [ ] 下落时多次执行问题（约第 370 行）
- [ ] 位置更新逻辑未完成（约第 373 行）
- [ ] Player roll 处理（约第 425 行）

---

### TASK-003: 完成 MovePlanner 多格推动与磁力传播逻辑提取

**状态:** 待处理
**前置依赖:** TASK-001, TASK-002
**涉及文件:** `Assets/Scripts/Model/MovePlanner.cs`

**具体内容:**

- [ ] 从旧代码提取多格推动（multi-tile push）逻辑到 MovePlanner
- [ ] 提取磁力附着传播（magnet attach propagation）逻辑
- [ ] 确保新 MovePlanner 与旧 Game.TryPlanMove 行为一致
- [ ] 编写对比测试验证迁移正确性

---

### TASK-008: 制定并执行旧架构 → MVP 架构完整迁移计划

**状态:** 待处理
**涉及目录:**
- `Assets/Scripts/Logic/` → `Assets/Scripts/Model/`
- `Assets/Scripts/Controller/`
- `Assets/Scripts/View/`
- `Assets/Scripts/Core/`

**新旧架构对应关系:**

| 旧架构 | 新架构 | 状态 |
|--------|--------|------|
| `Game.cs` (单例) | `GamePresenter.cs` + `GameBoard.cs` | 共存中 |
| `Mover.cs` | `MoverModel.cs` | 共存中 |
| `Player.cs` | `PlayerModel.cs` | 共存中 |
| `EventManager.cs` (静态) | `GameEventBus.cs` (实例) | 迁移中 |
| `LogicalGrid.cs` | `GameBoardGridQuery.cs` | 共存中 |
| `State.cs` (静态栈) | `CommandStack.cs` | 共存中 |

**迁移步骤:**

- [ ] 梳理旧 Logic 层所有公共 API 调用点
- [ ] 确认新 Model 层覆盖所有旧功能
- [ ] 逐步替换调用点（Game.instance → GameServices.Instance.Presenter）
- [ ] LevelEditor.Refresh() 适配新架构
- [ ] 全量回归测试

---

## 中优先级

### TASK-004: 完成 EventManager → GameEventBus 迁移

**状态:** 待处理
**涉及文件:**
- `Assets/Scripts/Logic/Event/EventManager.cs` (待移除)
- `Assets/Scripts/Core/GameEventBus.cs` (目标)

**具体内容:**

- [ ] 检索所有 `EventManager.onXxx +=` 调用点
- [ ] 替换为 `GameEventBus` 实例化事件订阅
- [ ] 移除 `EventManager.cs` 文件
- [ ] 更新相关文档

---

### TASK-005: 统一所有脚本的命名空间

**状态:** 待处理
**涉及范围:** 全部 .cs 文件

**当前问题:** 部分文件使用 `Assets.Scripts` 命名空间，部分无命名空间。

**具体内容:**

- [ ] 制定命名空间规范（如 `GridGame.Model`, `GridGame.Logic` 等）
- [ ] 为所有文件添加统一命名空间
- [ ] 确保跨层引用正确

---

### TASK-007: 修复 RollCube 坐标系不一致

**状态:** 待处理
**涉及文件:** `Assets/Scripts/Logic/Entity/RollCube.cs`

**具体内容:**

- [ ] 审查 RollCube 中的坐标计算逻辑
- [ ] 统一与项目其他部分的坐标系约定（Z 轴为深度方向）
- [ ] 验证 2D / 3D 模式下的行为一致性

---

### TASK-009: 为 Model 层编写单元测试

**状态:** 待处理
**测试框架:** Unity Test Framework (NUnit)
**涉及目录:** `Assets/Scripts/Model/`

**优先测试模块:**

- [ ] `GameBoard` — 实体注册与查询
- [ ] `MovePlanner` — 移动规划逻辑
- [ ] `CommandStack` — 撤销 / 重做
- [ ] `MoverModel` / `PlayerModel` — 核心模型状态

---

## 低优先级

### TASK-006: 统一代码注释语言

**状态:** 待处理
**涉及范围:** 全部 .cs 文件

**当前问题:** 中文和英文注释混杂。

**具体内容:**

- [ ] 确定统一语言标准（中文或英文）
- [ ] 批量替换注释语言
- [ ] 保持 XML 文档注释的一致性

---

### TASK-010: 迁移完成后清理旧架构遗留代码

**状态:** 待处理
**前置依赖:** TASK-008
**涉及范围:**

- `Assets/Scripts/Logic/Entity/` 中已被 Model 替代的类
- `Game.instance` 单例引用
- `Utils` 中仅被旧代码使用的委托方法

**具体内容:**

- [ ] 移除已被 Model 层替代的旧 Entity 类
- [ ] 移除 `Game.instance` 单例
- [ ] 清理 `Utils` 中的旧委托方法
- [ ] 更新 `CLAUDE.md` 反映新架构
- [ ] 更新 `docs/ARCHITECTURE.md`

---

## 依赖关系

```
TASK-001 (MagnetField) ──┐
                          ├──→ TASK-003 (MovePlanner 提取)
TASK-002 (Mover/Game) ───┘

TASK-008 (MVP 迁移) ──→ TASK-010 (旧代码清理)
```

其余任务（TASK-004, 005, 006, 007, 009）相互独立，可并行推进。

---

## 统计

| 优先级 | 数量 | TODO 合计 |
|--------|------|-----------|
| 高     | 4    | ~21       |
| 中     | 4    | —         |
| 低     | 2    | —         |
| **总计** | **10** | **~24** |
