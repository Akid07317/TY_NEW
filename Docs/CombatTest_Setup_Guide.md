# CombatTest 接线清单

本清单用于尽快把 `Assets/_Game/Scenes/CombatTest.unity` 接成一个可验证“玩家 vs 三类敌人”的小型战斗沙盒。目标不是漂亮，而是能尽快验证输入、移动、攻击状态、量表，以及近战 / 机动 / 远程三条敌人行为链路。

## 0. 一键搭建入口

当前项目已经提供一键生成入口：

- `CampusRPG/Setup/Build CombatTest Scene`
- `CampusRPG/Setup/Build CombatTest Scene (Force Rebuild)`
- `CampusRPG/Setup/Repair CombatTest Prefab Wiring`
- `CampusRPG/Setup/Repair CombatTest Scene Lighting`
- `CampusRPG/Setup/Repair CombatTest Scene NavMesh`
- `CampusRPG/Setup/Local Preview/Apply Imported Enemy Avatar Chain To CombatTest Enemy Prefabs`

它会自动：

- 生成或更新 `CombatTest` 所需的占位数据资产
- 生成 `PF_Player_CombatTest`、`PF_Enemy_Melee_CombatTest`、`PF_Enemy_Mobile_CombatTest`、`PF_Enemy_Ranged_CombatTest`
- 为玩家和三类敌人生成一套低成本代理可视外形与材质，便于在正式模型缺席时先判断空间感、朝向和攻击压迫感
- 生成 `SO_AudioSettings`
- 生成 `PF_Projectile_SpellBolt`
- 生成 `PF_VFX_ProjectileImpact_SpellBolt`
- 生成 `AC_Player_CombatTest`、玩家攻击片段，以及基础动作层片段
  当前玩家控制器除了攻击状态，还会生成 `Locomotion / Block / Airborne / Dodge / CombatRoll / AirDodge / Hit / GuardBreak / Death` 基础状态
  仓库默认只生成可提交的 proxy / approved `_Game` 动作片段；只有在你本地手动打开预览开关并执行 local preview 菜单时，才会重建 imported preview 动作
  当前 SwordArt 片段包含 `Sidewind Cut`、`Rising Cleave`、`Iron Gate Break`、`Falling Star`、`Cross Step` 与 `Moon Sever`；其中 `Falling Star` 使用空中 `Heavy + Neutral/Backward` 触发，`Moon Sever` 使用空中 dodge 后 Light 触发，`Cross Step` 使用 roll 后 Light 触发，`Rising Cleave` 保留空中/前推 heavy 的追击角色
  玩家攻击片段现在会保留一小段导入动作的收招尾巴，而不是只按命中窗口长度硬裁；运行时也会把 `forwardMovement` 用到玩家攻击前送上
  新增的 `SwordArt_` 运行时恢复策略会额外保留下砸、横切和破防类招式的可见 follow-through，避免 `Falling Star`、`Moon Sever` 等绑定素材在 hit window 结束后立刻被切回 locomotion；命中窗口和输入触发仍以 attack SO / SwordArt SO 为准
  因此现在已经可以直接评估移动、格挡、闪避、受击和死亡的整体手感，而不只是静态壳子加命中事件
- 当项目里已经导入兼容的 Humanoid 角色与动作资源时，你可以手动切到 local preview 模式，让玩家本地 `CombatTest` 动画片段重建成真实动作副本，并把玩家 prefab 切到导入的人物外观；详细规则见 [素材来源清单](Docs/Asset_Source_List.md)
- 敌人 imported Avatar chain 目前不属于标准 build / repair 链；如果你要实验，只能走单独的 local preview 菜单。它会给 enemy root 挂单独的 `Animator + EnemyCombatAnimationRelay`，而不是再把 skinned humanoid 塞进旧 proxy 表现链。Gatekeeper `Sky Hook` / `Pursuit Slam` / `Gate Slam` 的 `ResponseRead` / `AntiAirRead` / `ChaseRollRead` / `GuardBreakRead` 会随 Startup / Advance / Recovery 渐入渐出；当前本地 `AC_Enemy_ImportedPreview_EnemyMelee/Mobile/Ranged.controller` 也已带 `Attack_AntiAir`、`Attack_ChaseRoll` 与 `Attack_GuardBreak` state，便于判断绑定动作是否有预备、出手和回收，而不是一进攻击态就满值弹姿态。
- `CombatTest` 场景本身不实例化 `Gatekeeper`。如果要看 `Sky Hook / Pursuit Slam / Gate Slam`，请改到 `Assets/_Game/Scenes/BossTest.unity` 或含 `Boss_Gatekeeper` 的场景，并用 `CampusRPG/Setup/Local Preview/Start Boss Read Capture Driver/*` 或终端 `Tools/unity-cli/ty-new-ghostsamurai-observe-boss-reads` 触发运行时观察链；这条链会在当前 scene instance 上临时挂 imported enemy preview，不要求把场景保存成 local-preview 脏态。
- 重建 `Assets/_Game/Scenes/CombatTest.unity`
- 在场景内放入 `CombatDebugHUD` 与正式 `SwordArtHUD`；Debug HUD 会显示技能状态、当前/候选 SwordArt、玩家当前 Animator clip / normalized time / blend weight、锁定目标当前 Animator clip / normalized time / blend weight，以及 roll、air dodge、破防、反空和追滚的短反馈行，并按 Game 视图宽度收缩、在接近底部 `SwordArtHUD` 前停止继续下画；如果信息被折叠，最后一行会显示 `+N debug lines hidden`，避免绑定素材走查时左上调试层压住动作或静默吞掉状态。锁定目标的 `Target Anim` 会排在目标 HP、技能状态和操作帮助之前，短 Game 视图中优先保留敌人当前动画读招证据。当前攻击会显示 compact `Atk:` 行，例如 `Atk: MoonSever Act 0.25/0.72 hit .20-.32`，把 `Startup` / `Active` / `Recovery` / `Done` 与 hit window 直接暴露出来，方便逐招核对命中点和收招拖尾；短 Game 视图里会优先保留攻击阶段和 `Target Anim`，再显示较重复的 action cue。左上调试层会绘制半透明深色底板，避免白字压在浅天空、浅地面或本机预览素材上读不清；需要专心看角色身体、武器轨迹和敌人起手时，可按 `F1` 或反引号键 `` ` `` 折叠 Debug HUD，只保留一条小提示，正式 `SwordArtHUD` 仍会显示。`SwordArtHUD` 会在屏幕下方显示当前触发、最近触发、cancel 链接窗口和候选招式，优先覆盖 `Cross Step`、`Falling Star` 与 `Iron Gate Break`。当前玩家 roll / air dodge / 核心 SwordArt 与 Gatekeeper 反空/追滚 cue 会触发轻量 camera impulse；相机冲击带优先级，低优先级移动反馈不会覆盖 `Falling Star`、`Iron Gate Break`、GuardBreak 或 `Pursuit Slam` 这类更重要的读招/命中反馈。动作反馈同时会播放经过 `SO_AudioSettings` 全局 SFX 音量、per-cue cooldown、mix group、空间衰减和短暂 priority dominance 策略处理的程序生成 one-shot chirp；低优先级 roll / air dodge 声效不会在同一拍盖住 `Pursuit Slam`、`Falling Star`、`Iron Gate Break` 或 GuardBreak 这类更重要提示。Debug HUD 还会短暂显示最近一次 SFX 决策，例如 `SFX: PursuitSlam play p30 BossResponse`、`SFX: Roll held p30 0.07s` 或 `SFX: Roll cd 0.08s`，用于实听时判断声效是已播放、被冷却挡住，还是被高优先级读招压住；短屏下这行只是辅助信息，不会挤掉 `Atk`、`Target Anim`、`Tgt Atk` 这些核心走查证据。Debug HUD 里的 Boss 读招会使用 compact 行，例如 `Boss: RollCatch PursuitSlam - delay dodge`，短 Game 视图中会优先保留 `Atk` / `Target Anim` / `Tgt Atk` / compact Boss cue，内部 `State` 行可后移；顶部正式 Boss cue 仍保留完整解法文案。Boss 顶部 cue 会用响应式安全宽高显示短解法提示，并在短 Game 视图里与底部 `SwordArtHUD` 保持最小间距，方便在手感走查时判断动作层级和主解法是否读得出来
- `Gate Slam` 破防现在也会进入 Boss 读招观察链：顶部正式 cue 会显示 `Guard Break Incoming`，左上 Debug HUD compact 行会显示 `Boss: GuardBreak GateSlam - dodge; guard breaks`，方便和 `Attack_GuardBreak`、`GuardBreakRead`、`Tgt Atk` 同屏核对。
- `Gate Slam` 硬挡失败后的玩家反馈也已独立：`PlayerHitState` 会按 `GuardBreak` 保留约 `0.16s` 破防受击，不再被普通受击 `0.12s` 上限截断；这段时间内移动、跳跃、dodge、轻重攻击和 skill 都不能立即取消。`PlayerCombatAnimationRelay` 会请求 `GuardBreak` Animator state，当前 CombatTest 生成链会绑定 `AN_Player_GuardBreak_CombatTest`，local preview 优先尝试盾挡受击 / 重受击素材，public-safe proxy baseline 则使用专属 guard-drop / collapse 曲线，不再只复用普通 `Hit` motion 慢放。走查时要确认“闪避是主解、硬挡会短暂失控”能从身体反馈、HUD、SFX 和镜头一起读出来。
- Boss 顶部正式 cue 的攻击名会按当前矩形宽度只在绘制层做中间省略，避免绑定 local preview / imported 资源后长显示名在窄 Game 视图里横向裁掉；内部完整攻击名仍保留给合同、调试和日志。
- Boss 顶部正式 cue 的短解法提示也会在绘制层做语义压缩，例如 `Delay dodge; lane catches rolls` 会显示为 `Delay dodge; lane`，`Land or guard; avoid air hang` 会显示为 `Land/guard; avoid air`；内部完整 `CurrentResponseHint` 仍保留给合同和调试，避免窄 Game 视图把“怎么解”这行硬裁掉。
- Boss 顶部正式 cue 的三行样式现在都显式不换行，并在各自 `Rect` 内裁切。绑定 local-preview 素材后如果攻击名或提示里混入长素材名、宽字形或临时调试前缀，文字最多被面板内裁掉，不会溢出到 Boss 身体、地面 telegraph、`SwordArtHUD` 或画面动作区域。
- 锁定目标正在攻击时，Debug HUD 会在 `Target Anim` 后显示 `Tgt Atk:` 短行，例如 `Tgt Atk: PursuitSlam Start 0.14/0.84 hit .28-.40`。这行用于核对敌人当前 startup / active / recovery、hit window、Boss cue 和画面起手是否一致；短 Game 视图会优先保留 `Target Anim`、`Tgt Atk` 与 compact Boss cue，玩家自身 `Anim Clip` 和内部 `State` 行会后移，避免检查 `Sky Hook` / `Pursuit Slam` 时敌人时序证据被折叠。
- `Atk:` / `Tgt Atk:` 行会为长 local-preview / imported 动作名动态压缩显示名，优先保留阶段、elapsed / total 和 hit window。绑定素材后如果显示名带素材包前缀或长变体名，HUD 仍应保留类似 `Act 0.25/0.72 hit .20-.32`、`Start 0.14/0.84 hit .28-.40` 的关键信息，而不是只显示一串被裁掉的动作名。

如果你只是想快速进入战斗测试，优先使用这个菜单，而不是按下面清单逐项手工创建。

如果你已经有现成的 `CombatTest` 角色 prefab，不想整包重建场景，只想把 `RequireComponent` 造成的重复组件清掉，并把玩家的 `PlayerCombatAnimationRelay` 接回 prefab，请使用 `Repair CombatTest Prefab Wiring`。它会就地修复 `PF_Player_CombatTest` 和三类敌人 prefab 的内部组件引用，并把玩家/敌人一起恢复到 public-safe proxy baseline；敌人 imported Avatar chain 即使之前做过 local preview，也会在这里被拆回稳定的 proxy 基线，不会重建整个场景。
当前修复流程也会把玩家的 `Animator`、`PlayerCharacter`、`PlayerStateMachine`、`PlayerMotor` 和 `PlayerCombatAnimationRelay` 的新动作层引用重新接齐。
如果当前 `CombatTest` 画面过曝、地面发白、角色细节被洗掉，可以先执行 `Repair CombatTest Scene Lighting`。它会把方向光、环境光和反射强度收回到适合本地预览读动作的范围，不会改玩家/敌人的正式基线接线。

当前默认入口在检测到以下目标已存在时，会先弹确认框，再执行覆盖：

- `Assets/_Game/Scenes/CombatTest.unity`
- `Assets/_Game/Prefabs/Characters/PF_Player_CombatTest.prefab`
- `Assets/_Game/Prefabs/Characters/PF_Enemy_Melee_CombatTest.prefab`
- `Assets/_Game/Prefabs/Characters/PF_Enemy_Mobile_CombatTest.prefab`
- `Assets/_Game/Prefabs/Characters/PF_Enemy_Ranged_CombatTest.prefab`

如果你已经在这些文件里做了手调，请先复制备份，再执行重建。`Force Rebuild` 入口保留给明确知道自己要覆盖的人，批处理和自动化仍会直接重建。

## 0.5 P0.5 后手感走查入口

P0 / P0.5 当前已经通过自动化合同回归，下一步不是继续盲目增加新动作，而是在 `CombatTest` 里做一次实机手感走查。走查记录统一填到 [动作手感研究](Action_Game_Feel_Research.md) 的“P0.5 后实机走查记录表”。

推荐进入顺序：

1. 打开 `Assets/_Game/Scenes/CombatTest.unity`。
2. 若只是做公开仓库安全基线走查，先执行 `CampusRPG/Setup/Repair CombatTest Prefab Wiring`，确认玩家和敌人都回到 proxy baseline。
3. 若画面过曝或角色细节被洗掉，执行 `CampusRPG/Setup/Repair CombatTest Scene Lighting`。
4. 只在需要判断本机 imported 预览动作时，才手动执行 local preview 菜单；走查结束后再执行 `Repair CombatTest Prefab Wiring` 回到 proxy baseline。
5. 如果当前正在走 `GhostSamurai` 本机研究线，刷新 preview 后可直接执行 `CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Flank Reads/Clean HUD`，或在终端写入 `Tools/unity-cli/ty-new-ghostsamurai-observe-swordarts flank-clean`，自动依次跑 `GroundDodge only`、`Sidewind Cut` 与 `Cross Step` 观察序列；若要继续看 `AirDodge + Light` 与 `AirDodge + Heavy`，执行 `CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Clean HUD` 或终端 `Tools/unity-cli/ty-new-ghostsamurai-observe-swordarts clean`；若这轮要专门对照空中 heavy 的两个分支，执行 `CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Air Heavy Reads/Clean HUD`，或终端写入 `Tools/unity-cli/ty-new-ghostsamurai-observe-swordarts airheavy-clean`，自动依次跑 `Rising Cleave` 与 `Falling Star` 的空中/空中闪避版本；若这轮要专门复核 `Iron Gate Break` 的挡架转攻和重击追接，再执行 `CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Iron Gate Break/Clean HUD`，或在终端写入 `Tools/unity-cli/ty-new-ghostsamurai-observe-swordarts irongate-clean`。若这轮要快速对照 `CombatTest` 里 melee / mobile / ranged 三类敌人的读招差异，执行 `CampusRPG/Setup/Local Preview/Start CombatTest Enemy Read Capture Driver/Clean HUD`，或在终端写入 `Tools/unity-cli/ty-new-ghostsamurai-observe-enemy-reads clean`，驱动会依次触发 `EnemyMelee / Guard Swing`、`EnemyMobile / Feint Dash` 与 `EnemyRanged / Arc Bolt`，并在运行时临时挂 imported enemy preview，不要求把 `CombatTest` 场景保存成 local-preview 脏态。若这轮只想盯 Bow 三段读招，执行 `CampusRPG/Setup/Local Preview/Start CombatTest Enemy Read Capture Driver/Ranged Variants/Clean HUD`，或在终端写入 `Tools/unity-cli/ty-new-ghostsamurai-observe-enemy-reads ranged-clean`；驱动会固定跑 `EnemyRanged / Anti-Air Shot`、`EnemyRanged / Chase Roll Shot` 与 `EnemyRanged / Guard Break Shot`，把 `Attack_Ranged_AntiAir`、`Attack_Ranged_ChaseRoll`、`Attack_Ranged_GuardBreak` 连成一条本机 Bow 观察链。
6. 按 `Action_Game_Feel_Research.md` 的表格逐项填写 `观察结果` 和 `判定`，判定只用 `Pass` / `Tune P0.5` / `Open P0.6`。

### C6 输入与镜头烟雾入口

下面 3 组入口用于发布候选前的技术烟雾，不替代真人手感签字：

- 普通 Guard：打开 `Assets/_Game/Scenes/CombatTest.unity`，执行 `CampusRPG/Setup/Local Preview/Start CombatTest Enemy Read Capture Driver/Guard Input Validation/Debug HUD`。驱动通过虚拟键盘的真实 `<Keyboard>/leftCtrl` 输入跑两拍，并且只接受目标为 Player、archetype 为 `EnemyMelee`、attack id 为 `Enemy_Melee`、damage 大于 0 的 `AttackCommitted`。2026-07-11 GUI 已得到 `[TY_NEW EnemyGuardInputDriver] PASS`：startup 拍 HP `100 -> 90`、Counter `0 -> 0`、`counterWindow=false`；active-guard 拍 HP `100 -> 100`、Counter `0 -> 20`、`counterWindow=true`，并观测到 startup、active guard 与 block stun。该结果证明当前 InputReader / 状态 / 伤害链，不等于物理 Ctrl 的主观手感
- Boss Guard Break：打开 `Assets/_Game/Scenes/BossTest.unity`，执行 `CampusRPG/Setup/Local Preview/Start Boss Input Capture Driver/Gate Slam Guard/Debug HUD`。已取得的 GUI 技术证据为 HP `100 -> 74.4`，并同时看到 `attackCommitted=true`、`block=true`、`guardStartup=true`、`activeGuard=true`、`guardBreak=true`、`outcome=PASS`
- Boss Dodge：同场景执行 `CampusRPG/Setup/Local Preview/Start Boss Input Capture Driver/Gate Slam Dodge/Debug HUD`。已取得的 GUI 技术证据为 HP `100 -> 100`、Agility `0 -> 25`、dodge alignment `1.00`，并同时看到 `attackCommitted=true`、`dodge=true`、`groundDodge=true`、`invulnerable=true`、`successfulDodge=true`、`outcome=PASS`
- Chapter01 镜头障碍：打开 `Assets/_Game/Scenes/Chapter01_Combined.unity`，依次执行 `CampusRPG/Debug/Chapter01/Start Camera Obstacle Gauntlet`、每个案例后执行 `CampusRPG/Debug/Chapter01/Next Camera Obstacle Case`，最后执行 `CampusRPG/Debug/Chapter01/Stop Camera Obstacle Gauntlet`。5 个案例是 `wide-wall`、`pillar-orbit`、`narrow-hall`、`back-left-corner`、`mantle-edge`；该工具只布置玩家、目标和障碍并记录 obstruction、sidestep、retraction、camera motion、side flips、`targetInViewport` 与 owner hidden 等 telemetry，不注入战斗输入，也永远不会自动输出 PASS。2026-07-11 Game View 技术烟雾中五案均 `occupiedEver=false`，前四案 `staticSeen=true`，mantle 案见 `PlayerMantleState`；窄廊 `sideFlips=1` 混有刻意输入，只作观察量。Stop 后章节 save 已按 SHA-256 `2679d6163e71ca45cf640cbcc35c85ff4bf4a3a9bfca4ab3d822ce89598ac0d8` 逐字节恢复。`targetInViewport` 只表示视口范围，不表示无遮挡；仍需人工判断构图、跳边与晕动是否可接受

上述 Boss / ordinary Guard 数值只证明指定设备路径、状态与结算合同在本轮 GUI 运行中闭环，不证明动作“好看”“自然”或手感已验收。镜头五案也只关闭技术烟雾，仍保留人工舒适度结论。`GhostSamuraiCombatEnemyReadCaptureDriverTests`、`GhostSamuraiBossReadCaptureDriverTests` 或 response-file compile 只能证明菜单/合同/程序集静态范围成立，不能替代 fresh Unity TestRunner XML，更不能替代 Game View 人工观察。

如果当前主树故意保留在 local preview 脏态，但又要补一轮“repair 后 baseline 是否仍然健康”的自动化证据，不要直接在主树跑 baseline 测试。改用：

```bash
Tools/unity-cli/ty-new-ghostsamurai-baseline-check --startup-timeout 90
```

它会在临时克隆里先执行 `Repair CombatTest Prefab Wiring`，再跑 `CombatTestAnimationAssetWiringTests + ReleaseCandidatePreflightTests`，避免为了取证去动当前人工调试中的主工作树。

如果这轮想先把 GhostSamurai 的 local-preview 研究线和 baseline 恢复线一起过一遍，再回 Unity GUI 做观察，直接跑：

```bash
Tools/unity-cli/ty-new-ghostsamurai-verify --startup-timeout 90
```

它会顺序串起 `ty-new-ghostsamurai-preview-check` 与 `ty-new-ghostsamurai-baseline-check`，并在终端最后打印下一组 `flank-clean / clean / airheavy-clean / irongate-clean / enemy-reads / boss-reads` 观察命令，作为本轮最短收尾入口。

进入 `P0.6-NewActions` 前必须满足下面任一条件：

- 现有参数、现有 clip、现有镜头或反馈无法表达走查观察到的问题。
- 当前动作合同导致玩家无法稳定读懂“该挡、该闪、该走”。
- 需要新状态、新 clip 或专属受击/破防反馈才能解决问题。

否则先留在 `Tune P0.5`，只调现有数据、镜头或反馈参数。

## 1. 场景最小构成

场景内至少保留以下对象：

- `Bootstrap`
- `Main Camera`
- `Directional Light`
- `Ground`
- `PlayerSpawn`
- `EnemySpawn_Melee`
- `EnemySpawn_Mobile`
- `EnemySpawn_Ranged`
- `CombatDebugHUD`
- `SwordArtHUD`

## 2. Bootstrap 对象

在 `Bootstrap` 对象上挂：

- `GameBootstrap`
- `InputReader`

`InputReader` 需要拖入：

- `Actions Asset` -> `Assets/_Game/Data/Input/CampusInputActions.inputactions`

如果后续要跨场景保留输入入口，可保持 `GameBootstrap.keepAliveAcrossScenes = true`。

## 3. 摄像机

### 当前最小方案

在 `Main Camera` 上先挂：

- `ThirdPersonCameraController`

当前版本中，`ThirdPersonCameraController` 已经负责：

- 跟随玩家
- 鼠标自由转镜
- 锁定目标时朝向收束
- 贴近墙体或柱体时用球形探测把镜头收回到阻挡体内侧，避免镜头跑到墙外看不到玩家

建议连接：

- `Follow Target` -> 玩家本体
- `Input Reader` -> `Bootstrap` 上的 `InputReader`

后续导入 `Cinemachine` 完成后，建议补：

- 一个自由跟随相机
- 一个锁定相机
- 玩家作为 Follow Target

### 当前阶段建议

先不追求完整锁定镜头，只要保证相机能跟着玩家并提供正确朝向参考即可。
当前默认相机已经带最小遮挡修正；如果后续切到 `Cinemachine`，至少要保留“贴墙不穿帮、玩家始终可见”这一行为基线。

## 4. 玩家对象

创建 `PF_Player` 时，最小需要：

- `CharacterController`
- `PlayerCharacter`
- `PlayerMotor`
- `PlayerStateMachine`
- `PlayerCombatController`
- `SkillController`
- `LockOnTargetSelector`
- `AttackExecutor`
- `HitboxController`
- `DamageableReceiver`
- `HealthComponent`
- `ManaComponent`
- `GaugeComponent`

当前自动生成的 `PF_Player_CombatTest` 默认恢复为 `CombatProxyVisualRoot` 代理外形：

- `Repair CombatTest Prefab Wiring` 与 `Build CombatTest Scene` 的标准路径都会把玩家拉回 proxy baseline，而不是根据本机素材目录自动改正式输出
- 如果你想做 local preview，先打开菜单 `CampusRPG/Setup/CombatTest/Prefer Imported Player Sources When Available`
- 然后手动执行 `CampusRPG/Setup/Local Preview/Rebuild CombatTest Imported Player Animations`
  当前这条 local preview 攻击链会优先尝试 `Assets/GhostSamurai_Animset/` 下的 katana / APose / Inplace 动作；如果本机没有这包，再回退到 `DoubleL` / `Kevin 1H` 的单手挥砍资源，最后才回退到 `2H / Polearm` 候选
- 如需把当前玩家 prefab 切到导入角色，再执行 `CampusRPG/Setup/Local Preview/Apply Imported Player Visuals To CombatTest Player Prefab`
  当前会优先使用 `Assets/JC_LP_MedievalCharacters_LITE/Prefabs/SM_MedievalMaleLite_01.prefab`；若该资源不存在，再回退到 `Assets/Kevin Iglesias/` 下的兼容 Humanoid prefab
  如果首选角色材质仍指向 HDRP / 不受支持 shader，本地预览现在会自动在 `Assets/_Game/Animations/Characters/CombatTest/LocalPreview/Materials/Player/` 下生成 built-in 兼容材质，避免玩家在 CombatTest 里整个人变成粉紫色
  如果角色材质已经正常但场景还是偏白，先执行一次 `CampusRPG/Setup/Repair CombatTest Scene Lighting` 再看 Game 视图，避免把“场景灯太亮”误判成“角色材质有问题”
  如果本机存在 `Assets/Free medieval weapons/Prefabs/Sword_OH.prefab`，同一步会优先把这把单手剑挂到 imported 右手骨；若缺失才回退到其他本地武器候选。应用后会自动隐藏 proxy 剑体，只留下前向标记方便读朝向
- local preview 结束后，再执行一次 `Repair CombatTest Prefab Wiring`，把 prefab 恢复回 public-safe baseline

如果目标是直接生成带 GhostSamurai 人物、Humanoid Avatar、刀和动作的内部 Mac 试玩包，不需要手工把主树停在 local-preview 脏态，使用：

```bash
Tools/unity-cli/ty-new-build-release art-mac --wall-timeout 1800
```

该入口只允许 `GhostSamurai_Animset` 同源人物/武器/动作，强制在临时克隆中应用 imported 绑定，输出到 `Builds/ReleaseCandidate/UserOwnedArt/Mac/TY_NEW.app`，随后销毁克隆。它不是 public-safe RC，也不能替代外发前的完整 EULA 核验。Windows 对应命令为 `art-windows`，当前机器仍受 Windows Build Support 缺失限制。

`PlayerCharacter` 需要连接：

- `Input Reader` -> `Bootstrap` 上的 `InputReader`
- `Motor` -> 本体 `PlayerMotor`
- `State Machine` -> 本体 `PlayerStateMachine`
- `Combat Controller` -> 本体 `PlayerCombatController`
- `Skill Controller` -> 本体 `SkillController`
- `Attack Executor` -> 本体 `AttackExecutor`
- `Damageable Receiver` -> 本体 `DamageableReceiver`
- `Health / Mana / Gauges` -> 对应本体组件
- `Camera Transform` -> `Main Camera`
- `Base Stats` -> 后续创建 `SO_PlayerBaseStats`

`PlayerCombatAnimationRelay` 当前负责两件事：

- 攻击时按 `AttackDefinitionSO.AnimationStateName` 直接切到对应攻击状态
- 平时持续同步 `GroundSpeed / IsGrounded / IsBlocking / VerticalSpeed`，驱动基础动作层在移动、起跳、格挡、闪避、受击、死亡之间切换

`PlayerCombatController` 需要至少配置：

- `Balance` -> `SO_CombatBalance`
- `Attack Executor` -> 本体 `AttackExecutor`
- `Hitbox Controller` -> 本体 `HitboxController`
- `Light Attack Combo` -> 3 个 `SO_AttackDefinition`
- `Heavy Attack`
- `Dodge Follow Up Attack`
- `Counter Attack`

`AttackExecutor` 原型阶段需要至少配置：

- `Attack Origin` -> 玩家武器前方的空物体，未填时默认使用角色本体
- `Target Mask` -> `Enemy` 层

`SkillController` 最少需要配置：

- `Owner` -> 本体 `PlayerCharacter`
- `Mana` -> 本体 `ManaComponent`
- `Attack Executor` -> 本体 `AttackExecutor`
- `Lock On Target Selector` -> 本体 `LockOnTargetSelector`
- `Cast Origin` -> 玩家施法点空物体，未填时回退角色本体
- `Skill 1` -> `SO_Skill_SpellBolt`
- `Skill 2` -> `SO_Skill_ForceBurst`

当前占位资产里，`SO_Skill_SpellBolt` 已默认挂到 `PF_Projectile_SpellBolt`，会沿锁定目标方向发射实体投射物，并在命中或撞到场景阻挡体时刷出 `PF_VFX_ProjectileImpact_SpellBolt`；当前发射与命中音效使用运行时生成的 one-shot chirp，并会经过 `SO_AudioSettings` 的全局 SFX 音量；`SpellBolt` 默认保持直射，`SO_Skill_ForceBurst` 仍保留近身范围爆发的原型判定。

`LockOnTargetSelector` 最少需要配置：

- `Input Reader` -> `Bootstrap` 上的 `InputReader`
- `Camera Controller` -> `Main Camera` 上的 `ThirdPersonCameraController`
- `Camera Transform` -> `Main Camera`
- `Target Mask` -> `Enemy` 层

如果当前还没有正式的“成功闪避判定”，可在原型阶段临时开启：

- `Prototype Grant Dodge Follow Up On Any Dodge`

正式章节前应关闭该原型选项，并改为由真实成功闪避事件驱动。

## 5. 敌人对象

创建 `PF_Enemy_Melee_A`、`PF_Enemy_Mobile_A`、`PF_Enemy_Ranged_A` 时，最小需要：

- `NavMeshAgent`
- `EnemyBrain`
- `EnemyStateMachine`
- `EnemySensing`
- `EnemyMotor`
- `EnemyAttackController`
- `DamageableReceiver`
- `HealthComponent`
- `LockOnTarget`
- 碰撞体

`EnemyBrain` 需要连接：

- `Archetype` -> 对应的 `SO_Enemy_Melee` / `SO_Enemy_Mobile` / `SO_Enemy_Ranged`
- `State Machine` -> 本体 `EnemyStateMachine`
- `Sensing` -> 本体 `EnemySensing`
- `Motor` -> 本体 `EnemyMotor`
- `Attack Controller` -> 本体 `EnemyAttackController`
- `Damageable Receiver` -> 本体 `DamageableReceiver`
- `Health` -> 本体 `HealthComponent`

`EnemyAttackController.attackOrigin` 默认可留空，未填时会回退到自身 Transform。

敌人当前默认固定走 `CombatProxyVisualRoot` 代理外形：

- `Build CombatTest Scene` 与 `Repair CombatTest Prefab Wiring` 的标准路径都会把三类敌人拉回 proxy baseline
- 如果当前场景里敌人再次提示 `no valid NavMesh`，先执行 `CampusRPG/Setup/Repair CombatTest Scene NavMesh`，把 `Ground` 和四面墙重新标成导航静态体并重烘当前 `CombatTest.unity`
- 如果你手动执行 `CampusRPG/Setup/Local Preview/Apply Imported Enemy Avatar Chain To CombatTest Enemy Prefabs`，会在本机给敌人单独挂一条 `Animator / Avatar / EnemyCombatAnimationRelay` 预览链，但它不属于正式默认输出
- 这条 local preview 会按 skinned mesh 的最低点自动补正 Y 偏移，避免敌人 imported 角色埋地
- `EnemyCombatAnimationRelay` 现在会在每次重新进入攻击 / 受击 / 死亡状态时强制从头重播对应 clip，避免 attack 看起来没触发
- 如果本机存在 `Assets/GhostSamurai_Animset/`，当前 enemy imported preview 控制器会优先改用 GhostSamurai 的 katana / Bow clip：近战 idle / walk / run 会优先走 `DefenseR_Loop`、`Strafe_Walk_F`、`Strafe_Run_F`，`Attack_AntiAir` 优先走 `Air_Attack03_Start`，`Attack_ChaseRoll` 优先走 `Slide_F`，`Attack_GuardBreak` 优先走 `SPAttack06`；远程 idle / walk / run / ranged attack 会优先走 `Bow_Idle`、`Bow_AimWalk_F`、`Bow_AimRun_F`、`Bow_Shoot_Start`。这条链仍然只服务本机读招研究，不改变正式章节默认基线。
- 这条 local preview 会生成 `Assets/_Game/Animations/Characters/CombatTest/LocalPreview/AC_Enemy_ImportedPreview.controller` 之类的本地资产；该目录只服务预览，不应提交
- 标准 `Repair` 会把 enemy root 上这条 Avatar 链拆掉，并重新启用 `EnemyVisualPresentationRelay`

当前占位资产里，`SO_Attack_Enemy_Ranged` 已默认挂到 `PF_Projectile_SpellBolt`，因此远程兵会走实体投射物链路，而不是直接在远距离瞬时结算伤害；该攻击当前会覆盖成最小弧线弹道，并且 AI 只会在存在 clear shot 时出手，没视线时会先侧移找角度；如果前摇期间失去视线，也会直接取消这次抬手并重新找角度；投射物本身也会被墙体等场景阻挡体拦截；命中时共用 `PF_VFX_ProjectileImpact_SpellBolt` 作为最小反馈，并播放经过 `SO_AudioSettings` 全局 SFX 音量的 one-shot 命中音效。

`LockOnTarget.targetTransform` 建议指向敌人胸口或头顶空物体，未填时默认使用自身 Transform。

## 6. 数据资产最小集合

在第一次进入 `CombatTest` 之前，至少创建这些 SO：

- `SO_PlayerBaseStats`
- `SO_AudioSettings`
- `SO_CombatBalance`
- `SO_Attack_Light_01`
- `SO_Attack_Light_02`
- `SO_Attack_Light_03`
- `SO_Attack_Heavy_01`
- `SO_Attack_DodgeFollowUp`
- `SO_Attack_Counter`
- `SO_Enemy_Melee`
- `SO_Enemy_Mobile`
- `SO_Enemy_Ranged`
- `SO_Skill_SpellBolt`
- `SO_Skill_ForceBurst`

如果不想手动创建，可以在 Unity 顶部菜单执行：

- `CampusRPG/Setup/Create CombatTest Placeholder Assets`

它会按当前项目结构生成一套可直接测试的占位资产。

第一版建议参数起点：

- 轻攻击总时长：`0.45 - 0.65s`
- 重攻击总时长：`0.7 - 0.95s`
- 闪避时长：`0.25s`
- 反击窗口：`0.8s`
- 闪避追击窗口：`0.8s`
- 近战兵攻击距离：`1.8 - 2.2`

## 7. 场景物理前置

首次测试前确认：

- `Ground` 具备 Collider
- 敌人所在区域已烘焙 NavMesh
- 玩家与敌人都具备碰撞体
- `EnemySensing.targetMask` 只指向玩家层

若未先整理 Layer，也可以临时让 `targetMask` 命中默认层，但正式开发前必须收口。

## 8. 第一轮通过标准

当 `CombatTest` 满足以下条件时，可进入下一轮实装：

1. 玩家可以移动和跳跃。
2. 玩家在待机、格挡、闪避、攻击之间会发生状态切换。
3. `Tab` 可以稳定锁定场上单个敌人，再按一次可解除。
4. `Q / E` 技能可以正确耗蓝、进入冷却并造成伤害。
5. 近战兵会直接压近，机动兵会侧移找角度，远程兵会在被贴脸时主动拉开距离。
6. 远程兵出手时会生成实体投射物，并只对合法目标生效。
7. 敌人攻击能扣玩家血，玩家攻击能扣敌人血。
8. `HealthComponent`、`GaugeComponent`、`Save` 的基础测试没有明显报错。

## 9. 当前已知空位

这份骨架目前仍有几处是“主干已打通，但仍是第一版原型”：

- 玩家攻击已接入可配置局部 Hitbox；当前默认 attack SO 使用 `AttackHitboxActivationMode.TimedWindow` 在运行时按 startup/active/recovery 驱动命中，相关攻击 clip 会刻意不放 Hitbox `AnimationEvent`，避免 TimedWindow 与动画事件重复结算。占位动画已提供最小读招和出手方向提示，但仍缺正式角色动画资源与更细的人工手调
- 如果项目里已导入兼容的 Humanoid 动作包，`CombatTest` 现在会优先使用真实近战动作来重建本地 clip；未导入时仍自动回退到占位动画
- 玩家与三类敌人当前使用的是低成本代理可视外形，不是正式模型资产；它们的职责是帮助判断朝向、距离与战斗空间，不替代最终美术资源
- 玩家格挡、破防与成功闪避已接状态、proxy/local-preview 动画入口、HUD/SFX 和镜头反馈，并有 ordinary Guard / Gate Slam Guard / Gate Slam Dodge 技术驱动可核对状态与数值；仍缺的是正式资源、逐镜头润色与真人手感签字，不能再概括成“没有动画和表现层反馈”
- 敌人当前已补出近战 / 机动 / 远程三类最小行为差异，远程兵已接最小投射物链路、弧线弹道、命中闪光、全局 SFX 音量和程序生成音效；Gatekeeper `Sky Hook` / `Pursuit Slam` / `Gate Slam` 也分别有 `AntiAirRead` / `ChaseRollRead` / `GuardBreakRead` 读招参数与 local-preview 起手 state，但仍缺更完整的资源化音频和专属 Boss 起手 clip
- 技能现在已接入最小施法执行，`SpellBolt` 已接最小投射物链路、命中闪光、全局 SFX 音量和程序生成音效，但仍缺动画事件、正式弹道表现和完整特效

这些空位是故意保留的，目的是先让主干可接，再逐步细化。
