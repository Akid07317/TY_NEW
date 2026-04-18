# 核心脚本架构与骨架

本文件定义第一版建议的脚本模块拆分、核心类职责和推荐实现顺序。目标不是一次性把所有代码写死，而是让后续开发按统一骨架推进，不在中途失控。

## 1. 架构原则

- 逻辑层与表现层解耦
- 状态机负责行为切换，Animator 负责播放
- 数据配置进入 `ScriptableObject`
- 核心系统必须能在测试场景独立运行
- 组件职责要小而清晰

## 2. 推荐命名空间

```text
CampusRPG.Core
CampusRPG.Input
CampusRPG.Camera
CampusRPG.Character
CampusRPG.Combat
CampusRPG.Composition
CampusRPG.Skills
CampusRPG.AI
CampusRPG.Interaction
CampusRPG.Save
CampusRPG.UI
```

## 3. 主模块骨架

### 3.1 Core

| 类名 | 职责 |
|---|---|
| `GameBootstrap` | 初始化输入、服务、初始场景流 |
| `GameContext` | 提供全局可访问的运行时引用 |
| `SceneRuntimeContext` | 聚合当前场景的 bootstrap、玩家、镜头、章节进度与回档服务引用 |
| `SceneRuntimeReferenceUtility` | 统一 bootstrap/input/player/camera/save/audio 的运行时回退解析 |
| `UpdateClock` | 可选的统一时序封装 |
| `DebugCommandService` | 调试入口与开发命令分发 |

### 3.2 Input

| 类名 | 职责 |
|---|---|
| `InputReader` | 封装 Input System 输入读取 |
| `PlayerInputRouter` | 将输入语义分发给角色系统 |
| `InputActionName` | 输入动作名常量或映射 |

建议：输入读取层不要直接知道具体角色状态机，避免耦合。

### 3.3 Camera

| 类名 | 职责 |
|---|---|
| `ThirdPersonCameraController` | 管理自由镜头与锁定镜头参数 |
| `ThirdPersonCameraOrbitUtility` | 统一第三人称镜头自由视角、锁定视角与跟随位姿计算 |
| `LockOnTargetSelector` | 搜索、筛选、切换锁定目标 |
| `LockOnTargetSearchUtility` | 统一锁定目标候选解析、合法性校验与评分搜索 |
| `CameraObstacleResolver` | 简化版遮挡修正 |

### 3.4 Character

| 类名 | 职责 |
|---|---|
| `PlayerCharacter` | 玩家主入口，聚合主要组件引用 |
| `PlayerMotor` | 地面移动、朝向、跳跃、重力 |
| `PlayerCombatController` | 轻重攻击、追击、反击与量表窗口管理 |
| `PlayerCombatRuntimeUtility` | 统一连段推进、反击/追击派生决策与窗口计时辅助 |
| `PlayerStateMachine` | 玩家 FSM 宿主 |
| `PlayerLocomotionState` | 待机、移动、转向 |
| `PlayerAttackState` | 轻重攻击、派生、输入缓存 |
| `PlayerBlockState` | 格挡与反击窗口 |
| `PlayerDodgeState` | 闪避与追击窗口 |
| `PlayerJumpState` | 起跳与落地 |
| `PlayerSkillState` | 技能施法 |
| `PlayerHitState` | 受击硬直 |
| `PlayerDeathState` | 死亡与恢复交接 |

### 3.5 Combat

| 类名 | 职责 |
|---|---|
| `HealthComponent` | HP 读写、死亡事件 |
| `ManaComponent` | MP 管理 |
| `GaugeComponent` | CounterGauge 与 AgilityGauge 管理 |
| `DamageableReceiver` | 统一受击入口，处理格挡、成功闪避和命中后的状态反馈 |
| `DamageableReactionUtility` | 统一受击前防御结果解析与命中后仇恨/硬直反馈规划 |
| `AttackExecutor` | 负责攻击启动、局部 Hitbox 命中与伤害投递，兼容旧范围判定 |
| `AttackHitboxExecutionUtility` | 统一攻击命中体配置解析、legacy 回退与可受击目标过滤 |
| `HitboxController` | 统一管理攻击判定窗口，并为动画事件提供激活入口 |
| `ProjectileController` | 投射物飞行、命中与销毁 |
| `ProjectileFlightUtility` | 统一投射物发射参数归一化与单帧轨迹步进计算 |
| `ProjectileImpactFeedbackUtility` | 统一投射物命中后的特效、音效与运行时销毁反馈 |
| `CombatResolver` | 统一伤害、硬直、击退结算 |
| `AttackContext` | 单次攻击运行时数据 |

### 3.6 Skills

| 类名 | 职责 |
|---|---|
| `SkillController` | 技能释放、冷却、资源校验 |
| `SkillCastUtility` | 统一技能伤害、朝向、落点与投射物发射计划 |
| `SkillRuntime` | 技能实例执行逻辑 |
| `SkillCaster` | 处理技能朝向、目标与释放时机 |
| `SkillDefinitionSO` | 技能配置资产 |

### 3.7 AI

| 类名 | 职责 |
|---|---|
| `EnemyBrain` | 敌人总控，聚合感知、移动、战斗 |
| `EnemyStateMachine` | 普通敌人 FSM 宿主 |
| `EnemySensing` | 索敌、距离、视野 |
| `EnemyMotor` | NavMeshAgent 的包装与速度控制 |
| `EnemyAttackController` | 执行近战或远程攻击 |
| `EnemyAttackExecutionUtility` | 统一攻击伤害、目标有效性校验与投射物发射计划 |
| `EnemyIdleGuardState` | 站岗或巡逻 |
| `EnemyAlertState` | 发现玩家后的准备 |
| `EnemyChaseState` | 追击 |
| `EnemyStrafeState` | 机动兵的横移与短闪避 |
| `EnemyMeleeAttackState` | 近战出手 |
| `EnemyRangedAttackState` | 远程出手 |
| `EnemyHitState` | 受击 |
| `EnemyDeathState` | 死亡与掉落 |
| `BossBrain` | Boss 特化总控 |
| `BossStateMachine` | Boss FSM |
| `BossActionSelector` | 招式选择与冷却 |

### 3.8 Interaction

| 类名 | 职责 |
|---|---|
| `InteractionDetector` | 检测可交互对象 |
| `InteractableBase` | 可交互对象基类 |
| `CheckpointInteractable` | 检查点激活 |
| `PickupInteractable` | 拾取物交互或接近拾取 |
| `DoorController` | 锁门、解锁、区域切换 |
| `DoorRequirementHintTrigger` | 玩家碰到未满足条件的门时抛出路线阻塞提示请求 |
| `EncounterController` | 遭遇战激活、清场判定与回档恢复 |
| `EncounterControllerUtility` | 统一遭遇战清场进度判断、成员全灭判定与刷新分支规划 |
| `EnemyEncounterMember` | 单个遭遇战敌人的绑定、击败回调与重置 |
| `TriggerVolume` | 教学、区域推进、Boss 触发 |

### 3.9 Save

| 类名 | 职责 |
|---|---|
| `SaveService` | 存取档总入口 |
| `CheckpointService` | 激活检查点与恢复 |
| `CheckpointRestoreCoordinator` | 编排检查点激活、死亡回档与自动存档 |
| `CheckpointRestoreCoordinatorUtility` | 统一检查点激活决策、恢复快照构建与当前章节存档拼装 |
| `CheckpointRestoreExecutor` | 执行检查点恢复顺序，串起检查点状态、章节进度、玩家与场景参与者恢复 |
| `CheckpointRuntimeRegistry` | 管理运行时检查点索引与查找 |
| `CheckpointRestorePlanner` | 计算恢复点、血蓝恢复值与存档基础数据 |
| `CheckpointRestoreSceneResetter` | 管理回档参与者注册并执行交互物、遭遇战、敌人的回档重置 |
| `ChapterProgressService` | 管理章节关键进度 |
| `ChapterProgressPersistence` | 统一章节进度的存档快照与恢复归一化 |
| `ChapterProgressStateUtility` | 统一章节进度的运行时状态变更、需求判断与快照回填 |
| `EncounterStateService` | 记录遭遇战是否被清理 |
| `ChapterSaveData` | 存档 DTO |
| `CheckpointRestoreSnapshot` | 检查点恢复快照 |
| `ChapterProgressSnapshot` | 章节进度快照 |

### 3.10 UI

| 类名 | 职责 |
|---|---|
| `HudPresenter` | 玩家 HUD 数据刷新 |
| `BossBarPresenter` | Boss 血条显示 |
| `BossAttackCuePresenter` | Boss 招式预警条与提示文案 |
| `BossAttackCuePlanner` | 计算 Boss 招式预警文案、颜色与可见时长 |
| `BossAttackPreviewUtility` | 统一 Boss 当前攻击预览入口，供多个展示 planner 复用 |
| `BossGroundTelegraphPresenter` | Boss 地面预警圈/直线预警带展示驱动 |
| `BossGroundTelegraphPlanner` | 计算 Boss 地面预警的形状、半径、长度、朝向与位置 |
| `BossImpactMarkerPresenter` | Boss 攻击落点与弹道冲击点提示 |
| `BossImpactMarkerPlanner` | 计算 Boss 落点标记的形状、寿命、朝向与位置 |
| `BossSpawnFlarePresenter` | Boss 登场地面光柱与开场提醒 |
| `BossSpawnFlarePlanner` | 计算 Boss 登场 flare 的持续时间、位置与缩放 |
| `BossArenaStatusPresenter` | Boss 战开场/清场状态提示，带短淡入淡出，并在章节完成时自动收掉 |
| `BossCombatHintView` | Boss 战开场时的解法提示 |
| `BossCombatHintPlanner` | 计算 Boss 开场战术提示文案 |
| `BossThreatPulsePresenter` | Boss 遭遇开始与出招时的屏幕脉冲压迫感 |
| `BossThreatPulsePlanner` | 计算 Boss 屏幕脉冲的颜色、持续时间与透明度曲线参数 |
| `BossPresentationRules` | Boss 展示层共用的激活判定与朝向解析规则 |
| `BossTelegraphVisualUtility` | Boss 世界预警类 presenter 共用的视觉实例与运行时材质管理 |
| `BossPresentationRig` | 一次性挂接 Boss 展示相关 presenter |
| `LockOnMarkerView` | 锁定目标指示器 |
| `InteractionPromptView` | 交互提示 |
| `AreaEntryView` | 区域进入时的一次性到达提示 |
| `AreaEntryPlanner` | 计算区域到达提示的标题与文案 |
| `CheckpointActivationView` | 检查点激活提示 |
| `CheckpointActivationPlanner` | 计算检查点提示文案 |
| `KeyItemAcquisitionView` | 关键物品获取提示 |
| `KeyItemAcquisitionPlanner` | 计算关键物品提示文案 |
| `KeyItemBeaconView` | 关键物品的世界空间引导标记与首次显现闪光 |
| `KeyItemRevealPulsePlanner` | 计算关键物品首次显现时的一次性地面扩散闪光 |
| `EncounterSealView` | 非 Boss 遭遇战开始时的封锁提示 |
| `EncounterSealPlanner` | 计算遭遇战开战提示文案 |
| `EncounterClearView` | 非 Boss 遭遇战清场提示 |
| `EncounterClearPlanner` | 计算遭遇战清场提示文案 |
| `ChapterRouteBlockHintView` | 路线被门禁拦住时的短时提示 |
| `ChapterRouteBlockHintPlanner` | 计算章节门禁阻塞提示文案 |
| `DebugPanelView` | 调试菜单 |
| `ChapterObjectiveView` | 当前章节目标与路线提示 |
| `ChapterObjectivePlanner` | 计算章节目标文案与区域标题 |
| `ChapterTutorialHintView` | 入口教学提示与输入里程碑跟踪 |
| `ChapterTutorialHintPlanner` | 计算入口教学提示的阶段文案 |
| `ChapterCompleteView` | 章节完成界面、短延迟出场、短淡入与轻背景压暗 |
| `ChapterCompletePlanner` | 计算章节完成总结卡的结果、奖励与存档文案 |

## 4. 核心接口建议

| 接口 | 作用 |
|---|---|
| `IDamageable` | 统一伤害入口 |
| `IBlockResponder` | 响应格挡成功 |
| `IDodgeResponder` | 响应成功闪避 |
| `IInteractable` | 统一交互接口 |
| `ILockOnTarget` | 提供锁定点与锁定有效性 |
| `ICheckpointRestoreParticipant` | 统一回档参与者注册与重置入口 |
| `ISaveParticipant` | 参与存档写入和恢复 |

## 5. 建议的数据结构

| 数据结构 | 用途 |
|---|---|
| `AttackRuntimeData` | 单次攻击运行时参数 |
| `DamageInfo` | 伤害类型、倍率、硬直、击退 |
| `GaugeChangeInfo` | 量表变化来源与增量 |
| `CheckpointRestoreSnapshot` | 检查点恢复快照 |
| `EncounterRuntimeState` | 遭遇战当前状态 |

## 6. 事件流建议

### 玩家攻击命中流程

1. `PlayerAttackState` 触发攻击。
2. `AttackExecutor` 创建本次攻击上下文。
3. 动画事件通知 `HitboxController` 开启。
4. `DamageableReceiver` 接收命中。
5. `CombatResolver` 计算伤害、硬直、击退。
6. 命中结果回传给 `HudPresenter`、特效与音效。

### 格挡成功流程

1. 玩家处于 `PlayerBlockState`。
2. 来袭攻击进入受击判断。
3. 若满足格挡条件，则取消伤害或降低伤害。
4. `GaugeComponent` 增加 `CounterGauge`。
5. 状态机打开 `CounterAttack` 输入窗口。

### 闪避成功流程

1. 玩家进入 `PlayerDodgeState`。
2. 攻击穿过玩家无敌帧窗口。
3. `GaugeComponent` 增加 `AgilityGauge`。
4. 状态机打开 `DodgeFollowUp` 输入窗口。

## 7. 推荐脚本创建顺序

严格建议按下面顺序实装：

1. `InputReader`
2. `PlayerMotor`
3. `PlayerStateMachine`
4. `HealthComponent` / `ManaComponent` / `GaugeComponent`
5. `AttackExecutor` / `HitboxController`
6. `SkillController`
7. `EnemyBrain` / `EnemyStateMachine`
8. `LockOnTargetSelector`
9. `CheckpointService` / `SaveService`
10. `HudPresenter` / `BossBarPresenter`

原因：这样可以先打通战斗核心，再补章节外围系统。

## 8. 第一版必须避免的架构陷阱

- 不要过早引入行为树
- 不要把所有业务逻辑塞进 Animator StateMachineBehaviour
- 不要把所有全局引用放进单一 God Object
- 不要在第一版设计复杂事件总线
- 不要为了“未来扩展”过度抽象

## 9. 本文档的使用方式

后续若开始实装代码，可直接把本文件当成脚本待办清单：

- 新系统先确认属于哪个模块
- 新类先确认是否已有职责重复
- 新数据先确认是否应该进入 SO
- 新场景逻辑先确认是否可由 `TriggerVolume + Service` 组合解决

只要持续遵守这套骨架，第一版就能在可控范围内逐步扩展，而不会在第三周开始出现结构性失控。
