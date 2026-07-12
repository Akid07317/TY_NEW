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

## 8. 关键物品硬门禁

门和提示物不是关键物品的唯一安全边界。`KeyItemPickup` 自身必须校验 `requiredEncounterId`，即使碰撞体因为侧路、传送或场景几何缺口被提前触发，也不能越权写入章节进度。

| 关键物品 | 拾取器硬前置 |
|---|---|
| `Pickup_GateSigil` | `EN_A03_INTERIOR` 已清空 |
| `Pickup_RitualCore` | `EN_A04_GATEKEEPER` 已清空 |

用于复核这条边界的 Editor-only 菜单：

- `CampusRPG/Debug/Chapter01/Teleport Player To Ritual Core`
- `CampusRPG/Debug/Chapter01/Teleport Player To Current Objective`
- `CampusRPG/Debug/Chapter01/Defeat Active Encounter Through Damage`
- `CampusRPG/Debug/Chapter01/Log Walkthrough State`

它只在 Play Mode 内把玩家移动到 RitualCore 拾取点，不修改场景资产，也不替玩家补进度。Gatekeeper 未清时，预期结果必须是：不获得 `KeyItem_RitualCore`、不显示章节完成卡、拾取物保持可用；Gatekeeper 已清时才允许完成章节。

其余三个入口用于重复整章走查：当前目标传送和状态日志都不直接写 `ChapterProgressService`；遭遇战击杀入口会先按正式 `EncounterController` 激活遭遇战，再通过 `HealthComponent.ReceiveDamage` 对活动成员造成致死伤害，章节推进仍由既有敌人死亡事件完成。它们只解决自动化环境不能持续按住 WASD / Block 的输入限制，不能作为绕过遭遇战进度合同的捷径。

## 9. 2026-07-10 GUI 通关验收记录

| 检查项 | 结果 |
|---|---|
| 存档隔离 | 先把既有完成存档改名备份；fresh Play 正常从 `CP01 / Area01_Entrance` 进入，原用户进度未覆盖 |
| 修复前负向复现 | 四场 Encounter 全未清时直接触碰 RitualCore，错误显示 `Chapter 01 Cleared`；复现存档只有 `KeyItem_RitualCore`、`clearedEncounterIds=[]`，确认是拾取器缺硬前置，不是 Boss 结算链误报 |
| 最小修复 | `KeyItemPickup` 新增 `requiredEncounterId` 校验；场景与 builder 同步配置 GateSigil=`EN_A03_INTERIOR`、RitualCore=`EN_A04_GATEKEEPER` |
| 回归保护 | `KeyItemPickupTests` 增两条前置拒绝/清场后放行测试；`Chapter01ProgressionSceneWiringTests` 锁定两个场景字段；`Chapter01RuntimeFlowPlayModeTests` 增 RitualCore 越权负向场景测试 |
| 编译 | Unity Bee/Roslyn 直接编译 Runtime、Editor、EditMode、PlayMode 四个程序集均为 exit 0；仅有工作区既有 obsolete API 警告 |
| 修后 GUI 负向复验 | fresh `CP01` 状态执行 `Teleport Player To Ritual Core` 后仍停留在 `Entrance Tutorial`，数帧内没有章节完成卡，也没有获得 RitualCore，越权结算已被硬门禁拒绝 |
| 完整 GUI 主线 | 从入口重新走到结算：A01 / A02 使用真实 LMB、Q、E 输入清场；CP02 正常恢复生命 / 法力；A03 和 Gatekeeper 因 Computer Use 无法持续按住移动 / 防御键，使用正式激活 + `HealthComponent.ReceiveDamage` 死亡链完成；随后实体拾取 GateSigil、进入 Area04、击败 Gatekeeper、实体拾取 RitualCore，最终显示 `Chapter 01 Cleared` |
| 最终章节状态 | 四个区域均已访问、四场 Encounter 均清空、GateSigil 与 RitualCore 均持有、`chapterCompleted=true`；原用户存档随后再次恢复，活动文件与备份 SHA-256 均为 `2679d6163e71ca45cf640cbcc35c85ff4bf4a3a9bfca4ab3d822ce89598ac0d8` |
| Unity 定向回归 | GUI Test Runner：`KeyItemPickupTests 4/4`、`Chapter01ProgressionSceneWiringTests 28/28`、`Chapter01RuntimeFlowPlayModeTests 2/2`，均为 `0 failed`；PlayMode XML：`/tmp/ty_new_chapter01_gui_20260710/TY_NEW_Chapter01RuntimeFlowPlayMode_2Passed.xml` |
| public-safe 主树夹具 | GUI Test Runner：`CombatTestAnimationAssetWiringTests 17/17`、`ReleaseCandidatePreflightTests 5/5`、`BuildSettingsSceneOrderTests 1/1`，加上 Chapter01 scene wiring 后为 `51/51`；`python3 Tools/ghostsamurai/generate_catalog.py --check` 通过 |
| 临时克隆完整门禁边界 | `ty-new-ghostsamurai-baseline-check --chapter01` 已复跑，但在 clone repair 阶段、测试前被 `attempt to write a readonly database` 和 Licensing IPC channel 连接失败阻断；日志为 `/tmp/ty_new_chapter01_baseline_20260710_final/TY_NEW_ghostsamurai_baseline_repair_20260710_214729.log`，没有 XML，不能把“主树 51/51”冒充成“临时克隆修复证明通过” |

本轮已完成首个 P0 修复和入口到结算的真实 GUI 闭环。A03 侧路与 Boss 终门几何仍可被绕到物品附近，但拾取器硬门禁已证明会拒绝越权进度，因此它不再阻断第一章主流程收口；剩余验证债仅是当前机器授权 / 数据库环境下无法产出“临时克隆修复后”的完整门禁 XML。
