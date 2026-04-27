# TY_NEW 野心路线图 V2

更新时间：`2026-04-26`

本路线图基于 `Development_Blueprint_V1`、夜间 P0-P7 队列、C0-C7 发布候选收口结果，以及当前已经开启的 `P0.7-Expression`。它不替代 V1：V1 继续负责“第一章能稳定交付”，V2 负责“TY_NEW 看起来、玩起来像一款真正有野心的第三人称动作 RPG”。

## 1. 路线判断

原路线偏保守的原因是目标被压成“第一章发布候选别炸”。这条线已经有价值：它让 Chapter01/Boss/runtime smoke/final gate/Mac build 全部进入可验证状态。

现在新增第二条线：

| 路线 | 目标 | 状态 |
|---|---|---|
| 稳定线 | C0-C7 发布候选收口，保证能打通、不污染 public repo、不长挂 Unity | 已基本闭合，剩人工烟雾与 Windows Build Support |
| 野心线 | 动作表达、地图表达、敌人回应、Boss 压迫、表现层升级 | 从 P0.7 开始推进 |

V2 的核心策略：先扩动作和空间，再让敌人回应，最后用集中 gate 收口。不要再把每个新动作都挡在“人工走查之后”。

## 2. 新总目标

第一章不只是“能通关”，而要做到：

1. 玩家有明显新动作：短闪、翻滚、空中闪避、空中追击、下砸、破防反馈。
2. 地图有动作游戏空间：宽场、窄廊、侧路、回环、Boss 前厅、Boss 房，而不是测试盒子串联。
3. 敌人会回应玩家机动性：反空、追滚、破防、远程压制、Boss 二择。
4. 招式系统能被玩家理解：输入语法、触发条件、当前候选招式、命中收益都能看出来。
5. 发布线仍然能兜底：每个野心切片最终回到 CombatTest / BossTest / Chapter01 gate。

## 3. V2 大阶段

| 阶段 | 名称 | 重点 | 退出条件 |
|---|---|---|---|
| A0 | 稳定线保留 | C0-C7 作为兜底，不再阻止新动作 | final gate 可随时重跑，Mac build 可产出 |
| A1 | P0.7 动作表达扩张 | CombatRoll、AirDodge、空中追击、下砸 SwordArt | CombatTest 中玩家至少有 3 种差异明显的规避/追击选择 |
| A2 | P1.5 地图表达扩张 | Chapter01 从四段灰盒升级为 5 区动作地图 | 入口、宽场、窄廊、侧路/捷径、Boss 前厅/Boss 房全部可跑 |
| A3 | P2.5 招式系统扩张 | SwordArt 从三条合同扩到可理解的招式语法 | 至少 6 条 SwordArt，有触发语法、HUD 提示和测试覆盖 |
| A4 | P6.5 敌人回应扩张 | 敌人针对 roll/air/guard 给回应 | Gatekeeper 已有反空、追滚、破防三类回应；反空/追滚 cue、追滚 lane、camera impulse、procedural SFX、音频触发策略与响应感知起手动画计划已落地 |
| A5 | P5.5 镜头和表现扩张 | arena camera、impact、finish、空间提示 | Boss/Chapter01 关键节点有 HUD、镜头、procedural SFX 与 per-cue 音频策略反馈层级 |
| A6 | P7.5 候选包扩张收口 | V2 内容重新纳入 release gate | V2 全量 gate、Mac build 与 post-build runtime smoke 已绿；Windows 模块补齐后继续作为发布候选包门 |

## 4. P0.7 动作表达扩张

当前已落地：

- `PlayerEvasiveActionType`
- `GroundDodge / CombatRoll / AirDodge`
- guard-cancel roll
- air dodge 每次离地一次，落地刷新
- roll / air dodge 独立距离、时长、无敌窗口
- `CombatRoll` / `AirDodge` 已有 CombatTest placeholder clip 与 Animator state
- `PlayerCombatAnimationRelay` 会按 `PlayerEvasiveActionType` 分流到 `Dodge` / `CombatRoll` / `AirDodge`
- `Falling Star` 下砸 SwordArt 已有 CombatTest attack SO、SwordArt SO、placeholder clip、Animator state 与玩家 prefab 接线
- `Cross Step` 翻滚反击已从 `Sidewind Cut` 复用升级为专属 SwordArt：roll 后 Light 需要 `AfterCombatRoll` 上下文，普通 after-dodge 侧向轻击仍保留 `Sidewind Cut`
- `Moon Sever` 空中横切已接入 `AfterAirDodge` 上下文：`AirDodge + Light` 在完整 recovery 后触发，Heavy 继续留给 `Falling Star` / `Rising Cleave`
- `PlayerStateMachineLifecycleTests = 21/21 Passed`
- `P0.7-A1` 窄 gate `42/42 Passed`
- `P0.7-A2` 窄 gate `57/57 Passed`
- `P0.7-A3` 窄 gate `67/67 Passed`
- `P0.7-A4` 窄 gate `27/27 Passed`；P0.7/SwordArt 集中 gate `69/69 Passed`
- `P2.5-A` Cross Step 窄 gate `64/64 Passed`
- `P2.5-C` Moon Sever 窄 gate `83/83 Passed`；表现层组合 gate `150/150 Passed`

下一步按这个顺序：

| 任务 | 状态 | 内容 | 验收 |
|---|---|---|---|
| P0.7-A1 | `Done` | 给 `CombatRoll` / `AirDodge` 接专属 placeholder clip 和 Animator state | CombatTest animator 含 `CombatRoll` / `AirDodge` 状态，状态机 relay 能按动作类型 crossfade |
| P0.7-A2 | `Done` | 新增下砸 SwordArt：`Falling Star` | 空中 `Heavy + Backward/Neutral` 可触发，下砸有独立 hitbox、前摇、落地恢复 |
| P0.7-A3 | `Done` | air dodge 后接空中追击窗口 | `AirDodge` 期间可预输入一次 Heavy SwordArt，结束后接 `Rising Cleave` 或 `Falling Star`；无匹配招式时回 locomotion，不白送无限循环 |
| P0.7-A4 | `Done` | roll 后追击/回避代价 | `CombatRoll` 期间轻击会缓存到完整 roll recovery 后；P2.5-A 已把 roll 专属轻击升级为 `Cross Step`，普通 after-dodge 侧向轻击仍可接 `Sidewind Cut`，不会中途免费取消 |
| P0.7-A5 | `P5.5-D Audio Mix Policy Done` | 命中、镜头、音频与敌人读招反馈 | `CombatDebugHUD` 已显示 roll / air dodge / `Cross Step` / `Moon Sever` / `Falling Star` / `Iron Gate Break` / 破防 / 反空 / 追滚状态；轻量 camera impulse、public-safe procedural SFX、per-cue cooldown / spatial attenuation 与 Gatekeeper 反空/追滚响应感知起手动画计划已接入，专属受击/命中 clip 后续继续补 |

## 5. P1.5 地图表达扩张

当前地图状态更像“章节流程灰盒”。V2 需要把它升级成动作游戏地图。

当前已落地：

- `Chapter01_MapZones` 五个动作区标记：入口教学、宽场混战、窄廊压迫、侧路/捷径、Boss 前厅与 Boss 房。
- 三段主路连接地面：`Connector_A01_A02_Floor`、`Connector_A02_A03_Floor`、`Connector_A03_A04_Floor`。
- 宽场左右回避 lane、中央掩体、窄廊柱/墙压迫、侧路/捷径灰盒、Boss 前厅补给标记与 Boss 房边界。
- Scene builder 已修正 NavMesh 重建顺序：先保存 `Chapter01_Combined` 到正式路径，再烘焙并保存有效 `NavMesh.asset` 引用。
- `P1.5-A` 窄 gate `26/26 Passed`，覆盖 `Chapter01ProgressionSceneWiringTests`、`Chapter01ProgressionSceneFlowTests` 与 `BuildSettingsSceneOrderTests`。
- `SO_Chapter01_MapDefinition` 已生成，包含五区 `MapZoneDefinition` 与五条 `RouteGateDefinition`；五个 scene marker 均挂 `ChapterMapZoneMarker` 并绑定回数据资产。
- `P1.5-B` 窄 gate `29/29 Passed`，新增覆盖地图定义资产、zone marker 绑定、门要求与 route gate 数据一致性。
- `P1.5-C` camera obstacle gauntlet gate `42/42 Passed`，覆盖 resolver 纯逻辑、Chapter01 窄廊柱子中心通道、内场背墙回收与 Build Settings。
- `P1.5-D` 侧路奖励/捷径消费已落地：`Zone04_SideRouteShortcut` 在地图数据里标记 `SideRouteCache` 可选奖励，route gate 继续表达 Interior 清场后打开捷径回环；scene marker 会从 `SO_Chapter01_MapDefinition` 读出同一份奖励/可选路线语义。
- `P1.5-D` 奖励/回环 gate `30/30 Passed`，覆盖 `Chapter01ProgressionSceneWiringTests`、`BuildSettingsSceneOrderTests` 与 `KeyItemAcquisitionViewTests`。
- `P1.5-E` public-safe 模块化灰盒已落地：`Chapter01_ModularGreybox` 下按五区生成入口门框、宽场侧栏/箱堆、窄廊梁柱 trim、侧路台阶/缓存台、Boss 前厅拱门和 arena rune 模块；全部使用 Unity primitive，不引入 Kenney/Quaternius 或本地 raw asset。
- `P1.5-E` 地图表达 gate `48/48 Passed`，覆盖 `Chapter01ProgressionSceneWiringTests`、`Chapter01ProgressionSceneFlowTests`、`CameraObstacleResolverTests` 与 `BuildSettingsSceneOrderTests`。

目标结构：

| 区域 | 功能 | 设计要求 |
|---|---|---|
| Zone 01 入口教学 | 安全读场、移动/锁定/轻击/格挡教学 | 视线明确，敌人少，路线不迷 |
| Zone 02 宽场混战 | 验证 roll、锁定、远程压制、群敌 | 至少两条侧向回避空间，中央有遮挡物 |
| Zone 03 窄廊压迫 | 验证 guard、block stun、相机 obstacle | 窄通道、柱子、门槛、视线压迫 |
| Zone 04 侧路/捷径 | 给探索和回环感 | 一个可选小遭遇或奖励，清场后打开捷径 |
| Zone 05 Boss 前厅 + Boss 房 | 节奏降噪、补给、Boss 考核 | Boss 前有整备空间，Boss 房支持横扫、突进、反空 |

P1.5 任务：

| 任务 | 内容 | 验收 |
|---|---|---|
| P1.5-A | `Done`：`Chapter01_Combined` 五区灰盒重排 | 场景根节点清楚，区域 trigger 与 objective 不乱；当前 gate `26/26 Passed` |
| P1.5-B | `Done`：`MapZoneDefinition` / `RouteGateDefinition` 数据层 | 区域名、目标、门禁、捷径、遭遇战已进入 `SO_Chapter01_MapDefinition`，scene marker 与门要求有合同覆盖；当前 gate `29/29 Passed` |
| P1.5-C | `Done`：NavMesh 与 camera obstacle gauntlet | 5 区 NavMesh 已烘焙；Chapter01 窄廊柱子中心通道不误侧滑，内场背墙会沿 boom 回收且不误用窄柱 sidestep |
| P1.5-D | `Done`：侧路奖励与捷径 | `Zone04` 已有 `SideRouteCache` 可选奖励语义，Interior 清场后 route gate 打开捷径回环，marker 从地图数据读取同一状态 |
| P1.5-E | `Done`：public-safe 模块化灰盒 | `Chapter01_ModularGreybox` 已给五区补 primitive/proxy 模块地标；若后续导入 Kenney/Quaternius，仍必须先更新素材清单 |

## 6. P2.5 招式系统扩张

最初三条 SwordArt 已有合同，但还像原型。V2 要把它变成玩家能理解、能选择的招式系统。

当前已落地：

- `Sidewind Cut`、`Rising Cleave`、`Iron Gate Break`、`Falling Star`、`Cross Step`、`Moon Sever` 六条 SwordArt 已进入 CombatTest 数据/动画/Animator/prefab 接线。
- `AfterCombatRoll` 与 `AfterAirDodge` 上下文已加入 `SwordArtContextTags`，让 roll 后轻击优先解析为 `Cross Step`，air dodge 后轻击优先解析为 `Moon Sever`，不会继续和普通短闪侧切混在一起。
- `Cross Step` / `Moon Sever` public-safe placeholder clip、专属 attack/SwordArt SO 与 Animator state 已生成。
- `P2.5-A` 窄 gate `64/64 Passed`，覆盖解析器、玩家状态机、CombatTest 动画资产、SwordArt 资产和 prefab public-safe baseline。
- `P2.5-C` 窄 gate `/tmp/TY_NEW_p25_moon_sever_gate.xml = 83/83 Passed`；P2.5 表现层组合 gate `/tmp/TY_NEW_p25_moon_sever_expression_combo_gate.xml = 150/150 Passed`。

目标招式池：

| 招式 | 触发 | 角色 |
|---|---|---|
| `Sidewind Cut` | dodge 后左右轻击 | 侧向切入 |
| `Rising Cleave` | 前推/空中重击 | 向上或前压追击 |
| `Iron Gate Break` | 格挡/重击后重击 | 破防/重压制 |
| `Falling Star` | 空中下砸重击 | 空中终结、落地爆点 |
| `Cross Step` | roll 后轻击 | 翻滚反击 |
| `Moon Sever` | air dodge 后轻击 | 空中横切/追击 |

P2.5 任务：

| 任务 | 内容 | 验收 |
|---|---|---|
| P2.5-A | `Done`：`Cross Step` roll 后专属轻击 | `AfterCombatRoll` 上下文、专属 attack/SwordArt SO、placeholder clip、Animator state、prefab 接线与解析/状态机/资产 gate 已完成 |
| P2.5-B | `Done`：SwordArt HUD / feedback | `SwordArtHUD` 已显示当前触发、最近触发、cancel 链接窗口和候选招式，优先覆盖 `Cross Step` / `Falling Star` / `Iron Gate Break`；Debug HUD 继续保留为调试层 |
| P2.5-C | `Done`：`Moon Sever` 空中横切 | `AfterAirDodge` 上下文、`AirDodge + Light` 派生、专属 attack/SwordArt SO、placeholder clip、Animator state、prefab 接线与解析/状态机/资产/表现 gate 已完成；Heavy 继续留给 `Falling Star` / `Rising Cleave`，避免抢输入 |

验收：

- SwordArt 解析器支持方向、上下文、窗口和优先级。
- HUD 能显示当前候选招式或最近触发招式。
- CombatTest 至少能复现每条招式。
- 每条招式有失败代价，不能全是免费强招。

## 7. P6.5 敌人回应扩张

玩家动作变强后，敌人也要变聪明。否则新动作只会让游戏变简单。

当前已落地：

- `AttackDefinitionSO.EnemyTargetResponse` 可标注 `AntiAir` / `ChaseRoll`，敌人动画计划会从 `BreaksGuard` 推导 `GuardBreak` 读招。
- `EnemyAttackSelectionResolver` 会读取玩家当前规避动作和目标高度，Boss 目标离地时优先选反空回应，目标进入 `CombatRoll` 时优先选追滚回应。
- Gatekeeper 新增 `Sky Hook`：高速直线 projectile，`enemyTargetResponse = AntiAir`，接入 `SO_Enemy_Gatekeeper` 第五招。
- Gatekeeper 新增 `Pursuit Slam`：延迟前压近战，`enemyTargetResponse = ChaseRoll`，接入 `SO_Enemy_Gatekeeper` 第六招。
- 生成器 public-baseline 修复：`CreateCombatTestAssets()` 显式传入 `false`，避免 `UnityEngine.Object` 隐式转 bool 后误启 imported preview。
- `BossAttackCuePlanner` 会把 `Sky Hook` 显示为 `Anti-Air Incoming`，把 `Pursuit Slam` 显示为 `Roll Catch Incoming`，并使用专属 accent color。
- `BossGroundTelegraphPlanner` / `BossImpactMarkerPlanner` 会把 `ChaseRoll` 近战显示为前压 lane，避免 `Pursuit Slam` 被读成普通圆形近战或瞬移偷袭。
- `EnemyCombatAnimationPlanUtility` 会把 `Sky Hook` 解析为 `Attack_AntiAir` / `Anti-Air Read`，把 `Pursuit Slam` 解析为 `Attack_ChaseRoll` / `Roll Catch Read`；`EnemyCombatAnimationRelay` 会读取 `EnemyAttackState.CurrentAttackDefinition`，在 Animator 缺少专属 state 时回退到既有 ranged/mobile attack state。
- `EnemyCombatAnimationRelay` 现在会把 `EnemyAttackState` 的 Startup / Advance / Recovery 相位传给 `EnemyCombatAnimationPlanUtility`；`ResponseRead` / `AntiAirRead` / `ChaseRollRead` 不再一帧满值常亮，而是在 startup 渐入、advance 保持、recovery 渐出，避免 imported/local-preview 敌人读招姿态突兀弹出。
- `Gate Slam` / `BreaksGuard` 会解析为 `Attack_GuardBreak` / `Guard Break Read`，并写入 `GuardBreakRead` 参数；旧 Animator 缺少专属 state 时回退到普通 melee attack。
- `CombatImportedEnemyVisualUtility` 生成 local-preview enemy AnimatorController 时会补 `Attack_AntiAir` / `Attack_ChaseRoll` / `Attack_GuardBreak` 状态和 `ResponseRead` / `AntiAirRead` / `ChaseRollRead` / `GuardBreakRead` 参数，先复用 public-safe 可用 clip，不导入新资源。
- P6.5-A gate：资产接线 `2/2 Passed`，Boss 选择器 PlayMode `8/8 Passed`，P0.7+P6.5 集中 EditMode `69/69 Passed`。
- P6.5-B gate：追滚资产接线 `2/2 Passed`，Boss 选择器 PlayMode `9/9 Passed`，P0.7+P6.5 集中 EditMode `69/69 Passed`。
- P6.5-C gate：Boss cue / ground telegraph / impact marker / Gatekeeper 资产接线 `14/14 Passed`。
- P6.5-D gate：敌人响应动画计划窄 gate `/tmp/TY_NEW_p65_enemy_response_animation_gate.xml = 32/32 Passed`；表现层组合 gate `/tmp/TY_NEW_p65_enemy_response_animation_combo_gate.xml = 58/58 Passed`。
- P6.5-E gate：敌人回应读招参数相位混合窄 gate `/tmp/TY_NEW_enemy_response_read_blend_gate.xml = 8/8 Passed`；敌人回应组合 gate `/tmp/TY_NEW_enemy_response_read_blend_combo_gate.xml = 16/16 Passed`。
- P6.5-F gate：破防敌人读招窄 gate `/tmp/TY_NEW_guard_break_enemy_read_gate.xml = 11/11 Passed`；表现层组合 gate `/tmp/TY_NEW_guard_break_enemy_read_expression_combo.xml = 54/54 Passed`。
- P6.5-G gate：当前 local-preview enemy controller 资产刷新守门 `/tmp/TY_NEW_local_preview_guardbreak_asset_gate.xml = 12/12 Passed`；直接检查 `AC_Enemy_ImportedPreview_EnemyMelee/Mobile/Ranged.controller` 均含 `Attack_GuardBreak` 与 `GuardBreakRead`，避免生成器绿但主工程绑定素材仍 stale。
- GuardBreak 玩家失败反馈已从普通受击反馈里拆出：`Gate Slam` 的 `0.16s` guard-break hit stun 不再被普通 hit `0.12s` 上限截断，破防结束前也不会被 movement / jump / dodge / light / heavy / skill 立即取消；窄 gate `/tmp/TY_NEW_guardbreak_hit_reaction_gate.xml = 41/41 Passed`。
- GuardBreak 玩家 Animator 也已从普通 `Hit` state 里拆出并完成专属 clip asset 刷新：`PlayerCombatAnimationRelay` 对 `GuardBreak` hit reaction 请求专属 `GuardBreak` state；生成链新增 `AN_Player_GuardBreak_CombatTest`，local preview 优先尝试盾挡受击 / 重受击素材，proxy baseline 使用 guard-drop / collapse 姿态曲线，不再只靠 `Hit` motion `0.68x` 慢放。第一次 GUI 菜单刷新发生在 Unity assembly reload 前所以 gate 红灯；reload 完成后再次执行 `CampusRPG/Setup/Create CombatTest Player Animation Assets`，当前磁盘已生成 `AN_Player_GuardBreak_CombatTest.anim`（guid `b6b3ec6e431a7444aae7755ea430bacc`），活动 `GuardBreak` state speed=1 且指向专属 clip。验证：`/tmp/TY_NEW_guardbreak_clip_gate_after_refresh.xml = 1/1 Passed`；`/tmp/TY_NEW_guardbreak_clip_relay_combo_gate.xml = 10/10 Passed`。剩余风险：controller YAML 仍有一个未引用的旧 `GuardBreak` orphan state；真正的手感结论还要 GUI/人工硬挡 `Gate Slam` 确认。

| 敌人回应 | 目的 | 首个落点 |
|---|---|---|
| 反空 | 回应 AirDodge / 空中 SwordArt | `Done`：Gatekeeper `Sky Hook` + `Anti-Air Incoming` cue + `Attack_AntiAir` 起手计划 + phase-aware `AntiAirRead` |
| 追滚 | 回应 roll 逃离 | `Done`：Gatekeeper `Pursuit Slam` + `Roll Catch Incoming` cue + 前压 lane + `Attack_ChaseRoll` 起手计划 + phase-aware `ChaseRollRead` |
| 破防二择 | 回应一直举盾 | `Done`：`Gate Slam` 保留破防 + `Attack_GuardBreak` 起手计划 + phase-aware `GuardBreakRead` |
| 远程压制 | 逼玩家移动而不是原地输出 | Ranged enemy arc bolt / line bolt 分工更清楚 |
| 群敌导演 | 避免 3 个敌人同时无脑打 | 简单 attack token / pressure budget |

验收：

- 敌人强招有更明显 tell。
- 玩家能用 roll/air/guard 解，但不能一个动作解所有题。
- BossTest 能单独验证 Boss 对新动作的回应。

## 8. P5.5 镜头与表现扩张

V2 的表现重点不是大电影，而是动作可读性。

当前已落地：

- `CombatDebugHudActionFeedbackUtility` 已接入 `CombatDebugHUD`：能把 `CombatRoll`、`AirDodge`、`Cross Step`、`Moon Sever`、`Falling Star`、`Iron Gate Break`、`GuardBreak`、`Sky Hook` 与 `Pursuit Slam` 翻译成可见短反馈行；`CombatDebugHudLayoutUtility` 会按屏幕宽度钳制左上调试行，并在接近底部 `SwordArtHUD` 前停止绘制，溢出时用 `+N debug lines hidden` 告诉人工走查还有信息被折叠，避免调试层压住动作和招式 HUD。绑定素材逐招观察时可按 `F1` 或反引号键 `` ` `` 折叠 Debug HUD，只留小提示，正式 `SwordArtHUD` 不受影响。
- `BossAttackCuePlan` 已补 response hint，`BossAttackCueLayoutUtility` 已把顶部 cue 面板改成响应式安全宽高，并与底部 `SwordArtHUD` 保持最小间距：`Sky Hook`、`Pursuit Slam`、破防重击和直线/抛物线远程 cue 会同时显示“正在来什么”和短解法，避免绑定素材后只看到招式名却不知道该落地、延迟闪避、离开落点或避开破防。
- `P5.5-A` HUD 反馈 gate `/tmp/TY_NEW_p55_action_feedback_hud_gate.xml = 49/49 Passed`，覆盖 HUD utility、技能 HUD、Boss cue 和玩家状态机生命周期。
- `ActionCameraFeedbackUtility` 已接入 `PlayerCombatAnimationRelay` 与 `BossAttackCuePresenter`：`CombatRoll`、`AirDodge`、`Sidewind Cut`、`Cross Step`、`Moon Sever`、`Rising Cleave`、`Iron Gate Break`、`Falling Star` 会触发轻量 action impulse；Gatekeeper `Sky Hook` / `Pursuit Slam` 进入读招 cue 时也会触发对应 response impulse。
- `P5.5-B` 镜头冲击 gate `/tmp/TY_NEW_p55_action_camera_impulse_gate.xml = 20/20 Passed`，覆盖 action camera utility、玩家动画 relay、Boss cue presenter 与第三人称相机 runtime state；表现层组合 gate `/tmp/TY_NEW_p55_p65_camera_feedback_combo_gate.xml = 60/60 Passed`，继续覆盖 HUD、Boss cue/ground/impact 和 camera obstacle。
- `ActionCameraFeedbackUtility` 的 camera impulse plan 已补 `Priority`，`ThirdPersonCameraController` 会保留当前更高优先级读招冲击：`CombatRoll` / `AirDodge` 这类移动反馈不会在同一拍覆盖 `Falling Star`、`Iron Gate Break`、GuardBreak 或 Gatekeeper `Pursuit Slam` 读招冲击。优先级窄 gate `/tmp/TY_NEW_action_camera_impulse_priority_gate.xml = 22/22 Passed`，表现层组合 gate `/tmp/TY_NEW_action_camera_impulse_priority_combo_gate.xml = 72/72 Passed`。
- `P2.5-B` 正式 SwordArt HUD 已落地：`SwordArtHudPresenter` 会显示当前触发、最近触发、cancel 链接窗口和候选招式；`CombatDebugHUD` 会在运行时兜底生成 `SwordArtHUD`，CombatTest builder 也会在新建场景时接入正式 HUD。
- `ProceduralAudioUtility` 已扩展为动作音频计划层：`CombatRoll`、`AirDodge`、`Sidewind Cut`、`Cross Step`、`Moon Sever`、`Rising Cleave`、`Iron Gate Break`、`Falling Star`、GuardBreak，以及 Gatekeeper `Sky Hook` / `Pursuit Slam` 都能解析到不依赖第三方资源的 one-shot chirp，并分别由 `PlayerCombatAnimationRelay` 与 `BossAttackCuePresenter` 触发。
- `P5.5-C` procedural SFX gate `/tmp/TY_NEW_p55_procedural_sfx_gate.xml = 25/25 Passed`；表现层组合 gate `/tmp/TY_NEW_p55_procedural_sfx_combo_gate.xml = 41/41 Passed`，覆盖 procedural audio、HUD、SwordArt HUD、camera impulse、Boss cue/ground/impact 和第三人称相机 runtime state。
- `P5.5-D` 已把 action cue 从“能响”推进到“有播放策略”：`ProceduralActionAudioPlan` 现在携带 `MixGroup`、`CooldownSeconds`、`SpatialBlend`、`MinDistance`、`MaxDistance`、`Priority` 与 `DominanceSeconds`，`TryPlayActionCue` 会按 cue id 做短 cooldown，并在高优先级读招/破防/重招短窗口内拒绝低优先级 movement chirp，避免 roll / air dodge 声效盖住 `Pursuit Slam`、`Falling Star` 或 GuardBreak；`CombatDebugHUD` 会显示最近一次 SFX 决策，例如 `SFX: PursuitSlam play p30 BossResponse`、`SFX: Roll held p30 0.07s` 或 `SFX: Roll cd 0.08s`，方便绑定素材实听时区分“没触发”和“被策略压住”；短屏下该 `SFX:` 行被合同锁定为辅助信息，不能挤掉 `Atk`、`Target Anim` 或 `Tgt Atk` 核心读招证据；仍然不导入第三方音频资源。
- `P5.5-D` audio mix policy gate `/tmp/TY_NEW_p55_audio_mix_policy_gate.xml = 32/32 Passed`；P5.5-D priority polish 窄 gate `/tmp/TY_NEW_procedural_sfx_priority_gate.xml = 11/11 Passed`，表现层组合 gate `/tmp/TY_NEW_sfx_priority_expression_combo_gate.xml = 59/59 Passed`；P5.5-D SFX decision debug 窄 gate `/tmp/TY_NEW_sfx_decision_debug_hud_gate_after_fix.xml = 27/27 Passed`，表现层组合 gate `/tmp/TY_NEW_sfx_decision_expression_combo_gate_after_fix.xml = 53/53 Passed`；SFX 短屏优先级窄 gate `/tmp/TY_NEW_combat_debug_hud_sfx_priority_gate.xml = 15/15 Passed`，组合 gate `/tmp/TY_NEW_combat_debug_hud_sfx_priority_combo_gate.xml = 51/51 Passed`；Boss cue compact debug 窄 gate `/tmp/TY_NEW_compact_boss_cue_debug_hud_gate_after_fix.xml = 22/22 Passed`，表现层组合 gate `/tmp/TY_NEW_compact_boss_cue_expression_combo_gate.xml = 52/52 Passed`，覆盖 procedural audio、HUD、SwordArt HUD、camera impulse、Boss cue/ground/impact、玩家动画 relay 与第三人称相机 runtime state。
- `CombatDebugHUD` layout / contrast safety gate `/tmp/TY_NEW_combat_debug_hud_contrast_gate_2.xml = 9/9 Passed`，覆盖 240px 窄宽度不出屏、360x240 短屏不压底部 `SwordArtHUD`，并给绑定素材走查时的左上调试层增加半透明深色底板，避免白字压在浅天空和浅地面上读不清。
- `CombatDebugHUD` overflow hint gate `/tmp/TY_NEW_combat_debug_hud_overflow_gate.xml = 9/9 Passed`，覆盖溢出时内容行数量、隐藏行数量和 `+N debug lines hidden` 提示。
- `CombatDebugHUD` observation toggle gate `/tmp/TY_NEW_combat_debug_hud_toggle_gate.xml = 6/6 Passed`，覆盖 `F1` / 反引号键 `` ` `` 双入口收起提示、窄视图不出屏和常规视图保持小占位，方便在绑定素材下专心看角色动作、武器轨迹和敌人起手。
- `CombatDebugHUD` animator clip observation gate `/tmp/TY_NEW_combat_debug_hud_anim_clip_gate.xml = 7/7 Passed`，覆盖 Debug HUD 显示当前 Animator clip、normalized time、blend weight，并把标准 `AN_Player_*_CombatTest` 名称压短成走查时可读的动作名，方便区分“gameplay 状态正确”与“实际绑定素材/Animator state 没切对”。
- `CombatDebugHUD` target animator clip observation gate `/tmp/TY_NEW_combat_debug_hud_target_anim_gate.xml = 7/7 Passed`，把同一套 clip / normalized time / blend weight 观察扩到锁定目标，`AN_Enemy_*_CombatTest` 会压短为 `Attack_AntiAir` / `Attack_ChaseRoll` 等读招名，方便核对 Gatekeeper 反空、追滚和普通敌人起手是否真的播到绑定素材。
- `CombatDebugHUD` target read priority gate `/tmp/TY_NEW_combat_debug_hud_target_priority_gate.xml = 8/8 Passed`，把锁定目标的 `Target Anim` 与 Boss response cue 提前到资源数值、技能状态和目标 HP 之前；360x240 短 Game 视图里即使出现 `+N debug lines hidden`，也优先保留敌人当前动画读招证据，避免绑定素材走查时看不到 `Attack_AntiAir` / `Attack_ChaseRoll` 是否真的播出。
- `CombatDebugHUD` attack phase timing gate `/tmp/TY_NEW_combat_debug_hud_attack_phase_gate.xml = 9/9 Passed`，把当前攻击从原始 `Attack Time` 升级成 `Startup` / `Active` / `Recovery` / `Done` 与 hit window 可视化；绑定素材逐招走查时可以直接核对命中点、收招拖尾和输入窗口是否贴合真实动作，而不是只看 elapsed / total 秒数。
- `CombatDebugHUD` attack phase priority gate `/tmp/TY_NEW_combat_debug_hud_attack_phase_priority_gate.xml = 10/10 Passed`，短 Game 视图里有当前攻击时优先保留 `Attack Phase` 和 `Target Anim`；较重复的 SwordArt / action cue 会排到后面，避免真正判断命中点与敌人起手的两行被 `+N debug lines hidden` 吃掉。
- `CombatDebugHUD` attack phase compact gate `/tmp/TY_NEW_combat_debug_hud_attack_phase_compact_gate.xml = 11/11 Passed`，运行时 HUD 会把当前攻击压成 `Atk: MoonSever Act 0.25/0.72 hit .20-.32` 这类短句，保留阶段、elapsed/total 和 hit window，但减少短 Game 视图里被横向裁掉的风险；完整 `Attack Phase` 文案仍保留在 utility 合同里供文档和测试使用。
- `CombatDebugHUD` target attack timing gate `/tmp/TY_NEW_combat_debug_hud_target_attack_timing_gate.xml = 13/13 Passed`，组合 gate `/tmp/TY_NEW_target_attack_timing_boss_enemy_combo_gate.xml = 41/41 Passed`。锁定目标处于 `EnemyAttackState` 时，Debug HUD 会显示 compact `Tgt Atk:` 行，例如 `Tgt Atk: PursuitSlam Start 0.14/0.84 hit .28-.40`，把敌人当前 startup / active / recovery 与 hit window 放到 `Target Anim` 后面；当目标正在攻击时，玩家 `Anim Clip` 会后移，短 Game 视图优先保留敌人读招、敌人命中窗口和 Boss cue。
- `CombatDebugHUD` long imported/local action name timing gate `/tmp/TY_NEW_combat_debug_hud_long_name_timing_gate.xml = 17/17 Passed`，Boss/HUD 组合 gate `/tmp/TY_NEW_combat_debug_hud_long_name_timing_boss_combo_gate.xml = 31/31 Passed`。`Atk:` / `Tgt Atk:` compact 行会按 48 字符预算动态压缩长动作名，优先保留 `Act` / `Start`、elapsed / total 和 hit window，避免绑定素材后素材包前缀或长变体名把命中窗口挤掉。
- `CombatDebugHUD` compact Boss cue gate `/tmp/TY_NEW_compact_boss_cue_debug_hud_gate_after_fix.xml = 22/22 Passed`，表现层组合 gate `/tmp/TY_NEW_compact_boss_cue_expression_combo_gate.xml = 52/52 Passed`。Debug HUD 里的 Boss response 行会压缩成 `Boss: RollCatch PursuitSlam - delay dodge` 或 `Boss: AntiAir SkyHook - land/guard`，并在目标攻击时把内部 `State` 行后移，保证 360x240 短 Game 视图里优先保留 `Atk`、`Target Anim`、`Tgt Atk` 和 compact Boss cue；顶部正式 Boss cue 仍保留完整文案。
- `Gate Slam` 破防也已纳入同一条 Boss response HUD 观察链：顶部正式 cue 会显示 `Guard Break Incoming` 与 `Dodge; guard breaks`，左上 Debug HUD compact 行会显示 `Boss: GuardBreak GateSlam - dodge; guard breaks`，避免绑定素材走查时只看到慢重起手却不知道“硬挡会被破防”。窄 gate `/tmp/TY_NEW_guardbreak_debug_hud_gate.xml = 26/26 Passed`。
- `BossAttackCue` 顶部正式 cue 攻击名宽度保护 gate `/tmp/TY_NEW_boss_cue_attack_name_compact_gate.xml = 7/7 Passed`，表现层组合 gate `/tmp/TY_NEW_boss_cue_attack_name_expression_combo_gate.xml = 36/36 Passed`。顶部 cue 会按当前攻击名矩形宽度只在绘制层做中间省略，防止绑定 local preview / imported 资源后过长攻击显示名横向裁掉读招；内部 `CurrentAttackName` 仍保留完整名，Boss 行为、攻击选择、解法文案、camera、SFX 和素材绑定均不变。
- `BossAttackCue` 顶部正式 cue 解法提示宽度保护 gate `/tmp/TY_NEW_boss_cue_response_hint_compact_gate.xml = 8/8 Passed`，表现层组合 gate `/tmp/TY_NEW_boss_cue_response_hint_expression_combo_gate.xml = 37/37 Passed`。顶部 cue 的 `CurrentResponseHint` 仍保留完整语义，但绘制层会把 `Delay dodge; lane catches rolls`、`Land or guard; avoid air hang` 等已知长提示压成可扫读短句，未知长提示再中间省略，避免窄 Game 视图里把真正的解法信息裁掉。
- `BossAttackCue` 顶部正式 cue 样式裁切保护 gate `/tmp/TY_NEW_boss_cue_style_clip_gate_after_fix.xml = 9/9 Passed`，表现层组合 gate `/tmp/TY_NEW_boss_cue_style_clip_expression_combo_gate.xml = 38/38 Passed`。`BossAttackCueStyleUtility` 会把 cue label、攻击名和解法提示三行都锁为不换行、`TextClipping.Clip`，让绘制层即使遇到 local-preview 长素材名或宽字形，也只能在深色面板内裁切，不会溢出遮挡 Boss 身体语言、地面 telegraph 或底部 `SwordArtHUD`。
- `SwordArt` follow-through runtime gate `/tmp/TY_NEW_swordart_followthrough_runtime_gate.xml = 12/12 Passed`，组合 gate `/tmp/TY_NEW_swordart_followthrough_runtime_state_combo_gate.xml = 41/41 Passed`。`PlayerCombatRuntimeUtility` 现在对 `SwordArt_` 状态保留更多可见 recovery：例如 `Falling Star` 会从约 `0.72s` 基础承诺提升到约 `0.96s` 可见时长，`Moon Sever` 会保留约 `0.70s`，避免绑定素材时下砸、横切和回收动作被过早切回 locomotion；hit window、输入触发和 SwordArt 解析规则不变。
- `ChapterObjectiveView` layout safety gate `/tmp/TY_NEW_chapter_objective_layout_gate.xml = 3/3 Passed`，覆盖 Chapter01 目标提示在 240x144 小窗口内不会固定 360px 出屏，title / heading / body 均被钳制在面板内。

| 表现层 | 内容 |
|---|---|
| 规避反馈 | roll / air dodge 的短残影、尘土、镜头轻推 |
| 命中反馈 | 下砸、破防、反空命中有不同 hit stop / impulse |
| Arena 镜头 | Boss 房使用更稳定的距离、障碍回收和锁定策略 |
| 地图提示 | Zone 入口、捷径开启、Boss 前厅用空间提示而不是满屏文字 |
| 收尾反馈 | Boss 倒地 -> RitualCore -> 完成卡保持干净但更有仪式感 |

## 9. V2 集中 Gate

不回到“每个小动作都卡一下”的保守节奏，但每个纵切完成后必须有集中 gate。

| Gate | 覆盖 |
|---|---|
| `p07_action_expression` | PlayerStateMachine、CombatBalance、AnimationRelay、SwordArt、CombatTest wiring |
| `p15_map_expression` | Chapter01 scene wiring、NavMesh、route gate、checkpoint restore、camera obstacle |
| `p65_enemy_response` | BossTest runtime、enemy attack matrix、反空/追滚/破防合同 |
| `v2_final_gate` | `Tools/unity-cli/ty-new-v2-gate`：release preflight、P0.7、P1.5、P2.5、P5.5、P6.5、runtime smoke、full EditMode/PlayMode |

最新 P7.5-A 结果：

- 新增 `Tools/unity-cli/ty-new-v2-gate`，默认覆盖 release preflight、P0.7、P1.5、P2.5、P5.5、P6.5、runtime smoke，并可继续追加 full EditMode / full PlayMode；`--skip-full` 用于先跑 V2 集中窄门。
- 初次集中 gate 在 `p65_enemy_response_runtime` 暴露一条 PlayMode 顺序问题：`BossTestRuntimeFlowPlayModeTests` 加载场景后，`EnemyAttackControllerTests` 的反空选择 fixture 仍贴近原点，可能被 BossTest 灰盒物理体污染射线。已把反空/追滚选择 fixture 移到远离场景几何的位置，不改 Boss AI 选择逻辑。
- 修复后先跑 P6.5 runtime 窄 gate `/tmp/TY_NEW_v2_p65_runtime_after_fix.xml = 10/10 Passed`。
- 随后执行 `Tools/unity-cli/ty-new-v2-gate --skip-full --startup-timeout 45 --results-dir /tmp/ty_new_v2_gate_p75_after_fix_20260426_2303`，结果：release preflight `6/6`、P0.7 `106/106`、P1.5 `51/51`、P2.5 `84/84`、P5.5 `71/71`、P6.5 EditMode `39/39`、P6.5 runtime `10/10`、runtime smoke `6/6`，合计 `373/373 Passed`。
- 继续执行不带 `--skip-full` 的 `Tools/unity-cli/ty-new-v2-gate --startup-timeout 45 --results-dir /tmp/ty_new_v2_gate_full_20260426_2313`，结果：上述 V2 分组继续全绿，full EditMode `369/369 Passed`，full PlayMode `26/26 Passed`，合计 `768/768 Passed`。
- V2 后 Mac 候选包构建已通过：`Tools/unity-cli/ty-new-build-release mac --use-temp-clone`，日志 `/tmp/TY_NEW_release_mac_20260426_232659.log`，产物 `/var/folders/gr/wz4nf8n16tv_rfss0zpyrk280000gn/T/TY_NEW_build_clone.a6Wubm/Builds/ReleaseCandidate/Mac/TY_NEW.app`，大小约 `106M`。日志中仍有本地 DoubleL ladder rig warning 与早段 access token warning，但 entitlement 后续 resolved，最终 `Build Finished, Result: Success`。
- V2 后追加了一次最接近实机的非交互 runtime smoke：`Tools/unity-cli/unity-run-tests PlayMode --group-filter '^(CampusRPG\.Tests\.PlayMode\.(SmokeBootPlayModeTests|Chapter01RuntimeFlowPlayModeTests|BossTestRuntimeFlowPlayModeTests))$' --use-temp-clone --startup-timeout 45 --results /tmp/TY_NEW_v2_post_mac_runtime_smoke.xml --log /tmp/TY_NEW_v2_post_mac_runtime_smoke.log`，结果 `6/6 Passed`。覆盖 `BossTest` 锁场/清场、`Chapter01_Combined` Boss -> `RitualCore` -> 章节完成、`MainMenu`/核心运行时对象加载。

## 10. 立即执行顺序

下一轮不要再重新讨论大方向，按下面走：

1. `P7.5-A`：已完成 V2 全量 gate、V2 后 Mac 候选包构建和 post-build runtime smoke，`/tmp/ty_new_v2_gate_full_20260426_2313 = 768/768 Passed`，`/tmp/TY_NEW_v2_post_mac_runtime_smoke.xml = 6/6 Passed`，Mac `TY_NEW.app` 已产出。
2. 下一轮优先进入真正 GUI/手工层的 `CombatTest` / `BossTest` / `Chapter01_Combined` 实机烟雾；重点观察新动作输入分工、SwordArt HUD、Gatekeeper 反空/追滚读招、camera obstacle、RitualCore 和章节完成卡。
3. Windows 构建仍取决于当前 Unity 是否安装 Windows Build Support；`StandaloneWindows64 unsupported` 不算代码红灯。
4. `P6.5-E`、`P1.5-F`、`P2.5-D` 只有实机或 gate 暴露明确缺口时再开，不为了数量堆新招。

## 11. 仍然不做

更野心勃勃不等于失控。V2 仍然不做：

- 开放世界
- 背包装备
- 多职业
- 程序生成地图
- 两阶段以上 Boss
- 复杂任务树
- 大规模第三方 raw asset 直接进入 public baseline

真正的野心放在动作表达、地图节奏、敌人回应和第一章体验密度上。
