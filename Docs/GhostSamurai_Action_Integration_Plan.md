# GhostSamurai 动作接入设计

本文件只服务本机研究预览。`Assets/GhostSamurai_Animset/` 不进入公开仓库默认基线；正式发布链仍以 `_Game` 下 proxy / approved 资产为准。素材边界见 [素材来源清单](Asset_Source_List.md)。

## 1. 动画库概况

本地包位于 `Assets/GhostSamurai_Animset/`。本轮已把“估计量级”改成可重复生成的真实清单：运行 `python3 Tools/ghostsamurai/generate_catalog.py` 会刷新 [GhostSamurai 动画清单](GhostSamurai_Clip_Catalog.md)。
清单现在除了分类统计，也会自动输出“玩家 core / 攻击 / SwordArt / 敌人读招 / 处决研究”的映射覆盖表，作为本轮 local-preview 研究证据面。
`Tools/ghostsamurai/clip_mappings.json` 现在是这条研究线的共享锚点：目录生成脚本和编辑器测试都从这里取映射，避免“文档表、候选链、验证期望”继续各写一份后漂移。

当前统计基线：

- `FBX 1134` 个
- `katana 668` 个
- `Bow 457` 个
- `Root 511` 个
- `Inplace 510` 个
- `Other / Pose / Sample / Unmarked 113` 个

本轮最关键的目录面：

| 分支 | 主要用途 | 接入优先级 |
|---|---|---|
| `Animation/katana/APose/Attack/` | 刀攻击、空中攻击、SP 攻击、跳斩 | 玩家与 Boss 主接入源 |
| `Animation/katana/APose/Defense/` | 格挡、格挡行走、破防、招架 | 玩家防御、Boss 破防、反击读招 |
| `Animation/katana/APose/Deflect/` | 左右弹反、失败、处决 / 被处决 | Counter、IronGate、精英敌人响应 |
| `Animation/katana/APose/Dodge/` | Avoid / Slide 多方向闪避 | Dodge、CombatRoll、追滚惩罚 |
| `Animation/katana/APose/Movement/` | 八方向 walk / run / strafe、jump | 玩家本地预览基础动作 |
| `Animation/katana/APose/Hit/Die/Execution/` | 受击、重受击、死亡、处决 | Hit、GuardBreak、Death、Boss 处决演出候选 |
| `Animation/Bow/` | 弓、瞄准、蹲伏、空中射击、弓移动 | 远程敌人与后续弓兵原型 |

其中优先看的细分数量如下：

- `APose / Attack = 103`，其中 `Root 51 / Inplace 51 / Other 1`
- `APose / Defense = 96`，其中 `Root 45 / Inplace 45 / Other 6`
- `APose / Deflect = 90`，其中 `Root 30 / Inplace 30 / Sample 30`
- `APose / Dodge = 38`，其中 `Root 19 / Inplace 19`
- `APose / Movement = 75`，其中 `Root 36 / Inplace 38 / Other 1`
- `APose / Hit = 34`，其中 `Root 17 / Inplace 17`
- `APose / Die = 22`，其中 `Root 11 / Inplace 11`
- `APose / Execution = 69`，其中 `Root 22 / Inplace 22 / Other 25`
- `Common / Base = 56`，`CommonCrouch = 37`，`Unarm&Equip = 14`
- `Bow` 主分支总计 `457`，其中 `Attack 97`、`Movement 118`、`Dodge 34`、`Hit 35`、`Die 22`

导入时会出现一批 Avatar rig mismatch warning，但 clip 仍可被 `AssetDatabase` 读取。自动化应把这些 warning 当成“需要逐步挑选和验证动作”的信号，而不是立即阻断本地研究预览。

## 2. 玩家动作映射

第一目标不是一口气把所有状态都变复杂，而是先把 `CombatTest` 当前已有输入和状态都映射到一组“更像样、可读、能调手感”的 GhostSamurai 候选上，再用 SO / Animator / HUD / 测试逐步收紧。

### 2.1 基础动作与移动

这批动作本轮已经进入候选路径合同测试，重点是“玩家看得懂自己现在在做什么”，而不是先追求华丽。

| 游戏动作 | 首选 GhostSamurai 候选 | 目标问题 |
|---|---|---|
| `Idle` | `GhostSamurai_APose_Idle` | 让导入预览站姿、重心和持刀姿态先稳定下来 |
| `Walk_Forward` | `GhostSamurai_APose_Strafe_Walk_F_Loop_Inplace` | 锁定近身博弈时保持前压可读 |
| `Walk_Backward` | `GhostSamurai_APose_Strafe_Walk_B_Inplace` | 后撤不是滑冰，能直观看出“边看边退” |
| `Walk_Left/Right` | `GhostSamurai_APose_Strafe_Walk_L/R_Inplace` | 为侧向解招保留清楚身体朝向 |
| `Run_Forward` | `GhostSamurai_APose_Strafe_Run_F_Loop_Inplace` | 保持中立推进的刀手姿态 |
| `Run_8Way` | `GhostSamurai_APose_Strafe_Run_FL/FR/BL/BR_Inplace` | 给锁定八向移动真正的方向差异 |
| `Airborne` | `GhostSamurai_APose_Jump_Loop_Inplace` | 空中停留和追击入口先看清姿态 |
| `Block` | `GhostSamurai_DefenseR_Loop_Inplace`、`GhostSamurai_DefenseL_Loop_Inplace` | 强化“格挡可解”，先把架势读出来 |
| `Dodge` | `GhostSamurai_APose_Dodge_F_Inplace`、`GhostSamurai_APose_Avoid_F_Inplace` | 短闪要短、要清楚，不和 roll 混 |
| `CombatRoll` | `GhostSamurai_APose_Slide_F_Inplace` | roll 要明显是更长位移、更大承诺 |
| `AirDodge` | `GhostSamurai_APose_Avoid_F_1_Inplace` | 空中规避要和下砸、空中攻击分得开 |
| `Hit` | `GhostSamurai_APose_Hit_F_Inplace` | 普通受击短促，不抢敌人下一拍起手 |
| `GuardBreak` | `GhostSamurai_DefenseR_Broken_Inplace`、`GhostSamurai_DefenseL_Broken_Inplace` | 破防必须一眼看出“硬挡错了” |
| `Death` | `GhostSamurai_APose_Die01_Inplace` | 先选最稳、最短、落地不飘的死亡版本 |

### 2.2 玩家攻击 / 反击套

| 游戏动作 | 首选 GhostSamurai 候选 | 设计意图 |
|---|---|---|
| `Light_01` | `Attack01_1_ALL_Inplace` | 快速横切，startup 短，给玩家最基础的起手 |
| `Light_02` | `Attack04_Inplace`、`Attack02_5_ALL_Inplace`、`Attack01_2_ALL_Inplace` | 二段改用跨族前切动作，先保证肉眼读感和第一刀分开 |
| `Light_03` | `SPAttack02_Inplace`、`Attack06_Inplace`、`Attack03_3_ALL_Inplace` | 三段改用特殊大收尾动作，优先解决三连击同质问题 |
| `Heavy_01` | `Attack03_4_ALL_Inplace`、`Attack06_Inplace` | 明显蓄势 / 大幅度斩击，用于破韧和大硬直 |
| `DodgeFollowUp` | `Dodge_Attack_F_Inplace`、`Attack02_1_ALL_Inplace` | 闪避后前切，作为成功闪避奖励 |
| `DodgeFollowUp_Enhanced` | `Dodge_Attack_B_Inplace`、`Attack02_2_ALL_Inplace` | 回身切，适合锁定目标绕身后的反击 |
| `Counter` | `LAttack_DeflectR_CounterExecution_Inplace` | 成功弹反后的短处决感，但不能盖过 Boss 读招 |
| `Counter_Enhanced` | `RAttack_DeflectL_CounterExecution_Inplace`、`SPAttack06_Inplace` | 更强 counter，用于完美格挡或高资源反击 |

当前判断：玩家 core local-preview 套已经不是“少量候选”，而是 `Idle / Walk / Run / Airborne / Block / Dodge / CombatRoll / AirDodge / Hit / GuardBreak / Death` 加 `Light 1-3 / Heavy / DodgeFollowUp / Counter / 6 条 SwordArt` 的整套优先级链。接下来不要盲目扩动作数量，而是继续拿合同、HUD 和 CombatTest 去筛。

## 3. SwordArt 设计

现有 SwordArt 先按“能读、能解、能调”的方向扩展。自动化每轮最多把一个 SwordArt 做成完整闭环：clip 候选、SO 参数、Animator 状态、测试、HUD 观察点。

| SwordArt | 候选 clip | 手感定位 |
|---|---|---|
| `Sidewind Cut` | `Attack02_1_ALL`、`Dodge_Attack_F` | 闪避后横切，短前摇、短硬直，适合补刀 |
| `Cross Step` | `Attack02_4_ALL` | roll 后穿步斩，强调位移终点和朝向 |
| `Rising Cleave` | `Attack03_4_ALL`、`Attack06` | 前推重斩，打断轻敌但不能无脑压 Boss |
| `Iron Gate Break` | `DefenseR_Parry_Up_Execution`、`SPAttack06` | 防反破门招，应该有清楚的挡架转攻 |
| `Falling Star` | `JumpAttack04`、`Air_Attack03_Start/Loop/End` | 空中下砸，必须保留落地 recovery |
| `Moon Sever` | `SPAttack03`、`SPAttack05` | 空中 dodge 后横向大斩，重在轨迹可见 |

### 3.1 本轮纵切：`Iron Gate Break`

这轮只把一招收紧成“文档、SO、运行时反馈、测试都一致”的完整合同，不顺手扩别的招。

| 合同项 | 当前落地 |
|---|---|
| 玩家要解决的问题 | 对“敌人举盾/强行硬挡/收招慢的重挥”给出一个清楚的破防重斩，不再让重击链只有纯伤害差别 |
| 输入 / 触发 | `HeavyAttack`；`AfterBlock` 或 `AfterHeavy` 上下文，`triggerWindowSeconds = 0.35`，`cancelWindowSeconds = 0.22` |
| 预览候选 clip | `GhostSamurai_DefenseR_Parry_Up_Execution_Inplace` 主姿态，回退 `GhostSamurai_APose_SPAttack06_Inplace` |
| Startup / Active / Recovery | `0.14 / 0.12 / 0.34`；运行时仍会保留额外 follow-through，不把重招硬切回 locomotion |
| Forward movement | 总前送 `0.55m`，改为 `0.08s` 后开始、`0.22s` 内分布式前送；先亮出挡架转攻，再把位移压到出手段 |
| Hitbox | `Box`；`center = (0, 0, 1.125)`，`halfExtents = (0.92, 1.00, 0.90)`，覆盖近身破门而不是远距突刺 |
| 命中反馈 | `hitStopSeconds = 0.08`，并打开 `breaksGuard`，让音频 / 镜头 / HUD 都把它读成 guard-break attack |
| 资源成本 | `15 MP`。本轮已把 `resourceCost` 接进运行时扣费链；HUD 预览 / cancel 窗口会直接显示 `15 MP`，若 MP 不足则提示 `NEED 15 MP`，实际输入回退到原本 `Counter/Heavy` 路径，不把 `Iron Gate Break` 悄悄白出 |
| 敌人如何回应 | 普通盾兵不能继续硬挡；机动敌人应在 startup 侧移或后撤；Boss / 精英应优先用 dodge / deflect 提前回应，而不是等 active 吃满破防 |

当前生成链也已把 `Iron Gate Break` 的 local-preview 首选 clip 从 `Counter` 共享的 `Deflect` 候选里拆开，改成优先走 `DefenseR_Parry_Up_Execution_Inplace`，再回退到 `SPAttack06` / `Attack03_4_ALL`。这样本机预览先读出“挡架转攻”，不会再被误看成单纯的 counter-execution。

### 3.2 本轮补齐：`Moon Sever`

这轮不再扩第七条招式，只把 `Moon Sever` 从“已有 clip + 已能触发”补成和 `Iron Gate Break` 同级的合同锚点，重点锁住它与 `Falling Star` 的节奏差异。

| 合同项 | 当前落地 |
|---|---|
| 玩家要解决的问题 | 给 `AirDodge` 成功脱离后的玩家一个清楚、横向、可读的空中追击，不把所有空中 heavy/light 入口都压成 `Falling Star` 那种下砸节奏 |
| 输入 / 触发 | `LightAttack`；必须同时满足 `Airborne + AfterDodge + AfterAirDodge`，`triggerWindowSeconds = 0.28`，`cancelWindowSeconds = 0.16` |
| 预览候选 clip | `GhostSamurai_APose_SPAttack03_Inplace` 主姿态，回退 `GhostSamurai_APose_SPAttack05_Inplace`；local-preview 导入时长 override 固定为 `0.72s`，避免横切只剩一拍短空挥 |
| Startup / Active / Recovery | `0.10 / 0.10 / 0.26`；public-safe baseline 维持短促，imported preview 则保留更长横切 follow-through，和 `Falling Star` 的长落地承诺分开 |
| Forward movement | 总前送 `0.58m`，`movementSpeedScale = 0.72`；保持空中横切的追击感，但不拉成长位移 roll punish，也不做 `Iron Gate Break` 那种分布式重前送 |
| Hitbox | `Box`；`center = (0, 0, 1.075)`，`halfExtents = (0.782, 0.85, 0.86)`，覆盖玩家身前的横向收割面，不把判定做成下砸圆柱 |
| 命中反馈 | `damageMultiplier = 1.65`，`hitStopSeconds = 0.065`；强于轻击，但明显轻于 `Falling Star` 的重落地反馈 |
| 资源成本 | `12 MP`。本轮已把 `resourceCost` 接进运行时扣费链；`AirDodge + Light` 仍会先给 HUD 预览，但 MP 不足时会显示 `NEED 12 MP`，真正落地时回退普通 `Light`，不会把 `Moon Sever` silently 误出 |
| 敌人如何回应 | 近战 / 机动敌人应在玩家 `AirDodge` 前半拍就读出并落地防守，不能等横切 active 再硬吃；远程敌人更适合在 `Moon Sever` recovery 反压，而不是把它当成 `Falling Star` 一样只等落地反空 |

当前自动化目标不是再给 `Moon Sever` 加特效或新状态，而是把“它为什么存在、和 `Falling Star` 有什么不同、local preview 为什么要保留更长横切尾段”写成文档和测试都能复核的证据。

### 3.2A 本轮补齐：`Rising Cleave / Falling Star` 空中 Heavy 对照

这轮不再扩第七条招式，而是把空中 heavy 的两个分支补成“看得见差异、测得出合同、终端一条命令就能复核”的观察链。重点是锁住 `Forward Heavy` 与 `Neutral/Backward Heavy` 的分工，避免本地预览又把两招看成同一类空中大挥。

| 合同项 | `Rising Cleave` | `Falling Star` |
|---|---|---|
| 玩家要解决的问题 | 给前推或空中追击一个更像“追上去砍”的 forward chase，不让空中 heavy 只有下砸答案 | 给中立/后撤空中 heavy 一个更像“压下去砸”的 slam 入口，保留落地承诺 |
| 输入 / 触发 | `HeavyAttack`；方向不限，但要命中 `ForwardInput` 或 `Airborne` 任一上下文，`triggerWindowSeconds = 0.30`，`cancelWindowSeconds = 0.20` | `HeavyAttack`；只接受 `Neutral/Backward`，必须满足 `Airborne`，`triggerWindowSeconds = 0.32`，`cancelWindowSeconds = 0.18` |
| 预览候选 clip | `GhostSamurai_APose_Attack03_4_ALL_Inplace` 主姿态，回退 `GhostSamurai_APose_Attack06_Inplace`；preview override `1.00s` | `GhostSamurai_APose_JumpAttack04_Inplace` 主姿态，回退 `GhostSamurai_APose_Air_Attack03_Start_Inplace`；preview override `1.05s` |
| Startup / Active / Recovery | `0.18 / 0.12 / 0.38`，更像向前追击后的中承诺重斩 | `0.16 / 0.14 / 0.42`，active 更重、recovery 更长，读成落地砸击 |
| Forward movement | 总前送 `0.62m`，`movementSpeedScale = 0.58`，范围 `2.35m`；强调向前吃空间 | 总前送 `0.38m`，`movementSpeedScale = 0.52`，范围 `2.05m`；强调砸地而不是追人 |
| Hitbox | `Box`；`center = (0, 0, 1.175)`，`halfExtents = (0.851, 0.925, 0.94)` | `Box`；`center = (0, 0, 1.025)`，`halfExtents = (0.989, 1.075, 0.86)` |
| 资源成本 | `0 MP`，当前先测身体语言和输入语法，不额外加资源门槛 | `0 MP`，当前先让空中重击分支稳定可读，再决定要不要加成本 |
| 敌人如何回应 | 机动敌人/Boss 应在 startup 读出前追意图，优先侧移、反空或预留近身 counter space | 敌人应把它读成更重的落地 slam，优先拉开或延后反压，而不是像 `Rising Cleave` 一样只做近身换位 |

### 3.3 本轮补齐：`Sidewind Cut / Cross Step`

这轮不再扩新招数，而是把 `SwordArt 1` 这组“闪后横切 vs roll 后穿步斩”补成可观察、可测试、可对照的合同。重点不是数值变强，而是先把“短 dodge 奖励”和“长 roll counter”分成两种清楚的身体语言。

| 合同项 | `Sidewind Cut` | `Cross Step` |
|---|---|---|
| 玩家要解决的问题 | 给成功侧闪后的玩家一刀短、快、贴身的 flank 奖励，不再让地面 dodge follow-up 都落成 generic `DodgeFollowUp` | 给成功 `CombatRoll` 后的玩家一刀更长位移、更大承诺的穿步斩，明确区分短 dodge 与长 roll 的收益 |
| 输入 / 触发 | `LightAttack`；必须满足 `AfterDodge + Left/Right`，`triggerWindowSeconds = 0.25`，`cancelWindowSeconds = 0.18` | `LightAttack`；必须满足 `AfterDodge + AfterCombatRoll`，方向不限，`triggerWindowSeconds = 0.30`，`cancelWindowSeconds = 0.18` |
| 预览候选 clip | `GhostSamurai_APose_Dodge_Attack_F_Inplace` 主姿态，回退 `GhostSamurai_APose_Attack02_1_ALL_Inplace` | `GhostSamurai_APose_Attack02_4_ALL_Inplace` 主姿态，回退 `GhostSamurai_APose_Attack02_4_Inplace` |
| Startup / Active / Recovery | `0.08 / 0.10 / 0.25` | `0.09 / 0.10 / 0.27` |
| Forward movement | 总前送 `0.72m`，`movementSpeedScale = 0.82`；只够把“闪开后补一刀”读清，不拉成长 chase | 总前送 `0.86m`，`movementSpeedScale = 0.84`；终点更深，明确读成 roll counter 而不是短 dodge 奖励 |
| Hitbox | `Box`；`center = (0, 0, 1.025)`，`halfExtents = (0.759, 0.825, 0.82)`，覆盖贴身侧切面 | `Box`；`center = (0, 0, 1.125)`，`halfExtents = (0.828, 0.90, 0.90)`，覆盖更深一点的穿步终点 |
| 资源成本 | `0 MP`，当前先只考察身体语言和触发上下文，不额外加资源门槛 | `0 MP`，当前先验证“更长承诺但不额外吃蓝”的 roll follow-up 节奏 |
| 敌人如何回应 | 近战 / 机动敌人若没把 dodge 读空，应在玩家收招后立刻追回一拍；盾兵仍可继续正常格挡，不把它当 guard-break | 机动敌人和 Boss 应更适合用延迟挥砍、追滚或 reposition 去抓 `Cross Step` recovery，不能让它变成“白赚位移重置” |

后续可新增但暂不默认接线的研究候选：

| 新招式候选 | 候选 clip | 适合解决的问题 |
|---|---|---|
| `Iaido Flash` | `SPAttack01`、`Attack04` | 快速拔刀类消耗技，给玩家一个短爆发 |
| `Severing Rain` | `SPAttack04`、`Attack02_5/6_ALL` | 多段压制，适合测试连段命中窗口 |
| `Mirror Deflect` | `LAttack_DeflectL90/180`、`RAttack_DeflectR90/180` | 方向性弹反，后续服务高阶读招 |
| `Execution Probe` | `Execution`、`CounterExecuted` 系列 | 只做研究预览，不进入正式战斗循环 |

### 3.4 本轮补齐：`Execution` 研究角色拆分

这一类之前只有“处决候选”三个笼统占位，容易把攻击方、受体和伏击双边混在一起。当前先不把终结技硬接进正式战斗循环，而是先把 `Assets/GhostSamurai_Animset/Animation/katana/APose/Execution/` 拆成四条研究锚点，并把 Root / Inplace / Sample 的用途说清楚，后续要做双人终结或背刺时就不用重新翻 raw asset。

| 研究锚点 | 首选 clip | 当前用法 | 这轮解决的问题 |
|---|---|---|---|
| `Execution_Attacker` | `GhostSamurai_Execution01_Root`、`GhostSamurai_Execution06_Inplace` | 攻击方终结动作；先看 Root 推进，再看 Inplace 原地收势 | 不再把“攻击方主体姿态”和“受体倒伏”混成一个候选名 |
| `Executed_Victim` | `GhostSamurai_Executed01_Inplace`、`GhostSamurai_Executed05_Root` | 受体被终结姿态；先看单体倒伏，再看双人配对位移 | 让未来终结研究能单独评估受体塌陷、跪倒和落地读感 |
| `Ambush_Attacker` | `GhostSamurai_Ambush01_Root`、`GhostSamurai_Ambush03_Inplace` | 伏击/背刺起手；先看扑进交换站位，再看原地定格 | 把“伏击方起手”从普通终结攻击方里拆开，避免后续背刺研究还要重新筛 |
| `Ambushed_Victim` | `GhostSamurai_Ambushed03_Inplace`、`GhostSamurai_Ambushed02_Root` | 伏击受体姿态；先看失守塌陷，再看双人位移 | 把“被抓取的一侧”明确出来，不再误拿 `Ambush` 攻击方 clip 当受体参考 |

当前判断：

- `Root` 更适合先看站位交换、推进距离和双人配对是否靠谱。
- `Inplace` 更适合先看单体身体语言、收势和终结定格是否顺。
- `Sample` 先保留给双人配对观察，不作为首选接线候选。
- 这轮只把 catalog / manifest / 文档研究锚点补齐，不改正式战斗循环，也不把 execution raw clip 接进 release-safe baseline。

## 4. 敌人与 Boss 映射

敌人当前仍默认回到 proxy baseline；GhostSamurai 敌人接入只走 local preview。自动化应优先服务 Gatekeeper / 精英敌人的读招验证，而不是把普通敌人全部换装。

| 敌人动作 | 候选 clip | 读招目标 |
|---|---|---|
| Gatekeeper `Sky Hook` | `Air_Attack03_Start/Loop/End`、`JumpAttack02/03` | 明显抬身 / 反空，不让玩家误以为普通平砍 |
| Gatekeeper `Pursuit Slam` | `Slide_F` 接 `Attack03_4_ALL` | 追滚惩罚，先位移再砸地 |
| Gatekeeper `Gate Slam` | `SPAttack06`、`DefenseR_Parry_Up_Execution` | 破防读招，玩家应看出“不能硬挡” |
| Melee pressure | `Attack01_1_ALL`、`Attack02_2_ALL` | 普通敌人只要节奏清楚，不抢 Boss 视觉层级 |
| Mobile feint | `Avoid_L/R` 接 `Attack02_1_ALL` | 侧向诱骗，验证锁定和镜头 |
| Ranged enemy | `Bow_AimWalk`、`Bow_AirShoot`、`Bow_CrouchShoot` | 弓兵原型候选，等近战链稳定后再上 |

### 4.1 本轮落地：enemy imported preview 控制器先读 GhostSamurai

这轮不去改正式 `BossTest` / `Chapter01` 基线，只把 `CampusRPG/Setup/Local Preview/Apply Imported Enemy Avatar Chain To CombatTest Enemy Prefabs` 生成出来的 imported enemy preview 控制器改成优先挑 GhostSamurai clip。目标不是“敌人马上换成最终资源”，而是先让本机 preview 里的 startup / active / recovery 更像真正的刀剑/弓读招。

| 预览控制器状态 | 首选 clip | 这轮解决的问题 |
|---|---|---|
| `EnemyMelee` idle / walk / run | `DefenseR_Loop` + `Strafe_Walk_F_Loop` + `Strafe_Run_F_Loop` | 让近战敌人站姿和推进都像持刀压迫，不再主要靠 Kevin/DoubleL 混合站姿 |
| `Attack_Melee` | `Attack01_1_ALL_Inplace` | 普通近战起手回到短、清楚、可挡的横切 |
| `Attack_Mobile` | `Attack03_4_ALL_Inplace` | mobile 敌人保持更重的前压挥砍，和普通近战区分开 |
| `Attack_AntiAir` | `Air_Attack03_Start_Inplace` | 反空起手先有明显抬刀/上挑感，不再只是普通 ranged state 加速 |
| `Attack_ChaseRoll` | `Slide_F_Inplace` | 追滚状态先读成“前压追上来”，和常规 mobile 挥砍拆开 |
| `Attack_GuardBreak` | `SPAttack06_Inplace`、`DefenseR_Parry_Up_Execution_Inplace`、`Attack03_4_ALL_Inplace` | 破防状态先有大承诺下砸感，帮助 `Gate Slam` 类重招读懂“不能硬挡” |
| `EnemyRanged` idle / walk / run | `Bow_Idle` / `Bow_AimWalk_F` / `Bow_AimRun_F` | 远程敌人读招先进入弓手姿态，而不是近战 dummy 在放投射物 |
| `Attack_Ranged` | `Bow_Shoot_Start_Inplace` | 弓兵起手先能看见拉弓，而不是继续吃 DoubleL bow 单一候选 |
| `Attack_AntiAir` for ranged preview | `Bow_AirShoot_Start_Inplace` | 让“抬手打空中目标”在本机 preview 里和地面射击分开 |

注意：

- 这仍然只是 `local preview / research source`。正式场景默认链不直接依赖 `Assets/GhostSamurai_Animset/`。
- 这轮优先保证 imported preview 控制器的状态 motion 有差异；`Pursuit Slam` 还没有做成“slide + slam”双段拼接，它现在先用单 clip 把“追上来”读清。
- `Attack_GuardBreak` 现在保留 `SPAttack06 -> DefenseR_Parry_Up_Execution -> Attack03_4_ALL` 的 GhostSamurai 候选链，避免 `Gate Slam` 预览一旦首选 clip 不可用就立刻退化成普通近战挥砍。
- 若后续要把 Gatekeeper 本体也切到 imported humanoid preview，应该单开显式 local-preview 路径，而不是把 `BossTest` / `Chapter01` 正式场景直接换成 imported chain。

### 4.1A 本轮修复：敌人 walk 与攻击轮询池

用户在 GUI 里检查时看到“敌人的行走动画又没有了”，本轮定位后不是 walk clip 缺失：`EnemyMelee` / `EnemyMobile` 仍首选 `GhostSamurai_APose_Strafe_Walk_F_Loop_Inplace`，`EnemyRanged` 仍首选 `GhostSamurai_Bow_AimWalk_F_Inplace`。真正风险在运行时 `GroundSpeed` 只吃实际位移 / NavMeshAgent velocity；本地预览刷新、agent 刚起步或 chase / strafe 状态速度采样短暂为 0 时，BlendTree 会掉回 idle，看起来像 walk 丢了。

本轮修复策略：

- `EnemyCombatAnimationPlanUtility` 对 `EnemyChaseState` 保底推到 `0.36` locomotion band，对 `EnemyStrafeState` 保底推到 `0.24` locomotion band；这样 chase / strafe 进入 locomotion 时，即使瞬时速度采样为 0，也会先落在 walk 可见区，不再直接回 idle。
- `EnemyCombatAnimationRelay` 不再只播放 `Attack_Melee` / `Attack_Mobile` / `Attack_Ranged` 的单一 state；每次重新进入攻击状态时按 `Attack_*_01 / _02 / ...` 轮询，当前攻击期间保持同一个 variant，不会每帧乱跳。
- `CombatImportedEnemyVisualUtility` 生成 imported enemy preview controller 时，会把攻击候选池展开成 variant states；基础 state 仍保留为 fallback，runtime 若没有 variant state 会自动退回旧 state name。

当前攻击池统计要分两层看：raw FBX 是文件数，controller variant 是 Unity 从这些 FBX 里展开出的 `AnimationClip` 子资产数。刷新 imported preview 后，当前三套 enemy controller 实际生成 `Attack_Melee_* = 96`、`Attack_Mobile_* = 96`、`Attack_Ranged_* = 54`；katana 两组已经打到本地预览上限，Bow 组少一些。生成器会排除 `AutoBackup` 子 clip，Bow 池还会排除 Idle / Hold 命名片段，避免首发攻击落到导入备份或站姿片段。

| 攻击池 | Raw Inplace FBX | Controller variant states | 动作族 | 本轮接线方式 |
|---|---:|---:|---|---|
| `katana/APose/Attack/Inplace` -> `Attack_Melee_*` | `51` | `96` | `Air_Attack / Attack01 / Attack02 / Attack03 / Attack04 / Attack05 / Attack06 / Attack07 / JumpAttack / SPAttack` 共 `10` 族 | 普通近战轮询池，保留 `Attack01` 动作族为 `_01` 起手 |
| `katana/APose/Attack/Inplace` -> `Attack_Mobile_*` | `51` | `96` | 同上，首选 `Attack03` 动作族 | 机动敌人轮询池，保留更大位移动作为 `_01` 起手 |
| `Bow/Attack/Inplace` -> `Attack_Ranged_*` | `30` | `54` | `Shoot / AirShoot / RainShoot` 为主要攻击族，另有 idle / hold / aim idle 过渡片段 | 远程轮询池；实际攻击轮询排除 `Idle` / `Hold` 命名片段，避免普通射击随机播成站姿 |
| 合计 | `81` | `246` | 约 `13` 个实战可用动作族 | 近战 / 机动 / 远程 enemy preview 都开始按重新进攻次数换 clip |

手动观察时要确认两件事：

- 敌人进入 chase / strafe 后应该先能看到 walk / aim-walk 身体语言，即使刚刷新场景时 agent 速度还没有稳定。
- 同一个敌人连续出招时，`Attack_Melee_01 -> Attack_Melee_02 -> ...` 或对应 archetype 的 variant 会轮询推进；如果用户刚在 Unity GUI 里看不到变化，先执行 `CampusRPG/Setup/Local Preview/Refresh Imported Combat Preview`。命令行对应入口是 `CampusRPG.Editor.CodexLocalPreviewBatchRunner.RefreshImportedCombatPreview`，这是“之前的解决方案”的关键步骤：现在它不只重新生成 controller / prefab，还会打开 `CombatTest.unity` 并执行 `CombatTestSceneBuilder.RefreshCombatTestScenePrefabInstancesFromSources()`，把场景里的 `Player` / `Enemy_Melee_A` / `Enemy_Mobile_A` / `Enemy_Ranged_A` 从源 prefab 强制同步一遍，同时保留场景里的名字和站位。脚本改完但没跑这一步时，Unity 里仍可能看到旧 scene instance 缓存。

### 4.2 本轮补齐：response-read 预备姿态层

上一轮 imported enemy preview 虽然已经把 `Attack_AntiAir / Attack_ChaseRoll / Attack_GuardBreak` 分成了不同 state，也把 `ResponseRead / AntiAirRead / ChaseRollRead / GuardBreakRead` 参数从运行时写进 Animator，但上半身层本身只有一个静态 `Hold`。结果是 HUD 和状态名在变，真正的“读招前身体语言”仍然不够清楚。

这轮把这块补成一个完整纵切：`CombatPose` 层保留 idle hold，但新增 `Read_AntiAir / Read_ChaseRoll / Read_GuardBreak` 三个上半身预备姿态，并由 `EnemyCombatAnimationRelay` 按 `responseReadNormalized` 推层权重。这样 imported preview 里的读招不再只有 attack state 本身，而会在 startup / advance / recovery 先亮出一层更清楚的预备姿态。

| 预备姿态 | 首选 clip | 类别 | 这轮解决的问题 |
|---|---|---|---|
| `Read_AntiAir` | `GhostSamurai_DefenseR_Parry_Up_Execution_Inplace` | `Defense` | 在 `Sky Hook` 前先抬刀亮出“上段/反空”意图，不再只看 attack clip 第一拍 |
| `Read_ChaseRoll` | `GhostSamurai_APose_Slide_Start_Inplace` | `Dodge` | 在 `Pursuit Slam` 前先压低身体、亮出追滚前压，而不是直接跳到冲刺末端 |
| `Read_GuardBreak` | `GhostSamurai_RAttack_DeflectL_CounterExecution_Inplace` | `Deflect` | 在 `Gate Slam` 前先给出大承诺蓄势，让“不能硬挡”先从身体语言读出来 |
| `Read_Ranged_AntiAir` | `GhostSamurai_Bow_AirShoot_Start_Inplace` | `Bow` | 弓兵打空中目标前先抬弓，而不是和地面射击同帧抬手 |
| `Read_Ranged_ChaseRoll` | `GhostSamurai_Bow_Dodge_F_Inplace` | `Bow` | 远程追滚前先侧身压步，和普通射击 body language 分开 |
| `Read_Ranged_GuardBreak` | `GhostSamurai_Bow_Shoot_SP04_Inplace` | `Bow` | 远程重压制前先给更重的预备姿态，不再只靠 cue 提示 |

当前设计判断：

- 这层仍然只服务 imported/local-preview 观察，不改变正式 release scenes 的默认 Animator。
- `Hold` 仍保留给 idle/低速站姿；真正 response read 出现时，relay 会把 `CombatPose` 权重从轻微 idle overlay 提升到明显可见的读招姿态。
- 这轮优先解决“看不看得懂读招”，不是追求完美过渡；后续若发现 `Slide_Start` 或 `Deflect` 作为上半身姿态还不够清楚，再继续替换候选，而不是先把更多新状态塞进正式链。

## 5. 激进接入节奏

自动化 `TY_NEW GhostSamurai 激进整合` 每小时跑一次，建议按下面顺序收敛：

1. `P0 Catalog`：生成或更新 GhostSamurai clip manifest，按 katana / bow / attack / defense / dodge / hit / die / execution 分类。本轮已落地到 [GhostSamurai 动画清单](GhostSamurai_Clip_Catalog.md)。
2. `P1 Player Core`：保证玩家基础动作、Light 1-3、Heavy、Block、Dodge、Hit、GuardBreak、Death 都有 GhostSamurai local preview 候选，并通过候选路径测试。本轮已把玩家 core locomotion / defense / reactive 路径并入 `CombatImportedPlayerAnimationSelectionTests`。
3. `P2 SwordArt`：每轮只完整推进一个 SwordArt，避免一次改太多 SO、Animator 和测试。
4. `P3 Boss Reads`：把 `Sky Hook`、`Pursuit Slam`、`Gate Slam` 的 local preview clip 接到可观察状态，重点看 startup、active、recovery 是否和 HUD 一致。
5. `P4 Bow / Execution`：近战手感稳定后，再把弓兵和处决类动作做成独立研究分支；当前 `Execution` 已先拆成 `Execution_Attacker / Executed_Victim / Ambush_Attacker / Ambushed_Victim` 四条研究锚点。
6. `P5 Baseline Proof`：每轮结束都要能说明如何回到 public-safe proxy baseline，不能让 release 场景硬依赖 GhostSamurai 目录。

## 6. 验证入口

本地研究预览优先使用：

```bash
Tools/unity-cli/ty-new-ghostsamurai-preview-check --startup-timeout 90
```

这条包装脚本会先执行 `python3 Tools/ghostsamurai/generate_catalog.py --check`，确认 [GhostSamurai 动画清单](GhostSamurai_Clip_Catalog.md) 仍和 `Tools/ghostsamurai/clip_mappings.json` + 本机 `Assets/GhostSamurai_Animset/` 扫描结果一致；随后把 local-preview 研究线常用的窄门绑在一起：`GhostSamuraiCatalogManifestTests`、`CombatImportedPlayerAnimationSelectionTests`、`CombatImportedPlayerVisualUtilityTests`、`CombatImportedEnemyAvatarPreviewTests`、`PlayerCombatAnimationRelayTests`、`PlayerCombatControllerTests`、`PlayerCombatRuntimeUtilityTests`、`SwordArtResolverTests`、`CombatTestIronGateBreakContractTests`、`CombatTestMoonSeverContractTests`、`CombatTestFlankSwordArtContractTests`、`CombatTestPlayerLungeCaptureDriverTests`、`GhostSamuraiCombatEnemyReadCaptureDriverTests` 与 `GhostSamuraiBossReadCaptureDriverTests`。它故意不带 `CombatTestAnimationAssetWiringTests`，因为主工作树可能正停在 imported/local-preview 脏态，直接把 public-safe baseline 断言混进来会把“预览中”误报成“回不去”。

若主工程 Unity GUI 已经开着，优先直接走菜单：

- `CampusRPG/Setup/Local Preview/Refresh Imported Combat Preview`

只有在主工程 GUI 没占用、并且需要终端刷新时，才使用 batch bridge；外层必须带明确 timeout：

```bash
python3 - <<'PY'
import subprocess

subprocess.run(
    [
        "/Applications/Unity/Hub/Editor/6000.4.2f1/Unity.app/Contents/MacOS/Unity",
        "-batchmode",
        "-quit",
        "-projectPath",
        "<PROJECT_ROOT>",
        "-executeMethod",
        "CampusRPG.Editor.CodexLocalPreviewBatchRunner.RefreshImportedCombatPreview",
        "-logFile",
        "/tmp/TY_NEW_refresh_ghostsamurai_preview.log",
    ],
    check=True,
    timeout=240,
)
PY
```

宽门禁仍应分两条看：

- local preview 门禁：GhostSamurai catalog/manifest、一组候选路径解析、`_Game` 生成片段、CombatTest 可观察性。
- release-safe 门禁：正式场景不能依赖 `Assets/GhostSamurai_Animset/`、`Assets/DoubleL/`、`Assets/Kevin Iglesias/`、`Assets/ithappy/` 等 local-preview-only 目录。

若本轮只是先确认“双线都绿”，再切回 GUI 做身体语言观察，优先使用：

```bash
Tools/unity-cli/ty-new-ghostsamurai-verify --startup-timeout 90
```

它会顺序执行 `preview-check` 与 `baseline-check`，直接打印 `passed/total/failed` 摘要，并把下一组 `observe-swordarts` / `observe-enemy-reads` / `observe-boss-reads` 命令一起列出来，作为本轮最短收尾入口。

当前新增一个更适合自动化收尾的恢复证明入口：

```bash
Tools/unity-cli/ty-new-ghostsamurai-baseline-check --startup-timeout 90
```

它会在 temp clone 里先排除 `Assets/GhostSamurai_Animset/`、`Assets/DoubleL/`、`Assets/Kevin Iglesias/`、`Assets/ithappy/`、`Assets/JC_LP_MedievalCharacters_LITE/` 等 local-preview-only raw asset roots，再执行 `Repair CombatTest Prefab Wiring`，最后跑 `CombatTestAnimationAssetWiringTests + ReleaseCandidatePreflightTests`。用途不是刷新 preview，而是证明“当前主树就算正停在 GhostSamurai / imported source 脏态，只要走 repair 路径，public-safe proxy baseline 仍可恢复且能通过窄门验证”；同时这也避免 fresh clone 先导入整包本地预览素材，把 baseline proof 自己拖成超时。

如果某轮还顺手碰到了 `Chapter01` baseline，则改用：

```bash
Tools/unity-cli/ty-new-ghostsamurai-baseline-check --chapter01 --startup-timeout 90
```

当前宽门禁可能仍会因为已有本地预览脏状态失败；自动化应先报告失败来源，再做最小修复，不要用 reset / revert 抹掉人工调试痕迹。

### 6.1 2026-04-29 双线验证快照

- 当前主工作树确实停在 imported/local-preview 脏态：直接把 `CombatTestAnimationAssetWiringTests` 和 local-preview 研究夹具一起跑，会命中 8 条 baseline 断言失败，症状集中在 `PF_Player_CombatTest` 仍挂 imported Avatar、玩家基础 clip 仍保留 imported 曲线、enemy prefab 仍停在 imported preview 可视状态，以及 `Light_02` / `Counter` 片段时长仍是 imported preview 裁切值。
- 本轮先用 `Tools/unity-cli/ty-new-ghostsamurai-preview-check --startup-timeout 90 --results-dir /tmp/ty_new_ghostsamurai_preview_20260429_round5` 与 `Tools/unity-cli/ty-new-ghostsamurai-baseline-check --startup-timeout 90 --results-dir /tmp/ty_new_ghostsamurai_baseline_20260429_round5` 做第 5 轮复核时，temp clone 先暴露出 `GhostSamuraiBossReadCaptureDriver.cs` 旧副本缺 `using CampusRPG.Editor` 的编译口子；症状是两条线都在进入测试前直接报 `CombatImportedEnemyVisualUtility` / `CombatProxyVisualKind` 未解析。这说明阻塞点是本地研究入口本身，而不是 release-safe baseline 被第三方素材绑死。
- 随后用 `Tools/unity-cli/ty-new-ghostsamurai-preview-check --startup-timeout 90 --results-dir /tmp/ty_new_ghostsamurai_preview_20260429_round6` 复跑 local-preview 研究线，结果升级为 `65/65 Passed`。这条线现在会先执行 `python3 Tools/ghostsamurai/generate_catalog.py --check`，再覆盖 `GhostSamuraiCatalogManifestTests`、`CombatImportedPlayerAnimationSelectionTests`、`CombatImportedPlayerVisualUtilityTests`、`CombatImportedEnemyAvatarPreviewTests`、`PlayerCombatAnimationRelayTests`、`PlayerCombatControllerTests`、`PlayerCombatRuntimeUtilityTests`、`SwordArtResolverTests`、`CombatTestIronGateBreakContractTests`、`CombatTestMoonSeverContractTests`、`CombatTestFlankSwordArtContractTests`、`CombatTestPlayerLungeCaptureDriverTests` 与 `GhostSamuraiBossReadCaptureDriverTests`，说明 GhostSamurai catalog/manifest、候选解析、敌人 Bow/katana preview 控制器、三组 SwordArt 合同，以及 `BossTest` 读招观察驱动都没有被当前 dirty state 直接打坏。
- 再用 `Tools/unity-cli/ty-new-ghostsamurai-baseline-check --startup-timeout 90 --results-dir /tmp/ty_new_ghostsamurai_baseline_20260429_round6` 在 temp clone 里先执行 `Repair CombatTest Prefab Wiring` 后复核，结果仍是 `22/22 Passed`，证明 public-safe proxy baseline 仍可恢复，且 release scene 没被 GhostSamurai / imported source 目录绑死。
- 最新又补了一条总入口：`Tools/unity-cli/ty-new-ghostsamurai-verify --startup-timeout 90 --results-root /tmp/ty_new_ghostsamurai_verify_20260429_round7`。它在同一轮里串起 `preview-check` 与 `baseline-check`，并从 XML 直接抽出摘要；当前结果为 local-preview 研究线 `73/73 Passed`、baseline 恢复线 `22/22 Passed`。这说明最新的 player core、SwordArt、CombatTest 三类敌人读招、Gatekeeper 读招与 public-safe repair proof 目前都还能一起站住，不需要手工再去翻 XML 才知道这轮是否过线。
- 本轮又补了一刀验证护栏：`unity-run-tests` 现在会把“退出码 0 但实际 `0 tests`”直接判成失败，防止 `--group-filter` 误配后出现假绿灯。随后把 `ty-new-ghostsamurai-preview-check` / `baseline-check` 的筛选条件收成 `CampusRPG.Tests.EditMode` 程序集 + fixture 关键词，并实测 `Tools/unity-cli/ty-new-ghostsamurai-verify --startup-timeout 90 --results-root /tmp/ty_new_ghostsamurai_verify_20260429_round8` 得到 local-preview 研究线 `87/87 Passed`、baseline 恢复线 `22/22 Passed`。这说明当前 GhostSamurai catalog、玩家 core / SwordArt、CombatTest 三类敌人读招、Gatekeeper 读招和 public-safe repair proof 都是在“真实执行了测试”的前提下过线，不再依赖 0-test 假阳性。
- 结论：后续每轮都应把“local-preview 研究线”与“baseline 恢复证明线”分开跑，再根据需要决定是否在 GUI 里继续观察当前 dirty state，而不是拿 baseline 断言去直接评判本机预览树。

### 6.2 CombatTest 手动观察清单

这份清单只服务本机 `GhostSamurai` 研究预览。进入顺序固定为：

1. 在主工程 Unity GUI 中执行 `CampusRPG/Setup/Local Preview/Refresh Imported Combat Preview`。
2. 打开 `Assets/_Game/Scenes/CombatTest.unity` 并进入 Play。
3. 若要快速复核 `Sidewind Cut / Cross Step` 的 flank 读感，直接运行 `CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Flank Reads/Clean HUD`；若 Unity GUI 已开，也可在终端执行 `Tools/unity-cli/ty-new-ghostsamurai-observe-swordarts flank-clean`。若要复核 `AirDodge + Light` 与 `AirDodge + Heavy`，继续用 `CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Clean HUD` 或终端 `Tools/unity-cli/ty-new-ghostsamurai-observe-swordarts clean`。若要专门对照 `Rising Cleave` 与 `Falling Star` 的空中 heavy 分工，改用 `CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Air Heavy Reads/Clean HUD`，或终端 `Tools/unity-cli/ty-new-ghostsamurai-observe-swordarts airheavy-clean`。若这轮要专门复核 `Iron Gate Break`，改用 `CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Iron Gate Break/Clean HUD`，或终端 `Tools/unity-cli/ty-new-ghostsamurai-observe-swordarts irongate-clean`。同一模式现在支持重复触发；wrapper 会附带新的 request stamp，避免第二次同模走查被旧请求静默吞掉。
4. 默认 `flank-clean` capture driver 会依次给出 `GroundDodge only / spacing reset`、`Sidewind Cut hit / Dodge Left + Light`、`Sidewind Cut whiff / Dodge Right + Light`、`Cross Step hit / Roll + Light` 与 `Cross Step whiff / Roll + Light` 五个标签；默认 `clean` 会依次给出 `AirDodge only / spacing reset`、`Moon Sever hit / AirDodge + Light`、`Moon Sever whiff / AirDodge + Light`、`Falling Star hit / AirDodge + Heavy` 与 `Falling Star whiff / AirDodge + Heavy`；`airheavy-clean` 会依次给出 `Rising Cleave hit / Airborne + Forward Heavy`、`Falling Star hit / Airborne + Neutral Heavy`、`Rising Cleave hit / AirDodge + Forward Heavy`、`Falling Star hit / AirDodge + Heavy` 与 `Falling Star whiff / AirDodge + Heavy`；`irongate-clean` 则会依次给出 `Iron Gate Break hit / AfterBlock + Heavy`、`Heavy_01 hit / queue Iron Gate Break`、`Iron Gate Break hit / AfterHeavy + Heavy`、`Heavy_01 whiff / queue Iron Gate Break` 与 `Iron Gate Break whiff / AfterHeavy + Heavy`，便于对照 `Atk:` 行、前送量和身体语言。
5. 保持 `CombatDebugHUD` 与 `SwordArtHUD` 打开；若要专心看身体语言，可按 `F1` 暂时折叠 Debug HUD。
6. 若这轮要专门看 `CombatTest` 三类敌人的默认读招差异，留在 `CombatTest`：执行 `CampusRPG/Setup/Local Preview/Start CombatTest Enemy Read Capture Driver/Clean HUD`，或终端 `Tools/unity-cli/ty-new-ghostsamurai-observe-enemy-reads clean`。驱动会依次给出 `EnemyMelee / Guard Swing`、`EnemyMobile / Feint Dash` 与 `EnemyRanged / Arc Bolt` 三个标签，在运行时把 imported enemy preview 临时挂到当前三类敌人实例上，再逐个切进 `EnemyAttackState`，便于同屏核对 `Tgt Atk:`、敌人身体语言和 melee / mobile / ranged 的 archetype 差异，而不必把 `CombatTest` scene 永久保存成 local-preview 脏态。同一模式也支持重复触发；wrapper 会附带新的 request stamp，避免第二次同模观察被旧请求静默吞掉。
7. 若这轮要专门复核 Bow 三段读招，也留在 `CombatTest`：执行 `CampusRPG/Setup/Local Preview/Start CombatTest Enemy Read Capture Driver/Ranged Variants/Clean HUD`，或终端 `Tools/unity-cli/ty-new-ghostsamurai-observe-enemy-reads ranged-clean`。驱动会固定让 `EnemyRanged` 依次播放 `EnemyRanged / Anti-Air Shot`、`EnemyRanged / Chase Roll Shot` 与 `EnemyRanged / Guard Break Shot`，并在运行时把 attack 定义临时克隆成 `Attack_Ranged_AntiAir`、`Attack_Ranged_ChaseRoll`、`Attack_Ranged_GuardBreak` 三条观察链；第一段会把玩家抬到空中，第二段会先让玩家进入 roll，第三段会先让玩家进入 block，方便同屏核对 `Tgt Atk:`、GhostSamurai Bow body language 和 `Read_Ranged_*` 预备姿态是否一致。
8. 若这轮要看 `Gatekeeper` 读招，改走 `BossTest`：先在主工程 GUI 里打开 `Assets/_Game/Scenes/BossTest.unity`，再执行 `CampusRPG/Setup/Local Preview/Start Boss Read Capture Driver/Clean HUD`，或终端 `Tools/unity-cli/ty-new-ghostsamurai-observe-boss-reads clean`。驱动会按 `Sky Hook / Anti-Air`、`Pursuit Slam / Roll Catch`、`Gate Slam / Guard Break` 三段顺序，把 imported enemy preview 临时挂到当前 `Boss_Gatekeeper` 实例上，再强制逐招进入 `EnemyAttackState`，便于同屏核对 `Boss cue`、`Tgt Atk:` 和身体语言，而不必把 `BossTest` scene 永久保存成 local-preview 脏态。同一模式现在也支持重复触发；wrapper 会附带新的 request stamp，避免第二次同模观察被旧请求静默吞掉。
9. `BossTest` 或 `CombatTest` 观察结束后直接退出 Play 即可丢弃场景内的 runtime preview；若本轮还把 `CombatTest` prefab 保持在 imported/local-preview 脏态，收口时仍执行 `CampusRPG/Setup/Repair CombatTest Prefab Wiring`，必要时再跑一次 `Tools/unity-cli/ty-new-ghostsamurai-baseline-check --startup-timeout 90` 留恢复证据。

| 观察块 | 动作 / 输入 | 现在应看到的 local-preview 身体语言 | 过关点 |
|---|---|---|---|
| 玩家 core | `Idle`、锁定 `Walk/Run` 八向 | 持刀站姿、前压/后撤/横移方向清楚；不再是 generic dummy 滑步 | 朝向、重心和移动方向一致；`Target Anim` 不被玩家自身姿态误导 |
| 玩家防御 | `Block`、硬挡失败 `GuardBreak` | `DefenseR_Loop` 架势稳定；`DefenseR_Broken` 明显读出“硬挡错了” | 玩家能一眼区分“还在防”和“已经被破防失控” |
| 玩家规避 | `Dodge`、`CombatRoll`、`AirDodge` | `Dodge_F` 短而清楚；`Slide_F` 明显更长、更重；`Avoid_F_1` 与下砸入口分开 | 三种规避的承诺感和位移长度能直接看出来，不再像同一招换名 |
| 玩家基础攻击 | `Light 1-3`、`Heavy` | 三段轻击方向连续，第三刀更重；`Heavy` 是明显蓄势大挥砍 | `Atk:` 行时序和身体动作一致；不会把 `Heavy` 看成加长版轻击 |
| 玩家追击 / 反击 | `DodgeFollowUp`、`DodgeFollowUp_Enhanced`、`Counter`、`Counter_Enhanced` | 闪后前切、回身切、弹反短处决感和高收益反击彼此分开 | 不再复用成同一套轻击体感；玩家能看懂“这是闪后赚到的一刀还是弹反反击” |
| SwordArt 1 | `Sidewind Cut`、`Cross Step` | `Sidewind Cut` 偏闪后横切；`Cross Step` 是 roll 后穿步斩 | 能看清二者触发上下文和终点姿态差异，不混成“闪后都一样” |
| SwordArt 2 | `Rising Cleave`、`Iron Gate Break` | `Rising Cleave` 是前推重斩；`Iron Gate Break` 先亮挡架再转攻 | `Iron Gate Break` 不再像 `Counter` 或普通 `Heavy`；玩家能从动作里读到“这是破门/破防招” |
| SwordArt 3 | `Falling Star`、`Moon Sever` | `Falling Star` 保留落地 recovery；`Moon Sever` 是空中 dodge 后横切 | 空中两招不抢输入、不共用同一视觉节奏；`Moon Sever` 不应像短空挥 |
| 敌人近战 / 机动 | melee / mobile idle、walk、run、普通 attack | 近战先站出持刀压迫；mobile 的前压重挥比普通近战更重 | 玩家能仅靠身体语言区分“普通压前”和“更重的机动前压” |
| 敌人远程 | ranged idle、walk、run、`Attack_Ranged` | `Bow_Idle`、`Bow_AimWalk_F`、`Bow_AimRun_F`、`Bow_Shoot_Start` | 弓兵不再像近战 dummy 放投射物；走位和抬手都是弓手语言 |
| Gatekeeper 读招 | `Sky Hook`、`Pursuit Slam`、`Gate Slam` | `Sky Hook` 有明显抬刀/反空；`Pursuit Slam` 先追上来；`Gate Slam` 是慢重破防 | `Boss` cue、`Tgt Atk:`、敌人身体语言三者一致，玩家能分清“该落地/该延迟闪/不能硬挡” |

当前最大人工风险不是测试红灯，而是 GUI 观察还没做完：若 `CombatTest` 里看见某招的 `Atk:` / `Tgt Atk:` 时序与身体动作不一致，优先回到对应映射行检查候选 clip 是否错位，再决定是否改 SO 时长、前送或切换候选。

### 6.3 2026-07-10 三敌读招人工验收结果

- `EnemyMelee / Guard Swing`：Debug HUD 同帧显示 `Tgt Atk: GuardSwing Rec 0.34/0.63 hit .18-.28`，锁定目标为 `Enemy_Melee_A`；近战前压挥砍与 recovery 标签一致。
- `EnemyMobile / Feint Dash`：同帧显示 `Tgt Atk: FeintDash Start 0.10/0.48 hit .12-.20`，目标动画为 `GhostSamurai_APose_Attack02_2_Inplace`；贴身前压/侧切身体语言与 mobile archetype 一致。
- `EnemyRanged / Arc Bolt`：同帧时序为 `Tgt Atk: ArcBolt Start 0.03/0.64 hit .22-.32`，但首轮 `Target Anim` 实际显示 `__preview__Take 001`，没有满足本节要求的 `GhostSamurai_Bow_Shoot_Start` 语义。根因是 FBX 同时暴露正式 clip 与 `__preview__Take 001` 生成子片段，而加载器只排除了名称完全等于 `__preview__` 的对象。
- 修复后 `CombatImportedEnemyVisualUtility` 会拒绝所有 `__preview__*` clip；controller 定向 fixture `11/11 Passed`，并直接断言 ranged base state 使用 `GhostSamurai_Bow_Shoot_*`、所有攻击变体都不是生成 preview clip。
- 最终双线门禁：`/tmp/ty_new_ghostsamurai_verify_20260710_enemy_reads_fixed/preview/TY_NEW_ghostsamurai_preview_20260710_161143.xml = 93/93 Passed`；`/tmp/ty_new_ghostsamurai_verify_20260710_enemy_reads_fixed/baseline/TY_NEW_ghostsamurai_baseline_20260710_161425.xml = 22/22 Passed`。
- 观察结束后已退出 Play，并在主工程执行 `CampusRPG/Setup/Repair CombatTest Prefab Wiring`；日志确认 player/enemy proxy baseline restored，CombatTest scene 与四个 player/enemy prefab 不再包含 `GhostSamurai`、`LocalPreview` 或 imported preview 引用。

结论：本清单原先的“CombatTest 三敌 GUI 观察未完成”风险已经解除。后续若再次刷新 local preview，只需要抽查 `Target Anim` 仍为 `GhostSamurai_Bow_Shoot_*`，无需继续扩候选加载器；第一章主线应转入 `Chapter01_Combined` 真实通关走查。
