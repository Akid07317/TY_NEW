# CombatTest 接线清单

本清单用于尽快把 `Assets/_Game/Scenes/CombatTest.unity` 接成一个可验证“玩家 vs 三类敌人”的小型战斗沙盒。目标不是漂亮，而是能尽快验证输入、移动、攻击状态、量表，以及近战 / 机动 / 远程三条敌人行为链路。

## 0. 一键搭建入口

当前项目已经提供一键生成入口：

- `CampusRPG/Setup/Build CombatTest Scene`
- `CampusRPG/Setup/Build CombatTest Scene (Force Rebuild)`
- `CampusRPG/Setup/Repair CombatTest Prefab Wiring`

它会自动：

- 生成或更新 `CombatTest` 所需的占位数据资产
- 生成 `PF_Player_CombatTest`、`PF_Enemy_Melee_CombatTest`、`PF_Enemy_Mobile_CombatTest`、`PF_Enemy_Ranged_CombatTest`
- 为玩家和三类敌人生成一套低成本代理可视外形与材质，便于在正式模型缺席时先判断空间感、朝向和攻击压迫感
- 生成 `SO_AudioSettings`
- 生成 `PF_Projectile_SpellBolt`
- 生成 `PF_VFX_ProjectileImpact_SpellBolt`
- 生成 `AC_Player_CombatTest`、玩家攻击片段，以及基础动作层片段
  当前玩家控制器除了攻击状态，还会生成 `Locomotion / Block / Airborne / Dodge / Hit / Death` 基础状态
  当项目里存在兼容的 Humanoid 动作资源时，这些本地片段会优先复制导入动作；缺失时才回退到占位动作
  玩家攻击片段现在会保留一小段导入动作的收招尾巴，而不是只按命中窗口长度硬裁；运行时也会把 `forwardMovement` 用到玩家攻击前送上
  因此现在已经可以直接评估移动、格挡、闪避、受击和死亡的整体手感，而不只是静态壳子加命中事件
- 当项目里已经导入兼容的 Humanoid 角色与动作资源时，会优先把玩家本地 `CombatTest` 动画片段重建成真实动作副本，并尝试把玩家 prefab 切到导入的人物外观
- 重建 `Assets/_Game/Scenes/CombatTest.unity`
- 在场景内放入 `CombatDebugHUD`

如果你只是想快速进入战斗测试，优先使用这个菜单，而不是按下面清单逐项手工创建。

如果你已经有现成的 `CombatTest` 角色 prefab，不想整包重建场景，只想把 `RequireComponent` 造成的重复组件清掉，并把玩家的 `PlayerCombatAnimationRelay` 接回 prefab，请使用 `Repair CombatTest Prefab Wiring`。它会就地修复 `PF_Player_CombatTest` 和三类敌人 prefab 的内部组件引用，不会重建整个场景。
当前修复流程也会把玩家的 `Animator`、`PlayerCharacter`、`PlayerStateMachine`、`PlayerMotor` 和 `PlayerCombatAnimationRelay` 的新动作层引用重新接齐。

当前默认入口在检测到以下目标已存在时，会先弹确认框，再执行覆盖：

- `Assets/_Game/Scenes/CombatTest.unity`
- `Assets/_Game/Prefabs/Characters/PF_Player_CombatTest.prefab`
- `Assets/_Game/Prefabs/Characters/PF_Enemy_Melee_CombatTest.prefab`
- `Assets/_Game/Prefabs/Characters/PF_Enemy_Mobile_CombatTest.prefab`
- `Assets/_Game/Prefabs/Characters/PF_Enemy_Ranged_CombatTest.prefab`

如果你已经在这些文件里做了手调，请先复制备份，再执行重建。`Force Rebuild` 入口保留给明确知道自己要覆盖的人，批处理和自动化仍会直接重建。

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

当前自动生成的 `PF_Player_CombatTest` 还会额外挂一套 `CombatProxyVisualRoot` 代理外形：

- 它不是正式角色模型，而是为了在缺少 `fbx/glb` 等角色资源时，先看清角色前向、胸口朝向和攻击距离
- 仓库默认基线会固定使用这套代理外形和本地代理动作，避免把 Unity Asset Store 第三方资源误写进公开仓库
- 如果你本地想临时用导入模型评估手感，可以手动执行 `CampusRPG/Setup/Apply Imported Player Visuals To CombatTest Player Prefab (Local Preview)`，并在需要导出仓库基线前重新执行 `Repair CombatTest Prefab Wiring`
- 如果你本地想临时用第三方动作源重建 `AN_Player_*`，先勾上 `CampusRPG/Setup/CombatTest/Use Imported Player Sources For Local Preview`；这个开关只用于本地预览，重跑生成后不要把结果提交到公开仓库
- 如果后续给 prefab 接入你自己的正式角色模型，只要保留子物体 `Renderer`，重跑修复脚本时也会自动跳过这套代理外形，不会强行覆盖正式模型

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

- 玩家攻击已接入可配置局部 Hitbox，并默认通过占位 `AnimatorController + AnimationEvent` 驱动；当前占位动画已经补上最小读招和出手方向提示，但仍缺正式角色动画资源和更细的手调
- 如果项目里已导入兼容的 Humanoid 动作包，`CombatTest` 现在会优先使用真实近战动作来重建本地 clip；未导入时仍自动回退到占位动画
- 玩家与三类敌人当前使用的是低成本代理可视外形，不是正式模型资产；它们的职责是帮助判断朝向、距离与战斗空间，不替代最终美术资源
- 玩家格挡与成功闪避已有统一受击入口，但仍缺动画和表现层反馈
- 敌人当前已补出近战 / 机动 / 远程三类最小行为差异，远程兵已接最小投射物链路、弧线弹道、命中闪光、全局 SFX 音量和程序生成音效，但仍缺更完整的资源化音频和命中特效
- 技能现在已接入最小施法执行，`SpellBolt` 已接最小投射物链路、命中闪光、全局 SFX 音量和程序生成音效，但仍缺动画事件、正式弹道表现和完整特效

这些空位是故意保留的，目的是先让主干可接，再逐步细化。
