# 项目总文档

## 1. 项目概述

| 项目项 | 本项目执行方案 |
|---|---|
| 产品定位 | `Unity 3D 第三人称动作 RPG`，第一版只做校园主题的第一章，主打战斗手感和短流程高密度体验 |
| 目标体验 | `读招明确`、`防反与闪避都有价值`、`章节完整可通关`、`低成本可扩展` |
| 目标时长 | 理想时长 `15-20 分钟`，必要时压缩为 `10-15 分钟` |
| 技术基线 | `Unity 6000.4.2f1`、`C#`、`Input System`、`Cinemachine`、`Animator`、`CharacterController`、`NavMeshAgent`、`ScriptableObject`、`Built-in Render Pipeline` |
| 发布身份 | 产品名 `TY_NEW`，版本 `0.1.0`，Standalone 包名 `com.don.tynew` |
| 美术方向 | `低多边形`、`偏像素`、`中立风`，优先现成素材，强调统一而非精细度 |
| 开发模式 | 独立开发、少插件、代码模块化、注释清晰、调试优先 |
| 第一版必须达成 | 玩家核心战斗闭环、完整第一章、3 种普通敌人、1 个 Boss、检查点、自动存档、Win/Mac 构建 |
| 第一版明确不做 | 联机、开放世界、装备与背包、商店、多职业、复杂任务树、复杂成长树、程序生成地图 |

项目成败判断标准不是系统数量，而是“玩家能否顺畅地从章节开始打到 Boss 结束，并且战斗过程有明确节奏和反馈”。

## 2. 第一章玩法目标与章节流程

主目标：穿过封锁校区，击败守门者，取得“术式核心”并完成章节。

| 区域 | 目标时长 | 功能定位 | 第一版必须做 | 以后可扩展 |
|---|---:|---|---|---|
| 入口教学区 | 3-4 分钟 | 建立输入与战斗基础 | 移动、相机、锁定、轻重攻击、格挡、闪避、交互、首场教学战斗 | 可选对话、世界观提示 |
| 教学楼外战斗区 | 4-6 分钟 | 建立战斗节奏 | 开阔区域混战、第一次敌种混编、掉落回复物与强化材料 | 额外支线战斗、可选宝箱 |
| 校舍内部推进区 | 4-5 分钟 | 强化推进与资源管理 | 房间式推进、锁门清怪、关键物品“门禁印记”、Boss 前整备点 | 轻机关、隐藏房间 |
| Boss 区 | 3-5 分钟 | 完成章节考核 | Boss 单阶段战斗、胜利掉落“术式核心”、章节完成结算 | 开场演出、追加奖励 |

建议流程：

1. 新游戏进入入口教学区。
2. 完成基础战斗教学并激活 `CP01`。
3. 进入教学楼外战斗区，完成混编遭遇战并激活 `CP02`。
4. 进入校舍内部推进区，获取“门禁印记”，前往 Boss 门前并激活 `CP03`。
5. 击败校园守门者，取得“术式核心”，播放章节完成界面并自动存档。

## 3. 玩家系统设计总览

| 系统 | 第一版必须做 | 以后可扩展 |
|---|---|---|
| 移动 | `WASD` 地面移动；自由镜头下朝输入方向转身，锁定时保持面敌并使用前后左右步态；走跑一体，不做体力条 | 冲刺、更复杂的攀爬、特殊移动 |
| 镜头 | 自由镜头与锁定镜头两种模式 | 动态 FOV、战斗特写 |
| 锁定 | 锁定最近敌人，敌人死亡或超距自动解锁 | 左右切换锁定目标 |
| 轻攻击 | 地面 `3 连`，每段只在尾段输入窗口内排下一段，命中带轻微硬直 | 空中连段、更多派生 |
| 重攻击 | 地面 `1 段终结`，高硬直高收益 | 蓄力重击、破防重击 |
| 闪避 | 定向闪避；短启动后进入无敌帧；锁定且无输入时默认为后撤步，成功穿招后累积 `AgilityGauge` | 完美闪避、空中闪避 |
| 闪避追击 | 成功闪避后短窗口内按轻攻击触发 | 追击分支、空中追击 |
| 格挡 | 按住格挡，进入姿态后有短启动窗口；启动完成后的成功格挡累积 `CounterGauge`；少数慢重击会造成 block stun 或破防，要求玩家改用闪避 | 完美格挡、破防对抗 |
| 格挡反击 | 成功格挡后短窗口内按重攻击触发 | 连锁反击、武器差异 |
| 强力反击 | `CounterGauge` 满后可释放一次强化反击 | 量表转化、更多终结技 |
| 强化追击 | `AgilityGauge` 满后可释放一次强化闪避追击 | 持续 Buff、技能联动 |
| 跳跃 | 基础跳跃与落地；允许借低矮掩体做可控 mantle，不作为主战斗轴心 | 空中重击、平台战 |
| 技能 | `2 个法系技能`，消耗 MP 并带冷却 | 技能升级与替换 |
| 交互 | 检查点、门、拾取物、机关、章节结算物 | NPC、分支交互 |

推荐第一版默认键位：

- `WASD`：移动
- `Mouse Delta`：视角
- `LMB`：轻攻击
- `RMB`：重攻击
- `Left Ctrl`：格挡
- `Left Shift`：闪避
- `Space`：跳跃
- `Q`：技能 1
- `E`：技能 2
- `Tab`：锁定
- `F`：交互

## 4. 敌人与 Boss 设计总览

| 单位 | 职责 | 第一版必须做 | 玩家对应解法 | 以后可扩展 |
|---|---|---|---|---|
| 近战兵 | 稳定前压与基础考核 | 巡逻、索敌、追击、近战二连、受击硬直、死亡掉落 | 格挡后反击最稳 | 突进、连段派生 |
| 机动兵 | 干扰玩家节奏 | 侧移、短闪避、快速单段出手 | 闪避追击、预判截击 | 假动作、后撤反打 |
| 远程兵 | 中距离压制与站位惩罚 | 投射物攻击、贴身后后撤、低血量 | 快速贴近、技能打断 | 蓄力射击、范围法术 |
| 校园守门者 | 章节终局考核 | 单阶段 Boss，清晰前摇，近中距离混合招式，普通招可格挡，慢重击破防并要求闪避 | 读前摇、防反与闪避追击轮替 | 第二形态、剧情演出 |

Boss V1 发布底线固定 5 招，避免第一章收尾时继续膨胀：

1. 横扫二连
2. 直线突刺
3. 下砸重击（破防，主解法是闪避）
4. 单发术式投射
5. 地面冲击波

V2 进攻线允许在不改变单阶段 Boss 边界的前提下补“解法回应槽”：当前 Gatekeeper 已追加 `Sky Hook` 反空与 `Pursuit Slam` 追滚，用来回应 `AirDodge` / 空中 SwordArt 和 `CombatRoll`；两招已有专属 cue 颜色/文案、前压 lane、轻量 camera impulse 和程序生成 SFX，但它们仍服务读招清晰，不进入多阶段 Boss。

Boss 设计核心不是复杂，而是让玩家把“格挡反击”和“闪避追击”都用出来。

## 5. 数值与资源系统简表

| 项目 | 建议值 | 说明 |
|---|---:|---|
| 玩家 HP | `100` | 死亡后从最近检查点恢复 |
| 玩家 MP | `100` | 技能施法资源 |
| Attack | `20` | 作为基础伤害系数 |
| Defense | `10` | 简单线性减伤即可 |
| CounterGauge | `0-100` | 有效格挡成功后累积，满值触发强化反击 |
| AgilityGauge | `0-100` | 成功闪避累积，满值触发强化追击 |
| 轻攻击倍率 | `1.0 / 1.1 / 1.4` | 第三段作为小终结 |
| 重攻击倍率 | `1.8` | 高收招风险 |
| 格挡反击倍率 | `1.5` | 成功格挡后的即时收益 |
| 强力反击倍率 | `2.5` | CounterGauge 满后替换普通反击 |
| 闪避追击倍率 | `1.4` | 成功闪避后的收益动作 |
| 强化追击倍率 | `2.0` | AgilityGauge 满后替换普通追击 |
| 技能 1 | `20 MP / 6 秒 CD` | 快速术式弹，打断远程兵 |
| 技能 2 | `35 MP / 12 秒 CD` | 近身冲击结界，解围与压制 |

掉落只做三类：

- `即时回复道具`
- `技能强化材料`
- `章节关键物品`

第一版不做等级、装备、背包和全局成长树，只做章节内固定成长节点。

## 6. 目录结构建议

```text
/Docs
  Project_Master_Document.md
  Development_Blueprint_V1.md
  Unity_Project_Setup_Checklist.md
  Core_Script_Architecture.md
/Assets/_Game
  /Art
  /Audio
  /Animations
  /Materials
  /VFX
  /UI
  /Data
    /Characters
    /Combat
    /Skills
    /Enemies
    /Drops
    /Chapter
  /Prefabs
    /Characters
    /Combat
    /Gameplay
    /Environment
    /UI
  /Scenes
  /Scripts
    /Runtime
      /Core
      /Input
      /Camera
      /Character
      /Combat
      /Skills
      /AI
      /Interaction
      /Save
      /UI
    /Editor
    /Tests
      /EditMode
      /PlayMode
```

第一版必须坚持“正式内容全部进入 `Assets/_Game`”，避免继续沿用模板散放结构。

## 7. C# 脚本命名规范

| 类型 | 命名规则 | 示例 |
|---|---|---|
| 命名空间 | `项目名.模块.子模块` | `CampusRPG.Combat.Player` |
| MonoBehaviour | `对象 + 职责` | `PlayerCombatController` |
| 状态机 | `对象 + StateMachine` | `EnemyStateMachine` |
| 状态类 | `对象 + 行为 + State` | `PlayerDodgeState` |
| ScriptableObject | 统一 `SO` 后缀 | `EnemyArchetypeSO` |
| 接口 | `I` 前缀 | `IDamageable` |
| 枚举 | 业务化名词，不缩写 | `CheckpointRestoreMode` |
| 存档数据类 | `Data` 后缀 | `ChapterSaveData` |
| 测试类 | `目标 + Tests` | `SaveServiceTests` |

补充规则：

- 一个公开类对应一个文件。
- 文件名必须与类名一致。
- 避免使用模糊名称，如 `GameManager`、`DataManager`。
- 公开字段尽量少，优先 `[SerializeField] private`。
- 注释只解释设计意图、边界条件和时序，不写流水账。

## 8. 核心状态机设计

### 玩家状态机

| 状态 | 职责 | 主要转移 |
|---|---|---|
| `LocomotionState` | 地面移动、待机、转向、锁定步态 | 可转攻击、格挡、闪避、跳跃、技能、交互 |
| `AttackState` | 轻连段、重击、追击、反击执行；轻击只在尾段 buffer 窗口内排下一段 | 连段继续留在本状态，结束回 Locomotion |
| `BlockState` | 持续格挡；进入后先过短启动窗口，之后才进入有效防御判定；格挡硬直期间延迟反击和松手退出 | 成功格挡后打开反击窗口，松开、硬直结束或破防受击后退出 |
| `DodgeState` | 闪避位移；进入后先过短启动窗口，之后进入无敌帧 | 成功闪避后打开追击窗口，结束回 Locomotion |
| `MantleState` | 低矮障碍翻越与落点控制 | 完成回 Locomotion |
| `JumpState` | 起跳、空中控制、落地 | 落地回 Locomotion |
| `SkillState` | 技能施法、MP 消耗、CD 设置 | 结束回 Locomotion |
| `InteractState` | 交互短动作 | 完成回 Locomotion |
| `HitState` | 受击硬直；可携带 `Standard` / `GuardBreak` 反应类型供动画 relay、HUD、相机 impact impulse 和后续专属反馈读取 | 结束回 Locomotion |
| `DeathState` | 死亡与重生交接 | 调用检查点恢复流程 |

### 敌人状态机

| 状态 | 职责 |
|---|---|
| `IdleGuardState` | 原地站岗或简单巡逻 |
| `AlertState` | 发现玩家后的锁定与转向 |
| `ChaseState` | 追击并接近玩家 |
| `StrafeState` | 机动兵侧移或短闪避 |
| `MeleeAttackState` | 近战攻击执行 |
| `RangedAttackState` | 远程攻击执行 |
| `HitState` | 受击硬直 |
| `ReturnState` | 丢失目标后返回站位 |
| `DeathState` | 死亡与掉落处理 |

### Boss 状态机

推荐采用：

`Intro -> CombatIdle -> SelectAction -> Attack -> Recover -> Hit -> Death`

第一版必须做到：

- 状态切换逻辑全部代码可追踪
- 动画事件只控制 Hitbox、位移窗口、判定窗口
- 不允许动画控制器直接决定战斗逻辑分支

## 9. ScriptableObject 数据设计清单

| SO 名称 | 用途 | 关键字段 | 第一版必须做 | 以后可扩展 |
|---|---|---|---|---|
| `PlayerBaseStatsSO` | 玩家基础属性 | HP、MP、Attack、Defense、移动速度/加减速、mantle 参数 | 是 | 多角色模板 |
| `CombatBalanceSO` | 全局战斗参数 | 量表积累、格挡启动/反击窗口、闪避启动/无敌帧、闪避距离、Hit Stop | 是 | 难度配置 |
| `AttackDefinitionSO` | 单段攻击定义 | 动画、倍率、命中框、前后摇、可取消窗口 | 是 | 属性伤害、破甲 |
| `ComboChainSO` | 连段关系 | 下一段攻击、输入窗口、派生条件 | 是 | 武器分支树 |
| `SkillDefinitionSO` | 技能配置 | MP、CD、Prefab、释放时间、目标模式 | 是 | 升级分支 |
| `EnemyArchetypeSO` | 敌人原型配置 | 血量、速度、感知距离、攻击集、掉落表 | 是 | 难度变体 |
| `BossPatternSO` | Boss 招式权重配置 | 距离条件、冷却、招式列表 | 是 | 多阶段模式 |
| `DropTableSO` | 掉落表 | 条目、概率、保底 | 是 | 条件掉落 |
| `EncounterDefinitionSO` | 遭遇战配置 | 出生点、敌群、清场条件、门控逻辑 | 是 | 波次战 |
| `CheckpointDefinitionSO` | 检查点定义 | ID、恢复点、激活条件、恢复策略 | 是 | 多类型检查点 |
| `ChapterProgressionSO` | 章节推进结构 | 区域顺序、关键物品、成长节点 | 是 | 多章节共用模板 |
| `ChapterMapDefinitionSO` | 章节地图表达层 | 五区定义、目标提示、路线门、捷径、遭遇/奖励关联 | 是 | 地图 UI、任务追踪、分区加载 |

## 10. 场景与 Prefab 组织方案

### 场景

第一版建议场景列表：

- `Bootstrap.unity`
- `MainMenu.unity`
- `CombatTest.unity`
- `BossTest.unity`
- `Chapter01_Combined.unity`

第一版建议用 `单主章节场景`，不要过早做 Additive 分区流式加载。

### 主章节场景根节点

```text
Chapter01_Combined
  _Systems
  _PlayerSpawn
  _Lighting
  _NavMesh
  _Triggers
  Chapter01_MapZones
  Area01_Entrance
  Area02_Outdoor
  Area03_Interior
  Area04_Boss
```

V2 地图表达层已经在不改变章节进度 ID 的前提下叠加到 `Chapter01_Combined`：`Chapter01_MapZones` 标记入口教学、宽场混战、窄廊压迫、侧路/捷径、Boss 前厅/Boss 房五个动作区；主路线由三段 connector floor 连通，场景仍保持单主章节场景和 public-safe primitive/proxy baseline。`SO_Chapter01_MapDefinition` 现在记录五区目标、遭遇/奖励、门禁和捷径关系，`Zone04_SideRouteShortcut` 已标记 `SideRouteCache` 可选奖励与 Interior 清场后的捷径回环语义；场景 marker 通过 `ChapterMapZoneMarker` 绑定回数据资产，后续地图 UI、目标提示和真实奖励拾取物可以直接消费数据层。

### Prefab 分类

| 类别 | 第一版必须做 | 以后可扩展 |
|---|---|---|
| Characters | 玩家、3 类敌人、Boss | 精英敌人、NPC |
| Combat | Projectile、WeaponHitbox、ImpactVFX | 复杂范围技 |
| Gameplay | Checkpoint、Pickup、TriggerVolume、LockedDoor | 机关链与谜题 |
| UI | HUD、BossBar、LockOnMarker、InteractionPrompt | 完整菜单系统 |
| Environment | 门、围栏、路障、章节关键机关 | 更多可互动场景物 |

## 11. 第一版开发阶段拆分（按周）

| 周次 | 阶段目标 | 核心任务 |
|---|---|---|
| 第 1 周 | 搭出战斗纵切 | 玩家移动、相机、锁定、轻重攻击、格挡、闪避、2 技能、1 个近战敌人、CombatTest、基础 HUD |
| 第 2 周 | 完成前半章 | 三类敌人、掉落系统、入口教学区、教学楼外战斗区、CP01/CP02、初步流程控制 |
| 第 3 周 | 完成整章闭环 | 校舍内部推进区、Boss、CP03、自动存档、章节完成、BossTest |
| 第 4 周 | 调优与发布 | 数值调参、Bug 修复、镜头与打击感优化、Windows/Mac 构建与回归测试 |

## 12. 每周交付物定义

| 周次 | 交付物 | 验收标准 |
|---|---|---|
| 第 1 周 | `战斗纵切` | 在 CombatTest 中能完成完整一对一战斗，轻重攻击、格挡、闪避、技能均可用 |
| 第 2 周 | `前半章可玩` | 从章节开始到 `CP02` 全流程可跑通，3 种敌人全部接入 |
| 第 3 周 | `整章可通关` | 从开局到 Boss 击败并取得术式核心完整闭环，死亡后可恢复 |
| 第 4 周 | `候选发布版本` | Windows、Mac 均可构建运行，阻断级 Bug 清零，数值基本稳定 |

## 13. 风险点与砍功能优先级建议

| 风险点 | 危险原因 | 对策 | 延期时先砍什么 |
|---|---|---|---|
| 战斗手感不成立 | 项目核心卖点缺失 | 第 1 周先做打击反馈、硬直、Hit Stop、锁定 | 砍剧情包装与额外演出 |
| 相机和锁定不稳定 | 直接影响可玩性 | 先做最近目标锁定，不做复杂遮挡与目标切换 | 砍镜头特效 |
| 动画资源不匹配 | 独立开发最易拖慢 | 统一采用 Humanoid 动作集，控制招式数量 | 砍额外派生招式 |
| Boss 膨胀 | 容易吃掉最后一周 | 固定单阶段 4-5 招即可 | 砍多余招式和演出 |
| 场景体量过大 | 一个月无法支撑 | 强制四区域单线推进 | 砍支路、隐藏房间 |
| 存档恢复复杂 | 容易出现软锁 | 只存章节进度、检查点、关键物品、遭遇战清理状态 | 砍动态世界状态 |

建议砍线顺序：

1. 剧情演出
2. 额外环境机关
3. Boss 招式数量
4. 区域长度
5. 章节内成长分支

## 14. 测试计划

| 测试类型 | 第一版必须做 | 说明 |
|---|---|---|
| EditMode | `GaugeFillTests`、`SaveSerializeTests`、`EncounterStateTests` | 保障核心数据和逻辑不易回归 |
| PlayMode | `CheckpointRestoreTests`、`BossResetTests`、`LockOnAcquireTests` | 覆盖章节关键闭环 |
| 烟雾测试 | 新开局、死亡恢复、Boss 开战、Boss 击败、章节结算 | 每日必跑 |
| 人工战斗测试 | 记录格挡成功率、闪避追击触发率、Boss 读招清晰度 | 每周至少两轮 |
| 构建测试 | 先用 `ReleaseCandidateBuildUtility` 验证 Win/Mac 构建输入与输出路径，再各做完整通关验证 | 发布前必须完成 |

构建档位保持分离：`PublicSafe` 仍是公开仓库和正式 RC 的默认门；`UserOwnedGhostSamurai` 只用于项目所有者内部验证真实人物、Avatar、刀与动作，输出到独立目录，且完整授权凭证确认前不能冒充正式外发包。

GhostSamurai 自带人物是无贴图纯色动画样机，不把灰模误报为材质绑定失败，也不把色块调色板写成“已恢复贴图”。内部候选由构建工具生成 7 个确定性的 Built-in `Standard` 语义材质并做技术预检；动作 state / motion / Humanoid / 非 proxy 曲线由自动化守门，动作自然度、脚滑、手刀对位、读招与手感只由项目所有者人工签字。

必须补的调试能力：

- 回血
- 回蓝
- 加满 CounterGauge
- 加满 AgilityGauge
- 传送到任意检查点
- 重置当前遭遇战
- 直接进入 Boss 房

## 15. 第一版完成标准（Definition of Done）

1. 玩家可从 `New Game` 进入章节并完整通关至取得“术式核心”。
2. 四个区域全部存在且功能完整，至少三个检查点可正常恢复。
3. 玩家核心操作全部可用：移动、锁定、轻三连、重攻击、格挡、闪避、跳跃、2 技能、交互。
4. 三类普通敌人与 Boss 都具备完整行为链：发现、追击、攻击、受击、死亡、掉落。
5. `CounterGauge` 和 `AgilityGauge` 都能在正式章节中稳定触发强化动作。
6. 自动存档覆盖章节开始、检查点激活、Boss 门前、Boss 击败后四类时机。
7. 存档恢复能正确还原当前检查点、关键物品、固定成长和已清除遭遇战。
8. 关键参数已 ScriptableObject 化，不依赖散落硬编码常量。
9. `CombatTest` 与 `BossTest` 两个测试场景均存在并可独立验证。
10. Windows 与 Mac 构建通过统一 release candidate 构建入口生成，可启动并完成整章，无持续 Console Error。
