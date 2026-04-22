# Chapter01 骨架说明

本说明对应当前第一章灰盒场景：

- `Assets/_Game/Scenes/Chapter01_Combined.unity`
- `Assets/_Game/Data/Chapter/SO_Chapter01_Progression.asset`

目标不是描述未来完整版设计，而是明确“当前可落地的运行时骨架”到底包含哪些固定 ID、门禁条件和重建入口，避免后续继续扩内容时把存档、遭遇战和关卡触发写散。

## 1. 一键重建入口

Unity 菜单：

- `CampusRPG/Setup/Build Chapter01 Combined Scene`
- `CampusRPG/Setup/Build Chapter01 Combined Scene (Force Rebuild)`
- `CampusRPG/Setup/Repair Chapter01 Baseline And Traversal Wiring`

当前构建器会自动：

- 确保 `CombatTest` 占位资产与玩家 / 敌人 prefab 可用，并恢复 public-safe proxy baseline
- 生成或更新 `SO_Chapter01_Progression.asset`
- 重建 `Chapter01_Combined.unity`
- 搭起检查点、区域触发、门禁、关键物品和 Encounter 骨架

注意：

- 普通入口检测到目标文件已存在时会弹确认框
- `Force Rebuild` 会直接覆盖当前章节骨架输出
- `Repair Chapter01 Baseline And Traversal Wiring` 会先把 `CombatTest` prefab 恢复到 proxy baseline，再同步 `Chapter01` 的 Resume / mantle 接线
- 如果已经对 `Chapter01_Combined.unity` 做了手工布置，请先备份

## 2. 章节流转

当前灰盒主流程如下：

1. 从 `Area01_Entrance` 出生并激活 `CP01`
2. 进入入口教学遭遇战 `EN_A01_TUTORIAL`
3. 清空后开启 `Door_A01_To_A02`
4. 进入教学楼外遭遇战 `EN_A02_COURTYARD`
5. 清空后开启 `Door_A02_To_A03`
6. 进入 `Area03_Interior` 的锁门清怪遭遇战 `EN_A03_INTERIOR`
7. 清空后解除印记房封锁，拾取 `KeyItem_GateSigil`
8. `Door_A03_To_A04` 打开，进入 Boss 区
9. 击败守门者遭遇战 `EN_A04_GATEKEEPER`
10. `Door_A04_To_RitualCore` 打开
11. 拾取 `KeyItem_RitualCore`，章节完成

## 3. 固定 ID 清单

### 区域 ID

| 区域 | ID | 用途 |
|---|---|---|
| 入口教学区 | `Area01_Entrance` | 默认出生区与首个区域访问记录 |
| 教学楼外战斗区 | `Area02_Courtyard` | 第二段混编战斗区 |
| 校舍内部推进区 | `Area03_Interior` | 锁门清怪与门禁印记获取区 |
| Boss 区 | `Area04_Boss` | 守门者战与章节收尾区 |

### 检查点 ID

| 检查点 | ID | 位置 |
|---|---|---|
| 初始检查点 | `CP01` | 入口出生点 |
| 中段检查点 | `CP02` | 教学楼外战斗区前段 |
| 后段检查点 | `CP03` | 校舍内部推进区后段 |

### Encounter ID

| 遭遇战 | ID | 当前内容 |
|---|---|---|
| 入口教学战 | `EN_A01_TUTORIAL` | 2 个近战敌人 |
| 教学楼外战斗 | `EN_A02_COURTYARD` | 1 近战 + 1 机动 + 1 远程 |
| 校舍内部清怪 | `EN_A03_INTERIOR` | 1 近战 + 1 机动 + 1 远程，清空后放出门禁印记 |
| 守门者战 | `EN_A04_GATEKEEPER` | 1 个守门者占位 Boss |

### 关键物品 ID

| 物品 | ID | 作用 |
|---|---|---|
| 门禁印记 | `KeyItem_GateSigil` | 打开 Boss 区入口门 |
| 术式核心 | `KeyItem_RitualCore` | 章节完成关键物品 |

## 4. 门禁条件

| 门 / 封锁物 | 开启条件 |
|---|---|
| `Door_A01_To_A02` | `EN_A01_TUTORIAL` 已清空 |
| `Door_A02_To_A03` | `EN_A02_COURTYARD` 已清空 |
| `Door_A03_To_A04` | 已获得 `KeyItem_GateSigil` |
| `Door_A04_To_RitualCore` | `EN_A04_GATEKEEPER` 已清空 |

## 5. 当前 Encounter 规则

当前 `EncounterController` 采用以下最小规则：

- 遭遇战默认在玩家进入触发区后激活
- Encounter 激活前，其成员敌人保持未激活
- `EN_A03_INTERIOR` 激活时会同时封锁入口与印记房，清空后自动解除
- 所有成员敌人死亡后，Encounter 自动写入 `ChapterProgressService`
- 已清空 Encounter 会在存档恢复后保持清空，不再重复阻塞
- 未清空 Encounter 在检查点恢复后整组重置，不保留半血或半清场状态

这套规则是为了贴合第一版存档边界：

- 存“已清空的 Encounter 标记”
- 不存“单个敌人的实时血量和位置”

## 6. 当前 Boss 占位说明

`EN_A04_GATEKEEPER` 当前仍是第一版占位 Boss：

- 复用现有敌人 prefab
- 使用单独的 `SO_Enemy_Gatekeeper` 数值资产
- 行为层仍基于现有近战 AI

它的目标是先承担“章节末段最终校验”的职责，而不是现在就追求完整 Boss 表演。后续可在不改动章节门禁和存档 ID 的前提下，替换成正式 Boss prefab 与招式组。

## 7. 后续扩展建议

继续扩第 1 章时，优先遵守以下约束：

- 不要随意修改已有 `Area` / `Checkpoint` / `Encounter` / `KeyItem` ID
- 如果要新增 Encounter，先决定它是否应该进入存档边界
- 如果要替换守门者 Boss，实现层优先替换 prefab / archetype，不先改流程 ID
- 若要加入真实掉落和奖励，优先挂在 Encounter 完成或关键物品拾取后，不要把章节推进散落到多个零散触发器
