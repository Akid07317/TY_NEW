# 并行问题跟踪表（Review Action Track）

## 1. 目的

本文件来自 `2026-04-15` 的全项目代码审查，作为主开发蓝图之外的并行问题流使用。

它不替代 `Docs/Development_Blueprint_V1.md` 的功能排期，而是专门处理以下问题：

- 已经进入代码主干，但会直接伤害可玩性的运行时缺陷
- 会削弱“一个核心，多模块”目标的结构性问题
- 会让后续扩展、测试或迭代成本陡增的工程问题

主进程继续围绕“战斗纵切 -> 章节流 -> Boss -> 发布候选”推进；副进程负责持续清理高风险缺陷，保证主进程不是建立在脆弱代码之上。

## 2. 执行规则

| 规则 | 说明 |
|---|---|
| 主进程不改 | 功能推进仍以 `Docs/Development_Blueprint_V1.md` 为准，不因低优先级问题频繁打断 |
| `P0` 当周清零 | 任何会破坏主流程、战斗可信度、存档恢复的 `P0`，必须在当前周里程碑结束前关闭 |
| `P1` 按模块并行 | `P1` 问题允许作为副进程并行处理，但不得拖到发布候选后 |
| 每次修复都补验证 | 每个问题关闭时，必须补至少一项验证：`EditMode`、`PlayMode`、人工复现步骤三选一 |
| 不做无边界重构 | 架构问题优先做“最小可落地修补”，避免在第一版阶段开启大范围重写 |
| 周回顾必检查 | 每周回顾时先看本表，再决定是否允许进入下一周交付物 |

## 3. 当前问题列表

| ID | 级别 | 模块 | 问题 | 风险 | 建议处理方向 | 状态 |
|---|---|---|---|---|---|---|
| `P0-SAVE-001` | `P0` | 存档 / 检查点 / 玩家状态 | 死亡后只切入 `PlayerDeathState`，但死亡态为空，`SaveService` 与 `CheckpointService` 也没有接到运行时主流程，导致“章节级自动存档 + 检查点恢复”目前停留在组件存在、流程未闭环的状态 | 主章节承诺的恢复链路不成立，属于阻断级缺陷 | 已补 `CheckpointRestoreCoordinator`、`CheckpointRuntimeAnchor`、玩家恢复接口，并在 `CombatTest` 场景接入默认检查点与自动存档 | `Closed` |
| `P0-COMBAT-001` | `P0` | 敌人战斗 | 敌人进入攻击态后，延迟结束即直接对当前目标调用 `ReceiveDamage`，没有距离重检、角度校验或命中盒确认，容易出现“前摇开始时在范围内，结算时已脱离仍被打中”的远距离命中 | 会直接破坏战斗手感和玩家对读招/闪避的信任 | 已为敌人攻击补出手帧距离/朝向重检，并修正冷却递减时序，避免用连续空挥“等待 CD” | `Closed` |
| `P1-INPUT-001` | `P1` | 输入 | `InputReader` 把匿名回调绑定到共享 `InputActionAsset` 上，但没有成对解绑；随着场景重载、重复进出测试场景或重新生成对象，存在重复触发输入事件的风险 | 会制造偶发双击、重复攻击、重复菜单呼出等难复现问题 | 已改为运行时克隆 `InputActionAsset`，并在 `OnDisable` / `OnDestroy` 成对注册解绑；已补输入生命周期回归测试 | `Closed` |
| `P1-COMBAT-002` | `P1` | 玩家体术 | 轻攻击结束后使用取模推进下一段，导致第三段后仍可重新回到第一段，和项目文档中定义的“地面轻攻击 3 连”不一致 | 连招终结感被削弱，也会影响后续数值和平衡判断 | 已改为显式三段终结，末段结束后重置到起手；已补轻连段终结测试 | `Closed` |
| `P1-ARCH-001` | `P1` | 架构 / 模块化 | 当前运行时代码只有一个 `CampusRPG.Runtime.asmdef`，同时多个系统通过 `FindAnyObjectByType` 做跨模块取依赖；目录有模块划分，但编译边界与依赖注入边界还没建立起来 | 项目继续扩展到第一章完整内容后，耦合会明显上升，测试成本也会继续变高 | 已补 `SceneRuntimeContext`、`GameBootstrap.Active` 与执行顺序约束，运行时代码中的全局场景查找已清零，并拆出 `CampusRPG.Runtime.Input` / `CampusRPG.Runtime.Core` 两个最小运行时程序集 | `Closed` |
| `P1-STATE-001` | `P1` | 玩家状态机 / 生命周期 | `PlayerStateMachine` 只在 `Initialize()` 时绑定输入与死亡事件，`OnDisable()` 会解绑，但重新启用后没有重绑路径；一旦玩家对象被关开一次，就可能失去输入响应或死亡切换 | 会制造难复现的“角色突然不吃输入/不进死亡态”问题，伤害主流程可信度 | 已补 `OnEnable` 重绑与防重复订阅保护，并新增状态机生命周期回归测试 | `Closed` |
| `P1-INTERACTION-001` | `P1` | 交互 / 存档同步 | `KeyItemPickup` 与一次性 `TriggerVolume` 不会根据读档后的章节进度同步自身状态，导致已拿过的关键物、已消费的区域触发器在重新进场后重新出现 | 不一定阻断主流程，但会削弱章节可信度，并给门禁/引导制造“看起来没存住”的错觉 | 已补交互侧进度同步与两条 EditMode 回归测试，并完成真实 Unity EditMode 验证：`KeyItemPickupTests`、`TriggerVolumeTests` 均通过 | `Closed` |
| `P0-ENCOUNTER-001` | `P0` | 章节推进 / 存档恢复 | 未清场的 Encounter 在玩家死亡回档时，只会重置当前仍处于激活状态的敌人；已经死亡并被禁用的成员不会随回档恢复，导致“未清场遭遇战”可能以半清空状态跨检查点恢复 | 会直接破坏章节门禁、公平性和存档恢复可信度，属于新的阻断级缺陷 | 已为 `EncounterController` 补检查点恢复重置入口，并由 `CheckpointRestoreCoordinator` 在读档后统一重置 Encounter，再通过新增 Encounter 回档测试收口 | `Closed` |
| `P0-BUILD-001` | `P0` | 构建环境 / 批处理 | `macOS` 下 `Unity Hub` 常驻的旧版 `UnityLicensingClient` 会与 `Unity 6000.4.2f1` 编辑器产生授权协议冲突；授权问题解除后，工程还暴露出 HDRP 模板残留导致的包编译阻塞，直接卡死 `-batchmode` 场景生成和自动化测试 | 会阻断主干的自动化构建、编辑器工具执行和测试回归，属于环境级阻断 | 已确认根因并收敛方案：批处理前退出 `Unity Hub`、在临时克隆目录执行自动化、移除 HDRP 运行时包与项目管线引用；已验证 `Chapter01` 场景构建链路恢复 | `Closed` |
| `P2-TOOLS-001` | `P2` | 编辑器工具 | `CombatTest` 生成工具偏向“一键覆盖式”重建，当前适合初始化，不适合设计细调后的重复执行 | 后期一旦在测试场景里做人工调参，重新生成有覆盖风险 | 已为默认菜单补覆盖确认弹窗，并在 `CombatTest` 搭建文档中明确列出会被重建的场景与 Prefab；批处理仍保留强制重建能力 | `Closed` |
| `P2-TEST-001` | `P2` | 测试 | 当前自动化测试只覆盖了极少量数据类和一个锁定 happy path，尚未覆盖死亡恢复、敌人攻击命中时机、输入订阅生命周期、轻连段终结等高风险链路 | 发布前容易靠人工回归兜底，成本高且漏测概率大 | 已补齐 `CheckpointRestoreTests`、`EnemyAttackRangeTests`、`InputReaderLifecycleTests`、`PlayerComboTerminationTests`，并完成最新一轮真实 Unity `EditMode 15/15`、`PlayMode 4/4` 回归 | `Closed` |

## 4. 并行排期建议

| 时间窗 | 主进程目标 | 副进程必须完成 |
|---|---|---|
| 当前至第 2 周结束 | 战斗纵切稳定、前半章可玩 | 关闭 `P0-SAVE-001`、`P0-COMBAT-001` |
| 第 3 周 | 后半章、Boss、章节闭环 | 关闭 `P1-INPUT-001`、`P1-COMBAT-002` |
| 第 4 周 | 调优与发布候选 | 继续扩展章节内容，并在新增系统进入主干时沿用当前的最小程序集边界与自动化回归基线 |

## 5. 关闭标准

一个问题只有在同时满足以下条件后才允许从 `Open` 改为 `Closed`：

1. 代码主干中已经落地，而不是只停留在文档或待办说明。
2. 有明确验证记录，至少能证明主问题已不可复现。
3. 没有引入新的阻断级 Console Error 或主流程回归。
4. 若问题涉及系统边界，修复方案不能进一步扩大耦合。

## 5.1 本次关闭记录（2026-04-15）

- `P0-SAVE-001`：已通过新增 `CheckpointRestoreCoordinatorTests` 验证死亡后会回到激活检查点，并重新写回自动存档。
- `P0-COMBAT-001`：已通过新增 `EnemyAttackControllerTests` 验证越界目标不会被命中，近距离目标仍可正常吃伤害。
- `P1-INPUT-001`：已通过新增 `InputReaderLifecycleTests` 验证重复启停不会累积 `performed` 订阅，且不再污染共享输入资源。
- `P1-COMBAT-002`：已通过新增 `PlayerCombatControllerTests` 验证第三段轻攻击结束后不会循环回第一段，必须重新起手。
- `P1-ARCH-001`：已通过移除运行时全局场景查找、补 `SceneRuntimeContext` / `GameBootstrap.Active`，并拆出 `Input`、`Core` 两个独立运行时程序集验证最小编译边界可用。
- `P1-STATE-001`：已补 `PlayerStateMachine` 在重新启用时的重绑逻辑与去重保护，并新增生命周期测试验证启停后不会丢失输入和死亡事件订阅。
- `P1-INTERACTION-001`：已通过真实 Unity EditMode 回归验证 `KeyItemPickupTests` 与 `TriggerVolumeTests`，确认读档/回档后的交互物状态同步路径成立。
- `P0-ENCOUNTER-001`：已补 `EncounterController.ResetForCheckpointRestore()` 与协调器侧的 Encounter 统一重置，并通过新增 Encounter 回档重置测试验证未清场遭遇战不会以半清空状态跨回档。
- `P0-BUILD-001`：已确认 `Unity Hub` 常驻旧版授权客户端与 `6000.4.2f1` 授权协议不兼容，并清理 HDRP 模板残留；通过批处理成功生成 `Chapter01_Combined.unity` 与章节进度资产，验证构建主链恢复。
- `P2-TOOLS-001`：已为 `CombatTest` 重建入口补覆盖确认，并在接线文档中明确标注会被覆盖的场景与 Prefab。
- `P2-TEST-001`：已补齐四条关键回归测试，并通过最近一轮 `EditMode 5/5`、`PlayMode 4/4` 回归确认主链路稳定。

## 5.2 当前验证阻塞（2026-04-15）

- `2026-04-16` 追加回归：`EncounterControllerTests` 通过补齐测试侧成员生命周期初始化后恢复稳定；最新真实 Unity 回归为 `EditMode 15/15`、`PlayMode 4/4` 全通过。
- 后续环境约束：只要在 `macOS` 上继续使用 `Unity 6000.4.2f1` 批处理，就默认保持“退出 `Unity Hub` + 优先对临时克隆目录跑自动化”的执行纪律，直到确认 Hub 侧授权客户端版本与编辑器对齐。

## 6. 审查结论摘要

当前项目已经有一个能继续长出的动作 RPG 代码骨架，但主干还没有完全达到“一个核心，多模块”的稳定状态。

真正需要优先处理的不是再加新系统，而是先把以下三件事做实：

- 死亡、检查点、自动存档恢复闭环接通
- 敌人攻击命中逻辑从“直接扣血”升级为“可信命中”
- 模块边界从“能跑”提升到“可维护、可复测”

这三件事处理到位后，主进程再继续扩展章节内容，整体风险会低很多。
