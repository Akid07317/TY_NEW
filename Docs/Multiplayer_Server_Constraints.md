# 多人服务端改造约束文档

更新时间：2026-07-10
适用项目：TY_NEW / Unity 6000.4.2f1
文档性质：约束与准入门槛，不是完整实施方案

> 公开仓库说明：历史公网验证记录已使用 `<ECS_USER>`、`<ECS_HOST>` 和 `<SSH_KEY_PATH>` 脱敏。请按自己的服务器、安全组和凭据复跑，不要复用这些占位值。

## 1. 结论约束

当前项目不能直接迁移到阿里云 2 核 2G ECS 当多人游戏服务端。

当前项目本质上仍是 Unity 单机动作 RPG 客户端工程。现有 Windows/macOS Standalone 构建、玩家本地输入、本地移动、本地攻击命中、本地伤害结算、本地敌人 AI、本地 JSON 存档，都不能直接等价为 headless authoritative game server。

2026-07-06 进展补充：项目已新增 Linux Dedicated Server 构建链、TCP health 探活骨架和最小 gameplay TCP 协议，可产出 `Builds/DedicatedServer/Linux/TYServer.x86_64`。P1 包已部署到 ECS，并已通过 ECS 本机和公网 `7777` gameplay 探针。P1 当前只做到服务端在 `7777` 接受 TCP 连接、返回欢迎行、处理 `HELLO/JOIN`、`PING`、`STATE`、`QUIT`，并维护最小房间/玩家计数。这不等于已经完成玩家同步、权威移动或服务端权威战斗。

2026-07-06 P1.5 进展补充：项目已引入 Netcode for GameObjects `2.6.0` 和 Unity Transport `2.6.0`，Dedicated Server 默认可启动 NGO/UTP server，正式多人通道使用 UDP `7777`，P1 TCP 探针仍保留在 TCP `7777`。当前新增的是最小 NetworkPlayer prefab、客户端命令行自动连接入口、health 网络状态字段和本地 EditMode/smoke 合同；本机 macOS server + 两个 batchmode client 已验证 `networkConnectedClients=2 networkSpawnedPlayers=2`，两个客户端退出后回落到 `0/0`。最新客户端侧 smoke 还验证了 client1/client2 均看到 `remote=1` 的对方 avatar，且 client2 先退出后 client1 观察到 `remote=0`，说明最小 spawn/despawn 可见性已在本机闭环内成立。基础位置同步也已通过本机验证：client1 使用 smoke movement 移动，client2 观察到同一 remote avatar 位置从 `z=3.20` 更新到 `z=7.20`，远端移动距离 `4.00`。P1.5 Linux 包已部署到 ECS 且服务端已监听 UDP `7777`；阿里云安全组 UDP 入方向放行后，公网双客户端 NGO/UTP 验证已通过，ECS health 达到 `networkConnectedClients=2 networkSpawnedPlayers=2`，client1/client2 均看到对方 avatar，client2 退出后 client1 观察到 despawn，client2 观察到 client1 远端移动距离 `5.61`。这不等于已经把 CombatTest 的正式玩家 prefab、战斗、敌人、动画和 UI 全部网络化。

2026-07-06 P2 进展补充：NetworkPlayerAvatar 已从“RPC 到达即位移”改成“客户端提交输入意图，服务端按 tick 推进位置/朝向”。HP 已接入 server-write `NetworkVariable<int>`，默认 `100`；客户端 smoke 可提交过量 HP 意图，服务端单次最多接受 `25` 点，客户端只观察同步结果。本机和 ECS 公网双客户端 smoke 均已通过 `P2_MULTIPLAYER_OK`，覆盖 `connected=2 spawned=2`、互相可见、client2 退出后的 despawn、远端位置同步和 HP 从 `100` 到 `75` 的同步观察。P2 仍然只是最小网络玩家骨架，不等于正式 CombatTest 玩家 prefab、正式攻击命中、敌人 AI、动画表现和 UI 都已经网络化。

2026-07-06 P3 进展补充：NetworkPlayerAvatar 已新增服务端权威攻击意图链路。客户端 smoke 只发送攻击意图和序号，不发送最终伤害、目标或命中事实；服务端验证序号、冷却、距离和朝向后写入目标 HP，固定单次伤害 `25`。本机和 ECS 公网双客户端 smoke 均已通过 `P3_MULTIPLAYER_OK`，并验证客户端请求 `9999` 伤害时服务端只应用 `25`，目标 HP 从 `100` 到 `75`。P3 仍然只是最小网络玩家骨架上的单次攻击验证，不等于正式 CombatTest 玩家 prefab、连招、受击、死亡、敌人 AI、动画表现和 UI 都已经网络化。

2026-07-06 P3.5 进展补充：P3 攻击骨架已升级为服务端白名单攻击配置解析。客户端只发送攻击意图、序号和 `attackId`，服务端通过 `NetworkServerAttackProfile` 解析 `Light_01` 的伤害、范围、半角和冷却；非法 attackId 不会产生伤害。本机和 ECS 公网双客户端 smoke 均已通过，输出 `P3_ATTACK_HIT_OK attackId=Light_01 ... clientRequestedDamage=9999 serverAppliedDamage=25`。P3.5 仍然只是最小网络玩家骨架上的攻击配置验证，不等于正式 CombatTest 玩家 prefab、连招、受击、死亡、敌人 AI、动画表现和 UI 都已经网络化。

2026-07-06 P4 进展补充：NetworkPlayerAvatar 已新增服务端权威死亡状态同步。HP 归零后由服务端写入 server-write `NetworkVariable<bool>` 死亡状态，客户端 smoke 日志输出 `deaths=`；P4 smoke 使用 4 次 `Light_01` 攻击意图验证服务器按白名单 profile 分 4 次各结算 `25` 伤害，目标 HP 从 `100` 到 `0` 后同步 `death=false->true`。本机和 ECS 公网双客户端 smoke 均已通过，输出 `P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 ... client2ObservedLocalDeathLater=true` 和 `P4_MULTIPLAYER_OK ... deathSync=true`。P4 仍然只是最小 NetworkPlayerAvatar 骨架上的死亡事实同步，不等于正式 CombatTest 玩家 prefab、死亡动画、受击硬直、复活、敌人 AI 或 UI 都已经网络化。

2026-07-06 P4.5/P5 前置进展补充：已新增 `NetworkPlayerDeathStateBridge`，作为网络死亡事实进入正式 CombatTest 玩家死亡链路的薄桥。桥接逻辑会把 `NetworkPlayerAvatar.IsDead` 应用到 `HealthComponent` 和 `PlayerStateMachine`，从而进入 `PlayerDeathState`，后续正式网络玩家 prefab 可以复用这条接口，而不需要让客户端决定死亡。定向 EditMode 已通过 `20/20 Passed`，新增测试验证桥接会把正式玩家血量归零并切入 `PlayerDeathState`；包含该桥接代码的新 Linux 包已部署到 ECS，公网 P4 smoke 仍通过 `P4_MULTIPLAYER_OK ... deathSync=true`。这仍不是完整正式 CombatTest 网络 prefab 替换，也还没有完成死亡动画、复活 UI 和敌人 AI 的网络化。

2026-07-06 P5 第一阶段进展补充：`PF_Player_CombatTest` 已挂接 `NetworkPlayerDeathStateBridge`，并将桥接组件序列化引用到正式玩家自身的 `HealthComponent` 和 `PlayerStateMachine`。`CombatTestSceneBuilder.BuildPlayerPrefab()` 和 `RepairPlayerPrefab()` 也会持续确保该组件存在，避免后续重建/修复 CombatTest 玩家 prefab 时丢失网络死亡入口。定向 EditMode 已通过 `21/21 Passed`，新增测试 `CombatTestPlayerPrefab_HasNetworkDeathStateBridgeWiredToFormalPlayerState` 验证 prefab 本体的桥接引用；Linux Dedicated Server 构建仍通过 `Build Finished, Result: Success.`。本阶段未重新部署 ECS 包，因为服务端运行时代码未变；已对当前 ECS 服务复跑公网 P4 smoke，仍通过 `P4_DEATH_SYNC_OK` 和 `P4_MULTIPLAYER_OK ... deathSync=true`。这仍不是完整正式网络玩家 prefab 替换，`NetworkPlayerAvatar` 与正式玩家移动/攻击/动画/复活/UI 的合并仍待后续阶段完成。

2026-07-06 P5 第二阶段进展补充：已新增 server-safe 正式网络玩家 prefab `Assets/_Game/Resources/Multiplayer/PF_NetworkPlayerCombatTest.prefab`，Resources 路径为 `Multiplayer/PF_NetworkPlayerCombatTest`。该 prefab 根节点包含 `NetworkObject` 和 `NetworkPlayerAvatar`，子节点为 unpack 后的正式 CombatTest 玩家组件树，并剥离了 local-preview-only 视觉依赖，只保留 proxy baseline；子节点上的 `NetworkPlayerDeathStateBridge` 已接到根 `NetworkPlayerAvatar`、正式 `HealthComponent` 和 `PlayerStateMachine`。`DedicatedServerBuildUtility` 已新增生成/验证入口，`ValidateBuildInputs()` 会校验该 formal prefab 不依赖 `Assets/Free medieval weapons`、`Assets/JC_LP_MedievalCharacters_LITE` 等预览目录。定向 EditMode 已通过 `23/23 Passed`，新增测试覆盖 formal prefab 结构、依赖边界、根节点 pose 驱动正式玩家子树，以及权威死亡切入 `PlayerDeathState`。Linux Dedicated Server 构建仍通过 `Build Finished, Result: Success.`。`probe_p15_multiplayer.py` 和 ECS 验证脚本已支持可选 `--network-player-prefab` / `TY_NEW_NETWORK_PLAYER_PREFAB`，但默认 ECS 服务仍使用原最小 prefab；本阶段未替换或重新部署 ECS 远端服务，公网默认 P4 smoke 仍通过 `P4_DEATH_SYNC_OK` 和 `P4_MULTIPLAYER_OK ... deathSync=true`。

2026-07-06 P5 第三阶段进展补充：formal network player prefab 已通过本机 Mac server/client 专项 smoke。修复点是 `ServerRuntimeBootstrap` 现在会在命令行 `--network-player-prefab` 路径不同于 ServerBoot 场景序列化默认 prefab 时，按 active Resources 路径加载 prefab，避免服务端继续使用 `Multiplayer/PF_NetworkPlayerAvatar` 而客户端使用 `Multiplayer/PF_NetworkPlayerCombatTest` 导致 `NetworkConfig mismatch`。新增定向 EditMode 用例后 `DedicatedServerBuildUtilityTests` 通过 `24/24 Passed`；本机 Mac server/client 重新构建成功；专项 smoke 显式传 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest`，通过 `P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 ... client2ObservedLocalDeathLater=true` 和 `P4_MULTIPLAYER_OK host=127.0.0.1 gamePort=7891 healthPort=7892 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`。这仍是本机 formal prefab 验证，尚未构建并部署新的 Linux 包到 ECS，也尚未完成正式玩家动画、复活 UI、敌人 AI 或完整 CombatTest 战斗网络化。

2026-07-06 P5 第四阶段进展补充：formal network player prefab 已完成 Linux Dedicated Server 构建、ECS 部署和公网专项 smoke。新包 `Builds/DedicatedServer/TYServer-linux-x86_64-p5-formal-prefab.tar.gz` 约 `81M`，SHA256 为 `0072b7853325fbdd064e4497eaba000a804b9321cacbb1455ddeb566fc05f2b5`；ECS 远端 SHA256 校验一致，systemd 已以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest` 启动。公网验证通过 health、UDP 入站和双客户端 smoke：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`，死亡同步证据为 `P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`。远端服务日志确认 active path 为 `Multiplayer/PF_NetworkPlayerCombatTest`，并记录 4 次 `Light_01 damage=25`，最终 `health=25->0 targetDead=True`。这说明 formal CombatTest 玩家子树已能承载当前最小网络同步骨架，但仍不等于正式玩家动画、复活 UI、敌人 AI 或完整 CombatTest 战斗状态机都已经网络化。

2026-07-07 P5.5 第一阶段进展补充：formal network player prefab 已新增 `NetworkPlayerPresentationBridge`，用于把 `NetworkPlayerAvatar` 的权威 HP/死亡状态稳定投射到正式 CombatTest 玩家子树。该桥会约束 formal 子节点本地 pose、同步 `HealthComponent`、通过 `NetworkPlayerDeathStateBridge` 进入正式 `PlayerDeathState`，并持续 suppress 子节点 `PlayerCharacter` 的本地单机驱动，避免非拥有端或本地状态机自跑破坏网络同步。`MultiplayerClientSmokeReporter` 已新增 `formalDeaths=` 和 `formalDrivers=`，`probe_p15_multiplayer.py` 已在 formal prefab smoke 中要求 formal death sync。定向 EditMode 已通过 `24/24 Passed`；本机 formal smoke 通过 `P5_FORMAL_DEATH_SYNC_OK ... client2ObservedLocalFormalDeathLater=true`；P5.5 Linux 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p55-presentation.tar.gz` 约 `80M`，SHA256 为 `f21ee3976b927bc05e2349e7c7e37cb867f01a7dfd61f7e425c8f1a7e77aaac9`，已部署到 ECS 并通过公网 formal smoke：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true disconnected=0`。这说明当前 formal 玩家表现层已能跟随最小权威网络骨架进入正式死亡状态；仍不等于动画过渡、受击硬直、复活 UI、敌人 AI 或完整 CombatTest 战斗状态机都已网络化。

2026-07-07 P5.6 第一阶段进展补充：`NetworkPlayerPresentationBridge` 已能从权威 HP 下降推导一次 formal `PlayerHitState` 表现，并用 sticky 观测标记输出到 smoke 的 `formalHits=`；权威死亡仍进入 formal `PlayerDeathState`，formal 子节点本地驱动继续保持 `suppressed`。本阶段没有新增网络变量、RPC 或服务端权威规则，只把既有 `NetworkPlayerAvatar` HP/death 事实投射到正式玩家表现层。定向 EditMode 仍为 `24/24 Passed`；本机 formal smoke 通过 `P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`，总结为 `formalHitSync=true formalDeathSync=true`。由于未改 NGO prefab 结构和服务端逻辑，本阶段未重新部署 ECS 包；使用当前 P5.5 ECS 服务端和本地新客户端/探针完成公网兼容回归，输出 `P5_FORMAL_HIT_SYNC_OK ... client2ObservedLocalFormalHitLater=true`、`P5_FORMAL_DEATH_SYNC_OK ... client2ObservedLocalFormalDeathLater=true` 和 `P4_MULTIPLAYER_OK host=<ECS_HOST> ... formalDeathSync=true formalHitSync=true disconnected=0`。

2026-07-07 P5.7 第一阶段进展补充：`NetworkPlayerAvatar` 已新增服务端写入的 `AttackPresentationSequence` / `AttackPresentationCode`，只在服务端通过 attackId 白名单、序号和冷却检查后发布 formal 攻击表现事实；客户端仍不能提交最终命中、目标或伤害。`NetworkPlayerPresentationBridge` 消费该事实后让 formal CombatTest 子树进入 `PlayerAttackState`，并立即清理本地 hitbox 准备态，避免表现层产生本地命中判定；formal `PlayerHitState` / `PlayerDeathState` 优先级高于攻击表现。`MultiplayerClientSmokeReporter` 新增 `formalAttacks=`，`probe_p15_multiplayer.py` 在 formal prefab smoke 中要求 client2 观察到远端攻击者 `formalAttacks=false->true`，并输出 `P5_FORMAL_ATTACK_SYNC_OK`。定向 EditMode 为 `24/24 Passed`；Mac server/client 构建成功；本机 formal smoke 通过 `P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`，总结为 `formalAttackSync=true formalHitSync=true formalDeathSync=true`。由于本阶段新增 NGO NetworkVariables，旧 ECS P5.5 服务端不再适合作为兼容回归目标；本阶段未构建或部署新的 Linux/ECS 包，下一步必须先打新 Linux 包并替换 ECS 后再做公网 P5.7 验证。

2026-07-07 P5.8 进展补充：P5.7 formal attack presentation 改动已构建为 Linux Dedicated Server 包并部署到 ECS。新包 `Builds/DedicatedServer/TYServer-linux-x86_64-p58-formal-attack.tar.gz` 约 `80M`，SHA256 为 `a3134726feece31b3fa43de0f7feeaaca7ee4dac61f918e73a0ca69feb7ba812`；ECS 远端 SHA256 校验一致，systemd 已以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest` 启动。公网验证通过 health、UDP 入站和双客户端 formal smoke：`P5_FORMAL_ATTACK_SYNC_OK attackId=Light_01 ... client2ObservedRemoteFormalAttackLater=true`、`P5_FORMAL_HIT_SYNC_OK ... client2ObservedLocalFormalHitLater=true`、`P5_FORMAL_DEATH_SYNC_OK ... client2ObservedLocalFormalDeathLater=true`，最终 `P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true disconnected=0`。smoke 后公网 health 回落到 `networkConnectedClients=0 networkSpawnedPlayers=0`。这说明 formal CombatTest 玩家子树已能在 ECS 公网环境中根据服务端认可的攻击事实进入正式 `PlayerAttackState`，同时 formal hit/death 仍由服务端权威 HP/death 事实驱动；仍不等于敌人 AI、复活 UI、完整连招预测或完整 CombatTest 战斗状态机都已网络化。

2026-07-07 P5.9 维护补充：已清理 ECS `/opt/ty-new-server` 下历史 AppleDouble `._*` 残留文件，并在部署脚本 `Deploy/DedicatedServer/deploy_p1_gameplay.sh` 中加入解包后的 scoped `sudo find /opt/ty-new-server -name '._*' -type f -delete` 清理，避免后续覆盖部署时残留再次触发 Unity 插件扫描 warning。清理后远端 `find /opt/ty-new-server -name '._*'` 无输出，重启 `ty-new-server.service` 后日志不再出现 `Failed to open plugin: ... ._*`，服务保持 `active`。公网回归重新通过 `P1.5_HEALTH_OK`、`P1.5_UDP_INGRESS_OK`、`P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK` 和 `P4_MULTIPLAYER_OK ... formalDeathSync=true formalAttackSync=true formalHitSync=true disconnected=0`；smoke 后 health 回落到 `networkConnectedClients=0 networkSpawnedPlayers=0`。

2026-07-07 P6.0 第一阶段进展补充：已新增 server-owned `NetworkEnemyAvatar` 和 `Assets/_Game/Resources/Multiplayer/PF_NetworkEnemyAvatar.prefab`，并在 `MultiplayerNetworkSessionService` 的 NGO `NetworkConfig.Prefabs` 中注册该敌人 prefab。服务端启动后会生成一只最小网络敌人；当一轮 smoke 的两个客户端连接后，服务端写入敌人 HP/death 权威事实，客户端只观察 `enemyHealths=` / `enemyDeaths=`。`MultiplayerClientSmokeReporter` 和 `probe_p15_multiplayer.py` 已新增 P6 敌人观测合同，定向 EditMode 通过 `26/26 Passed`；本机 Mac server/client 重新构建成功；本机 P6 smoke 通过 `P6_NETWORK_ENEMY_SYNC_OK ... client1ObservedEnemyHealthDrop=50 ... client2ObservedEnemyHealthDrop=50 ... EnemyDeathLater=true` 和 `P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=7967 healthPort=7968 ... formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true disconnected=0`。这只是“服务端生成敌人网络对象并同步位置/HP/死亡事实”的最小闭环；P6.0 本机阶段尚未 ECS 公网部署，后续 P6.1 已补上公网部署验证，但正式 `EnemyBrain`、NavMesh、敌人攻击、敌人受击动画和掉落仍未接入。

2026-07-07 P6.1 ECS 部署补充：P6.0 网络敌人同步改动已构建为 Linux Dedicated Server 包并部署到 ECS。新包 `Builds/DedicatedServer/TYServer-linux-x86_64-p61-network-enemy.tar.gz` 约 `79M`，SHA256 为 `c077808293939d1b365f7d58767037536eb0f4c51bf438cdfbbb7e907ff750bb`；ECS 远端 SHA256 校验一致，`ty-new-server.service` 继续以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest` 启动并保持 `active`。公网验证通过 health、UDP 入站和双客户端 formal smoke：`P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`、`P6_NETWORK_ENEMY_SYNC_OK ... client1ObservedEnemyHealthDrop=50 ... client2ObservedEnemyHealthDrop=50`，最终 `P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true disconnected=0`。smoke 后公网 health 回落到 `networkConnectedClients=0 networkSpawnedPlayers=0`。远端日志确认 active path 为 `Multiplayer/PF_NetworkPlayerCombatTest`，生成 `Multiplayer/PF_NetworkEnemyAvatar`，记录敌人 `health=50->0 enemyDead=True`，并记录 4 次 `Light_01 damage=25`，最终玩家目标 `health=25->0 targetDead=True`。这仍只是“最小 server-owned 网络敌人事实同步”，不等于正式 `EnemyBrain`、NavMesh、敌人攻击、受击动画、掉落或完整 CombatTest 敌人战斗状态机已经网络化。

2026-07-07 P6.2 第一阶段进展补充：`PF_NetworkEnemyAvatar` 已从最小网络敌人升级为 server-owned formal CombatTest 敌人承载 prefab：根节点仍是 `NetworkObject` + `NetworkEnemyAvatar`，子节点为 unpack 后的 `PF_Enemy_Melee_CombatTest` 正式敌人组件树，并剥离 local-preview-only 视觉依赖，改用服务端安全 proxy 视觉。新增 `NetworkEnemyPresentationBridge` 会把 `NetworkEnemyAvatar` 的权威 HP/death 投射到正式 `HealthComponent` 和 `EnemyStateMachine`，并在客户端持续 suppress `EnemyBrain`、`EnemySensing`、`EnemyMotor`、`EnemyAttackController` 与 `NavMeshAgent`，确保客户端只观察敌人位置、HP 和死亡事实，不本地运行敌人 AI/寻路/攻击。定向 EditMode `DedicatedServerBuildUtilityTests` 已通过 `27/27 Passed`；本机 Mac server/client 构建成功；本机 P6.2 smoke 通过 `P6_NETWORK_ENEMY_SYNC_OK` 和 `P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`，总结为 `P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=7977 healthPort=7978 ... networkEnemySync=true formalNetworkEnemySync=true disconnected=0`。本阶段尚未构建或部署新的 Linux/ECS 包，也尚未验证服务端真实 `EnemyBrain` 目标选择、NavMesh 寻路、敌人攻击、敌人受击动画或掉落。

2026-07-07 P6.3 第一阶段进展补充：已新增可选的 server-authored 网络敌人攻击 smoke。只有服务端启动参数带 `--enable-network-enemy-attack-smoke` 时，`NetworkEnemyAvatar` 才会在两个客户端连接后选择一个存活 `NetworkPlayerAvatar` 作为目标，由服务端直接写入该玩家 server-write HP，固定造成 `25` 点伤害，并发布敌人攻击表现序号；客户端只观察玩家 HP 下降、formal 玩家受击表现和 `enemyFormalAttacks=`，不能提交敌人命中或伤害。`NetworkEnemyPresentationBridge` 已能消费敌人攻击表现事实，让 formal 敌人子树短暂进入 `EnemyAttackState`，客户端敌人本地 `EnemyBrain`/NavMesh/攻击驱动仍保持 `suppressed`。探针新增 `--require-network-enemy-attack-sync`，本机 Mac server/client 构建成功；P6.3 本机 smoke 通过 `P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedLocalHealthStart=100 client1ObservedLocalHealthLater=75 ... client2ObservedRemoteHealthLater=75 ... FormalEnemyAttackLater=true`，最终 `P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=7987 healthPort=7988 ... networkEnemySync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。本阶段未构建或部署新的 Linux/ECS 包，也仍未验证正式 `EnemyBrain` 目标选择、NavMesh 寻路、敌人 `EnemyAttackController.TryAttack` 命中、受击动画、掉落或仇恨目标同步。

2026-07-07 P6.4 ECS 部署补充：P6.3 server-authored 网络敌人攻击事实已构建为 Linux Dedicated Server 包并部署到 ECS。新包 `Builds/DedicatedServer/TYServer-linux-x86_64-p64-enemy-attack.tar.gz` 约 `79M`，SHA256 为 `20057640fbd333faadcd22d3f845f10071c39876ed8f91928e5b987c70d9bc02`；ECS 远端 SHA256 校验一致，`ty-new-server.service` 以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --enable-network-enemy-attack-smoke` 启动并保持 `active`。公网验证通过 health、UDP 入站和双客户端 P6 smoke：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedLocalHealthDrop=25 client2ObservedRemoteHealthDrop=25 ... FormalEnemyAttackLater=true`，最终 `P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemySync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。远端日志确认服务端执行敌人攻击事实 `health=100->75`，之后玩家互打与敌人死亡 smoke 仍通过，smoke 后 health 回落到 `networkConnectedClients=0 networkSpawnedPlayers=0`。这仍是 smoke hook 验证，不等于正式 `EnemyBrain`/NavMesh/`EnemyAttackController.TryAttack` 已成为服务端真实 gameplay tick。

2026-07-07 P6.5 本机进展补充：P6.4 的 timer-style 敌人攻击 smoke 已推进为 P6.5 formal `EnemyAttackController.TryAttack` 提交验证。新增服务端启动参数 `--enable-network-enemy-formal-attack-smoke`，本机 server 会把网络敌人生成在 formal melee 攻击范围内，由正式 `EnemyAttackController.TryAttack()` 作为攻击提交门槛；提交成功后仍只由服务端写 `NetworkPlayerAvatar` 的 server-write HP，并发布 formal 敌人攻击表现事实，客户端继续只观察 `enemyFormalAttacks=` 和玩家 HP 下降。定向 EditMode `DedicatedServerBuildUtilityTests` 通过 `27/27 Passed`；Mac server/client 构建成功；本机 P6.5 smoke 通过 `P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedLocalHealthDrop=25 client2ObservedRemoteHealthDrop=25`，最终 `P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=7997 healthPort=7998 ... networkEnemySync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。服务端日志确认走新路径：`[MultiplayerEnemy] Formal smoke enemy attack applied ... health=100->75`。本阶段尚未构建 Linux 包或部署 ECS，也仍未验证真实 `EnemyBrain` 目标选择、NavMesh 追击、非 smoke 的敌人攻击循环、受击动画、掉落或仇恨目标同步。

2026-07-07 P6.6 本机进展补充：P6.5 formal 敌人攻击验证已推进到服务端 formal `EnemyBrain` 自动感知/选中目标后进入攻击提交。新增服务端启动参数 `--enable-network-enemy-brain-attack-smoke` 和探针参数 `--use-brain-network-enemy-attack-smoke`；该模式会先等待两个客户端接入，再在服务端放开 formal 敌人驱动，让 `EnemyBrain`/`EnemySensing` 选中 formal `PlayerCharacter` 目标并进入 `EnemyAttackState`，随后通过 smoke bridge 调用正式 `EnemyAttackController.TryAttack(CurrentTarget)` 产生 `EnemyAttackCommit` 事件，最终仍只由服务端写 `NetworkPlayerAvatar.ApplyServerEnemyDamage(25)` 和发布 formal 敌人攻击表现事实。客户端 formal 敌人驱动继续保持 suppressed，只观察玩家 HP、敌人攻击表现和网络敌人 HP/death。Mac server/client 构建成功；本机 P6.6 smoke 通过 `P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=local client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=remote client2ObservedTargetHealthDrop=25`，最终 `P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8047 healthPort=8048 ... networkEnemySync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。服务端日志确认新路径：`Brain smoke enemy attack status ... sensedTarget=FormalPlayer_CombatTest`、`Brain smoke enemy attack committed ... attackId=Enemy_Melee`、`Brain smoke enemy attack applied ... health=100->75`。本阶段尚未构建 Linux 包或部署 ECS；它验证的是 Brain 自动选目标后的本机 smoke bridge，不等于完整 NavMesh 追击、距离外追击后攻击、非 smoke 循环、敌人受击动画、掉落或仇恨同步都已完成。

2026-07-07 P6.7 ECS 部署补充：P6.6 EnemyBrain 敌人攻击 smoke 已构建为 Linux Dedicated Server 包并部署到 ECS。新包 `Builds/DedicatedServer/TYServer-linux-x86_64-p67-brain-enemy.tar.gz` 约 `79M`，SHA256 为 `6c46f5dca899128aaf09abcab122278d8de791529410e3b75579ce8a9a58eaf4`；ECS 远端 SHA256 校验一致，`ty-new-server.service` 当前以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --enable-network-enemy-brain-attack-smoke` 启动并保持 `active`。公网验证通过 health、UDP 入站和双客户端 P6 smoke：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=remote client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=local client2ObservedTargetHealthDrop=25`，最终 `P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemySync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。远端日志确认 `Brain smoke enemy attack status ... sensedTarget=FormalPlayer_CombatTest`、`Brain smoke enemy attack committed ... attackId=Enemy_Melee`、`Brain smoke enemy attack applied ... health=100->75`，smoke 后 health 回落到 `networkConnectedClients=0 networkSpawnedPlayers=0`。这说明 P6.6 的 Brain smoke bridge 已经在 2 核 2G ECS 公网路径跑通；仍不等于完整 NavMesh 距离外追击、非 smoke 攻击循环、多敌人仇恨、受击动画或掉落已经网络化。

2026-07-07 P6.8 本机进展补充：P6.7 EnemyBrain 攻击 smoke 已推进到距离外追击后进入攻击范围再提交。新增服务端启动参数 `--enable-network-enemy-brain-chase-attack-smoke` 和探针参数 `--use-brain-chase-network-enemy-attack-smoke`；该模式把 formal 网络敌人生成在 `-1,0,5`，服务端放开 formal 敌人驱动，让 `EnemyBrain` / `EnemySensing` 选中 formal 玩家，`EnemyMotor` 在 ServerBoot 无有效 NavMesh 时走 fallback 追击，`NetworkEnemyPresentationBridge` 再把 formal 子树位姿提交回网络根节点供客户端观察。Mac server/client 构建成功；本机 smoke 通过 `P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=2.57 client2ObservedEnemyMoveDistance=2.57`、`P6_NETWORK_ENEMY_ATTACK_SYNC_OK ... client1ObservedTargetHealthDrop=25 ... client2ObservedTargetHealthDrop=25`，最终 `P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8059 healthPort=8060 ... networkEnemyChaseSync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。服务端日志确认 `Armed brain chase smoke enemy attack ... spawnPosition=-1.00,0.00,5.00`、`state=EnemyChaseState ... currentTargetDistance=3.37`、`Brain smoke enemy attack applied ... health=100->75 enemyPosition=-1.00,0.00,1.97`。本阶段尚未构建 Linux 包或部署 ECS；它验证的是无 NavMesh 场景下的本机 fallback 追击同步，不等于 baked NavMesh 路径追击、非 smoke 攻击循环、多敌人仇恨、受击动画或掉落已经完成。

2026-07-07 P6.9 本机进展补充：ServerBoot 已新增 server-only `ServerNavMeshGround` 并生成 baked NavMesh 数据 `Assets/_Game/Scenes/ServerBoot/NavMesh.asset`，`ServerBoot.unity` 的 `m_NavMeshData` 不再为空。`DedicatedServerBuildUtility.ValidateBuildInputs()` 会校验 ServerBoot 包含导航地面和 baked NavMesh 数据，定向 EditMode `DedicatedServerBuildUtilityTests` 通过 `29/29 Passed`，其中新增 `ServerBootScene_ContainsBakedNavMeshForEnemyChaseSmoke`。探针新增 `--require-network-enemy-navmesh-chase`，要求服务端日志出现 `navMeshReady=True` 且服务端日志不出现 fallback warning。本机 P6.9 smoke 通过 `P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`、`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=3.67 client2ObservedEnemyMoveDistance=3.67` 和 `P6_NETWORK_ENEMY_ATTACK_SYNC_OK ... client1ObservedTargetHealthDrop=25 ... client2ObservedTargetHealthDrop=25`，最终 `P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8061 healthPort=8062 ... networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。服务端日志确认 `EnemyChaseState ... navMeshAgentEnabled=True navMeshReady=True`、`EnemyAttackState ... navMeshReady=True`、`Brain smoke enemy attack applied ... health=100->75 enemyPosition=-1.00,0.00,1.95`。本阶段尚未构建 Linux 包或部署 ECS；客户端日志仍可能因客户端显示场景无 NavMesh 出现一次 agent warning，但客户端敌人驱动保持 suppressed，不影响服务端权威导航结论。

2026-07-07 P6.10 ECS 部署补充：P6.9 ServerBoot baked NavMesh 敌人追击改动已构建为 Linux Dedicated Server 包并部署到 2 核 2G ECS。新包 `Builds/DedicatedServer/TYServer-linux-x86_64-p610-navmesh-chase.tar.gz` 约 `79M`，SHA256 为 `0367f23c10a10647a3e6ea12a51395cb43d0771347ed3f7d706f8c10e2573fbe`；ECS 远端 SHA256 校验一致，`ty-new-server.service` 当前以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --enable-network-enemy-brain-chase-attack-smoke` 启动并保持 `active`。`verify_p15_ecs_multiplayer.sh` 已补充 `TY_NEW_REQUIRE_NETWORK_ENEMY_CHASE_SYNC=1`、`TY_NEW_REQUIRE_NETWORK_ENEMY_NAVMESH_CHASE=1` 和远端 `/var/log/ty-new/server.log` 实时 tail，使公网 smoke 能在同一轮输出 `networkEnemyNavMeshChaseSync=true`。公网验证通过 health、UDP 入站和双客户端 P6.10 smoke：`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=3.27 client2ObservedEnemyMoveDistance=3.62`、`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`、`P6_NETWORK_ENEMY_ATTACK_SYNC_OK ... network enemy attack ...`，最终 `P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。远端日志确认服务端路径为 `EnemyChaseState ... navMeshAgentEnabled=True navMeshReady=True`、`EnemyAttackState ... navMeshReady=True`、`Brain smoke enemy attack applied ... health=100->75 enemyPosition=-1.00,0.00,2.00`，且未出现 `Failed to create agent because there is no valid NavMesh` fallback warning；smoke 后公网 health 回落到 `networkConnectedClients=0 networkSpawnedPlayers=0`。这说明 baked NavMesh chase smoke 已经在 2 核 2G ECS 公网路径跑通；仍不等于完整非 smoke 敌人 gameplay tick、多敌人仇恨、受击动画、掉落或正式容量已经完成。

2026-07-07 P6.11 本机进展补充：网络敌人已从 P6.10 专项 brain chase attack smoke 推进到最小非 smoke 服务端敌人 gameplay tick。新增服务端启动参数 `--enable-network-enemy-server-tick` / `--network-enemy-server-tick`，由 `ServerRuntimeBootstrap` 传入 `MultiplayerNetworkSessionService.ConfigureServerEnemyGameplayTick()`，再由 `NetworkEnemyAvatar` 在双客户端接入后持续放开 server formal 敌人驱动。该路径复用正式 `EnemyBrain.Update()`、`EnemyStateMachine`、`EnemyMotor` 的 baked NavMesh 追击和 `EnemyAttackController.AttackCommitted`，网络层只把 formal driver 位姿提交到 `NetworkEnemyAvatar`，并把第一次正式攻击 commit 转成 server-write 玩家 HP 与 formal 敌人攻击表现事实。探针新增 `--require-network-enemy-server-tick` 和总结字段 `networkEnemyServerTick=true`；本机 smoke 通过 `P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true`、`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=3.62 client2ObservedEnemyMoveDistance=3.62`、`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetHealthDrop=25 client2ObservedTargetHealthDrop=25`，最终 `P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8071 healthPort=8072 ... networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。服务端日志确认 `Server tick enemy status ... state=EnemyChaseState ... navMeshReady=True serverTick=True`、`Server tick enemy status ... state=EnemyAttackState ... navMeshReady=True serverTick=True` 和 `Server tick enemy attack applied ... health=100->75 enemyPosition=-1.00,0.00,2.00`。本阶段仍为本机 Mac server/client 验证，敌人 death 事实仍按 P6 smoke 合同触发，网络伤害目前只发布首个正式敌人 attack commit；尚未构建 Linux 包或部署 ECS，也尚未验证多敌人、仇恨切换、掉落、受击动画或长时间稳定性。

2026-07-07 P6.12 ECS 部署补充：P6.11 最小非 smoke 服务端敌人 gameplay tick 已构建为 Linux Dedicated Server 包并部署到 2 核 2G ECS。新包 `Builds/DedicatedServer/TYServer-linux-x86_64-p612-server-tick.tar.gz` 约 `79M`，SHA256 为 `b9cf2bfe4bd33662e2bcbbe78cdefad5c1009287f235174c5ec546be74e726e7`；ECS 远端 SHA256 校验一致，`ty-new-server.service` 当前以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --enable-network-enemy-server-tick` 启动并保持 `active`。公网验证通过 health、UDP 入站和双客户端 P6.12 smoke：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true`、`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=3.46 client2ObservedEnemyMoveDistance=3.62`、`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`、`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetHealthDrop=25 client2ObservedTargetHealthDrop=50`，最终 `P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。远端日志确认 `Server tick enemy status ... navMeshReady=True serverTick=True`、`Server tick enemy attack committed ... attackId=Enemy_Melee` 和 `Server tick enemy attack applied ... health=100->75`；smoke 后公网 health 回落到 `networkConnectedClients=0 networkSpawnedPlayers=0`。这说明最小非 smoke 敌人 server tick 已经在 2 核 2G ECS 公网路径跑通；仍不等于多敌人仇恨、受击动画、掉落、长时间稳定性或正式容量已经完成。

2026-07-07 P6.13 本机进展补充：P6.12 的最小非 smoke 敌人 server tick 已推进到受控连续攻击节奏。`NetworkEnemyAvatar` 现在只让旧 brain smoke bridge 保持一次性攻击，server gameplay tick 路径允许在正式 `EnemyAttackController` 冷却结束后继续接收 `AttackCommitted` 并写入 server-write 玩家 HP；新增规则测试覆盖 server tick 可重复、brain smoke 不重复、敌人死亡后不接收。探针新增 `--min-network-enemy-server-tick-attacks`，可要求服务端日志至少出现 N 条 `Server tick enemy attack applied`。本机 Mac server/client 构建成功；本机 P6.13 smoke 要求至少 2 次 server tick 攻击，实际通过 `P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=3`、`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=2.08 client2ObservedEnemyMoveDistance=2.08`、`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetHealthDrop=25 client2ObservedTargetHealthDrop=25`，最终 `P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8073 healthPort=8074 ... networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。服务端日志确认连续三次 `Server tick enemy attack applied`：`health=100->75`、`75->50`、`50->25`。本阶段尚未构建 Linux 包或部署 ECS；仍未验证多敌人仇恨、目标切换、受击动画、掉落或长时间稳定性。

2026-07-08 P6.14 ECS 部署补充：P6.13 连续敌人 server tick 攻击已构建为 Linux Dedicated Server 包并部署到 2 核 2G ECS。新包 `Builds/DedicatedServer/TYServer-linux-x86_64-p614-server-tick-repeat.tar.gz` 约 `80M`，SHA256 为 `edb24e7644d5a7687b235ae53698613a10f7d3993d92b3b4f23cc7fed61b2625`；ECS 远端 SHA256 校验一致，`ty-new-server.service` 当前以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --enable-network-enemy-server-tick` 启动并保持 `active`。公网验证通过 health、UDP 入站和双客户端 P6.14 smoke：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=5`、`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=2.07 client2ObservedEnemyMoveDistance=3.62`、`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`、`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetHealthDrop=25 client2ObservedTargetHealthDrop=25`，最终 `P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。远端日志确认连续 `Server tick enemy attack applied` 至少 5 次，包含 `health=100->75`、`25->0`、`100->75`、`75->50`、`50->25`；smoke 后公网 health 回落到 `networkConnectedClients=0 networkSpawnedPlayers=0`。这说明连续敌人 server tick 攻击已经在 2 核 2G ECS 公网路径跑通；仍不等于多敌人仇恨、目标选择稳定性、受击动画、掉落、长时间稳定性或正式容量已经完成。

2026-07-08 P6.15 ECS 部署补充：P6.14 的连续敌人 server tick 已推进到最小目标选择/死亡后切换合同并完成公网双客户端验证。`NetworkPlayerAvatar.ApplyServerEnemyDamage()` 现在会同步 formal 玩家子树 `HealthComponent`，让正式 `EnemyBrain` 在目标网络 HP 归零后能通过既有 living-target 检查清除死亡目标并重新感知；`NetworkEnemyAvatar` 新增 `Server tick enemy target acquired` / `Server tick enemy target switched` 服务端日志，其中切换日志记录 `previousTargetDead=True`。探针新增 `--require-network-enemy-target-switch`、`--min-network-enemy-initial-target-attacks` 和 server tick death delay 覆盖参数，要求服务端日志证明初始目标死亡前不切换、死亡后切到另一名 live target。定向 EditMode `DedicatedServerBuildUtilityTests` 通过 `30/30 Passed`；本机 P6.15 smoke 通过 `P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=7`、`P6_NETWORK_ENEMY_TARGET_SWITCH_OK initialTargetOwner=1 initialTargetAttackCount=3 switchedTargetOwner=2 previousTargetDead=true`。Linux 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p615-target-switch.tar.gz` 约 `79M`，SHA256 为 `8ef303b46694a1757e81decba93bcc513dd09a857ca2a36cdcda7cc2671b7b1f`；ECS 远端 SHA256 校验一致，`ty-new-server.service` 以 `--enable-network-enemy-server-tick --network-enemy-server-tick-death-delay-seconds 24` 启动并保持 `active`。公网 P6.15 smoke 通过 health、UDP 入站和双客户端验证：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=7`、`P6_NETWORK_ENEMY_TARGET_SWITCH_OK initialTargetOwner=1 initialTargetAttackCount=3 switchedTargetOwner=2 previousTargetDead=true`，最终 `P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyServerTick=true networkEnemyTargetSwitch=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。远端日志确认先锁定 `targetOwner=1` 并打到 `targetDead=True`，随后 `previousTargetDead=True nextTargetOwner=2`，再继续攻击 `targetOwner=2`；仍不等于多敌人仇恨、受击动画、掉落、长时间稳定性或正式容量已经完成。

2026-07-08 P6.16 本机与 ECS 部署补充：P6.15 的单只敌人目标切换之后，已先补齐多敌人最小生成/身份/客户端可见性合同。`MultiplayerNetworkSessionSettings` 新增 `NetworkEnemyCount` 默认 `1`，`ServerRuntimeBootstrap` 新增 `--network-enemy-count`，服务端会按 count 生成多只 `NetworkEnemyAvatar`，并在 spawn 前写入稳定 `enemyId` 与分散 `spawnPosition`；`NetworkEnemyAvatar.BuildSpawnPosition(1)` 为第二只敌人返回 `2.00,0.00,3.00`，客户端 smoke reporter 已按 `enemyId` 排序输出 `enemyCount` 与 `enemies=`。探针新增 `--network-enemy-count`、`--min-network-enemy-count` 和 count-only formal prefab 跳过选项，可独立校验多敌人可见性而不强制 HP/death/attack smoke。定向 EditMode `DedicatedServerBuildUtilityTests` 通过 `30/30 Passed`；Mac server/client 构建成功；本机 P6.16 smoke 通过 `P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=2 client1ObservedEnemyCount=2 client2ObservedEnemyCount=2 client1EnemyIds=1,2 client2EnemyIds=1,2`，最终 `P3_MULTIPLAYER_OK host=127.0.0.1 gamePort=8081 healthPort=8082 ... networkEnemyCount=true disconnected=0`。Linux 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p616-multi-enemy-count.tar.gz` 约 `79M`，SHA256 为 `f87ecea9e33584722d04aa8c010c511d14f90e7ece4711abaa23b65a5f0f1fe8`；ECS 远端 SHA256 校验一致，`ty-new-server.service` 以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --network-enemy-count 2 --enable-network-enemy-server-tick --network-enemy-server-tick-death-delay-seconds 24` 启动并保持 `active`。公网 P6.16 count-only smoke 通过 `P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=2 client1ObservedEnemyCount=2 client2ObservedEnemyCount=2 client1EnemyIds=1,2 client2EnemyIds=1,2`，最终 `P3_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyCount=true disconnected=0`。远端日志确认 `networkEnemyCount=2`，并生成 `enemyId=1 spawnPosition=-1.00,0.00,5.00` 与 `enemyId=2 spawnPosition=1.00,0.00,5.00`；仍不等于多敌人仇恨目标分配、并发攻击节奏、受击动画、掉落或长时间稳定性完成。

2026-07-08 P6.17 本机进展补充：P6.16 的多敌人生成/可见性之后，已补齐多敌人最小目标分配合同。`probe_p15_multiplayer.py` 新增 `--require-network-enemy-target-distribution` 和 `P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK`，会读取服务端 `Server tick enemy attack applied` 日志，要求多只 enemy 的首次攻击目标覆盖至少两个不同 `targetOwner`，避免把死亡后切换误判成初始目标分配成功。本机 P6.17 smoke 使用 `--network-enemy-count 2 --require-network-enemy-target-distribution` 通过：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=6`、`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=2 uniqueTargetCount=2 enemyTargets=1->1,2->2 enemyAttackCounts=1:3,2:3`，最终 `P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8085 healthPort=8086 ... networkEnemyCount=true networkEnemyServerTick=true networkEnemyTargetDistribution=true networkEnemyAttackSync=true disconnected=0`。这说明本机 server tick 下两只敌人能分别锁定并攻击不同玩家；本阶段尚未构建新的 Linux 包或做 ECS 公网 P6.17 复验，也仍不等于多敌人长期仇恨、目标去重策略、并发攻击平衡、受击动画或掉落完成。

2026-07-08 P6.18 ECS 验证补充：P6.18 未产出新的 Linux 包，复用当前 ECS 上已部署的 P6.16 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p616-multi-enemy-count.tar.gz`（SHA256 `f87ecea9e33584722d04aa8c010c511d14f90e7ece4711abaa23b65a5f0f1fe8`）和运行参数 `--network-enemy-count 2 --enable-network-enemy-server-tick --network-enemy-server-tick-death-delay-seconds 24`，补齐公网多敌人目标分配验证。`verify_p15_ecs_multiplayer.sh` 已支持 `TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_DISTRIBUTION=1`，且该模式不会强制旧的客户端敌人 HP/death 同步断言，只校验 health、UDP 入站、多敌人可见性、NavMesh server tick 和服务端日志中的目标分配。公网双客户端 smoke 已通过：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=2 client1ObservedEnemyCount=2 client2ObservedEnemyCount=2 client1EnemyIds=1,2 client2EnemyIds=1,2`、`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=6`、`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=2 uniqueTargetCount=2 enemyTargets=1->1,2->2 enemyAttackCounts=1:3,2:3`，最终 `P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyCount=true networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true networkEnemyTargetDistribution=true disconnected=0`。这说明 P6.17 的多敌人目标分配合同已在 2 核 2G ECS 公网路径跑通；仍不等于长期仇恨、目标去重策略、并发攻击平衡、受击动画、掉落、长时间稳定性或正式容量已经完成。

2026-07-08 P6.19 本机进展补充：P6.18 的“首次目标分配”之后，已新增短窗口目标保持/去重合同。`probe_p15_multiplayer.py` 新增 `--require-network-enemy-target-retention` 和 `P6_NETWORK_ENEMY_TARGET_RETENTION_OK`，会解析服务端 `Server tick enemy attack applied` 日志，要求多只 enemy 在前 N 次攻击内持续攻击同一个仍存活的 target，同时多只 enemy 的 retained target 仍覆盖至少两个不同玩家。本机 P6.19 smoke 使用两只 server tick 敌人和 `--min-network-enemy-target-retention-attacks 3` 通过：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=6`、`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=2 uniqueTargetCount=2 enemyTargets=1->1,2->2 enemyAttackCounts=1:3,2:3`、`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=2 minRetainedAttacks=3 uniqueTargetCount=2 enemyTargets=1->1,2->2 retainedAttackCounts=1:3,2:3`，最终 `P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8095 healthPort=8096 ... networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0`。这说明本机短窗口内两只敌人没有抢同一个 live target，也没有在 retained window 内抖动切换；P6.19 当阶段尚未构建新的 Linux 包或做 ECS 公网复验，该缺口已由 P6.20 补上。本合同仍不等于长时间仇恨、并发攻击平衡、受击动画、掉落或正式容量完成。

2026-07-08 P6.20 ECS 验证补充：P6.20 未产出新的 Linux 包，复用当前 ECS 上已部署的 P6.16 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p616-multi-enemy-count.tar.gz`（SHA256 `f87ecea9e33584722d04aa8c010c511d14f90e7ece4711abaa23b65a5f0f1fe8`）和运行参数 `--network-enemy-count 2 --enable-network-enemy-server-tick --network-enemy-server-tick-death-delay-seconds 24`，补齐公网短窗口目标保持验证。`verify_p15_ecs_multiplayer.sh` 已支持 `TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_RETENTION=1` 和 `TY_NEW_MIN_NETWORK_ENEMY_TARGET_RETENTION_ATTACKS`；该模式会自动启用 target distribution、server tick、NavMesh 日志和至少两只敌人，并把 `min-network-enemy-server-tick-attacks` 提升到 `enemy_count * retention_attacks`。公网双客户端 smoke 已通过：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=2 ... client1EnemyIds=1,2 client2EnemyIds=1,2`、`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=6`、`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=2 uniqueTargetCount=2 enemyTargets=1->1,2->2 enemyAttackCounts=1:3,2:3`、`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=2 minRetainedAttacks=3 uniqueTargetCount=2 enemyTargets=1->1,2->2 retainedAttackCounts=1:3,2:3`，最终 `P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0`。这说明 P6.19 的短窗口目标保持合同已在 2 核 2G ECS 公网路径跑通；仍不等于更长窗口仇恨、并发攻击平衡、受击动画、掉落、长时间稳定性或正式容量已经完成。

2026-07-09 P6.21 探索补充：P6.21 尝试把目标保持窗口从每只敌人连续 3 次攻击提升到 4 次攻击，并调整探针允许 retained window 的最后一击是击杀击，避免把合理的 `25->0 targetDead=True` 误判为掉目标。本阶段没有产出新的 Linux 包；ECS 公网两次探索均只得到每只敌人 3 次 `Server tick enemy attack applied`，失败为 `expected>=8 actual=6`。临时把 ECS systemd 的 `--network-enemy-server-tick-death-delay-seconds` 从 `24` 调到 `45` 后结果不变，说明瓶颈不是 death delay，而是当前 formal AI/bridge 在该 smoke 中自然只产出每只敌人 3 次 attack commit。临时参数已恢复为 `24` 并重启，health 复查为 `status=ok`、`networkListening=true`。P6.21 未通过，P6.20 仍是当前最后一个通过的 ECS 多敌人目标保持里程碑。

2026-07-09 P6.22 进展补充：P6.22 已修复 P6.21 暴露的四击目标保持缺口，并完成本机与 2 核 2G ECS 公网验证。修复点是 server gameplay tick 专用 fallback：当 formal `EnemyAttackController.TryAttack` 未提交，但服务端确认 cooldown ready、formal target 在攻击范围内且 clear shot 成立时，由服务端权威路径消耗 `EnemyAttackController` cooldown 并写入网络玩家 HP；该路径只在 `serverEnemyGameplayTickEnabled && serverEnemyGameplayTickActive` 下生效。定向 EditMode `DedicatedServerBuildUtilityTests = 32/32 Passed`；本机 smoke 与 ECS 公网 smoke 均通过 `P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=8`、`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK ... enemyAttackCounts=1:4,2:4` 和 `P6_NETWORK_ENEMY_TARGET_RETENTION_OK minRetainedAttacks=4 ... retainedAttackCounts=1:4,2:4`。新 Linux 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p622-target-retention.tar.gz` 约 `80M`，SHA256 `df6617f7963d7ec229b52ccb721d7db1dd072ee43b5db5a9aff5864586d3ebb8`，已部署 ECS 且远端 SHA256 校验一致。远端日志确认第 4 击为 `attackId=ServerGameplayTickFallback health=25->0 targetDead=True`，smoke 后 health 回落到 `networkConnectedClients=0 networkSpawnedPlayers=0`。这说明 P6.22 已把两只 server tick 敌人的目标保持窗口推进到四击击杀；仍不等于完整正式敌人攻击动画、受击硬直、仇恨系统、掉落、长时间稳定性或正式容量完成。

2026-07-09 P6.23 进展补充：P6.23 已把目标保持验证从两只 server tick 敌人扩到三只，并完成本机与 2 核 2G ECS 公网验证。新增 `--network-enemy-server-tick-damage`，默认仍为 `25`，P6.23 smoke 使用 `10`，避免三只敌人在 `3 * 4` 次保留窗口打满前把两名 `100` HP 玩家过早击杀；server gameplay tick 伤害结算现在会优先保留上一轮攻击的 live `targetOwner`，避免第三只敌人在 retained window 内因 formal AI 近距离抖动而换目标。定向 EditMode `DedicatedServerBuildUtilityTests = 32/32 Passed`；本机和 ECS 公网 smoke 均通过 `P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=3`、`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=20`、`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=3 uniqueTargetCount=2 enemyTargets=1->1,2->2,3->2` 和 `P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=3 minRetainedAttacks=4 ... retainedAttackCounts=1:4,2:4,3:4`。新 Linux 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p623-three-enemy-retention.tar.gz` 约 `79M`，SHA256 `29ab4ba9ea03251be40ba92d756838aec050e3aebf71eeeec8264df200b92edf`，已部署 ECS 且远端 SHA256 校验一致；`ty-new-server.service` 当前使用 `--network-enemy-count 3 --network-enemy-server-tick-damage 10 --network-enemy-server-tick-death-delay-seconds 90`。这说明 2 核 2G ECS 可承载当前三敌人四击目标保持 smoke；仍不等于超过三只敌人、长时间仇恨、正式平衡、掉落、断线重连或正式容量完成。

2026-07-09 P6.24 进展补充：P6.24 不新增代码或新包，复用 P6.23 ECS 部署，完成同一 ECS server 进程连续两轮公网三敌人四击目标保持 smoke。两轮启动前 health 分别为 `uptimeSeconds=977.8` 和 `1104.9`，都保持 `networkConnectedClients=0 networkSpawnedPlayers=0`；两轮都通过 `P1.5_UDP_INGRESS_OK`、`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=3`、`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=20`、`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK` 和 `P6_NETWORK_ENEMY_TARGET_RETENTION_OK ... retainedAttackCounts=1:4,2:4,3:4`，最终均输出 `P6_MULTIPLAYER_OK ... networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0`。第 1 轮目标为 `enemyTargets=1->3,2->4,3->4`，第 2 轮目标为 `enemyTargets=1->5,2->6,3->6`，说明服务未重启而是连续接收新会话。重复验证后 health-only 复查为 `uptimeSeconds=1235.8`、`networkConnectedClients=0 networkSpawnedPlayers=0`。这说明 P6.23 的公网结果可在同一进程里重复复现；仍不等于长时间压测、并发压测、正式容量、断线重连、掉落或正式平衡完成。

2026-07-09 P6.25 进展补充：P6.25 不新增代码或新包，复用 P6.23 ECS 部署，临时把 ECS service 从三只敌人提升到四只敌人做目标保持探索。四敌人 + `--network-enemy-server-tick-damage 10` 明确失败：health、UDP 入站、四敌人可见性和 NavMesh 均通过，但 target retention 因 `enemyId=3 expected>=4 actual=3` 未满足四击保持，说明两名 `100` HP 玩家在该伤害下承压过早。随后四敌人 + `--network-enemy-server-tick-damage 5` 通过公网 smoke：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=4`、`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=40`、`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK ... enemyTargets=1->1,2->2,3->2,4->1` 和 `P6_NETWORK_ENEMY_TARGET_RETENTION_OK ... retainedAttackCounts=1:4,2:4,3:4,4:4`。探索结束后 ECS 已恢复为稳定三敌人 + tick damage `10`，health-only 复查为 `connected=0 spawned=0`。这说明 2 核 2G ECS 可以跑通四敌人低伤害 smoke，但不能把四敌人 `10` 伤害、正式平衡或容量结论视为已通过。

2026-07-10 P6.26 进展补充：P6.26 不新增 Unity runtime 代码或 Linux 包，新增 `Deploy/DedicatedServer/verify_p625_ecs_four_enemy_retention.sh`，把 P6.25 的手工 ECS service 参数探索固化为可复跑脚本。脚本会先检查远端 `ty-new-server.service` 是否处于 P6.23 稳定基线（三敌人、tick damage `10`、death delay `90`），再备份 service、临时切到四敌人 + tick damage `5`，运行公网目标保持 smoke，最后恢复原 service 并做 health-only。脚本化 ECS 验证已通过：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=4`、`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=40`、`P6_NETWORK_ENEMY_TARGET_RETENTION_OK ... retainedAttackCounts=1:4,2:4,3:4,4:4`，最终 `P6_MULTIPLAYER_OK ... networkEnemyTargetRetention=true disconnected=0`；恢复后 service 回到三敌人 + tick damage `10`，health-only 为 `connected=0 spawned=0`。这提高了 P6.25 复验安全性，但仍不代表四敌人 `10` 伤害或正式容量已通过。

2026-07-10 P6.27 进展补充：已新增 `Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh`，把 enemy count、tick damage、death delay、retention attacks 和双客户端时长参数化，并对 unit 文件/effective `ExecStart`、重复 option、持久互斥锁、完整 service 备份、失败恢复和恢复后 health 建立统一合同。配套离线契约测试已在 sh/dash 下通过；真实 ECS 首轮复验使用四敌人 + tick damage `5`，通过四敌可见性、server tick、目标分配与 `retainedAttackCounts=1:4,2:4,3:4,4:4`，随后恢复三敌人 + damage `10`，health 回到 `connected=0 spawned=0`，备份与锁均已清理。

2026-07-10 P6.28 进展补充：P6.28 不新增 Unity runtime 或 Linux 包，复用 P6.23 包和 P6.27 通用工具完成五敌人 + tick damage `5` 的公网 smoke。双客户端都观察到 enemyId `1,2,3,4,5`；`serverTickAttackCount=40`；目标分布覆盖两个玩家，`enemyTargets=1->1,2->2,3->2,4->1,5->2`；五只敌人都达到 `retainedAttackCounts=1:4,2:4,3:4,4:4,5:4`。工具随后恢复三敌人 + damage `10` + delay `90`，health 为 `connected=0 spawned=0`，锁和备份已清理。这只是独立 multiplayer spike 的五敌低伤害单轮 smoke，不改变第一章正式范围，也不代表五敌常规伤害、正式平衡或容量。

2026-07-10 P6.29 进展补充：通用工具增加 `--rounds`、逐轮独立日志、每轮前后临时 `MainPID` 守门和轮末 health-only。真实 ECS 只安装一次五敌人 + tick damage `5` 临时 service，并在相同 PID `109706` 下连续两轮通过：两轮均有 `serverTickAttackCount=40`，五敌均达到 `retainedAttackCounts=1:4,2:4,3:4,4:4,5:4`，每轮结束均为 `connected=0 spawned=0`。最终恢复三敌人 + damage `10` + delay `90`，独立复查 health `0/0` 且锁目录不存在。该结果只证明短窗口双轮可复现和会话清理，不代表长时间稳定、五敌常规伤害或容量。

2026-07-10 P6.30 进展补充：探针新增 retention 失败结构化诊断，并在真实 ECS 五敌人 + tick damage `10` 下复验。四击合同要求 20 次攻击，实际也发生 20 次且两名玩家各承受 100 伤害后死亡；但 per-enemy 次数为 `1:6,2:5,3:3,4:4,5:2`，敌人 3/5 合计缺 3 次，正好由敌人 1/2 多出的 3 次抵消。诊断分类为 `health_budget_exhausted_with_uneven_enemy_scheduling`。这把失败定位为当前 smoke 的生命预算与攻击调度问题，不是 ECS 容量失败；五敌常规伤害仍未通过。实验后恢复三敌 + damage `10` + delay `90`，独立复查 health `0/0`、锁清理通过。

2026-07-10 P6.31 收口补充：新增不修改 service 的 `verify_p631_p6_closure.sh`。真实 ECS 直接在三敌、damage `10`、delay `90` 常驻进程上通过三敌四击回归：`serverTickAttackCount=20`、`enemyTargets=1->1,2->2,3->2`、`retainedAttackCounts=1:4,2:4,3:4`。回归前后 MainPID 均为 `112305`，health 均为 `0/0`，unit/effective baseline 与锁状态无漂移，最终输出 `P6_CLOSURE_OK p6Status=closed`。P6 至此关闭；公平调度、五敌常规伤害、长压测、容量和断线重连如需继续必须另开 P7。

可接受目标是：另开 multiplayer spike 分支，先做局域网 Dedicated Server 原型，再把最小 CombatTest 服务端上传到 2 核 2G ECS 做空跑和 2-4 人小房间验证。

2 核 2G ECS 只允许作为 P0-P6.31 的已部署工程验证环境，P6 已关闭。当前稳定常驻配置是三敌人 + tick damage `10` + delay `90`；五敌 damage `5` 仅为探索通过，五敌 damage `10` 为已知限制。P6.31 的同 PID closure smoke 仍不应作为正式公网动作联机服务器容量结论。

## 2. 当前事实约束

| 约束项 | 当前事实 | 影响 |
|---|---|---|
| Unity 版本 | `ProjectSettings/ProjectVersion.txt` 为 Unity 6000.4.2f1 | 后续网络包、Dedicated Server profile、CI 命令都必须按该版本验证 |
| 网络栈 | `Packages/manifest.json` 已加入 Netcode for GameObjects `2.6.0` 和 Unity Transport `2.6.0`；P1 TCP 行协议仍只作为探针 | 不能把 P1 行协议当最终联机同步方案，P1.5 NGO/UTP 也只是最小连接骨架，正式同步仍需逐步验证 |
| 构建目标 | `ReleaseCandidateBuildUtility` 仍只支持 macOS 和 Windows；`DedicatedServerBuildUtility` 已支持 Linux Dedicated Server | 多人服务端必须继续走 ServerBoot/Linux Server 专用构建链，不得上传普通客户端包 |
| 玩家输入 | `PlayerCharacter.Update()` 直接读取 `InputReader.MoveValue` 并 tick 本地 motor/state machine | 多人版本必须拆成客户端输入采集和服务端模拟 |
| 命中结算 | `AttackExecutor` 使用本地 `Physics.OverlapSphere/OverlapBox` 并直接调用 `ReceiveDamage()` | 攻击命中和扣血必须迁移到服务端权威 |
| 敌人 AI | `EnemyBrain.Update()` 本地 tick AI/attack，目标是 `Transform` | 多人版本必须改为服务端 actor/network id 驱动 |
| 素材边界 | `GhostSamurai_Animset` 等目录在 `Docs/Asset_Source_List.md` 中定义为 local preview only | 服务端包不得依赖 local-preview-only raw assets |

## 3. 禁止项

不得把现有普通 Windows/macOS 客户端 build 上传到 ECS 后称为服务端。

不得让客户端直接决定最终位置、最终血量、命中结果、敌人目标、掉落、经验、金币、任务进度或存档。

不得以客户端发来的 `hit`、`damage`、`target`、`position` 作为最终事实，只能作为可校验的输入意图或表现请求。

不得把 `Assets/GhostSamurai_Animset/`、`Assets/Kevin Iglesias/`、`Assets/DoubleL/`、`Assets/ithappy/`、`Assets/JC_LP_MedievalCharacters_LITE/` 等 local preview 目录作为服务端正式依赖。

不得在 2 核 2G 机器上同时堆 Unity server、数据库、Web 后台、日志平台、多个房间进程和复杂监控 agent，再用结果判断游戏服务端容量。

不得在 P0/P1 阶段做账号、匹配、排行榜、完整背包、正式商业化存档或大地图多人同步。

## 4. 服务端架构约束

服务端必须是 Linux Dedicated Server/headless 形态。构建链需要支持 `BuildTarget.StandaloneLinux64` 和 `StandaloneBuildSubtarget.Server`，运行时需要支持 `-batchmode`、`-nographics`、日志路径、端口、目标 tick/framerate 等启动参数。

服务端代码必须通过 `UNITY_SERVER` 或等价 runtime 分支隔离客户端专用系统，包括 Camera、Cinemachine、PostProcessing、UGUI、Input System、Audio、客户端存档 UI、本地预览逻辑。

第一版只允许以 CombatTest 为最小联机闭环，不允许直接把完整章节、剧情、Timeline、完整 UI 和所有美术资源搬进服务端场景。

服务端场景应只保留模拟必需内容：碰撞、导航、玩家 server prefab、基础攻击数据、少量敌人和必要的 server bootstrap。

网络对象不能长期依赖裸 `Transform` 引用表达身份。玩家、敌人、投射物、召唤物、掉落物至少需要稳定的 `NetworkObjectId`、server entity id 或 ActorId。

状态同步必须区分权威状态和表现状态。HP/MP/stamina/cooldown/buff/death 是服务端权威；动画、特效、伤害数字、相机震动、音效是客户端表现。

存档必须从客户端本地 JSON 转为服务端权威存储。原型期可先使用服务端 JSON/SQLite；正式测试前应明确 player id、版本迁移、备份和坏档恢复策略。

## 5. 网络技术约束

默认首选 Unity Netcode for GameObjects + Unity Transport 做原型，因为它贴近当前 GameObject/MonoBehaviour 工作流，并且与 Unity Dedicated Server 路线一致。

FishNet 可以作为 P2/P3 之后的替代评估项，尤其当客户端预测、同步手感或 NGO 开发成本明显不合适时再切换。

Mirror 可作为快速上手备选，但必须单独验证 Unity 6000.4.2f1 兼容性。

第一版不得自写底层 UDP 协议，除非目标明确转为网络底层学习项目。当前优先级是跑通权威移动、攻击命中和部署链路。

## 6. 2 核 2G ECS 容量约束

2 核 2G ECS 只作为工程验证环境，初始假设如下：

| 阶段 | 2 核 2G 适配性 | 约束 |
|---|---|---|
| P0 空跑 | 可以 | 单 Unity server 进程，ServerBoot/空场景，记录 CPU/内存/日志 |
| P1 最小连接骨架 | 可以 | TCP `7777` 接受连接，维护房间/玩家计数，支持 HELLO/PING/STATE 探针 |
| P1.5 双人同房间 | 可以 | 两个客户端连接同一服务器并互相可见，只同步位置和朝向，少量对象 |
| P2 权威移动/血量 | 大概率可以 | 2 人优先，4 人为上限验证 |
| P3 攻击命中 | 可验证 | 最小骨架已通过 2 人本机/ECS smoke；后续接正式 CombatTest 判定时仍需关注 Physics 查询、GC、tick cost |
| P4 死亡同步 | 可验证 | 最小骨架已通过 2 人本机/ECS smoke；后续接正式死亡动画、复活和 UI 时仍需关注状态机一致性 |
| P5 正式网络玩家 prefab | 可验证 | formal CombatTest 玩家子树已通过本机与 ECS attack/hit/death smoke |
| P6.1 网络敌人事实同步 | 可验证 | 本机与 ECS 公网均已通过一只 server-owned 网络敌人位置/HP/死亡同步 |
| P6.2 formal 网络敌人 prefab | 可验证 | 本机已通过 formal CombatTest 敌人子树 HP/death 投射和客户端 AI/NavMesh/攻击驱动 suppressed；尚未 ECS 部署 |
| P6.3 最小敌人攻击事实 | 可验证 | 本机已通过服务端写玩家 HP、formal 敌人攻击表现和客户端只观察的专项 smoke；尚未 ECS 部署，尚未接真实 `EnemyBrain`/NavMesh 攻击 tick |
| P6.4 ECS 敌人攻击事实 | 可验证 | P6.3 server-authored 敌人攻击 smoke 已构建 Linux 包并通过 ECS 公网双客户端验证；仍未接真实 `EnemyBrain`/NavMesh 攻击 tick |
| P6.5 formal 敌人攻击提交 | 本机可验证 | 本机已通过 formal `EnemyAttackController.TryAttack` 提交后写网络 HP；尚未 ECS 部署 |
| P6.6 EnemyBrain 敌人攻击提交 | 本机可验证 | 本机已通过服务端 formal `EnemyBrain` 自动选中目标并进入攻击态后的 smoke bridge 提交；尚未 ECS 部署，尚未验证 NavMesh 距离外追击或非 smoke 攻击循环 |
| P6.7 ECS EnemyBrain 敌人攻击提交 | 可验证 | P6.6 brain smoke 已构建 Linux 包并通过 ECS 公网双客户端验证；仍只是 smoke bridge，不代表完整 NavMesh 追击或非 smoke 攻击循环 |
| P6.8 EnemyBrain 距离外追击提交 | 本机可验证 | 本机已通过 formal 敌人从 `-1,0,5` 追击到攻击范围、同步 network enemy 位移并提交攻击；当前是 ServerBoot 无有效 NavMesh 时的 `EnemyMotor` fallback 追击，尚未 ECS 部署 |
| P6.9 ServerBoot baked NavMesh 追击 | 本机可验证 | ServerBoot 已包含 baked NavMesh；本机已通过服务端 `navMeshReady=True`、无服务端 fallback warning、network enemy 位移同步和攻击提交；尚未 ECS 部署 |
| P6.10 ECS baked NavMesh 追击 | 可验证 | P6.9 baked NavMesh chase smoke 已构建 Linux 包并通过 ECS 公网双客户端验证；仍是专项 smoke，不代表非 smoke 敌人 gameplay tick |
| P6.11 非 smoke 敌人 server tick | 本机可验证 | 本机已通过最小 server formal enemy tick 的 `navMeshReady=True` 追击、攻击提交和网络同步；尚未 ECS 部署 |
| P6.12 ECS 非 smoke 敌人 server tick | 可验证 | P6.11 server tick 已构建 Linux 包并通过 ECS 公网双客户端验证；仍只覆盖单只敌人和首个 attack commit，不代表正式容量 |
| P6.13 连续敌人 server tick 攻击 | 本机可验证 | 本机已通过服务端连续 3 次 `Server tick enemy attack applied` 和双客户端同步验证；尚未构建 Linux 包或部署 ECS |
| P6.14 ECS 连续敌人 server tick 攻击 | 可验证 | P6.13 连续攻击已构建 Linux 包并通过 ECS 公网双客户端验证，服务端日志计数 `serverTickAttackCount=5`；仍只覆盖单只敌人和短时 smoke |
| P6.15 敌人目标死亡后切换 | 可验证 | P6.15 已构建 Linux 包并通过 ECS 公网双客户端验证：`P6_NETWORK_ENEMY_TARGET_SWITCH_OK initialTargetOwner=1 initialTargetAttackCount=3 switchedTargetOwner=2 previousTargetDead=true`；仍只覆盖单只敌人和短时 smoke |
| P6.16 多敌人生成/身份/可见性 | 可验证 | P6.16 已构建 Linux 包并通过 ECS 公网 count-only 双客户端 smoke：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=2 ... client1EnemyIds=1,2 client2EnemyIds=1,2`；目标分配已由 P6.18 补上公网验证 |
| P6.17 多敌人目标分配 | 本机可验证 | 本机已通过两只 server tick 敌人首次攻击目标覆盖两个玩家：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK ... enemyTargets=1->1,2->2`；尚未构建 Linux 包或部署 ECS |
| P6.18 ECS 多敌人目标分配 | 可验证 | P6.18 复用 P6.16 已部署包完成 ECS 公网双客户端验证：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK ... enemyTargets=1->1,2->2`；仍不代表长期仇恨、并发攻击平衡或正式容量 |
| P6.19 多敌人短窗口目标保持 | 本机可验证 | 本机已通过两只 server tick 敌人各自连续 3 次攻击保持不同 live target：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK ... retainedAttackCounts=1:3,2:3`；ECS 公网复验已由 P6.20 补上 |
| P6.20 ECS 多敌人短窗口目标保持 | 可验证 | P6.20 复用 P6.16 已部署包完成 ECS 公网双客户端验证：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK ... retainedAttackCounts=1:3,2:3`；仍不代表更长窗口仇恨或正式容量 |
| P6.21 四击目标保持 | 未通过 | ECS 探索要求每只敌人连续 4 次保持目标，结果两次都是 `expected>=8 actual=6`，每只敌人只产出 3 次 `Server tick enemy attack applied`；需要先修 runtime 攻击循环或服务端重复提交路径 |
| P6.22 ECS 四击目标保持 | 可验证 | P6.22 已通过本机与 ECS 公网双客户端验证：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minRetainedAttacks=4 ... retainedAttackCounts=1:4,2:4`；第 4 击由 `ServerGameplayTickFallback` 完成 `25->0` 击杀 |
| P6.23 ECS 三敌人四击目标保持 | 可验证 | P6.23 已通过本机与 ECS 公网双客户端验证：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=3`、`P6_NETWORK_ENEMY_SERVER_TICK_OK ... serverTickAttackCount=20`、`P6_NETWORK_ENEMY_TARGET_RETENTION_OK ... retainedAttackCounts=1:4,2:4,3:4`；smoke 使用 `--network-enemy-server-tick-damage 10`，仍不代表正式平衡或容量 |
| P6.24 ECS 重复三敌人目标保持 | 可验证 | P6.24 复用 P6.23 包和 systemd 参数，在同一 ECS server 进程连续两轮通过三敌人四击目标保持 smoke；两轮后 health-only 复查为 `connected=0 spawned=0`，仍不代表长时间压测或并发容量 |
| P6.25 ECS 四敌人四击目标保持 | 可验证 | P6.25 复用 P6.23 包临时切到四只敌人；tick damage `10` 因 `enemyId=3 expected>=4 actual=3` 失败，tick damage `5` 通过 `retainedAttackCounts=1:4,2:4,3:4,4:4`；当前 ECS 已恢复三敌人 + tick damage `10`，仍不代表正式平衡或容量 |
| P6.26 ECS 四敌人脚本化复验 | 可验证 | 新增 `verify_p625_ecs_four_enemy_retention.sh`，会备份/临时覆盖/恢复远端 service；公网脚本化复验通过四敌人 + tick damage `5` 的四击目标保持，恢复后 health-only 为 `connected=0 spawned=0` |
| P6.27 ECS 通用临时参数工具 | 可验证 | `verify_ecs_network_enemy_retention_with_temp_service.sh` 已用四敌人 + tick damage `5` 在 ECS 跑通 unit/effective baseline 检查、持久备份、临时覆盖、目标保持、恢复 health 与锁清理 |
| P6.28 ECS 五敌人低伤害目标保持 | 可验证 | 五敌人 + tick damage `5` 单轮公网 smoke 通过 `serverTickAttackCount=40`、两个目标分布和 `retainedAttackCounts=1:4,2:4,3:4,4:4,5:4`；恢复后仍是三敌人 + damage `10`，不代表正式容量 |
| P6.29 ECS 五敌人同进程双轮目标保持 | 可验证 | 通用工具新增 `--rounds` 与临时 `MainPID` 守门；五敌人 + damage `5` 在相同 PID 下连续两轮通过四击保持，每轮结束 health `0/0`，随后恢复三敌常驻基线；不代表长时间稳定或容量 |
| P6.30 ECS 五敌人常规伤害失败诊断 | 已定位 | damage `10` 下总攻击数达到所需 20 次，但两名玩家各耗尽 100 HP；攻击分布 `1:6,2:5,3:3,4:4,5:2` 使敌人 3/5 未完成四击，分类为生命预算耗尽加调度不均，不是容量失败 |
| P6.31 P6 总收口 gate | 已关闭 | 三敌常驻 service 在同一 PID `112305` 下通过最终四击回归，前后 health `0/0`、unit/effective baseline 与实验锁无漂移；输出 `P6_CLOSURE_OK p6Status=closed` |
| P6.x 敌人 AI 同步 | 开始吃紧 | 少量敌人，服务端 `EnemyBrain`、NavMesh/AI tick 必须受控 |
| P7 公网压测 | 不适合作正式结论 | 只能做小样本，不得代表正式容量 |

2 核 2G 验证环境的默认运行边界：

- 1 个 Unity server 进程
- 1 个房间
- 2-4 名玩家
- 少量敌人
- 20Hz 或 30Hz server tick
- 不同机部署数据库、后台和日志服务，或暂时不部署

满足任一条件应升配到至少 4 核 4G/4 核 8G：

- 常驻内存超过 1.2-1.4G
- CPU 长时间超过 60%-70%
- 30Hz tick 经常超过 33ms
- 20Hz tick 经常超过 50ms
- GC spike 明显影响 tick
- 需要多个房间
- 需要公网长期 playtest
- 需要同机运行数据库、房间管理、日志或指标服务

## 7. 分阶段准入门槛

| 阶段 | 目标 | 进入下一阶段前必须满足 |
|---|---|---|
| P0 Linux Dedicated Server 空跑 | ECS 上 headless 启动最小服务端 | 30-60 分钟不崩；无 graphics/input/audio 依赖错误；CPU/内存/日志可观测 |
| P1 最小连接骨架 | 服务端 `7777` 接受连接并维护最小房间状态 | HELLO/PING/STATE/QUIT 正常；health 返回 gameplay 计数；断开后 active player/connection 归零 |
| P1.5 CombatTest 双人同房间 | 两个客户端连接同一服务器并互相可见 | 本机 server/client 已验证 connect/disconnect、server spawn/despawn 与基础位置同步；ECS Linux 包已实跑且 systemd/health/UDP 监听通过；公网双客户端连接、互相可见、despawn 和基础位置同步已通过 |
| P2 服务端权威移动和血量 | 客户端只发输入/HP 意图，服务端决定位置和 HP | NetworkPlayerAvatar 最小骨架已通过本机与 ECS smoke：服务端 tick 推进位置/朝向，HP server-write 同步，过量客户端 HP 意图被 clamp；正式 CombatTest 角色/战斗仍需后续阶段接入 |
| P3 服务端权威攻击命中 | 服务端决定命中和伤害 | NetworkPlayerAvatar 最小骨架已通过本机与 ECS smoke：客户端只发攻击意图，服务端验证序号/冷却/范围/朝向并固定写入 `25` 伤害；正式 CombatTest 连招/受击仍需后续阶段接入 |
| P4 服务端权威死亡同步 | 服务端决定 HP 归零后的死亡事实 | NetworkPlayerAvatar 最小骨架已通过本机与 ECS smoke：4 次 `Light_01` 由服务端结算 `100->0`，客户端观察到 `death=false->true`；正式 CombatTest 死亡动画、复活、UI 和状态机接入仍需后续阶段 |
| P5 正式网络玩家 prefab | 用 formal CombatTest 玩家子树承载当前 NetworkPlayerAvatar 同步骨架 | 本机 Mac server/client 与 ECS 公网均已通过 formal attack/hit/death 专项 smoke；formal 子树 `PlayerAttackState`、`PlayerHitState`、`PlayerDeathState` 都由服务端认可/权威事实驱动 |
| P6 敌人同步 | 敌人只在服务端思考，客户端只显示 | 已于 P6.31 关闭：三敌 damage `10` 常驻基线在同 PID 下通过最终四击回归，前后 health、参数和锁无漂移；五敌 damage `5` 作为探索通过，五敌 damage `10` 已由 P6.30 定位但未通过。公平调度、更多敌人、长时间仇恨、受击动画、掉落、断线重连和容量全部移出 P6；如需继续必须另开 P7 |
| P7 部署与压测 | 像真实服务端一样运行和恢复 | systemd/Docker、日志轮转、崩溃重启、指标、bot/压测客户端齐备 |

任何阶段没有达到准入门槛，不得继续扩玩法范围。

## 8. MVP 范围约束

多人 MVP 只做 CombatTest 小房间。

MVP 必须包含：

- 服务器启动和端口配置
- 客户端连接服务器
- 玩家 spawn/despawn
- 两名玩家互相可见
- 服务端权威移动
- 服务端权威 HP
- 一种基础攻击的服务端命中和扣血
- 断线清理
- 最小日志和错误输出

MVP 不包含：

- 正式账号系统
- 匹配系统
- 排行榜
- 完整背包/装备经济
- 大地图无缝同步
- 完整剧情章节联机
- 大量敌人
- 正式反作弊
- 正式数据库架构

## 9. 测试约束

现有 EditMode/PlayMode 测试只能证明单机逻辑，没有证明多人正确性。

多人改造必须新增以下测试或验证脚本：

- P1 TCP 握手/心跳/状态探针测试
- 多客户端连接/断开测试
- 延迟和丢包模拟测试
- 重复输入包测试
- 迟到输入包测试
- 伪造 RPC/非法命中测试
- 服务端 tick 稳定性测试
- 服务端长时间 soak test
- 客户端表现与服务端事实一致性测试

任何声称 2 核 2G 可用的结论，都必须附带 CPU、内存、GC、tick cost、玩家数、敌人数、带宽、RTT、丢包率和运行时长。

## 10. 上云部署约束

ECS 安全组只开放必要端口。SSH 和游戏端口分开管理，公网测试前必须记录端口、协议、来源限制和回滚办法。

服务端进程必须具备可重启能力。P5 前至少要有 systemd 或 Docker 管理、日志路径、崩溃退出码、构建版本号和构建 hash。

服务端包必须做资源剥离和场景裁剪，不能把完整客户端资源包直接作为 Linux server 包上传。

P1 当前部署包 `Builds/DedicatedServer/TYServer-linux-x86_64-p1-gameplay.tar.gz` 已在授权后上传 ECS 并完成最小连接验证。该包只应视为最小连接骨架验证包，因为它包含 Unity 服务端二进制、脚本程序集和服务端资源，不应被当作正式多人服务端发布包。

P1.5 历史 Linux 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p15-ngo.tar.gz` 已完成本机构建、本机双客户端验证和 ECS 公网双客户端验证，SHA256 为 `a07c3aaa5f385e5a86a163800a05dcb4fc1df6adc997f30107a36029e0b97c89`。该包只代表 P1.5 最小 NGO/UTP 连接骨架；当前远端服务已由 P3 包替换。

P2 历史 Linux 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p2-authoritative.tar.gz` 已完成本机构建、本机双客户端验证和 ECS 公网双客户端验证，SHA256 为 `fe70281161abbeae6c8e3876d902cae7af71dd3a8676c5b77a7012b804d7a495`。该包只代表服务端权威移动和 server-write HP 骨架；当前远端服务已由 P3 包替换。

P3 历史 Linux 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p3-attack.tar.gz` 已完成本机构建、本机双客户端验证和 ECS 公网双客户端验证，SHA256 为 `f80db23c1eddadf6571765e7cc37384b5b699238abedd2dd0a628d6683747266`。该包只代表服务端固定攻击命中骨架；当前远端服务已由 P3.5 包替换。

P3.5 历史 Linux 包仍使用 `Builds/DedicatedServer/TYServer-linux-x86_64-p3-attack.tar.gz` 包名，已完成本机构建、本机双客户端验证和 ECS 公网双客户端验证，SHA256 为 `f240c118570e36a2186daa4a015b5affdfbca0342ec85a52abf399254531a43a`。该包只代表服务端攻击配置白名单骨架；当前远端服务已由 P4 包替换。

P4 历史 Linux 包仍使用 `Builds/DedicatedServer/TYServer-linux-x86_64-p3-attack.tar.gz` 包名，已完成本机构建、本机双客户端验证和 ECS 公网双客户端验证，SHA256 为 `b56285d618d6812750db3778a9afa4f93d601911787cd6e7e9ac2a59cb1ca812`。该包只代表最小网络玩家死亡事实同步骨架；当前远端服务已由 P4.5/P5 前置桥接包替换。

P4.5/P5 前置当前 Linux 包仍使用 `Builds/DedicatedServer/TYServer-linux-x86_64-p3-attack.tar.gz` 包名，已完成 Linux 构建、ECS 部署和公网双客户端回归验证，当前 SHA256 为 `f79ba95fabc3eb8c4feddad30807b09454d68bd95f90cd73f9a51ca51e36d90f`。该包已上传 ECS 并替换远端服务，远端 SHA256 校验一致，systemd 为 `active`；公网验证输出 `P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`，死亡同步证据仍为 `P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`。服务端日志记录 4 次 `Light_01 damage=25`，最终 `health=25->0 targetDead=True`。

P5 第一阶段当前只更新正式 CombatTest 玩家 prefab 与 prefab 构建/修复路径，未替换 ECS 远端包。已完成本机 Unity 编译、定向 EditMode `21/21 Passed`、Linux Dedicated Server 构建成功，以及对当前 ECS 服务的公网 P4 回归：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`。当前远端服务仍是上方 SHA256 为 `f79ba95fabc3eb8c4feddad30807b09454d68bd95f90cd73f9a51ca51e36d90f` 的 P4.5/P5 前置部署包。

P5 第二阶段当前新增 formal network player prefab 和本地构建验证，未替换 ECS 远端包。新增资源路径为 `Assets/_Game/Resources/Multiplayer/PF_NetworkPlayerCombatTest.prefab` / `Multiplayer/PF_NetworkPlayerCombatTest`；该资源已验证不依赖 local-preview-only 目录。已完成 Unity 编译、定向 EditMode `23/23 Passed`、Linux Dedicated Server 构建成功，以及对当前 ECS 默认服务的公网 P4 回归：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`。当前远端服务仍是 SHA256 为 `f79ba95fabc3eb8c4feddad30807b09454d68bd95f90cd73f9a51ca51e36d90f` 的 P4.5/P5 前置部署包。

P5 第三阶段当前完成本机 formal prefab 专项 smoke，未替换 ECS 远端包。修复了服务端命令行 prefab override 被 ServerBoot 序列化默认 prefab 引用覆盖的问题；定向 EditMode `24/24 Passed`，Mac local server build 和 Mac release client build 均为 `Build Finished, Result: Success.`。本机专项验证输出：`P4_MULTIPLAYER_OK host=127.0.0.1 gamePort=7891 healthPort=7892 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`，死亡同步证据为 `P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`。当前远端服务仍是 SHA256 为 `f79ba95fabc3eb8c4feddad30807b09454d68bd95f90cd73f9a51ca51e36d90f` 的 P4.5/P5 前置部署包。

P5 第四阶段当前 Linux 包为 `Builds/DedicatedServer/TYServer-linux-x86_64-p5-formal-prefab.tar.gz`，大小约 `81M`，SHA256 为 `0072b7853325fbdd064e4497eaba000a804b9321cacbb1455ddeb566fc05f2b5`。该包已上传 ECS 并替换远端服务，远端 SHA256 校验一致；`ty-new-server.service` 当前以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest` 启动，systemd 为 `active`。公网 formal prefab 验证输出：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`，死亡同步证据为 `P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`。服务端日志确认 active path 覆盖为 `Multiplayer/PF_NetworkPlayerCombatTest`，并记录 4 次 `Light_01 damage=25`，最终 `health=25->0 targetDead=True`。

P5.5 第一阶段当前 Linux 包为 `Builds/DedicatedServer/TYServer-linux-x86_64-p55-presentation.tar.gz`，大小约 `80M`，SHA256 为 `f21ee3976b927bc05e2349e7c7e37cb867f01a7dfd61f7e425c8f1a7e77aaac9`。该包已通过低速 rsync 续传上传 ECS 并替换远端服务，远端 SHA256 校验一致；`ty-new-server.service` 继续以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest` 启动，systemd 为 `active`。公网 formal prefab 验证输出：`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathStart=false client2ObservedLocalFormalDeathLater=true`，总结为 `P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true disconnected=0`。客户端 smoke 证据包含 `formalDrivers=...:suppressed` 和 `formalDeaths=...:true`；远端服务日志确认 active path 为 `Multiplayer/PF_NetworkPlayerCombatTest`，并记录 4 次 `Light_01 damage=25`，最终 `health=25->0 targetDead=True`。

P5.6 第一阶段未产出新的 Linux 服务端包，也未替换 ECS 远端服务。当前 ECS 仍运行 SHA256 为 `f21ee3976b927bc05e2349e7c7e37cb867f01a7dfd61f7e425c8f1a7e77aaac9` 的 P5.5 formal presentation 包；本阶段只更新客户端表现桥、smoke reporter 和探针。Mac server/client 本机构建成功，本机 formal smoke 输出 `P5_FORMAL_HIT_SYNC_OK attackId=Light_01 client2ObservedLocalFormalHitStart=false client2ObservedLocalFormalHitLater=true` 与 `P5_FORMAL_DEATH_SYNC_OK ... client2ObservedLocalFormalDeathLater=true`，总结为 `formalDeathSync=true formalHitSync=true`。公网 ECS 兼容回归同样通过 `P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK` 和 `P4_MULTIPLAYER_OK host=<ECS_HOST> ... formalDeathSync=true formalHitSync=true disconnected=0`，回归后公网 health 正常，计数回落到 `networkConnectedClients=0 networkSpawnedPlayers=0`。

P5.7 第一阶段新增 NGO 复制字段，未产出新的 Linux 服务端包，也未替换 ECS 远端服务；当前 ECS 仍运行 P5.5 formal presentation 包，不应用它验证 P5.7 formal attack。Mac server/client 本机构建成功，本机 formal smoke 使用端口 `7927/7928` 通过：`P5_FORMAL_ATTACK_SYNC_OK attackId=Light_01 client2ObservedRemoteFormalAttackStart=false client2ObservedRemoteFormalAttackLater=true`、`P5_FORMAL_HIT_SYNC_OK attackId=Light_01 client2ObservedLocalFormalHitStart=false client2ObservedLocalFormalHitLater=true`、`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathStart=false client2ObservedLocalFormalDeathLater=true`，总结为 `P4_MULTIPLAYER_OK host=127.0.0.1 gamePort=7927 healthPort=7928 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true disconnected=0`。下一步需要构建新的 Linux Dedicated Server 包、上传 ECS、以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest` 启动后复跑公网 P5.7 smoke。

P5.8 当前 Linux 包为 `Builds/DedicatedServer/TYServer-linux-x86_64-p58-formal-attack.tar.gz`，大小约 `80M`，SHA256 为 `a3134726feece31b3fa43de0f7feeaaca7ee4dac61f918e73a0ca69feb7ba812`。该包已上传 ECS 并替换远端服务，远端 SHA256 校验一致；`ty-new-server.service` 继续以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest` 启动，systemd 为 `active`。公网 formal smoke 输出 `P5_FORMAL_ATTACK_SYNC_OK attackId=Light_01 client2ObservedRemoteFormalAttackStart=false client2ObservedRemoteFormalAttackLater=true`、`P5_FORMAL_HIT_SYNC_OK attackId=Light_01 client2ObservedLocalFormalHitStart=false client2ObservedLocalFormalHitLater=true`、`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathStart=false client2ObservedLocalFormalDeathLater=true`，总结为 `P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true disconnected=0`。远端日志确认 active path 为 `Multiplayer/PF_NetworkPlayerCombatTest`，记录 4 次 `Light_01 damage=25`，最终 `health=25->0 targetDead=True`；smoke 后公网 health 回落到 `networkConnectedClients=0 networkSpawnedPlayers=0`。

P5.9 已完成 ECS 历史 AppleDouble 残留清理和回归验证。远端 `/opt/ty-new-server` 下 `._*` 普通文件已删除，重启后启动日志不再出现 `Failed to open plugin: /opt/ty-new-server/TYServer_Data/Plugins/._lib_burst_generated.so`，服务保持 `active`。部署脚本已加入解包后 `._*` 清理，`sh -n Deploy/DedicatedServer/deploy_p1_gameplay.sh` 通过。公网 formal smoke 重新通过 `P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK` 和 `P4_MULTIPLAYER_OK host=<ECS_HOST> ... formalDeathSync=true formalAttackSync=true formalHitSync=true disconnected=0`，smoke 后公网 health 为 `connected=0 spawned=0`。

P6.1 历史 Linux 包为 `Builds/DedicatedServer/TYServer-linux-x86_64-p61-network-enemy.tar.gz`，大小约 `79M`，SHA256 为 `c077808293939d1b365f7d58767037536eb0f4c51bf438cdfbbb7e907ff750bb`。该包已上传 ECS 并替换远端服务，远端 SHA256 校验一致；`ty-new-server.service` 继续以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest` 启动，systemd 为 `active`，当前进程内存约 `143.4M`。公网 P6 smoke 输出 `P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`、`P6_NETWORK_ENEMY_SYNC_OK ... client1ObservedEnemyHealthDrop=50 ... client2ObservedEnemyHealthDrop=50`，总结为 `P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true disconnected=0`。远端日志确认 active path 为 `Multiplayer/PF_NetworkPlayerCombatTest`，生成 `Multiplayer/PF_NetworkEnemyAvatar`，记录敌人 `health=50->0 enemyDead=True`，并记录 4 次 `Light_01 damage=25`，最终玩家目标 `health=25->0 targetDead=True`；smoke 后公网 health 回落到 `connected=0 spawned=0`。

P6.4 当前 Linux 包为 `Builds/DedicatedServer/TYServer-linux-x86_64-p64-enemy-attack.tar.gz`，大小约 `79M`，SHA256 为 `20057640fbd333faadcd22d3f845f10071c39876ed8f91928e5b987c70d9bc02`。该包已上传 ECS 并替换远端服务，远端 SHA256 校验一致；`ty-new-server.service` 当前以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --enable-network-enemy-attack-smoke` 启动，systemd 为 `active`。公网 P6.4 smoke 输出 `P1.5_UDP_INGRESS_OK`、`P6_NETWORK_ENEMY_SYNC_OK`、`P6_FORMAL_NETWORK_ENEMY_SYNC_OK`、`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedLocalHealthDrop=25 client2ObservedRemoteHealthDrop=25`，总结为 `P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`。远端日志确认服务端敌人攻击事实 `damage=25 health=100->75 targetDead=False`，并在 smoke 后回落到 `connected=0 spawned=0`。

P6.23 当前 Linux 包为 `Builds/DedicatedServer/TYServer-linux-x86_64-p623-three-enemy-retention.tar.gz`，大小约 `79M`，SHA256 为 `29ab4ba9ea03251be40ba92d756838aec050e3aebf71eeeec8264df200b92edf`。该包已上传 ECS 并替换远端服务，远端 SHA256 校验一致；`ty-new-server.service` 当前以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --network-enemy-count 3 --enable-network-enemy-server-tick --network-enemy-server-tick-damage 10 --network-enemy-server-tick-death-delay-seconds 90` 启动，systemd 为 `active`。公网 P6.23 smoke 输出 `P1.5_UDP_INGRESS_OK`、`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=3`、`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=20`、`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=3 uniqueTargetCount=2 enemyTargets=1->1,2->2,3->2` 和 `P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=3 minRetainedAttacks=4 ... retainedAttackCounts=1:4,2:4,3:4`，总结为 `P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0`。smoke 后公网 health 回落到 `connected=0 spawned=0`。

P6.25 未产出新的 Linux 包，仍复用 P6.23 包做 ECS 参数探索。临时切到 `--network-enemy-count 4 --network-enemy-server-tick-damage 10` 时，四敌人可见性和 NavMesh 均正常，但 target retention 因 `enemyId=3 expected>=4 actual=3` 失败；临时切到 `--network-enemy-count 4 --network-enemy-server-tick-damage 5` 后公网 smoke 通过 `P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=4`、`P6_NETWORK_ENEMY_SERVER_TICK_OK ... serverTickAttackCount=40` 和 `P6_NETWORK_ENEMY_TARGET_RETENTION_OK ... retainedAttackCounts=1:4,2:4,3:4,4:4`。探索结束后 `ty-new-server.service` 已恢复到 P6.23 稳定参数：三只敌人、tick damage `10`、death delay `90`；恢复后 health-only 为 `connected=0 spawned=0`。

P6.26 未产出新的 Linux 包，新增脚本 `Deploy/DedicatedServer/verify_p625_ecs_four_enemy_retention.sh` 复跑 P6.25 四敌人低伤害目标保持合同。脚本验证流程已在 ECS 跑通：先临时切到 `--network-enemy-count 4 --network-enemy-server-tick-damage 5`，通过 `P1.5_UDP_INGRESS_OK`、`P6_NETWORK_ENEMY_SERVER_TICK_OK ... serverTickAttackCount=40` 和 `P6_NETWORK_ENEMY_TARGET_RETENTION_OK ... retainedAttackCounts=1:4,2:4,3:4,4:4`；然后自动恢复为 P6.23 稳定参数 `--network-enemy-count 3 --network-enemy-server-tick-damage 10 --network-enemy-server-tick-death-delay-seconds 90`，恢复后 health-only 为 `connected=0 spawned=0`。

P6.27 未产出新的 Linux 包，通用脚本 `verify_ecs_network_enemy_retention_with_temp_service.sh` 已在 ECS 上用四敌人 + tick damage `5` 复验通过；unit/effective baseline 检查、持久备份、恢复后 health 和实验锁清理均有成功输出。

P6.28 继续复用 P6.23 包，以五敌人 + tick damage `5` 通过公网目标保持 smoke：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=5`、`P6_NETWORK_ENEMY_SERVER_TICK_OK ... serverTickAttackCount=40`、`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK ... enemyTargets=1->1,2->2,3->2,4->1,5->2`、`P6_NETWORK_ENEMY_TARGET_RETENTION_OK ... retainedAttackCounts=1:4,2:4,3:4,4:4,5:4`。实验后 effective service 恢复三敌人 + damage `10` + delay `90`，health 为 `connected=0 spawned=0`，持久锁与备份已清理。

P6.29 不产出新 Linux 包；通用工具新增 `--rounds`、逐轮日志、临时 `MainPID` 守门和逐轮 health-only。五敌人 + tick damage `5` 在同一临时 PID `109706` 下连续两轮通过，每轮均有 `serverTickAttackCount=40` 和 `retainedAttackCounts=1:4,2:4,3:4,4:4,5:4`，且轮末 `connected=0 spawned=0`。最终恢复三敌人 + damage `10` + delay `90`，独立核验 effective service、health 和锁清理均通过；该结果不代表长时间稳定或正式容量。

P6.30 不产出新 Linux 包，只增强 `probe_p15_multiplayer.py` 的 retention 失败诊断。真实 ECS 五敌 + damage `10` 复验发生 20 次攻击，两名玩家各承受 10 次、100 伤害并死亡；per-enemy 次数 `1:6,2:5,3:3,4:4,5:2`，缺口 `3:1,5:2` 与超额 3 次相等，分类为 `health_budget_exhausted_with_uneven_enemy_scheduling`。实验失败后恢复三敌常驻参数，独立核验 health `0/0` 与锁清理通过；这是玩法预算/调度诊断，不是容量失败或五敌常规伤害通过。

P6.31 不产出新 Linux 包，新增只读式 closure gate。真实 ECS 三敌常驻进程在 PID `112305` 下通过三敌各四击保持，回归前后 PID 不变、health `0/0`、unit/effective baseline 和实验锁无漂移，最终输出 `P6_CLOSURE_OK p6Status=closed`。P6 正式关闭；后续默认回到第一章开发，任何公平调度、五敌常规伤害或容量工作都应作为独立 P7 重新立项。

部署前必须明确：

- ECS 实例规格和实例族
- OS 镜像
- 端口和协议
- 服务端启动命令
- 日志路径
- 包版本
- 回滚版本
- 连接测试方法
- 停服和重启方法

## 11. 决策门槛

继续多人路线的条件：

- P0/P1 能在合理时间内完成
- P2 权威移动手感可接受
- P3 权威命中在 80-120ms 延迟下仍可玩
- 2 核 2G 指标没有在最小场景下过早打满
- 单机主线没有被 multiplayer spike 破坏

暂停多人路线的条件：

- P0 仍无法稳定 headless 启动
- P2 移动重构侵入过大，影响单机主线交付
- P3 命中手感在公网延迟下不可接受
- 服务端包无法在 2G 内存边界内稳定运行
- 项目短期目标是可展示单机作品，而不是多人技术验证

## 12. 参考资料

- Unity Dedicated Server: https://docs.unity3d.com/Manual/dedicated-server.html
- Unity Dedicated Server build: https://docs.unity3d.com/Manual/dedicated-server-build.html
- Unity Dedicated Server optimizations: https://docs.unity3d.com/Manual/dedicated-server-optimizations.html
- Unity Netcode for GameObjects: https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects
- Unity Transport: https://docs.unity3d.com/Packages/com.unity.transport
- 阿里云 ECS 实例规格族： https://www.alibabacloud.com/help/en/ecs/user-guide/overview-of-instance-families
