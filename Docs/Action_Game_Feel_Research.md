# Action Game Feel Research

本文件记录公开资料中的动作手感原则，并把它们翻译成 `TY_NEW` 当前可执行的 CombatTest / Chapter01 调参规则。

## 1. 研究结论

当前项目不适合追求“炫技型动作堆料”。更适合的动作方向是：

- 玩家输入被合理承接，但不允许自由取消把读招节奏冲烂。
- 动作有承诺感，但失败要看得懂，不应因为硬切、空挥、镜头或状态误触发让玩家觉得系统不讲理。
- 命中和受击要清晰：命中窗口、位移、取消窗口由代码和 SO 数据控制，动画事件只做表现同步。
- CombatTest 优先保证 public-safe proxy baseline 可读；真正 imported 动作、IK 和角色外观只作为 local preview 或后续净化输出。

## 2. 公开资料提炼

### God of War 2018

资料：

- [GDC Vault: Evolving Combat in God of War for a New Perspective](https://www.gdcvault.com/play/1026423/Evolving-Combat-in-God-of)
- [GDC slides PDF: Evolving God of War's Combat for a New Perspective](https://media.gdcvault.com/gdc2019/presentations/Sheth_Mihir_EvolvingCombat.pdf)
- [PlayStation Blog: Game developers explain what makes God of War 2018's combat tick](https://blog.playstation.com/2022/10/04/game-developers-explain-what-makes-god-of-war-2018s-combat-tick/)

可用原则：

- 先定义战斗身份，再做动画技术。God of War 的 close camera 迫使团队重新设计 tracking / targeting / engagement，而不是简单沿用旧公式。
- 玩家不应因为看似近在眼前却打空而觉得笨拙。God of War 使用类似 motion-warping / suck-to-target 的目标辅助，但会按角度、距离、难度控制强度，避免侧向吸附让镜头眩晕。
- 近距离第三人称必须避免镜头和战斗系统互相打架。玩家的困难应该来自敌人和时机，不应该来自 camera / input / target selection 的混乱。
- 空中受击、击飞、juggle 等表现不能只交给物理自由落体；必要时要手调曲线，让它看起来合理并且玩起来舒服。

落到本项目：

- `AttackDefinitionSO.forwardMovement` 应继续保留为数据驱动，不让 root motion 或 animation event 成为唯一真相。
- 后续若做目标吸附，只允许小范围、正前方、可解释的辅助，并且需要 `angle falloff / stop distance / max range` 三个参数。
- CombatTest 人工走查时必须同时看动作和镜头：只看 clip 顺不顺不够，攻击是否让玩家丢失方向感同样是失败。

### For Honor

资料：

- [For Honor battle system as geometry of combat](https://www.gamereactor.eu/for-honors-battle-system-is-the-geometry-of-combat/)
- [GDC Vault: For Honor from launch to live period](https://gdcvault.com/play/1024949/-For-Honor-From-a)

可用原则：

- 近战不是只看动画快慢，而是位置、角度、环境和对手动作共同构成读招。
- 读对手、预测移动、在正确时机出手，比无脑高速连段更重要。

落到本项目：

- Boss 和精英敌人的攻击不要只做大范围炫效。每个威胁应能映射到一种清晰解法：格挡可解、闪避可解、走位可解。
- CombatTest 需要保留侧向回避空间；如果空间太窄，再好的闪避动画也会显得卡。

### Sekiro

资料：

- [PlayStation Blog: Sekiro final pre-launch interview](https://blog.playstation.com/2019/03/13/sekiro-shadows-die-twice-final-pre-launch-interview/)
- [PlayStation Blog: Miyazaki on Sekiro resurrection and combat flow](https://blog.playstation.com/2018/07/13/interview-miyazaki-on-the-limbs-and-lore-of-sekiro-shadows-die-twice/)

可用原则：

- 核心战斗系统可以改变玩家习惯，但必须让玩家愿意学习它。
- 高风险战斗需要节奏保护。Sekiro 的 resurrection 不是降低难度，而是保持高风险节奏不被反复跑图打断。
- 清晰的对刀/防御反馈能让玩家知道自己是在“做对动作”，不是靠血条猜。

落到本项目：

- `TY_NEW` 的 Boss 目标不是炫，而是读招清晰。每个重攻击应有前摇、命中窗口、恢复窗口和可解方式。
- 失败反馈要短、准、可恢复。受击、死亡、检查点恢复和敌人重置都应减少重复跑流程造成的挫败。

### Celeste

资料：

- [Celeste movement and input leniency discussion by Maddy Thorson](https://maddymakesgames.com/articles/celeste_and_forgiveness/index.html)

可用原则：

- 手感常来自宽容窗口，而不是纯粹更快的动画。
- Coyote time、jump buffer、corner correction 这类小规则让玩家觉得游戏“懂自己刚才想做什么”。

落到本项目：

- 招式输入采用短窗口 buffer 是正确方向，但 buffer 必须在无效时清理，不能变成后续误触发。
- 落地、空中、闪避后、重击后段都应有明确窗口。输入宽容要帮玩家，不要帮玩家绕过动作承诺。

### Monster Hunter / 重武器承诺感

资料：

- [Game Developer: How Capcom designed Monster Hunter: World to feel approachable and alive](https://www.gamedeveloper.com/design/how-capcom-designed-i-monster-hunter-world-i-to-feel-approachable-and-alive)

可用原则：

- 武器动作可以慢，但必须让玩家理解慢在哪里、强在哪里、什么时候不能贪。
- 可读 UI、反馈和训练环境可以降低入门门槛，不必牺牲核心动作承诺感。

落到本项目：

- 重击、`Iron Gate Break` 这类动作应该保留后段取消窗口和明显承诺，不要被早按秒切。
- `CombatDebugHUD` 的当前招式、攻击计时、取消窗口提示是有价值的调试工具，人工回归时应打开。

### Unity Animation Rigging

资料：

- [Unity Animation Rigging manual](https://docs.unity.cn/Packages/com.unity.animation.rigging%400.3/manual/index.html)

可用原则：

- 无动捕或动作资源不完全匹配时，可以用 IK / multi-parent / aim constraint 做手、武器、视线和道具关系修正。
- 这类修正适合补表现，不适合替代战斗逻辑。

落到本项目：

- 之后若接 imported 角色，优先修武器握持、手部锚点、朝向，而不是把判定绑死在动画骨骼事件上。
- public baseline 仍保持 proxy 可读，local preview 再做 imported / IK 试验。

## 3. TY_NEW 动作手感规则

### 输入

- 普通输入 buffer 建议保持短窗口：`0.15s - 0.30s`。
- buffer 命中后立即消费；解析失败时立即清理。
- buffer 不应全局同值：`dodge / guard` 偏短，连段输入中等，重技能和高承诺动作更保守。
- `hit stop`、落地、资源不足、受击打断时必须重新校验 buffer，避免旧输入在玩家已改变意图后被执行。
- 空中基础轻/重不触发地面攻击；只有匹配到可执行 Sword Art 时才进攻击。
- 空中 skill 允许，但仍锁跳跃、扣资源、进冷却。
- 未实现的空中 dodge 不复用地面 dodge；真正 `AirDodge` 必须单独状态、clip 和位移规则。

### 动作承诺

- 轻攻击可以顺，但不应取消掉全部恢复。
- 重攻击和 `Iron Gate Break` 必须有后段取消窗口，不能起手秒切。
- `startup` 不可取消；`active` 原则上不可取消，除非是明确的 hit-confirm；`recovery` 后段才允许 dodge / guard / 下一段。
- 命中取消、格挡取消、空挥取消必须分开配置，不能用同一条 cancel window 管全部情况。
- `Sidewind Cut` 应强调闪避后的侧向收益。
- `Rising Cleave` 应保留前推/空中入口，不污染地面中立重击。

### 位移和命中

- 攻击前送由 `AttackDefinitionSO.forwardMovement` 控制。
- 后续如果做辅助贴近，必须有角度限制、距离限制和强度上限。
- target assist 只允许在玩家意图明确时补偿近景镜头和输入精度：早段 `startup` 可轻微修正，`active` 前必须截止。
- 玩家明确推反方向、目标被墙遮挡、目标超出角度、连段中目标交叉时，不应强行换目标或拉回目标。
- 命中窗口优先用 timed window / SO 数据，动画事件只做同步辅助。

### 镜头和空间

- CombatTest 走查要同时看动作和镜头，不只看动画片段。
- 侧向闪避和侧向招式需要足够侧向空间。
- 近墙、近敌、锁定目标丢失时，镜头不能比敌人更像阻碍。
- camera obstacle 验收不只看“是否穿墙”，还要看敌人起手武器是否可见、镜头前推是否误导闪避距离、玩家身体是否遮住敌人。

### 反馈

- 命中反馈要短而清楚：音效、轻微 hit stop、受击姿态、HUD 状态足够即可。
- 不要用长硬直掩盖判定问题。
- Boss 读招提示应服务“格挡可解 / 闪避可解 / 走位可解”。
- 反馈需要分层：普通命中、重击命中、技能命中、格挡成功、格挡被破、玩家受击、敌人空挥不能全部一个反馈。
- `Gate Slam` 这类破防反馈允许使用短促 camera impulse，但只作为读懂受击层级的辅助，不能用长镜头震动掩盖读招或判定问题。
- 初始 hit stop 可保守试：轻击 `0.04s - 0.06s`，重击 `0.07s - 0.10s`，技能/破防 `0.10s - 0.14s`，普通格挡 `0.03s - 0.05s`。

## 4. CombatTest 手感走查脚本

每次调动作前，先准备七个最小走查场景：平地单敌人、双敌人、墙边、柱子绕圈、窄通道、台阶/高低差、mantle 边缘。每条记录都按这个格式写：`场景 / 动作 / 预期解法 / 实际结果 / 是否看懂 / 是否可重复 / 要改哪个参数`。

| 模块 | 人工观察动作 | 重点看什么 | 可调参数 |
|---|---|---|---|
| 锁定移动 / 后退 | 锁定敌人后前后左右移动，连续后退到墙边，再横移绕圈 | 后退是否被“镜头 + 墙”夹死；横移是否能形成走位解法；角色朝向是否抖动 | locked backward speed ratio、strafe acceleration/deceleration、turn speed、lock-on distance、camera yaw damping |
| 基础轻/重攻击 | 空挥、近距离、贴脸、斜 `30°`、敌人横移时攻击 | `forwardMovement` 是否穿敌或停在距离外；重击慢在哪里是否可见；轻击是否过度追踪 | forwardMovement distance/time curve、startup/active/recovery、hitbox start/end、attack turn cutoff |
| `Sidewind Cut` | 敌人正面、侧面、墙边、双敌人夹击时使用 | 是否真的表达侧向/绕位价值；是否被 assist 拉回正面；是否越过墙角或拖坏镜头 | lateral displacement、forwardMovement、assist angle cap、cancel-out time、camera recenter strength |
| `Rising Cleave` | 地面出招、空中重击进入、落地边缘使用 | 空中重击转招是否清晰；起跳/下落阶段是否误触地面逻辑；命中高度是否可信 | air-to-skill entry window、vertical velocity condition、hitbox height、landing recovery、MP consume timing |
| `Iron Gate Break` | 敌人普通格挡、硬防、可破防状态下使用 | 玩家是否理解它强在哪里；破防反馈是否明显；没破防时是否有代价 | guard break threshold、block stun duration、hit stop、enemy stagger/guard break reaction、recovery on whiff |
| Dodge | 静止、锁定定向、命中前、墙边 dodge | 输入方向是否稳定；i-frame 是否太早/太晚；dodge 是否变成万能解 | dodge distance、invuln start/end、startup lock、recovery、direction sampling time |
| 格挡 | 早格挡、临近格挡、持续格挡、被重击打中格挡 | 成功/失败/破防反馈是否区分；格挡是否因为启动太快压过 dodge 和走位 | guard startup、guard active hold、block stun、chip damage、guard break meter、block hit stop |
| 输入 buffer | recovery 前 `0.30/0.20/0.10/0.05s` 输入；hit stop、落地前、空中 dodge 被拒时输入 | 是否只触发一次；解析失败是否清理；空中非法输入是否落地后突然出地面攻击 | buffer duration by action、priority、consume-on-start policy、invalid-clear policy、landing revalidation、hitstop buffer handling |
| 取消窗口 | 每招 startup、active、early recovery、late recovery 分别尝试 dodge / guard / skill | 是否把重击变成安全 poke；命中取消和空挥取消是否代价不同；hit stop 内连按是否绕过承诺 | cancelStart/cancelEnd per move、hit-confirm-only cancel、whiff cancel delay、cancel target whitelist |
| 敌人读招 | 同一敌人三招：正面斩、慢重击、窄突刺/扇形，分别用格挡、闪避、走位解决 | 玩家是否能在起手阶段判断该挡、该闪、该走；失败时是否知道错在哪里 | telegraph duration、anticipation pose、enemy tracking cutoff、active frames、hitbox width/length、recovery punish window |
| target assist | 敌人在 `0/15/30/45/60°`；玩家推反方向；目标交叉移动；目标被墙遮挡 | assist 是否偷走玩家意图；是否打向错误目标；是否在 active 之后继续转身 | acquisition cone、max correction angle、rotation speed cap、assist start/end、stick intent threshold、target switch delay、line-of-sight requirement |
| camera obstacle | 墙贴背、柱子绕圈、窄走廊、角落、低顶棚、mantle 边缘战斗 | 起手武器是否可见；镜头是否突然前推导致距离判断失真；玩家是否遮住敌人 | camera distance、FOV、shoulder offset、obstacle layer、camera radius、minimum occlusion time、damping |
| hit stop / 受击反馈 | 轻击、重击、技能、格挡、破防、玩家受击逐帧看 | 冻结是否让打击更清楚；是否造成输入延迟感；轻重反馈是否有层级 | attacker/victim hit stop duration、camera impulse、shake amplitude、SFX timing、VFX lifetime、knockback/stagger duration |
| 资源 / cooldown | 空中 skill、MP 不足、cooldown 中、被打断时重复输入 | 扣 MP 和进 cooldown 的时机是否一致；失败输入是否残留；空中 skill 是否仍锁跳跃 | MP consume point、cooldown start point、refund/no-refund policy、air action lock、buffer clear on resource fail |

### P0.5 后实机走查记录表

本表用于 `82/82 Passed` 组合回归之后的人工实机观察。自动化不能把“没有人工观察”当成“手感已经确认”，也不能因为想进入 P0.6 就默认新增动作。每行都先填 `观察结果`，再决定 `判定`。

2026-07-11 口径补充：capture driver 分为两类。直接构造 attack / dodge 上下文的驱动只证明表现重放；通过 `InputSystem.QueueStateEvent` 注入完整 `KeyboardState` 的驱动，可以证明 InputReader -> 状态机 -> 伤害 / 计量技术链，但仍不等于物理键盘、人工反应时机或主观手感签字。camera gauntlet 只负责布置案例并记录遥测，永远输出 `manualSignoff=required automaticPass=false`。

判定只使用三类：

- `Pass`：动作、镜头和解法都能被稳定读懂，可以转向 `Chapter01` / Boss 闭环或发布前主线回归。
- `Tune P0.5`：只需要调现有参数、现有 clip、现有镜头或反馈，不新增状态 / clip / 系统。
- `Open P0.6`：观察结果证明现有合同无法表达需要，才允许开新动作、新状态、新 clip 或专属受击反馈。

总体收口中的虚拟键盘全链路结果额外标为 `Technical Pass / Human Feel Pending`；它不会被折算成上面的主观 `Pass`。

| 走查块 | 场景 | 必看动作 | 观察结果 | 判定 | 下一步 |
|---|---|---|---|---|---|
| 玩家承诺 | 平地单敌人 | `Light` 三段、`Heavy` 空挥/命中、尾段 buffer | Lunge Debug HUD 两轮完整重放 Light 空挥/命中、三段空挥/命中、Heavy 空挥/命中、锁定/非锁定、边距 Heavy 与贴墙 Light；HUD 与 Console 顺序一致，贴墙位移被截停 | `Pass` | 真实键鼠只需做一次短 smoke；没有证据要求改合同 |
| 防御轴 | 平地单敌人、双敌人 | `Guard` startup、成功格挡、startup 失败受击 | CombatTest ordinary Guard 两拍通过虚拟 `<Keyboard>/leftCtrl` 走真实 InputReader 和伤害链：startup 拍 `HP 100->90`、Counter `0->0`、窗口 `False`；active 拍 `HP 100->100`、Counter `0->20`、窗口 `True`，两拍提交均硬匹配 `Enemy_Melee / Enemy_Melee`。Boss Gate Slam Guard 同类技术链为 `HP 100->74.4`，`AttackCommitted/block/guardStartup/activeGuard/guardBreak=True` | `Technical Pass / Human Feel Pending` | 技术链不再补 driver；仅在需要主观手感签字时用物理 Ctrl 观察时机、身体、SFX 与 camera 合层 |
| 闪避轴 | 墙边、柱子、窄通道 | 定向 dodge、后撤 dodge、成功闪避后 follow-up | Flank / AirHeavy driver 已重放 GroundDodge、CombatRoll、AirDodge 与 follow-up；Boss Gate Slam Dodge 又以虚拟 `W+LeftShift` 走完整输入链，得到 `alignment=1.00`、`HP 100->100`、Agility `0->25`，且 `AttackCommitted/groundDodge/invulnerable/successfulDodge=True` | `Technical Pass / Human Feel Pending` | 障碍空间已纳入 camera 五案；物理 Shift 的手感、人工反应时机与成功穿招体感仍需人签字 |
| 招式轴 | 平地、墙边、空中 heavy | `Sidewind Cut`、`Rising Cleave`、`Iron Gate Break` | Flank、AirHeavy、IronGateBreak Debug HUD 已重放 Sidewind Cut、Cross Step、Rising Cleave、Falling Star、Iron Gate Break 的 hit/whiff 与候选 HUD；Falling Star 执行姿态/命中点同屏可见 | `Pass` | AirDodge 输入可靠性仍归闪避轴，不顺带冒充通过 |
| 敌人解法 | 三类普通敌人与 Gatekeeper | 正面斩、快攻、远程弹、慢重击、横扫 | Guard Swing、Feint Dash、Arc Bolt 的 `Tgt Atk:` / 身体语言已人工确认；BossTest 的 Sky Hook、Pursuit Slam、Gate Slam 与解法 cue 已重放，Gate Slam 的硬挡破防与定向闪避主解又完成虚拟键盘技术作答 | `Technical Pass / Human Feel Pending` | 技术主解不再补自动入口；仍不能据此声称玩家已经读懂或喜欢该节奏 |
| 镜头障碍 | 墙贴背、柱子绕圈、窄廊、后左角落、mantle 边缘 | 攻击、闪避、锁定移动同时观察镜头 | `Chapter01CameraObstacleCaptureDriver` 五案已用真实 Game View 操作走过；五案均 `occupiedEver=False`，宽墙 / 绕柱 / 窄廊 / 墙角观测到静态障碍，mantle 案观测到 `PlayerMantleState`。窄廊 `sideFlips=1` 混有刻意往返输入，只是观察量；`targetInViewport` 只表示在视口，不表示无遮挡 | `Technical Pass / Human Feel Pending` | 未见穿入占用体或持续左右 ping-pong；仍保留人工镜头舒适度 / 构图签字，不把遥测自动判成体验 Pass |
| 反馈层级 | 轻击、重击、技能、格挡、玩家受击 | hit stop、SFX/VFX、HUD 提示、受击姿态 | Light / Heavy / SwordArt HUD、Boss cue、telegraph 与攻击状态已同屏；Gate Slam 技术硬挡已触发真实伤害、GuardBreak 状态与提交链，但 bool / 数值不能证明玩家身体、SFX、camera impulse 的最终合层主观质量 | `Technical Pass / Human Feel Pending` | 只在人工 feel 签字时核对身体 / 声音 / 镜头层级；没有观察问题不新增系统 |

## 5. P0.5 合同表

P0.5 不先新增动作，而是先把现有动作写成可验收合同。第一轮至少覆盖 `Light`、`Heavy`、`Dodge`、`Guard`、`Sidewind Cut`、`Rising Cleave`、`Iron Gate Break`。

| 动作 | 必填合同 |
|---|---|
| 基础动作 | startup、active、recovery、buffer accept window、cancel window、resource consume timing |
| 位移和追踪 | `forwardMovement`、turning/tracking window、tracking cutoff、assist angle cap |
| 命中和反馈 | hitbox start/end、hit stop、SFX/VFX、受击/格挡/破防反应 |
| 失败代价 | whiff recovery、resource refund/no-refund、被打断时 buffer/cooldown 清理 |
| 验证方式 | CombatDebugHUD 观察项、人工走查场景、必要时补 EditMode 合同测试 |

敌人也必须有解法矩阵。第一章早期不需要很多招，优先做 `3 - 4` 个高质量敌人动作：正面斩主打格挡、慢重击主打闪避、窄突刺或扇形主打走位，横扫可用后撤或格挡。每个敌人攻击至少记录：预兆、追踪阶段、追踪截止点、命中范围、有效解法、失败反馈。

### 当前 CombatTest 敌人解法矩阵

| 敌人动作 | 当前合同 | 主解法 | 失败反馈/后续观察 |
|---|---|---|---|
| `Enemy_Melee` / `Guard Swing` | `0.18 startup / 0.10 active / 0.35 recovery`，range `1.55`，radius `0.42`，box，block stun `0.06s` | 正面格挡，近距离也可 dodge | 观察玩家是否能从前摇判断“该挡”；成功格挡会有极短 block stun，后续需要更明确的格挡命中 SFX/pose |
| `Enemy_Mobile` / `Feint Dash` | `0.12 / 0.08 / 0.28`，range `1.45`，radius `0.36`，box | 闪避或侧走位 | startup 短，不能再被提速；观察是否像无预兆小刀，必要时加更清楚起手姿态 |
| `Enemy_Ranged` / `Arc Bolt` | `0.22 / 0.10 / 0.32`，range `4.20`，arc projectile，speed `12` | 横向走位、距离管理 | 投射物要让玩家看见弹道高度；墙角和锁定镜头下需观察是否遮住弹道 |
| `Enemy_Gatekeeper` / `Gate Slam` | `0.28 / 0.12 / 0.42`，range `2.30`，radius `0.70`，box，breaks guard，guard-break hit stun `0.16s` | 闪避优先；硬挡会被破防 | Boss 慢重击入口，必须比普通近战更可读；命中反馈可高于普通敌人，后续需要专属破防姿态/音效 |
| `Enemy_Gatekeeper_Reach` / `Hall Sweep` | `0.40 / 0.14 / 0.55`，range `3.80`，radius `0.90`，wide box，block stun `0.10s` | 后撤/侧走位，格挡兜底但会被短暂压住 | 作为横扫/控场动作，重点看走位是否真的能离开命中范围 |
| `Enemy_Gatekeeper_Burst` / `Gate Lance` 与 `Enemy_Gatekeeper_Arc` / `Core Bolt` | straight projectile speed `17` vs arc projectile speed `13`、arc height `1.20` | 前者考横移反应，后者考读弹道和空间 | 两者必须保持速度、弹道和前摇差异，避免 Boss 远程招同质化 |
| `Enemy_Gatekeeper_SkyHook` / `Sky Hook` 与 `Enemy_Gatekeeper_RollCatcher` / `Pursuit Slam` | `Sky Hook` 标记 `AntiAir`，直线 projectile speed `20`；`Pursuit Slam` 标记 `ChaseRoll`，`0.28 / 0.12 / 0.44`，range `4.25`，forwardMovement `1.35` | 空中动作要用反空压回地面；长 roll 逃离要用延迟追击逼玩家确认时机 | UI 已有 `Anti-Air Incoming` / `Roll Catch Incoming` 专属 cue，追滚近战 ground / impact telegraph 会显示前压 lane，并已接轻量 camera impulse 与程序生成 SFX；实机仍重点看起手动作和命中/受击反馈是否足够清楚 |

2026-07-10 Boss GUI 观察：Boss capture driver 原先只枚举 active `EnemyBrain`，而 `BossTest` 的未激活 Encounter 会先禁用 Gatekeeper，导致观察入口开场失败。驱动现已包含 inactive member 并先激活 Encounter；复跑后 `Sky Hook / Pursuit Slam / Gate Slam` 均按 `0.60 / 4.10 / 7.60s` 触发，Gate Slam 的 `Guard Break: dodge; guard breaks`、红色地面 telegraph、目标攻击行和 Boss 身体起手可见。

2026-07-11 输入技术证据：`Gate Slam Guard` 使用完整虚拟 Ctrl `KeyboardState`，结果 `HP 100->74.4`，`AttackCommitted=True`、`block=True`、`guardStartup=True`、`activeGuard=True`、`guardBreak=True`；`Gate Slam Dodge` 使用虚拟 `W+LeftShift`，结果 `alignment=1.00`、`HP 100->100`、Agility `0->25`，`AttackCommitted=True`、`groundDodge=True`、`invulnerable=True`、`successfulDodge=True`。ordinary Guard 两拍同样通过 InputReader：startup 受击 `100->90` 且无 Counter / 窗口，active Guard 免伤并 `Counter +20`、窗口开启。三项都只标记虚拟 Input System 技术链通过，不等价于物理键盘或主观手感。

### 当前 camera obstacle gauntlet

GUI 入口为 `CampusRPG/Debug/Chapter01/Start Camera Obstacle Gauntlet`，每案结束用 `Next Camera Obstacle Case`，最终用 `Stop Camera Obstacle Gauntlet`。驱动在开始时备份 `slot_auto_chapter01.json`，停止时已将 SHA-256 `2679d6163e71ca45cf640cbcc35c85ff4bf4a3a9bfca4ab3d822ce89598ac0d8` 逐字节恢复；同时恢复 player / camera / A03 encounter / 屏障 / 三敌运行态。所有 METRICS 都保留 `manualSignoff=required automaticPass=false`。

2026-07-11 五案摘要：wide-wall `static=True minRetraction=0.621 occupied=False sideFlips=0`；pillar-orbit `static=True minRetraction=0.839 occupied=False sideFlips=0`；narrow-hall `static=True minRetraction=0.998 occupied=False sideFlips=1`；back-left-corner `static=True minRetraction=0.254 occupied=False sideFlips=0`；mantle-edge `PlayerMantleState static=False minRetraction=1.000 occupied=False sideFlips=0`。`targetInViewportThroughout` 只是 viewport bounds 指标；`maxFrameMotion` / `sideFlips` 会包含刻意移动，均不能单独判 Pass。

| 场景 | 当前自动化覆盖 | 走查重点 |
|---|---|---|
| 墙贴背 / 宽墙 | `ResolveAdjustedPosition_StopsBeforeWall_WhenDesiredCameraPointCrossesObstacle`、`ResolveAdjustedPosition_RetractsAlongBoom_WhenWideWallBlocksView` | 镜头收回到墙体内侧，不穿墙，不突然拉到玩家身体里 |
| 柱子 / 窄障碍 | `ResolveAdjustedPosition_SlidesAroundNarrowObstacle_InsteadOfCollapsingIntoPlayer`、`ResolveAdjustedPosition_PrefersCurrentSide_WhenAlternativesAreComparable` | 柱子挡视线时优先侧滑，且左右切换稳定 |
| 窄通道 | `ResolveAdjustedPosition_KeepsCenteredViewInNarrowCorridor_WhenPathIsClear` | 通道两侧墙不应让清晰路径下的镜头无故侧滑或收缩 |
| Chapter01 窄廊 / 墙背 | `Chapter01_CameraObstacleGauntlet_KeepsNarrowHallCenterClearBetweenPillars`、`Chapter01_CameraObstacleGauntlet_RetractsAgainstInteriorBackWallWithoutSidestep` | `Zone03` 柱子交错时中心通道仍保持清楚；内场背墙压迫时沿 boom 回收，不把宽墙误当窄柱横滑 |
| 角落 / 静态重叠 | `ResolveAdjustedPosition_DepentratesStaticOverlap_WhenProbeStartsInsideWall` | 镜头探针从墙体内开始时能退出，不停留在阻挡体里 |
| 动态角色遮挡 | `ResolveAdjustedPosition_DoesNotAcceptDynamicActorOverlap_WhenPathHasNoStaticObstacle`、`IsSegmentOccupiedByDynamicActor_IgnoresDeadActorBodies` | 活敌人身体不能把镜头卡死；死亡敌人不再顶开镜头 |

### 当前 CombatTest 初始合同快照

来源：`Assets/_Game/Data/Combat/*.asset`、`SO_CombatBalance.asset`、`PlayerAttackState`、`PlayerDodgeState`、`PlayerBlockState`、`PlayerCombatRuntimeUtility`。表中的 `recovery` 是运行时恢复段，已包含 `animationDurationSeconds` 对可见收势的延长。

| 动作 | 入口和 buffer | 时间合同 | 位移/命中 | 取消和失败代价 |
|---|---|---|---|---|
| `Light` combo | 地面轻击进入；第 1/2 段只在尾段 `inputBufferSeconds = 0.20s` 内接收下一段排队，当前段结束后才切，不是即时取消；combo reset `0.8s` | `Light_01` = `0.10 startup / 0.08 active / 0.292 recovery / 0.472 total`；`Light_02` = `0.10 / 0.08 / 0.3156 / 0.4956`；`Light_03` = `0.14 / 0.10 / 0.3972 / 0.6372` | 三段均 `forwardMovement 0.50`、`TimedWindow`、box hitbox、`hitStop 0.05`；movement scale 依次 `0.78 / 0.76 / 0.72` | 太早按轻击不会自动排下一段；第 3 段不再排下一段；空中基础轻击被 guard。P0.5 观察重点是尾段 buffer 是否顺但不抢输入、轻三连是否过度追踪 |
| `Heavy` | 地面重击进入；前推或空中 heavy 可能预览 `Rising Cleave`；重击后段 heavy 可能进入 `Iron Gate Break` | `0.20 startup / 0.12 active / 0.54 recovery / 0.86 total` | `forwardMovement 0.50`、movement scale `0.55`、range `2.30`、box half extents `{0.828, 0.9, 0.92}`、`hitStop 0.08`，比 Light 高一档 | `Iron Gate Break` cancel window 为 `0.22s`，因此从 `Heavy_01` 链入时约 `0.64s` 后才打开；起手和 active 不可取消 |
| `Dodge / Combat Roll / AirDodge` | 地面 lock-on dodge 仍是短闪；非锁定移动 dodge 进入 `CombatRoll`；格挡态 dodge cancel 进入 `CombatRoll`；空中 dodge 进入一次性 `AirDodge` | 短闪 gameplay `0.25s`、clip 约 `0.42s`；`CombatRoll` gameplay `0.42s`；`AirDodge` gameplay `0.28s`；三者都有独立 i-frame startup / invulnerable 配置 | 短闪 distance `2.80`；roll distance `3.60`；air dodge distance `2.35`，并给 `3.2` vertical velocity 维持空中动作感；方向由输入、相机和 lock-on 解析 | startup 未完成时不闪避伤害；成功穿招只登记一次，并打开 `0.80s` follow-up。`AirDodge` 每次离地只允许一次，落地后刷新；P0.7 观察重点是 roll 是否能承担脱离包围，air dodge 是否能接空中 SwordArt 而不变成无脑逃课 |
| `Guard` | block held 进入，松开即回 locomotion；格挡姿态立即成立，但有效防御要等 startup 结束；成功格挡开 counter window | guard startup `0.08s`；startup 后进入 active hold；block clip `0.80s`；P0.6-A 新增按攻击定义驱动的 block stun | 成功格挡免伤，给 counter gauge `+20`，counter window `0.80s`；普通 `Guard Swing` 只压 `0.06s`，`Hall Sweep` 压 `0.10s` | startup 未完成时不挡伤害，走普通受击/掉血路径，也不会给 counter gauge 或打开 counter window；`Gate Slam` 现在是 P0.6-A 破防入口，硬挡会掉血并进受击硬直；counter window 内重击仍可进入 `Counter`，若同一 heavy 输入匹配 `AfterBlock` 会优先消费 `Iron Gate Break` |
| `Sidewind Cut` | `LightAttack` + `Left/Right` + `AfterDodge`；trigger window `0.25s`；resource `0` | `0.08 startup / 0.10 active / 0.3274 recovery / 0.5074 total` | `forwardMovement 0.72`、movement scale `0.82`、range `2.05`、box half extents `{0.759, 0.825, 0.82}`、`hitStop 0.05` | cancel window `0.18s`。P0.5 重点是侧向收益不能被未来 assist 拉回正面，墙边不能越角或拖坏镜头 |
| `Rising Cleave` | `HeavyAttack` + 任意方向；需要 `ForwardInput` 或 `Airborne` 之一；trigger window `0.30s`；resource `0` | `0.18 startup / 0.12 active / 0.50 recovery / 0.80 total` | `forwardMovement 0.62`、movement scale `0.58`、range `2.35`、box half extents `{0.851, 0.925, 0.94}`、`hitStop 0.05` | cancel window `0.20s`。P0.5 重点是空中 heavy 入口可读、命中高度可信、落地前后不误触地面 heavy |
| `Iron Gate Break` | `HeavyAttack` + 任意方向；需要 `AfterBlock` 或 `AfterHeavy` 之一；trigger window `0.35s`；resource `0` | `0.14 startup / 0.12 active / 0.448 recovery / 0.708 total` | `forwardMovement 0.55`、movement scale `0.62`、range `2.25`、box half extents `{0.92, 1.0, 0.90}`、`hitStop 0.05` | cancel window `0.22s`；从 heavy 后段链入时必须等 late recovery，不能起手秒切。P0.5 重点是破防强度、空挥代价和格挡失败反馈 |

本轮 P0.5 已把 Guard 的“格挡姿态”和“有效防御判定”拆开：`PlayerStateMachine.IsBlocking` 仍服务动画和姿态，`HasActiveGuard` 才服务伤害防御。这样不会让一按下 block 就 frame-0 免伤，也不会为了防御判定去打断现有 block 动画表现。

Guard 的成功/失败反馈底线也已经用 `DamageableReceiver.ReceiveDamage()` 真实路径锁住：有效格挡免伤、给 `+20` counter gauge 并打开 `0.80s` counter window；startup 期被打中会掉血，且不打开反击窗口。P0.6-A 打开最小防御压制语义：攻击定义可标记 `blockStunSeconds` 或 `breaksGuard`，前者让玩家短暂保持 block stun、延迟即时反击，后者让 `Gate Slam` 这类慢重击穿透硬挡并进入受击硬直。破防分支会把 `PlayerHitReactionType.GuardBreak` 传给状态机、动画 relay 和调试 HUD，动画 relay 会请求相机做 `0.18m / 0.16s` 的短促 impact impulse，并切到专属 `GuardBreak` Animator state，先形成可测、可显示、可感知的专属反馈接线。最新 GuardBreak 玩家受击反馈已经从普通 hit clamp 拆出：普通受击仍保持 `0.04s - 0.12s` 短反馈，破防使用 `0.10s - 0.24s` 窗口，`Gate Slam` 当前 `0.16s` hit stun 不再被普通受击上限截断；破防结束前也不会被 movement / jump / dodge / light / heavy / skill 立即取消。当前 `CombatTest` 的 `GuardBreak` state 已升级为专属 `AN_Player_GuardBreak_CombatTest` 选择链：local preview 优先尝试 `OneHand_Up_Shield_Block_Hit_1_InPlace`、`Hit_F_2_InPlace` 或 `Hit_Reaction_Heavy`，public-safe proxy baseline 则用 guard-drop / collapse 曲线。2026-07-11 虚拟 Ctrl 技术链已确认 Gate Slam 会真正掉血并进入 GuardBreak；正式绑定素材下的身体 / SFX / camera 合层仍只由人工 feel 签字。

P0.7 开始不再冻结新动作：`CombatRoll`、`AirDodge` 和 guard-cancel roll 已进入运行时状态语义，并且已经接上专属 placeholder clip、air dodge 后一次性空中 SwordArt 追击、`Falling Star` 下砸、Gatekeeper `Sky Hook` 反空与 `Pursuit Slam` 追滚回应。`CombatRoll` 现在也有滚后轻击代价：滚中轻击只缓存一次，完整 recovery 后才接基础 Light 或专属 `Cross Step`，普通短闪侧向轻击则继续走 `Sidewind Cut`，两者不会混成同一个动作身份。P6.5-C 已补第一层读招反馈：反空/追滚有专属 cue 和颜色，追滚近战会画前压 lane；P5.5-A 又把 roll / air dodge / 下砸 / 破防 / 反空 / 追滚接进 `CombatDebugHUD` 短反馈行，方便实机走查时直接观察当前动作语义。P5.5-B 则把这些动作语义推进到轻量 camera impulse：roll / air dodge / 核心 SwordArt 和 Gatekeeper 反空/追滚 cue 都会给短促但不抢镜的镜头反馈。P5.5-C 进一步补了 public-safe procedural SFX：这些动作和回应会播放经过 `SO_AudioSettings` SFX 音量的 one-shot chirp。最新 Boss cue 会额外显示 response hint，把 `Sky Hook`、`Pursuit Slam`、破防重击和远程 projectile 的主解法直接写在短提示里，方便绑定素材走查时确认玩家是否能理解“该怎么答”。后续优先补敌人起手、命中/受击反馈、`Moon Sever` 和实机烟雾，而不是回到只调数值的保守路线。

2026-07-11 边界更新：Boss driver 已能激活 inactive Encounter，并通过虚拟 Ctrl / `W+LeftShift` 验证 GuardBreak 与 successful dodge 技术链；物理键盘反应时机、`0.16s` 失控体感、SFX 与 camera impulse 的最终合层仍是人工主观边界。

Dodge 同样已从 frame-0 i-frame 收成短启动合同：`dodgeInvulnerableStartupSeconds = 0.04s`，之后进入 `0.20s` 无敌帧；`DodgeFollowUp` 只在 `TryNotifySuccessfulDodge()` 真正登记成功闪避后打开，普通空闪不会白送追击。

Light combo 也从“整段动作都能排下一段”收成尾段输入合同：`PlayerAttackState` 只在当前攻击剩余时间小于等于 `CombatBalanceSO.InputBufferSeconds` 时接受下一段排队。早按不会自动连，窗口内按才会在当前段结束后接下一段。

当前没有正式 target assist 数据字段；P0.5 只记录验收边界，不默认新增吸附。若后续实现 assist，必须先补 `acquisition cone / max correction angle / assist start-end / line-of-sight requirement / target switch delay` 这些字段或等价合同。

## 6. P0.5 / P0.6 / P0.7 边界

`P0.5-Feel` 只动现有参数、现有 clip 的时序解释、现有 CombatTest 场景和现有数据表。优先包括 input buffer、cancel window、forwardMovement 曲线、hitbox 起止、attack tracking 截止点、dodge 距离/i-frame/recovery、guard startup/block stun、target assist 角度/距离/衰减、camera damping/FOV/distance/obstacle layer、hit stop/VFX/SFX/camera impulse。

`P0.6-NewActions` 才放需要新状态、新 clip、新测试的内容：真正 `AirDodge`、独立空中受击/落地恢复状态、破防专用受击 clip、parry/deflect、poise/hyper armor、launch/knockdown、敌人新增读招动作、特殊处决/追击、房间级 camera volume 或新的 target-switch 语义。

判断标准：如果只是“同一动作更早/更晚、更近/更远、更明显/更弱”，放 P0.5；如果会新增一种玩家可承诺行为、敌人可读行为、受击类型、状态转换或动画资产依赖，放 P0.6。

`P0.7-Expression` 是用户明确要求“别保守，直接走”后的进攻型动作扩展层。它允许先落运行时语义，再集中验证和补表现；优先级是：

1. 玩家表达：`AirDodge`、`CombatRoll`、guard-cancel roll、空中 SwordArt / 下砸。
2. 敌人回应：反空、追滚、破防后追击、Boss 二择。
3. 表现补强：专属 clip、SFX/VFX、镜头 impulse、HUD 状态。
4. 回归收口：统一 gate，而不是每个小参数都打断开发节奏。

## 7. 下一步建议

当前不再扩动作系统，按这个顺序完成总体收口：

1. ordinary Guard 与 Boss Guard / Dodge 的虚拟 Input System 技术链已通过，不再扩 capture driver；若发布前需要 feel 签字，只补一轮物理 Ctrl / Shift 的人工反应时机和合层观察。
2. Chapter01 五案例 camera gauntlet 的技术烟雾、METRICS 与存档恢复已完成；保留舒适度 / 构图人工签字，不把遥测自动判成体验 Pass。
3. C4 的 fresh XML，以及 C5 的 Windows 真机构建与 Mac 正式签名 / 公证归发布环境收口；当前 Mac 内部 RC 的 build / MainMenu launch smoke 已完成，不再混入动作合同判断。
4. 只有人工实测出现明确问题才进入 `Tune P0.5`；没有观察证据时不新增状态、动作或系统。
