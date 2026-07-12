# GhostSamurai 动画清单

> local research preview only。`Assets/GhostSamurai_Animset/` 只用于本机研究预览，不能作为公开仓库发布基线。

## 1. 总览

- 生成时间：`2026-04-29 07:49 UTC`
- 扫描目录：`Assets/GhostSamurai_Animset`
- FBX 总数：`1134`
- `katana`：`668`
- `Bow`：`457`
- 其它分支 / 模型 / 场景：`9`
- `Root`：`511`
- `Inplace`：`510`
- `Other / Pose / Sample / Unmarked`：`113`

## 2. 分类统计

| 分组 | 总数 | Root | Inplace | Other |
|---|---:|---:|---:|---:|
| APose / Attack | 103 | 51 | 51 | 1 |
| APose / Defense | 96 | 45 | 45 | 6 |
| APose / Deflect | 90 | 30 | 30 | 30 |
| APose / Dodge | 38 | 19 | 19 | 0 |
| APose / Movement | 75 | 36 | 38 | 1 |
| APose / Hit | 34 | 17 | 17 | 0 |
| APose / Die | 22 | 11 | 11 | 0 |
| APose / Execution | 69 | 22 | 22 | 25 |
| APose / Crouch | 33 | 16 | 16 | 1 |
| Common / Base | 56 | 27 | 28 | 1 |
| Common / CommonCrouch | 37 | 18 | 18 | 1 |
| Common / Unarm&Equip | 14 | 7 | 7 | 0 |
| Bow / Attack | 97 | 30 | 30 | 37 |
| Bow / Common | 59 | 30 | 29 | 0 |
| Bow / CommonCrouch | 36 | 18 | 18 | 0 |
| Bow / Crouch | 56 | 28 | 28 | 0 |
| Bow / Dodge | 34 | 17 | 17 | 0 |
| Bow / Hit | 35 | 18 | 17 | 0 |
| Bow / Die | 22 | 11 | 11 | 0 |
| Bow / Movement | 118 | 60 | 58 | 0 |

## 3. 最值得先试接的 Clip

这些条目不是“全部都接”，而是本地研究预览最值得优先验证的一线候选。

| 分类 | 先试 Clip |
|---|---|
| `Attack` | `GhostSamurai_APose_Attack01_1_ALL_Inplace`<br>`GhostSamurai_APose_Attack04_Inplace`<br>`GhostSamurai_APose_SPAttack02_Inplace`<br>`GhostSamurai_APose_Attack03_4_ALL_Inplace`<br>`GhostSamurai_APose_SPAttack03_Inplace`<br>`GhostSamurai_APose_SPAttack05_Inplace`<br>`GhostSamurai_APose_SPAttack06_Inplace`<br>`GhostSamurai_APose_JumpAttack04_Inplace` |
| `Defense` | `GhostSamurai_DefenseR_Loop_Inplace`<br>`GhostSamurai_DefenseL_Loop_Inplace`<br>`GhostSamurai_DefenseR_Broken_Inplace`<br>`GhostSamurai_DefenseL_Broken_Inplace`<br>`GhostSamurai_DefenseR_Parry_Up_Execution_Inplace` |
| `Deflect` | `GhostSamurai_LAttack_DeflectR_CounterExecution_Inplace`<br>`GhostSamurai_RAttack_DeflectL_CounterExecution_Inplace`<br>`GhostSamurai_RAttack_DeflectR90_Inplace`<br>`GhostSamurai_LAttack_DeflectL90_Inplace` |
| `Dodge` | `GhostSamurai_APose_Dodge_F_Inplace`<br>`GhostSamurai_APose_Slide_F_Inplace`<br>`GhostSamurai_APose_Avoid_F_1_Inplace`<br>`GhostSamurai_APose_Dodge_Attack_F_Inplace`<br>`GhostSamurai_APose_Dodge_Attack_B_Inplace` |
| `Movement` | `GhostSamurai_APose_Idle`<br>`GhostSamurai_APose_Strafe_Walk_F_Loop_Inplace`<br>`GhostSamurai_APose_Strafe_Run_F_Loop_Inplace`<br>`GhostSamurai_APose_Strafe_Run_FL_Inplace`<br>`GhostSamurai_APose_Strafe_Run_FR_Inplace`<br>`GhostSamurai_APose_Jump_Loop_Inplace`<br>`GhostSamurai_Common_Walk_Loop_Inplace` |
| `Hit` | `GhostSamurai_APose_Hit_F_Inplace`<br>`GhostSamurai_APose_Large_Hit_1_Inplace`<br>`GhostSamurai_APose_Large_Hit_2_Inplace`<br>`GhostSamurai_APose_Air_Hit_Inplace` |
| `Die` | `GhostSamurai_APose_Die01_Inplace`<br>`GhostSamurai_APose_Die03_Inplace`<br>`GhostSamurai_APose_Die05_Inplace` |
| `Execution` | `GhostSamurai_Execution01_Root`<br>`GhostSamurai_Execution06_Inplace`<br>`GhostSamurai_Ambush01_Root`<br>`GhostSamurai_Executed01_Inplace`<br>`GhostSamurai_Ambushed03_Inplace` |
| `Bow` | `GhostSamurai_Bow_Idle_Inplace`<br>`GhostSamurai_Bow_AimWalk_F_Inplace`<br>`GhostSamurai_Bow_Shoot_Start_Inplace`<br>`GhostSamurai_Bow_AirShoot_Start_Inplace`<br>`GhostSamurai_Bow_CrouchShoot_Start_Inplace`<br>`GhostSamurai_Bow_Dodge_F_Inplace`<br>`GhostSamurai_Bow_Large_Hit_Inplace` |

## 4. 动作映射覆盖

下表把当前 local-preview 设计里真正要用的 GhostSamurai clip 收成同源证据，避免“清单、设计、生成链”继续漂移。

### 玩家基础与移动

| 动作 | 分类 | 来源 | 首选 clip | 变体 | 备选 clip | 状态 | 目标 |
|---|---|---|---|---|---|---|---|
| `Idle` | `Movement` | APose / Base | `GhostSamurai_APose_Idle` | `Other` | - | `ready` | 稳定站姿与持刀重心 |
| `Walk_Forward` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Walk_F_Loop_Inplace` | `Inplace` | - | `ready` | 锁定前压姿态清楚 |
| `Walk_Backward` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Walk_B_Inplace` | `Inplace` | - | `ready` | 后撤不再滑冰 |
| `Walk_Left` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Walk_L_Inplace` | `Inplace` | - | `ready` | 侧向解招时保持朝向 |
| `Walk_Right` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Walk_R_Inplace` | `Inplace` | - | `ready` | 侧向解招时保持朝向 |
| `Run_Forward` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Run_F_Loop_Inplace` | `Inplace` | - | `ready` | 推进时仍保留持刀压迫 |
| `Run_Backward` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Run_B_Inplace` | `Inplace` | - | `ready` | 后撤跑不丢刀手姿态 |
| `Run_Left` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Run_L_Inplace` | `Inplace` | - | `ready` | 锁定横移读向更清楚 |
| `Run_Right` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Run_R_Inplace` | `Inplace` | - | `ready` | 锁定横移读向更清楚 |
| `Run_ForwardLeft` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Run_FL_Inplace` | `Inplace` | - | `ready` | 左前切线读招更清楚 |
| `Run_ForwardRight` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Run_FR_Inplace` | `Inplace` | - | `ready` | 右前切线读招更清楚 |
| `Run_BackwardLeft` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Run_BL_Inplace` | `Inplace` | - | `ready` | 左后退避轨迹更清楚 |
| `Run_BackwardRight` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Run_BR_Inplace` | `Inplace` | - | `ready` | 右后退避轨迹更清楚 |
| `Airborne` | `Movement` | APose / Movement | `GhostSamurai_APose_Jump_Loop_Inplace` | `Inplace` | - | `ready` | 空中停留与追击入口可见 |

### 玩家防御与受击

| 动作 | 分类 | 来源 | 首选 clip | 变体 | 备选 clip | 状态 | 目标 |
|---|---|---|---|---|---|---|---|
| `Block` | `Defense` | APose / Defense | `GhostSamurai_DefenseR_Loop_Inplace` | `Inplace` | `GhostSamurai_DefenseL_Loop_Inplace` | `ready` | 格挡架势一眼可读 |
| `Dodge` | `Dodge` | APose / Dodge | `GhostSamurai_APose_Dodge_F_Inplace` | `Inplace` | `GhostSamurai_APose_Avoid_F_Inplace` | `ready` | 短闪短而清楚 |
| `CombatRoll` | `Dodge` | APose / Dodge | `GhostSamurai_APose_Slide_F_Inplace` | `Inplace` | `GhostSamurai_APose_Slide_Start_Inplace` | `ready` | 长位移与高承诺分开 |
| `AirDodge` | `Dodge` | APose / Dodge | `GhostSamurai_APose_Avoid_F_1_Inplace` | `Inplace` | `GhostSamurai_APose_Avoid_B_1_Inplace` | `ready` | 空中规避与下砸入口拆开 |
| `Hit` | `Hit` | APose / Hit | `GhostSamurai_APose_Hit_F_Inplace` | `Inplace` | `GhostSamurai_APose_Large_Hit_1_Inplace` | `ready` | 普通受击短促不拖拍 |
| `GuardBreak` | `Defense` | APose / Defense | `GhostSamurai_DefenseR_Broken_Inplace` | `Inplace` | `GhostSamurai_DefenseL_Broken_Inplace`<br>`GhostSamurai_APose_Large_Hit_2_Inplace` | `ready` | 破防必须读出硬挡失败 |
| `Death` | `Die` | APose / Die | `GhostSamurai_APose_Die01_Inplace` | `Inplace` | `GhostSamurai_APose_Die03_Inplace` | `ready` | 死亡先选稳且不飘的版本 |

### 玩家攻击与反击

| 动作 | 分类 | 来源 | 首选 clip | 变体 | 备选 clip | 状态 | 目标 |
|---|---|---|---|---|---|---|---|
| `Light_01` | `Attack` | APose / Attack | `GhostSamurai_APose_Attack01_1_ALL_Inplace` | `Inplace` | `GhostSamurai_APose_Attack02_1_ALL_Inplace` | `ready` | 基础快速起手 |
| `Light_02` | `Attack` | APose / Attack | `GhostSamurai_APose_Attack04_Inplace` | `Inplace` | `GhostSamurai_APose_Attack02_5_ALL_Inplace`<br>`GhostSamurai_APose_Attack01_2_ALL_Inplace`<br>`GhostSamurai_APose_Attack02_2_Inplace` | `ready` | 二段改用跨族前切动作，先保证肉眼读感和第一刀分开 |
| `Light_03` | `Attack` | APose / Attack | `GhostSamurai_APose_SPAttack02_Inplace` | `Inplace` | `GhostSamurai_APose_Attack06_Inplace`<br>`GhostSamurai_APose_Attack01_3_ALL_Inplace`<br>`GhostSamurai_APose_Attack03_3_ALL_Inplace`<br>`GhostSamurai_APose_Attack03_3_Inplace` | `ready` | 三段改用特殊大收尾动作，优先解决三连击同质问题 |
| `Heavy_01` | `Attack` | APose / Attack | `GhostSamurai_APose_Attack03_4_ALL_Inplace` | `Inplace` | `GhostSamurai_APose_Attack06_Inplace` | `ready` | 重击保持大幅挥砍承诺 |
| `DodgeFollowUp` | `Dodge` | APose / Dodge | `GhostSamurai_APose_Dodge_Attack_F_Inplace` | `Inplace` | `GhostSamurai_APose_Attack02_1_ALL_Inplace` | `ready` | 成功闪避后的前切奖励 |
| `DodgeFollowUp_Enhanced` | `Dodge` | APose / Dodge | `GhostSamurai_APose_Dodge_Attack_B_Inplace` | `Inplace` | `GhostSamurai_APose_Attack02_2_ALL_Inplace` | `ready` | 绕身后的回身切 |
| `Counter` | `Deflect` | APose / Deflect | `GhostSamurai_LAttack_DeflectR_CounterExecution_Inplace` | `Inplace` | `GhostSamurai_DefenseR_Parry_Up_Execution_Inplace` | `ready` | 成功弹反后的短处决感 |
| `Counter_Enhanced` | `Deflect` | APose / Deflect | `GhostSamurai_RAttack_DeflectL_CounterExecution_Inplace` | `Inplace` | `GhostSamurai_APose_SPAttack06_Inplace` | `ready` | 高收益反击维持高承诺 |

### 玩家 SwordArt

| 动作 | 分类 | 来源 | 首选 clip | 变体 | 备选 clip | 状态 | 目标 |
|---|---|---|---|---|---|---|---|
| `Sidewind Cut` | `Attack` | APose / Dodge | `GhostSamurai_APose_Dodge_Attack_F_Inplace` | `Inplace` | `GhostSamurai_APose_Attack02_1_ALL_Inplace` | `ready` | 闪后横切，表达侧向收益 |
| `Cross Step` | `Attack` | APose / Attack | `GhostSamurai_APose_Attack02_4_ALL_Inplace` | `Inplace` | `GhostSamurai_APose_Attack02_4_Inplace` | `ready` | roll 后穿步斩，强调位移终点 |
| `Rising Cleave` | `Attack` | APose / Attack | `GhostSamurai_APose_Attack03_4_ALL_Inplace` | `Inplace` | `GhostSamurai_APose_Attack06_Inplace` | `ready` | 前推重斩，不变成无脑压制 |
| `Iron Gate Break` | `Defense` | APose / Defense | `GhostSamurai_DefenseR_Parry_Up_Execution_Inplace` | `Inplace` | `GhostSamurai_APose_SPAttack06_Inplace`<br>`GhostSamurai_APose_Attack03_4_ALL_Inplace` | `ready` | 防反破门招，先亮挡架再转攻 |
| `Falling Star` | `Attack` | APose / Attack | `GhostSamurai_APose_JumpAttack04_Inplace` | `Inplace` | `GhostSamurai_APose_Air_Attack03_Start_Inplace` | `ready` | 空中下砸并保留落地 recovery |
| `Moon Sever` | `Attack` | APose / Attack | `GhostSamurai_APose_SPAttack03_Inplace` | `Inplace` | `GhostSamurai_APose_SPAttack05_Inplace` | `ready` | 空中 dodge 后横向大斩 |

### 敌人与 Boss 读招预览

| 动作 | 分类 | 来源 | 首选 clip | 变体 | 备选 clip | 状态 | 目标 |
|---|---|---|---|---|---|---|---|
| `EnemyMelee_Idle` | `Defense` | APose / Defense | `GhostSamurai_DefenseR_Loop_Inplace` | `Inplace` | `GhostSamurai_APose_Idle` | `ready` | 近战敌人先站出持刀压迫 |
| `EnemyMelee_Walk` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Walk_F_Loop_Inplace` | `Inplace` | - | `ready` | 推进姿态不像默认 dummy 漫步 |
| `EnemyMelee_Run` | `Movement` | APose / Movement | `GhostSamurai_APose_Strafe_Run_F_Loop_Inplace` | `Inplace` | - | `ready` | 追击时仍保持持刀身体语言 |
| `Attack_Melee` | `Attack` | APose / Attack | `GhostSamurai_APose_Attack01_1_ALL_Inplace` | `Inplace` | `GhostSamurai_APose_Attack02_2_ALL_Inplace` | `ready` | 普通近战起手短且可挡 |
| `Attack_Mobile` | `Attack` | APose / Attack | `GhostSamurai_APose_Attack03_4_ALL_Inplace` | `Inplace` | `GhostSamurai_APose_Attack03_4_Inplace` | `ready` | 机动敌人保持更重前压 |
| `Attack_AntiAir` | `Attack` | APose / Attack | `GhostSamurai_APose_Air_Attack03_Start_Inplace` | `Inplace` | `GhostSamurai_APose_JumpAttack03_Inplace`<br>`GhostSamurai_APose_JumpAttack04_Inplace` | `ready` | Sky Hook 先读出抬刀反空 |
| `Read_AntiAir` | `Defense` | APose / Defense | `GhostSamurai_DefenseR_Parry_Up_Execution_Inplace` | `Inplace` | `GhostSamurai_RAttack_DeflectR90_Inplace`<br>`GhostSamurai_LAttack_DeflectL90_Inplace` | `ready` | 反空前先抬刀亮出上段读招，而不是直接跳到出手帧 |
| `Attack_ChaseRoll` | `Dodge` | APose / Dodge | `GhostSamurai_APose_Slide_F_Inplace` | `Inplace` | `GhostSamurai_APose_Attack03_4_ALL_Inplace` | `ready` | Pursuit Slam 先读成前压追上来 |
| `Read_ChaseRoll` | `Dodge` | APose / Movement | `GhostSamurai_APose_Slide_Start_Inplace` | `Inplace` | `GhostSamurai_APose_Dodge_Attack_F_Inplace`<br>`GhostSamurai_APose_Slide_F_Inplace` | `ready` | 追滚前先压低身体、亮出追身意图，而不是一进 attack 就砸下去 |
| `Attack_GuardBreak` | `Defense` | APose / Attack | `GhostSamurai_APose_SPAttack06_Inplace` | `Inplace` | `GhostSamurai_DefenseR_Parry_Up_Execution_Inplace`<br>`GhostSamurai_APose_Attack03_4_ALL_Inplace` | `ready` | Gate Slam 必须读成不能硬挡 |
| `Read_GuardBreak` | `Deflect` | APose / Deflect | `GhostSamurai_RAttack_DeflectL_CounterExecution_Inplace` | `Inplace` | `GhostSamurai_APose_SPAttack06_Inplace`<br>`GhostSamurai_DefenseR_Parry_Up_Execution_Inplace` | `ready` | 破防重招先给出大承诺蓄势，帮助玩家读出“不能硬挡” |
| `EnemyRanged_Idle` | `Bow` | Bow / Attack | `GhostSamurai_Bow_Idle_Inplace` | `Inplace` | `GhostSamurai_Bow_Common_Idle_Inplace` | `ready` | 弓兵静止先有拉弓架势 |
| `EnemyRanged_Walk` | `Bow` | Bow / Movement | `GhostSamurai_Bow_AimWalk_F_Inplace` | `Inplace` | `GhostSamurai_Bow_Common_StrafeWalkF_Inplace` | `ready` | 弓兵边走边压制 |
| `EnemyRanged_Run` | `Bow` | Bow / Movement | `GhostSamurai_Bow_AimRun_F_Inplace` | `Inplace` | `GhostSamurai_Bow_Common_StrafeRun_F_Inplace` | `ready` | 弓兵 reposition 时不退回近战姿态 |
| `Attack_Ranged` | `Bow` | Bow / Attack | `GhostSamurai_Bow_Shoot_Start_Inplace` | `Inplace` | `GhostSamurai_Bow_Shoot_Loop_Inplace` | `ready` | 远程起手先看见拉弓 |
| `Attack_Ranged_AntiAir` | `Bow` | Bow / Attack | `GhostSamurai_Bow_AirShoot_Start_Inplace` | `Inplace` | `GhostSamurai_Bow_AirShoot_Loop_Inplace` | `ready` | 空中目标有独立抬手逻辑 |
| `Read_Ranged_AntiAir` | `Bow` | Bow / Attack | `GhostSamurai_Bow_AirShoot_Start_Inplace` | `Inplace` | `GhostSamurai_Bow_AirShoot_Loop_Inplace` | `ready` | 弓兵打空中目标前先抬弓，而不是和地面射击完全同帧起手 |
| `Attack_Ranged_ChaseRoll` | `Bow` | Bow / Dodge | `GhostSamurai_Bow_Dodge_F_Inplace` | `Inplace` | `GhostSamurai_Bow_Shoot_SP01_Inplace` | `ready` | 弓兵也保留规避/追击读招 |
| `Read_Ranged_ChaseRoll` | `Bow` | Bow / Dodge | `GhostSamurai_Bow_Dodge_F_Inplace` | `Inplace` | `GhostSamurai_Bow_Shoot_SP01_Inplace` | `ready` | 弓兵追滚前先侧身压步，和普通射击 body language 分开 |
| `Attack_Ranged_GuardBreak` | `Bow` | Bow / Attack | `GhostSamurai_Bow_Shoot_SP04_Inplace` | `Inplace` | `GhostSamurai_Bow_Large_Hit_2_Inplace` | `ready` | 远程重招保留更重身体语言 |
| `Read_Ranged_GuardBreak` | `Bow` | Bow / Attack | `GhostSamurai_Bow_Shoot_SP04_Inplace` | `Inplace` | `GhostSamurai_Bow_Large_Hit_2_Inplace` | `ready` | 远程重招先亮出大动作蓄势，不再只靠 HUD 说明这是重压制 |

### 处决研究候选

| 动作 | 分类 | 来源 | 首选 clip | 变体 | 备选 clip | 状态 | 目标 |
|---|---|---|---|---|---|---|---|
| `Execution_Attacker` | `Execution` | APose / Execution | `GhostSamurai_Execution01_Root` | `Root` | `GhostSamurai_Execution06_Inplace` | `ready` | 攻击方终结研究：先看 Root 推进，再看 Inplace 定格 |
| `Executed_Victim` | `Execution` | APose / Execution | `GhostSamurai_Executed01_Inplace` | `Inplace` | `GhostSamurai_Executed05_Root` | `ready` | 受体终结研究：先看单体倒伏，再看双人配对位移 |
| `Ambush_Attacker` | `Execution` | APose / Execution | `GhostSamurai_Ambush01_Root` | `Root` | `GhostSamurai_Ambush03_Inplace` | `ready` | 伏击起手研究：区分扑进位移和原地定格 |
| `Ambushed_Victim` | `Execution` | APose / Execution | `GhostSamurai_Ambushed03_Inplace` | `Inplace` | `GhostSamurai_Ambushed02_Root` | `ready` | 伏击受体研究：验证被抓取后的失衡和收尾 |

## 5. 处决研究分层

把 `Execution / Executed / Ambush / Ambushed` 拆成攻击方与受体两侧，后续若真要做终结或背刺，本节就是挑 clip 的第一落点。

| 研究块 | 角色面 | 命名族 | 总数 | Root | Inplace | Other | 先试 clip | 优先用法 | 研究目标 |
|---|---|---|---:|---:|---:|---:|---|---|---|
| `Execution_Attacker` | 攻击方终结 | `ExecutionXX` | 21 | 7 | 7 | 7 | `GhostSamurai_Execution01_Root`<br>`GhostSamurai_Execution06_Inplace` | 先用 Root 看推进轨迹，再用 Inplace 看原地收势。 | 未来近身终结研究里的攻击方主体姿态。 |
| `Executed_Victim` | 受体被终结 | `ExecutedXX` | 21 | 7 | 7 | 7 | `GhostSamurai_Executed01_Inplace`<br>`GhostSamurai_Executed05_Root` | 先用 Inplace 看单体倒伏读感，再用 Root 看双人配对位移。 | 未来终结受体侧的失衡、跪倒和落地姿态。 |
| `Ambush_Attacker` | 伏击方起手 | `AmbushXX` | 12 | 4 | 4 | 4 | `GhostSamurai_Ambush01_Root`<br>`GhostSamurai_Ambush03_Inplace` | 先用 Root 看扑进和站位交换，再用 Inplace 看原地伏击定格。 | 未来背刺、处决起手或偷袭提示的攻击方姿态。 |
| `Ambushed_Victim` | 伏击受体 | `AmbushedXX` | 12 | 4 | 4 | 4 | `GhostSamurai_Ambushed03_Inplace`<br>`GhostSamurai_Ambushed02_Root` | 先用 Inplace 看受体塌陷和失守，再用 Root 看双人位移是否可靠。 | 未来伏击受体侧的被抓取、失衡和收尾姿态。 |

- `Other` 主要是同套 `Sample` 研究片段；它们先保留给双人配对观察，不作为首选接线候选。
- `Execution` 目录里还有 1 个未纳入上述四组的辅助 clip（当前是 `Enemy_Idle`），先不放进研究锚点。

## 6. 备注

- `Other` 主要是 pose、sample 或未显式带 `Root` / `Inplace` 后缀的 FBX。
- 若后续新增研究映射，先更新 `Docs/GhostSamurai_Action_Integration_Plan.md` 的设计表，再决定是否把对应 clip 接进 local preview 生成链。
