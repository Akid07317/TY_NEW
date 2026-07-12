# TY_NEW Dedicated Server ECS Runbook

更新时间：2026-07-10

> 公开仓库说明：所有 ECS 示例均使用 `<ECS_USER>`、`<ECS_HOST>` 和 `<SSH_KEY_PATH>` 占位；请在本地安全地替换为自己的运维参数。

## 当前状态

TY_NEW 现在已经可以产出 Linux Dedicated Server 包：

- 构建命令：`Tools/unity-cli/ty-new-build-dedicated-server linux --editor-mode --hub-licensing --licensing-ipc LicenseClient-don --wall-timeout 1800`
- 构建产物：`Builds/DedicatedServer/Linux/TYServer.x86_64`
- 探活端口：TCP `7778`
- P1 探针端口：TCP `7777`
- P1.5 NGO/UTP 游戏端口：UDP `7777`
- 当前状态：P6 已于 P6.31 正式收口。当前能力覆盖 ServerBoot/health/NGO-UTP、formal CombatTest 网络玩家、server-owned formal 网络敌人、ServerBoot baked NavMesh、非 smoke server tick 攻击、两/三敌目标分配与四击保持、三敌 damage `10` 常驻基线、五敌 damage `5` 双轮探索、临时 service 安全恢复工具、五敌 damage `10` 生命预算诊断，以及不修改 service 的最终 closure gate。
- P6 收口证据：三敌、damage `10`、delay `90` 最终公网回归通过 `serverTickAttackCount=20`、`enemyTargets=1->1,2->2,3->2`、`retainedAttackCounts=1:4,2:4,3:4`；回归前后 MainPID 均为 `112305`，health `0/0`，unit/effective baseline 和实验锁无漂移；输出 `P6_CLOSURE_OK p6Status=closed`。
- 当前非能力/P7 候选：五敌人常规伤害四击保持、公平攻击调度、更多敌人、长时间并发攻击、受击动画、掉落、客户端接入 UI、断线重连、账号/匹配、长压测和正式容量结论。P6.30 已解释五敌 damage `10` 失败，不等于该配置已通过或 ECS 容量不足。

## 2026-07-06 P1.5 本地接入记录

已引入正式多人网络栈：

- Netcode for GameObjects：`com.unity.netcode.gameobjects@2.6.0`
- Unity Transport：`com.unity.transport@2.6.0`
- P1.5 网络玩家 prefab：`Assets/_Game/Resources/Multiplayer/PF_NetworkPlayerAvatar.prefab`
- NGO prefab 注册：运行时由 `MultiplayerNetworkSessionService` 显式配置 player/enemy prefab，不依赖编辑器自动生成的根目录列表资产。
- Dedicated Server 默认启动 NGO server：UDP `7777`
- P1 TCP 行协议探针继续保留：TCP `7777`
- health 继续保留：TCP `7778`

本地验证：

- 资产修复：`CampusRPG.Editor.DedicatedServerBuildUtility.CreateOrRepairServerBootScene`
- P1.5 延迟启动修复：server/client NGO 启动延迟到 Unity `Start()`，避免 player `Awake()` 早于 Netcode ILPP message 注册而触发 `Allowed types is not equal`。
- 编译检查：`/tmp/unity_compile_p15_defer.log`，退出码 `0`
- 定向 EditMode：`/tmp/unity_editmode_p15_dedicated_results.xml`
- 最新定向 EditMode：`/tmp/unity_editmode_p15_defer_results.xml`
- 定向 EditMode 结果：`9/9 Passed`
- smoke movement 定向 EditMode：`/tmp/unity_editmode_p15_smoke_movement_results.xml`
- smoke movement 定向 EditMode 结果：`11/11 Passed`
- Dedicated Server smoke：`Tools/unity-cli/ty-new-build-dedicated-server smoke --hub-licensing --licensing-ipc LicenseClient-don --wall-timeout 600 --log /tmp/TY_NEW_dedicated_p15_smoke.log`
- smoke 结果：`Dedicated server smoke verification passed.`
- Linux Dedicated Server build：`Tools/unity-cli/ty-new-build-dedicated-server linux --hub-licensing --licensing-ipc LicenseClient-don --wall-timeout 1800 --log /tmp/TY_NEW_dedicated_p15_position_linux_build.log`
- build 结果：`Build Finished, Result: Success.`
- build 目录：`Builds/DedicatedServer/Linux`，约 `82M`
- P1.5 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p15-ngo.tar.gz`，约 `30M`
- P1.5 部署包 SHA256：`a07c3aaa5f385e5a86a163800a05dcb4fc1df6adc997f30107a36029e0b97c89`
- macOS 本地 server player build：`/tmp/TY_NEW_p15_smoke_movement_macos_server_build.log`，`Build Finished, Result: Success.`
- macOS 本地 client build：`/tmp/TY_NEW_p15_smoke_movement_mac_client_build.log`，`Build Finished, Result: Success.`

本机双客户端验证：

- server：`Builds/DedicatedServer/MacLocal/TYServer.app/Contents/MacOS/TY_NEW -batchmode -nographics --port 7797 --bind-address 127.0.0.1 --network-port 7797 --network-bind-address 127.0.0.1 --health-port 7798 --health-bind-address 127.0.0.1 --quit-after-seconds 120 -logFile /tmp/TY_NEW_p15_two_client_server.log`
- client 1：`Builds/ReleaseCandidate/Mac/TY_NEW.app/Contents/MacOS/TY_NEW -batchmode -nographics -multiplayer-client --server-address 127.0.0.1 --network-port 7797 --quit-after-seconds 25 -logFile /tmp/TY_NEW_p15_client1.log`
- client 2：`Builds/ReleaseCandidate/Mac/TY_NEW.app/Contents/MacOS/TY_NEW -batchmode -nographics -multiplayer-client --server-address 127.0.0.1 --network-port 7797 --quit-after-seconds 25 -logFile /tmp/TY_NEW_p15_client2.log`
- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 两个客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- server heartbeat 证据：`/tmp/TY_NEW_p15_two_client_server.log` 在 50-60 秒处记录 `networkConnectedClients=2 networkSpawnedPlayers=2`，70 秒后回落到 `0/0`

可复跑脚本：

```bash
Deploy/DedicatedServer/probe_p15_multiplayer.py --host 127.0.0.1 --game-port 7797 --health-port 7798
```

脚本会启动 macOS 本地 server player 和两个 batchmode client，等待 health 出现 `networkConnectedClients>=2` 与 `networkSpawnedPlayers>=2`，再等待两个客户端按 `--quit-after-seconds` 退出并确认计数回落到 `0/0`。

最新脚本验证：

- 命令：`Deploy/DedicatedServer/probe_p15_multiplayer.py --host 127.0.0.1 --game-port 7807 --health-port 7808 --server-log /tmp/TY_NEW_p15_script_server_7807.log --client1-log /tmp/TY_NEW_p15_script_client1_7807.log --client2-log /tmp/TY_NEW_p15_script_client2_7807.log`
- 结果：`P1.5_MULTIPLAYER_OK host=127.0.0.1 gamePort=7807 healthPort=7808 connected=2 spawned=2 disconnected=0`
- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- health-only 命令：`Deploy/DedicatedServer/probe_p15_multiplayer.py --health-only --host 127.0.0.1 --game-port 7817 --health-port 7818 --startup-timeout 10 --socket-timeout 2`
- health-only 结果：`P1.5_HEALTH_OK host=127.0.0.1 healthPort=7818 networkPort=7817 connected=0 spawned=0`
- 脚本重构后复测：`Deploy/DedicatedServer/probe_p15_multiplayer.py --host 127.0.0.1 --game-port 7837 --health-port 7838 --server-log /tmp/TY_NEW_p15_script_server_7837.log --client1-log /tmp/TY_NEW_p15_script_client1_7837.log --client2-log /tmp/TY_NEW_p15_script_client2_7837.log`
- 复测结果：`P1.5_MULTIPLAYER_OK host=127.0.0.1 gamePort=7837 healthPort=7838 connected=2 spawned=2 disconnected=0`
- 客户端互相可见验证：`Deploy/DedicatedServer/probe_p15_multiplayer.py --host 127.0.0.1 --game-port 7847 --health-port 7848 --server-log /tmp/TY_NEW_p15_visibility_server_7847.log --client1-log /tmp/TY_NEW_p15_visibility_client1_7847.log --client2-log /tmp/TY_NEW_p15_visibility_client2_7847.log --client1-quit-after-seconds 35 --client2-quit-after-seconds 15`
- 客户端互相可见结果：`P1.5_MULTIPLAYER_OK host=127.0.0.1 gamePort=7847 healthPort=7848 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true disconnected=0`
- client1 可见证据：`avatarCount=2 owned=1 remote=1 avatars=1:remote:-1.00,0.00,0.00|2:local:1.00,0.00,0.00`
- client2 可见证据：`avatarCount=2 owned=1 remote=1 avatars=1:local:-1.00,0.00,0.00|2:remote:1.00,0.00,0.00`
- client1 观察到 client2 退出：`avatarCount=1 owned=1 remote=0 avatars=2:local:1.00,0.00,0.00`
- 基础位置同步验证：`Deploy/DedicatedServer/probe_p15_multiplayer.py --host 127.0.0.1 --game-port 7857 --health-port 7858 --server-log /tmp/TY_NEW_p15_position_server_7857.log --client1-log /tmp/TY_NEW_p15_position_client1_7857.log --client2-log /tmp/TY_NEW_p15_position_client2_7857.log --client1-quit-after-seconds 35 --client2-quit-after-seconds 15 --min-remote-move-distance 0.25`
- 基础位置同步结果：`P1.5_MULTIPLAYER_OK host=127.0.0.1 gamePort=7857 healthPort=7858 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true disconnected=0`
- client2 远端位置同步证据：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=4.00`
- client2 观察到 client1 远端 avatar 位置从 `avatars=1:local:-1.00,0.00,0.00|2:remote:1.00,0.00,3.20` 更新到 `avatars=1:local:-1.00,0.00,0.00|2:remote:1.00,0.00,7.20`
- 完成审计本机复测：`P1.5_MULTIPLAYER_OK host=127.0.0.1 gamePort=7869 healthPort=7870 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true disconnected=0`
- 完成审计本机位置同步证据：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=4.00`

P1.5 Linux 包部署到 ECS 后，可在本机用同一个脚本连接已运行的公网 server：

```bash
Deploy/DedicatedServer/probe_p15_multiplayer.py --skip-server-start --host <ECS_HOST> --game-port 7777 --health-port 7778 --connected-timeout 60
```

该 ECS 模式不会上传包、不会重启 systemd，只会用本机两个 macOS client 连接指定 server，并通过 TCP health 判断远端 NGO/UTP 连接与 spawn/despawn 计数。

ECS 状态：

- 2026-07-06 已在用户授权后上传 P1.5、P2、P3 包；ECS 当前服务为 P3 包。
- 远端包 SHA256：`a07c3aaa5f385e5a86a163800a05dcb4fc1df6adc997f30107a36029e0b97c89`
- systemd 状态：`ty-new-server.service` 为 `active`
- ECS 本机 P1.5 health：`P1.5_HEALTH_OK host=127.0.0.1 healthPort=7778 networkPort=7777 connected=0 spawned=0`
- ECS UDP 监听：`ss -lunp` 显示 `0.0.0.0:7777` 由 `Unity Main Thre` 监听
- 公网 P1.5 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- 公网 P1 TCP 探针：`TY_NEW_GAME -> JOINED -> PONG -> ROOM -> BYE` 已通过
- 公网 P1.5 双客户端验证已通过：两个本机 batchmode client 连接公网 ECS server，公网 health 达到 `networkConnectedClients=2 networkSpawnedPlayers=2`
- 先前 ECS 抓包诊断：在本机客户端尝试连接期间，`sudo timeout 20 tcpdump -n -i any udp port 7777` 输出 `0 packets captured`，定位为阿里云安全组 UDP 入方向未放行
- 可复跑 UDP 入站诊断：`Deploy/DedicatedServer/probe_udp_ingress.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>`
- 当前 UDP 入站诊断结果：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 一键复跑公网验证：`Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>`
- 2026-07-06 一键验证结果：`P1.5_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true disconnected=0`
- 2026-07-06 单独 UDP 复测结果：tcpdump 捕获 `117.120.0.35 -> 172.24.54.177:7777`，输出 `P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 2026-07-06 公网互相可见证据：client1 `avatarCount=2 owned=1 remote=1`，client2 `avatarCount=2 owned=1 remote=1`
- 2026-07-06 公网 despawn 证据：client2 退出后 client1 输出 `avatarCount=1 owned=1 remote=0`
- 2026-07-06 公网基础位置同步证据：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=5.61`
- 当前可用权限检查：本机无 `aliyun` CLI、无 `~/.aliyun`；ECS 上无 `aliyun` CLI、无 `/home/<ECS_USER>/.aliyun`；ECS metadata `ram/` 与 `ram/security-credentials/` 均不可用。因此当前无法从 Codex 侧直接修改阿里云安全组。
- ECS 本机防火墙：`ufw` 为 inactive，iptables 默认 `ACCEPT`，nftables 无规则
- 当前结论：P3 包已部署，服务端已监听 UDP `7777`，阿里云安全组 UDP 入方向已生效，公网双客户端 NGO/UTP 最小闭环与服务端权威攻击命中 smoke 已通过

P1.5 客户端命令行入口：

```bash
TY_NEW_CLIENT -multiplayer-client --server-address <ECS_HOST> --network-port 7777
```

注意：P1.5 目前只是最小 NGO/UTP 连接和网络玩家对象骨架。它不是完整动作 RPG 联机，也还没有完成 CombatTest 正式角色 prefab 的网络化。

## 2026-07-06 P2 服务端权威移动与 HP 接入记录

P2 在 P1.5 最小连接骨架上新增：

- 客户端移动 RPC 只提交输入意图、序号和客户端时间戳，不再直接推进位置。
- 服务端在 `NetworkPlayerAvatar.Update()` 中按自己的 `deltaTime` 推进 `replicatedPosition` 和 `replicatedYaw`。
- 服务端保留最近输入，输入超时后停止移动，避免客户端断流后继续漂移。
- HP 使用 server-write `NetworkVariable<int>`，默认 `100`。
- smoke 客户端可提交过量 HP 意图；服务端单次最多接受 `25` 点，验证客户端不能直接决定最终 HP。
- smoke reporter 输出 `healths=...`，验证远端客户端能看到 HP 同步。

本机验证：

- Unity 编译：`/tmp/unity_compile_p2_authoritative.log`，退出码 `0`
- 定向 EditMode：`/tmp/unity_editmode_p2_authoritative_results.xml`
- 定向 EditMode 结果：`14/14 Passed`
- macOS 本地 server build：`/tmp/TY_NEW_p2_macos_server_build.log`，`Build Finished, Result: Success.`
- macOS 本地 client build：`/tmp/TY_NEW_p2_mac_client_build.log`，`Build Finished, Result: Success.`
- 本机 P2 smoke：`P2_MULTIPLAYER_OK host=127.0.0.1 gamePort=7871 healthPort=7872 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true disconnected=0`
- 本机 HP 同步证据：`P2_HEALTH_SYNC_OK client2ObservedRemoteHealthStart=100 client2ObservedRemoteHealthLater=75 client2ObservedRemoteHealthDrop=25`
- 本机位置同步证据：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=4.00`

Linux/ECS 验证：

- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p2_authoritative_linux_build.log`，`Build Finished, Result: Success.`
- P2 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p2-authoritative.tar.gz`，约 `30M`
- P2 部署包 SHA256：`fe70281161abbeae6c8e3876d902cae7af71dd3a8676c5b77a7012b804d7a495`
- ECS 远端 SHA256：`fe70281161abbeae6c8e3876d902cae7af71dd3a8676c5b77a7012b804d7a495`
- ECS systemd：`ty-new-server.service` 为 `active`
- ECS UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- ECS 公网 P2 smoke：`P2_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true disconnected=0`
- ECS 公网 HP 同步证据：`P2_HEALTH_SYNC_OK client2ObservedRemoteHealthStart=100 client2ObservedRemoteHealthLater=75 client2ObservedRemoteHealthDrop=25`
- ECS 公网位置同步证据：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=2.40`

P2 阶段仍是 NetworkPlayerAvatar 最小骨架，不等于 CombatTest 正式玩家 prefab、正式攻击命中、敌人 AI 或存档已经网络化。

## 2026-07-06 P3 服务端权威攻击命中记录

P3 在 P2 server-write HP 基础上新增：

- 客户端 smoke 只发送攻击意图和序号，不再发送最终伤害、目标或命中事实。
- 服务端验证攻击序号、冷却、距离和朝向后，才写入目标 `replicatedHealth`。
- 服务端固定单次攻击伤害为 `25`，客户端请求 `9999` 也只会造成 `25` 掉血。
- smoke movement 起点改为本地拥有 avatar 生成后计时，避免公网连接较慢时先移动出攻击范围。
- 服务端攻击链新增 `[MultiplayerCombat] Attack intent hit/missed` 日志，便于 ECS 侧定位。

本机验证：

- Unity 编译：`/tmp/unity_compile_p3_attack_spawnpair.log`，退出码 `0`
- 定向 EditMode：`/tmp/unity_editmode_p3_attack_spawnpair_results.xml`
- 定向 EditMode 结果：`16/16 Passed`
- macOS 本地 server build：`/tmp/TY_NEW_p3_macos_server_build_spawnpair.log`，`Build Finished, Result: Success.`
- macOS 本地 client build：`/tmp/TY_NEW_p3_mac_client_build_spawnpair.log`，`Build Finished, Result: Success.`
- 本机 P3 smoke：`P3_MULTIPLAYER_OK host=127.0.0.1 gamePort=7877 healthPort=7878 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true disconnected=0`
- 本机攻击命中证据：`P3_ATTACK_HIT_OK client2ObservedLocalHealthStart=100 client2ObservedLocalHealthLater=75 client2ObservedLocalHealthDrop=25 clientRequestedDamage=9999 serverAppliedDamage=25`
- 本机位置同步证据：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=2.93`

Linux/ECS 验证：

- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p3_attack_spawnpair_linux_build.log`，`Build Finished, Result: Success.`
- P3 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p3-attack.tar.gz`，约 `30M`
- P3 部署包 SHA256：`f80db23c1eddadf6571765e7cc37384b5b699238abedd2dd0a628d6683747266`
- ECS 远端 SHA256：`f80db23c1eddadf6571765e7cc37384b5b699238abedd2dd0a628d6683747266`
- ECS systemd：`ty-new-server.service` 为 `active`
- ECS UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- ECS 公网 P3 smoke：连续两次不重启服务复跑均通过 `P3_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true disconnected=0`
- ECS 公网攻击命中证据：`P3_ATTACK_HIT_OK client2ObservedLocalHealthStart=100 client2ObservedLocalHealthLater=75 client2ObservedLocalHealthDrop=25 clientRequestedDamage=9999 serverAppliedDamage=25`
- ECS 公网位置同步证据：第一轮 `P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=4.27`，第二轮 `P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=1.07`
- ECS 服务端命中日志：第一轮 `[MultiplayerCombat] Attack intent hit owner=1 targetOwner=2 sequence=1 damage=25 health=100->75 attackerPosition=-1.00,0.00,0.00 targetPosition=1.00,0.00,0.00`；第二轮 `[MultiplayerCombat] Attack intent hit owner=3 targetOwner=4 sequence=1 damage=25 health=100->75 attackerPosition=-1.00,0.00,2.00 targetPosition=1.00,0.00,2.00`

P3 当前仍是 NetworkPlayerAvatar 最小骨架，不等于正式 CombatTest 玩家 prefab、连招、受击、死亡、敌人 AI 或动画表现已经网络化。

## 2026-07-06 P3.5 服务端攻击配置白名单记录

P3.5 在 P3 单次权威攻击命中基础上新增：

- 客户端攻击 RPC 只提交攻击意图、序号和 `attackId`，仍不提交目标、命中事实或最终伤害。
- 服务端使用 `NetworkServerAttackProfile` 白名单解析攻击配置。
- 当前白名单第一条攻击为 `Light_01`，对齐 `SO_Attack_Light_01.asset` 的 `attackId`。
- 服务端 profile 决定伤害、范围、半角和冷却；非法 attackId 不会产生伤害。
- smoke 探针会显式传入并输出 `attackId=Light_01`，继续验证客户端请求 `9999` 时服务端只应用 `25`。

本机验证：

- Unity 编译：`/tmp/unity_compile_p35_attack_profile_retry.log`，退出码 `0`
- 定向 EditMode：`/tmp/unity_editmode_p35_attack_profile_results.xml`
- 定向 EditMode 结果：`17/17 Passed`
- macOS 本地 server build：`/tmp/TY_NEW_p35_macos_server_build.log`，`Build Finished, Result: Success.`
- macOS 本地 client build：`/tmp/TY_NEW_p35_mac_client_build.log`，`Build Finished, Result: Success.`
- 本机 P3.5 smoke：`P3_MULTIPLAYER_OK host=127.0.0.1 gamePort=7879 healthPort=7880 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true disconnected=0`
- 本机攻击配置证据：`P3_ATTACK_HIT_OK attackId=Light_01 client2ObservedLocalHealthStart=100 client2ObservedLocalHealthLater=75 client2ObservedLocalHealthDrop=25 clientRequestedDamage=9999 serverAppliedDamage=25`

Linux/ECS 验证：

- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p35_attack_profile_linux_build.log`，`Build Finished, Result: Success.`
- P3.5 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p3-attack.tar.gz`，约 `30M`
- P3.5 部署包 SHA256：`f240c118570e36a2186daa4a015b5affdfbca0342ec85a52abf399254531a43a`
- ECS 远端 SHA256：`f240c118570e36a2186daa4a015b5affdfbca0342ec85a52abf399254531a43a`
- ECS systemd：`ty-new-server.service` 为 `active`
- ECS UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- ECS 公网 P3.5 smoke：`P3_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true disconnected=0`
- ECS 公网攻击配置证据：`P3_ATTACK_HIT_OK attackId=Light_01 client2ObservedLocalHealthStart=100 client2ObservedLocalHealthLater=75 client2ObservedLocalHealthDrop=25 clientRequestedDamage=9999 serverAppliedDamage=25`
- ECS 服务端命中日志：`[MultiplayerCombat] Attack intent hit owner=2 targetOwner=1 sequence=1 attackId=Light_01 damage=25 health=100->75 attackerPosition=1.00,0.00,0.00 targetPosition=-1.00,0.00,0.00`

P3.5 当前仍是 NetworkPlayerAvatar 最小骨架，不等于正式 CombatTest 玩家 prefab、连招、受击、死亡、敌人 AI 或动画表现已经网络化。

## 2026-07-06 P4 服务端权威死亡同步记录

P4 在 P3.5 服务端攻击配置白名单基础上新增：

- `NetworkPlayerAvatar` 增加 server-write 死亡状态，HP 归零后由服务端写入 `death=true`。
- 死亡玩家在最小网络骨架中不再提交移动，也不会继续被服务端移动 tick 推进。
- 客户端 smoke reporter 新增 `deaths=`，用于验证客户端看到同一个死亡事实。
- smoke 客户端新增 `--smoke-attack-count` 和 `--smoke-attack-interval-seconds`，P4 验证用 4 次 `Light_01` 击杀目标；客户端仍只提交攻击意图，死亡不是客户端决定。

本机验证：

- Unity 编译：`/tmp/unity_compile_p4_death.log`，退出码 `0`
- 定向 EditMode：`/tmp/unity_editmode_p4_death_dedicated_results.xml`
- 定向 EditMode 结果：`19/19 Passed`
- 全量 EditMode：`/tmp/unity_editmode_p4_death_results.xml`，结果为 `482/498 Passed`；失败项为既有 CombatTest/Chapter01 资源基线和场景接线断言，`DedicatedServerBuildUtilityTests` 相关 19 项全部通过。
- macOS 本地 server build：`/tmp/TY_NEW_p4_macos_server_build.log`
- macOS 本地 client build：`/tmp/TY_NEW_p4_mac_client_build.log`
- 本机 P4 smoke：`P4_MULTIPLAYER_OK host=127.0.0.1 gamePort=7881 healthPort=7882 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`
- 本机死亡同步证据：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`
- 本机攻击命中证据：`P3_ATTACK_HIT_OK attackId=Light_01 client2ObservedLocalHealthStart=100 client2ObservedLocalHealthLater=0 client2ObservedLocalHealthDrop=100 clientRequestedDamage=9999 serverAppliedDamage=100`
- 本机服务端命中日志：4 次 `Light_01 damage=25`，最终 `health=25->0 targetDead=True`

Linux/ECS 验证：

- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p4_death_linux_build.log`
- P4 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p3-attack.tar.gz`，约 `30M`；文件名暂沿用 P3 攻击包名，内容已是 P4。
- P4 部署包 SHA256：`b56285d618d6812750db3778a9afa4f93d601911787cd6e7e9ac2a59cb1ca812`
- ECS 远端 SHA256：`b56285d618d6812750db3778a9afa4f93d601911787cd6e7e9ac2a59cb1ca812`
- ECS systemd：`ty-new-server.service` 为 `active`
- ECS UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- ECS 公网 P4 smoke：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`
- ECS 公网死亡同步证据：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`
- ECS 公网攻击命中证据：`P3_ATTACK_HIT_OK attackId=Light_01 client2ObservedLocalHealthStart=100 client2ObservedLocalHealthLater=0 client2ObservedLocalHealthDrop=100 clientRequestedDamage=9999 serverAppliedDamage=100`
- ECS 服务端命中日志：`health=100->75 targetDead=False`、`75->50 targetDead=False`、`50->25 targetDead=False`、`25->0 targetDead=True`。

P4 当前仍是 NetworkPlayerAvatar 最小骨架，不等于正式 CombatTest 玩家 prefab、死亡动画、复活、敌人 AI、UI 或完整战斗状态机已经网络化。

## 2026-07-07 P6.0 network enemy local smoke 记录

P6.0 新增一只 server-owned 最小网络敌人，用于证明敌人网络对象的生成、HP 同步和死亡事实同步链路。P6.0 本机阶段尚未部署 ECS；ECS 部署已在 P6.1 完成。P6.0/P6.1 都尚未接正式 `EnemyBrain`、NavMesh、敌人攻击或掉落。

新增本地资源和合同：

- 运行时脚本：`Assets/_Game/Scripts/Runtime/Multiplayer/NetworkEnemyAvatar.cs`
- Resources prefab：`Assets/_Game/Resources/Multiplayer/PF_NetworkEnemyAvatar.prefab`
- NGO 注册：`MultiplayerNetworkSessionService` 会把 `Multiplayer/PF_NetworkEnemyAvatar` 加入 `NetworkConfig.Prefabs`
- 客户端 smoke 字段：`enemyCount=`、`enemies=`、`enemyHealths=`、`enemyDeaths=`
- 探针开关：`--require-network-enemy-sync`

验证结果：

- 生成 prefab：`/tmp/unity_p60_create_enemy_prefab.log`
- 定向 EditMode：`/tmp/unity_editmode_p60_dedicated_results.xml = 26/26 Passed`
- Mac server build：`/tmp/unity_p60_macos_server_build.log`，`Build Finished, Result: Success.`
- Mac client build：`/tmp/unity_p60_mac_client_build.log`，`Build Finished, Result: Success.`

本机 P6 smoke 命令：

```bash
python3 Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --game-port 7967 \
  --health-port 7968 \
  --server-log /tmp/TY_NEW_p60_enemy_local_rerun_server.log \
  --client1-log /tmp/TY_NEW_p60_enemy_local_rerun_client1.log \
  --client2-log /tmp/TY_NEW_p60_enemy_local_rerun_client2.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --require-death-sync \
  --require-network-enemy-sync \
  --smoke-attack-count 4 \
  --smoke-attack-interval-seconds 0.75 \
  --smoke-move-delay-seconds 8 \
  --client1-quit-after-seconds 35 \
  --client2-quit-after-seconds 18
```

本机 P6 smoke 结果：

- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=3.10`
- formal 攻击表现同步：`P5_FORMAL_ATTACK_SYNC_OK attackId=Light_01 ... client2ObservedRemoteFormalAttackLater=true`
- formal 受击表现同步：`P5_FORMAL_HIT_SYNC_OK attackId=Light_01 ... client2ObservedLocalFormalHitLater=true`
- 网络死亡事实同步：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathLater=true`
- formal 死亡状态同步：`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathLater=true`
- 网络敌人同步：`P6_NETWORK_ENEMY_SYNC_OK ... client1ObservedEnemyHealthDrop=50 ... client2ObservedEnemyHealthDrop=50 ... client2ObservedEnemyDeathLater=true`
- 总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=7967 healthPort=7968 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true disconnected=0`

已知注意点：

- 如果 `--smoke-move-delay-seconds` 太早，client1 会在 4 次攻击完成前移动出攻击距离，导致 P4 death sync 失败；P6.0 本机通过值为 `8`。
- 当前敌人死亡由 `NetworkEnemyAvatar` 在两个客户端连接后触发，用于 smoke 合同；这不是正式敌人 AI。
- ECS 部署已在 P6.1 完成；当前 P6.1 ECS 服务包含 P6.0 新 NetworkPrefab，但仍只是最小网络敌人事实同步。

## 2026-07-07 P6.1 network enemy ECS 部署记录

P6.1 把 P6.0 的 server-owned 网络敌人改动构建为 Linux Dedicated Server 包，并部署到 ECS 完成公网双客户端 formal smoke。该阶段验证“敌人网络对象生成、HP 同步、死亡事实同步”已经在公网路径成立，但仍不代表正式敌人 AI 已经网络化。

构建与包：

- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p61_network_enemy_linux_build.log`
- build 结果：`Build Finished, Result: Success.`
- 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p61-network-enemy.tar.gz`
- 部署包大小：约 `79M`
- 本地 SHA256：`c077808293939d1b365f7d58767037536eb0f4c51bf438cdfbbb7e907ff750bb`
- ECS 远端 SHA256：`c077808293939d1b365f7d58767037536eb0f4c51bf438cdfbbb7e907ff750bb`

部署命令：

```bash
TY_NEW_SERVER_PACKAGE=<PROJECT_ROOT>/Builds/DedicatedServer/TYServer-linux-x86_64-p61-network-enemy.tar.gz \
Deploy/DedicatedServer/deploy_p1_gameplay.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

远端状态：

- `ty-new-server.service` 为 `active`
- systemd 启动参数包含 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest`
- 当前主进程内存约 `143.4M`
- 公网 smoke 后 health 回落：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`

公网 P6.1 验证命令：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_NETWORK_ENEMY_SYNC=1 \
TY_NEW_SMOKE_MOVE_DELAY_SECONDS=8 \
TY_NEW_CLIENT2_SECONDS=18 \
Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.1 验证结果：

- 公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=2.67`
- formal 攻击表现同步：`P5_FORMAL_ATTACK_SYNC_OK attackId=Light_01 client2ObservedRemoteFormalAttackStart=false client2ObservedRemoteFormalAttackLater=true`
- formal 受击表现同步：`P5_FORMAL_HIT_SYNC_OK attackId=Light_01 client2ObservedLocalFormalHitStart=false client2ObservedLocalFormalHitLater=true`
- 网络死亡事实同步：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`
- formal 死亡状态同步：`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathStart=false client2ObservedLocalFormalDeathLater=true`
- 网络敌人同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthStart=50 client1ObservedEnemyHealthLater=0 client1ObservedEnemyHealthDrop=50 client1ObservedEnemyDeathStart=false client1ObservedEnemyDeathLater=true client2ObservedEnemyHealthStart=50 client2ObservedEnemyHealthLater=0 client2ObservedEnemyHealthDrop=50 client2ObservedEnemyDeathStart=false client2ObservedEnemyDeathLater=true`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true disconnected=0`

ECS 服务端日志证据：

- 启动 active path：`networkPlayerPrefabResourcePath=Multiplayer/PF_NetworkPlayerCombatTest`
- 敌人 prefab：`Spawned server network enemy prefab=Multiplayer/PF_NetworkEnemyAvatar networkObjectId=1`
- 敌人死亡事实：`Smoke enemy death applied enemyId=1 health=50->0 enemyDead=True`
- 玩家正式攻击事实：4 次 `Light_01 damage=25`，最终 `health=25->0 targetDead=True`

已知边界：

- `PF_NetworkEnemyAvatar` 仍是最小 server-owned 网络敌人，不是正式 `EnemyBrain`。
- 本阶段没有验证 NavMesh、敌人寻路、敌人攻击、敌人受击动画、掉落或仇恨目标同步。
- 公网 smoke 依赖 `TY_NEW_SMOKE_MOVE_DELAY_SECONDS=8`，避免 client1 在 4 次 `Light_01` 完成前移动出攻击范围。

## 2026-07-07 P6.2 formal network enemy local smoke 记录

P6.2 把 `PF_NetworkEnemyAvatar` 从最小网络敌人升级为 formal CombatTest 敌人承载 prefab。本阶段只做本机 Mac server/client 验证，尚未构建或部署新的 Linux/ECS 包。

新增本地资源和合同：

- `Assets/_Game/Resources/Multiplayer/PF_NetworkEnemyAvatar.prefab` 根节点仍包含 `NetworkObject` 和 `NetworkEnemyAvatar`。
- 根节点下新增 unpack 后的 `FormalEnemy_Melee_CombatTest` 子树，来源为 `Assets/_Game/Prefabs/Characters/PF_Enemy_Melee_CombatTest.prefab`。
- formal 敌人子树保留 `EnemyBrain`、`EnemyStateMachine`、`HealthComponent`、`EnemySensing`、`EnemyMotor`、`EnemyAttackController` 和 `NavMeshAgent`。
- `NetworkEnemyPresentationBridge` 把 `NetworkEnemyAvatar` 权威 HP/death 投射到 formal `HealthComponent` 与 `EnemyStateMachine`。
- 客户端 `NetworkEnemyPresentationBridge` 持续 suppress `EnemyBrain`、`EnemySensing`、`EnemyMotor`、`EnemyAttackController` 和 `NavMeshAgent`，客户端只观察敌人位置、HP 和死亡事实。
- formal 敌人 prefab 生成时移除 local-preview-only imported visual 依赖，改用 server-safe proxy visual。
- smoke reporter 新增 `enemyFormalDeaths=` 与 `enemyFormalDrivers=`。
- 探针新增 `--require-formal-network-enemy-sync`；ECS 包装脚本新增 `TY_NEW_REQUIRE_FORMAL_NETWORK_ENEMY_SYNC=1` 开关。

验证结果：

- prefab 生成：`/tmp/unity_p62_create_enemy_prefab_retry.log`，`P6.2 formal network enemy prefab saved`
- Unity 编译：`/tmp/unity_compile_p62_formal_enemy.log`，退出码 `0`
- 定向 EditMode：`/tmp/unity_editmode_p62_dedicated_results.xml = 27/27 Passed`
- 全量 EditMode：`/tmp/unity_editmode_p62_formal_enemy_results.xml = 490/506 Passed`；失败项为既有 Chapter01/CombatTest 资源基线和动画接线断言，新增 P6.2 dedicated tests 均通过。
- Mac server build：`/tmp/unity_p62_macos_server_build.log`，`Build Finished, Result: Success.`
- Mac client build：`/tmp/unity_p62_mac_client_build.log`，`Build Finished, Result: Success.`

本机 P6.2 smoke 命令：

```bash
python3 Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --game-port 7977 \
  --health-port 7978 \
  --server-log /tmp/TY_NEW_p62_formal_enemy_local_server.log \
  --client1-log /tmp/TY_NEW_p62_formal_enemy_local_client1.log \
  --client2-log /tmp/TY_NEW_p62_formal_enemy_local_client2.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --require-death-sync \
  --require-network-enemy-sync \
  --require-formal-network-enemy-sync \
  --smoke-attack-count 4 \
  --smoke-attack-interval-seconds 0.75 \
  --smoke-move-delay-seconds 8 \
  --client1-quit-after-seconds 35 \
  --client2-quit-after-seconds 18
```

本机 P6.2 smoke 结果：

- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=2.81`
- formal 攻击表现同步：`P5_FORMAL_ATTACK_SYNC_OK attackId=Light_01 client2ObservedRemoteFormalAttackStart=false client2ObservedRemoteFormalAttackLater=true`
- formal 受击表现同步：`P5_FORMAL_HIT_SYNC_OK attackId=Light_01 client2ObservedLocalFormalHitStart=false client2ObservedLocalFormalHitLater=true`
- 玩家网络死亡事实同步：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`
- 玩家 formal 死亡状态同步：`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathStart=false client2ObservedLocalFormalDeathLater=true`
- 网络敌人同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthStart=50 client1ObservedEnemyHealthLater=0 client1ObservedEnemyHealthDrop=50 client1ObservedEnemyDeathStart=false client1ObservedEnemyDeathLater=true client2ObservedEnemyHealthStart=50 client2ObservedEnemyHealthLater=0 client2ObservedEnemyHealthDrop=50 client2ObservedEnemyDeathStart=false client2ObservedEnemyDeathLater=true`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK client1ObservedFormalEnemyDeathStart=false client1ObservedFormalEnemyDeathLater=true client2ObservedFormalEnemyDeathStart=false client2ObservedFormalEnemyDeathLater=true client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- 总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=7977 healthPort=7978 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true formalNetworkEnemySync=true disconnected=0`

已知边界：

- P6.2 本阶段没有构建或部署 Linux/ECS 包；当时 ECS 仍是 P6.1 最小网络敌人包，后续 P6.4 已替换为敌人攻击验证包。
- formal 敌人子树已嵌入 prefab，并已验证客户端本地 AI/NavMesh/攻击驱动为 `suppressed`。
- 敌人死亡事实仍由 P6 smoke 合同触发，不是正式 `EnemyBrain` 战斗结果。
- 下一阶段才验证服务端真实 `EnemyBrain` 目标选择、NavMesh 寻路、敌人攻击、敌人受击动画、掉落和仇恨目标同步。

## 2026-07-07 P6.3 network enemy attack local smoke 记录

P6.3 在 P6.2 formal 网络敌人 prefab 基础上新增最小 server-authored 敌人攻击事实。该阶段只做本机 Mac server/client 验证，尚未构建或部署新的 Linux/ECS 包。

新增本地资源和合同：

- `NetworkEnemyAvatar` 新增可选 server enemy attack smoke。服务端启动参数带 `--enable-network-enemy-attack-smoke` 时才启用；默认不改变 P6.2 行为。
- 敌人攻击 smoke 在两个客户端连接后选择一个存活 `NetworkPlayerAvatar`，由服务端直接写入该玩家 server-write HP，固定伤害 `25`。
- `NetworkEnemyAvatar` 新增敌人攻击表现序号/代码，客户端只读取，不提交命中或伤害。
- `NetworkEnemyPresentationBridge` 消费敌人攻击表现事实，让 formal 敌人子树短暂进入 `EnemyAttackState`，并输出 sticky 观测。
- `MultiplayerClientSmokeReporter` 新增 `enemyFormalAttacks=`。
- `probe_p15_multiplayer.py` 新增 `--require-network-enemy-attack-sync`，本机启动 server 时会自动传 `--enable-network-enemy-attack-smoke`。
- `verify_p15_ecs_multiplayer.sh` 新增 `TY_NEW_REQUIRE_NETWORK_ENEMY_ATTACK_SYNC=1` 和 `TY_NEW_MIN_NETWORK_ENEMY_ATTACK_HEALTH_DROP` 开关；ECS 使用该开关前还需要远端 systemd 启动参数包含 `--enable-network-enemy-attack-smoke`。

验证结果：

- Unity 编译：`/tmp/unity_compile_p63_enemy_attack_retry.log`，退出码 `0`
- 定向 EditMode：`/tmp/unity_editmode_p63_dedicated_results.xml = 27/27 Passed`
- Mac server build：`/tmp/unity_p63_macos_server_build.log`，`Build Finished, Result: Success.`
- Mac client build：`/tmp/unity_p63_mac_client_build.log`，`Build Finished, Result: Success.`

本机 P6.3 smoke 命令：

```bash
python3 Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --game-port 7987 \
  --health-port 7988 \
  --server-log /tmp/TY_NEW_p63_enemy_attack_local_server.log \
  --client1-log /tmp/TY_NEW_p63_enemy_attack_local_client1.log \
  --client2-log /tmp/TY_NEW_p63_enemy_attack_local_client2.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --require-death-sync \
  --require-network-enemy-sync \
  --require-formal-network-enemy-sync \
  --require-network-enemy-attack-sync \
  --smoke-attack-count 4 \
  --smoke-attack-interval-seconds 0.75 \
  --smoke-move-delay-seconds 8 \
  --client1-quit-after-seconds 35 \
  --client2-quit-after-seconds 18
```

本机 P6.3 smoke 结果：

- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=2.80`
- 敌人攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedLocalHealthStart=100 client1ObservedLocalHealthLater=75 client1ObservedLocalHealthDrop=25 client2ObservedRemoteHealthStart=100 client2ObservedRemoteHealthLater=75 client2ObservedRemoteHealthDrop=25 client1ObservedFormalEnemyAttackStart=false client1ObservedFormalEnemyAttackLater=true client2ObservedFormalEnemyAttackStart=false client2ObservedFormalEnemyAttackLater=true`
- 网络敌人同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthStart=50 client1ObservedEnemyHealthLater=0 client1ObservedEnemyHealthDrop=50 client1ObservedEnemyDeathStart=false client1ObservedEnemyDeathLater=true client2ObservedEnemyHealthStart=50 client2ObservedEnemyHealthLater=0 client2ObservedEnemyHealthDrop=50 client2ObservedEnemyDeathStart=false client2ObservedEnemyDeathLater=true`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK client1ObservedFormalEnemyDeathStart=false client1ObservedFormalEnemyDeathLater=true client2ObservedFormalEnemyDeathStart=false client2ObservedFormalEnemyDeathLater=true client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- 总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=7987 healthPort=7988 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`

本机 server 日志证据：

- 敌人攻击：`[MultiplayerEnemy] Smoke enemy attack applied enemyId=1 targetOwner=1 damage=25 health=100->75 targetDead=False enemyPosition=0.00,0.00,3.00 targetPosition=-1.00,0.00,0.00`
- 敌人死亡事实仍按 P6 smoke 合同触发：`[MultiplayerEnemy] Smoke enemy death applied enemyId=1 health=50->0 enemyDead=True`
- 玩家互打 smoke 仍通过 4 次 `Light_01`，最终 `targetDead=True`。

已知边界：

- P6.3 本阶段没有构建或部署 Linux/ECS 包；当时 ECS 仍是 P6.1 最小网络敌人包，后续 P6.4 已补上 ECS 部署验证。
- 敌人攻击事实由 `NetworkEnemyAvatar` 的 smoke hook 写入玩家网络 HP，用于验证服务端权威方向；还不是正式 `EnemyBrain` 决策结果。
- 尚未验证 NavMesh 寻路、`EnemyAttackController.TryAttack` 命中、敌人攻击距离/朝向/冷却、受击动画、掉落或仇恨目标同步。
- P6.3 smoke 中 `P3_ATTACK_HIT_OK` 仍用于玩家互打回归；该行的 HP drop 可能因采样窗口包含多次玩家攻击而大于单击伤害。P6.3 的敌人攻击证据以 `P6_NETWORK_ENEMY_ATTACK_SYNC_OK` 为准。

## 2026-07-07 P6.4 network enemy attack ECS 部署记录

P6.4 把 P6.3 的 server-authored 敌人攻击 smoke 构建为 Linux Dedicated Server 包，并部署到 ECS 完成公网双客户端验证。该阶段验证“服务端敌人攻击事实写玩家 HP、客户端只观察 formal 敌人攻击表现”已经在公网路径成立，但仍不是正式 `EnemyBrain`/NavMesh/`EnemyAttackController.TryAttack` gameplay tick。

构建与部署：

- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p64_enemy_attack_linux_build.log`，`Build Finished, Result: Success.`
- P6.4 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p64-enemy-attack.tar.gz`，约 `79M`
- P6.4 部署包 SHA256：`20057640fbd333faadcd22d3f845f10071c39876ed8f91928e5b987c70d9bc02`
- ECS 远端 SHA256：`20057640fbd333faadcd22d3f845f10071c39876ed8f91928e5b987c70d9bc02`
- systemd：`ty-new-server.service` 为 `active`
- 当前启动参数包含：`--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --enable-network-enemy-attack-smoke`

部署命令：

```bash
TY_NEW_SERVER_PACKAGE=<PROJECT_ROOT>/Builds/DedicatedServer/TYServer-linux-x86_64-p64-enemy-attack.tar.gz \
  Deploy/DedicatedServer/deploy_p1_gameplay.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.4 验证命令：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_NETWORK_ENEMY_SYNC=1 \
TY_NEW_REQUIRE_FORMAL_NETWORK_ENEMY_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_ATTACK_SYNC=1 \
TY_NEW_SMOKE_MOVE_DELAY_SECONDS=8 \
TY_NEW_CLIENT2_SECONDS=18 \
TY_NEW_CONNECTED_TIMEOUT=60 \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.4 验证结果：

- ECS health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=4.00`
- 玩家 formal 回归：`P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`
- 网络敌人同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthDrop=50 client2ObservedEnemyHealthDrop=50`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- 敌人攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedLocalHealthStart=100 client1ObservedLocalHealthLater=75 client1ObservedLocalHealthDrop=25 client2ObservedRemoteHealthStart=100 client2ObservedRemoteHealthLater=75 client2ObservedRemoteHealthDrop=25 client1ObservedFormalEnemyAttackStart=false client1ObservedFormalEnemyAttackLater=true client2ObservedFormalEnemyAttackStart=false client2ObservedFormalEnemyAttackLater=true`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`

ECS server 日志证据：

- active prefab：`Network player prefab command-line path overrides scenePath=Multiplayer/PF_NetworkPlayerAvatar activePath=Multiplayer/PF_NetworkPlayerCombatTest`
- 网络敌人生成：`[MultiplayerNetwork] Spawned server network enemy prefab=Multiplayer/PF_NetworkEnemyAvatar networkObjectId=1`
- 敌人攻击事实：`[MultiplayerEnemy] Smoke enemy attack applied enemyId=1 targetOwner=1 damage=25 health=100->75 targetDead=False enemyPosition=0.00,0.00,3.00 targetPosition=-1.00,0.00,0.00`
- 敌人死亡事实仍按 P6 smoke 合同触发：`[MultiplayerEnemy] Smoke enemy death applied enemyId=1 health=50->0 enemyDead=True`
- smoke 后 health 回落：`networkConnectedClients=0 networkSpawnedPlayers=0`

已知边界：

- `--enable-network-enemy-attack-smoke` 是 P6.4 专项验证开关，不应被误认为正式敌人 AI tick 已接入。
- 尚未验证服务端真实 `EnemyBrain` 目标选择、NavMesh 寻路、`EnemyAttackController.TryAttack`、攻击冷却/朝向/距离、敌人受击动画、掉落或仇恨目标同步。

## 2026-07-10 P6.31 P6 closure gate ECS 验证记录（P6 CLOSED）

P6.31 不新增 Unity runtime 或 Linux 包。新增 `Deploy/DedicatedServer/verify_p631_p6_closure.sh`，在不覆盖、不重启远端 service 的前提下完成 P6 总收口：前后检查 unit/effective baseline、实验锁、health `0/0` 与 MainPID，中间直接对当前三敌常驻 service 运行三敌各四击目标保持公网回归。

验证命令：

```bash
Deploy/DedicatedServer/verify_p631_p6_closure.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

真实 ECS 结果：

- 回归前：三敌、damage `10`、delay `90` 的 unit/effective baseline 均通过；无实验锁；health `connected=0 spawned=0`；捕获 MainPID `112305`。
- 公网回归：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=3`、`P6_NETWORK_ENEMY_SERVER_TICK_OK ... serverTickAttackCount=20`、`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK ... enemyTargets=1->1,2->2,3->2 enemyAttackCounts=1:10,2:6,3:4`、`P6_NETWORK_ENEMY_TARGET_RETENTION_OK ... retainedAttackCounts=1:4,2:4,3:4`、`P6_MULTIPLAYER_OK ... disconnected=0`。
- 回归后：health `0/0`；MainPID 仍为 `112305`；unit/effective baseline 仍匹配；实验锁仍不存在。
- 最终：`P6_CLOSURE_OK p6Status=closed enemyCount=3 tickDamage=10 deathDelaySeconds=90 retainedAttacks=4 pid=112305 serviceRestarted=false connected=0 spawned=0`。

本地 closure 契约已在 sh/dash 下通过，覆盖成功、baseline 漂移、锁占用、health 非零、验证失败、PID 漂移和危险 SSH 用户名拒绝；sh/dash/bash POSIX 语法与 dry-run 同样通过。

### P6 最终能力/限制矩阵

| 项目 | 收口状态 | 最终结论 |
|---|---|---|
| 三敌 damage `10` / delay `90` | 已通过并设为常驻基线 | P6.31 同 PID 三敌四击回归通过 |
| 五敌 damage `5` | 探索通过 | P6.29 同一临时 PID 连续两轮通过；不设为常驻 |
| 五敌 damage `10` | 已知限制 | P6.30 定位为 200 HP 耗尽与 per-enemy 调度不均；未通过 |
| 临时参数实验/恢复 | 已通过 | P6.27-P6.30 具备 baseline、锁、备份、失败恢复与恢复后 health 合同 |
| 长压测、容量、公平调度、断线重连 | P6 范围外 | 若启动，进入独立 P7，不从短窗口 smoke 外推 |

P6 到此关闭，后续默认回到第一章正式开发；不得以“继续 P6”的名义扩展 P7 范围。

## 2026-07-10 P6.30 five-enemy damage-10 retention failure diagnostic ECS 验证记录

P6.30 不新增 Unity runtime 代码、不产出新 Linux 包，继续复用 P6.23 包。改动集中在 `probe_p15_multiplayer.py`：当 target retention 失败时，在原始错误之后输出机器可读的 `P6_NETWORK_ENEMY_TARGET_RETENTION_DIAGNOSTIC`，包括完整攻击序列、每敌攻击次数与缺口、每目标累计命中/伤害/HP/死亡者，以及失败分类。配套离线测试为 `Deploy/DedicatedServer/tests/test_probe_p15_multiplayer_retention.py`。

验证命令（预期 smoke 返回非零，恢复合同必须成功）：

```bash
Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh --enemy-count 5 --tick-damage 10 --death-delay-seconds 90 --retention-attacks 4 --rounds 1 --client1-seconds 100 --client2-seconds 90 <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

真实 ECS 诊断版复验的关键输出：

```text
P1.5/P3/P3.5/P4 multiplayer probe failed: server tick target retention did not observe enough attacks for enemy: enemyId=3 expected>=4 actual=3
P6_NETWORK_ENEMY_TARGET_RETENTION_DIAGNOSTIC classification=health_budget_exhausted_with_uneven_enemy_scheduling observedAttacks=20 requiredAttacks=20 observedEnemyCount=5 requiredEnemyCount=5 attacksPerEnemyRequired=4 enemyAttackCounts=1:6,2:5,3:3,4:4,5:2 enemyAttackDeficits=3:1,5:2 missingEnemyAttackSlots=3 excessEnemyAttacks=3 targetBudgets=1:hits=10/damage=100/health=100->0/dead=true/killedByEnemy=1|2:hits=10/damage=100/health=100->0/dead=true/killedByEnemy=5 allTargetsDead=true attackSequence=1:1>1:100->90,2:2>2:100->90,3:1>1:90->80,4:2>2:90->80,5:4>1:80->70,6:1>1:70->60,7:2>2:80->70,8:4>1:60->50,9:3>2:70->60,10:1>1:50->40,11:2>2:60->50,12:4>1:40->30,13:3>2:50->40,14:5>2:40->30,15:1>1:30->20,16:2>2:30->20,17:4>1:20->10,18:3>2:20->10,19:5>2:10->0:dead,20:1>1:10->0:dead
```

证据解释：四击合同要求五敌合计至少 20 次攻击，实际也正好发生 20 次；两名玩家各承受 10 次、累计 100 伤害并死亡。由于敌人 1/2 分别多出 2/1 次，而敌人 3/5 分别缺 1/2 次，总量守恒但调度不均，因此玩家生命预算先耗尽。该结论描述的是当前 smoke 玩法合同，不是服务器吞吐或 ECS 容量结论。

恢复证据：临时诊断 service PID 为 `111907`；验证返回非零后输出 `SERVICE_RESTORE_OK` 和 `SERVICE_RESTORE_HEALTH_OK`。随后独立核验 effective `ExecStart` 已恢复三敌人、damage `10`、delay `90`，常驻 PID `112305`，公网 health `connected=0 spawned=0`，锁目录不存在。

本地验证：`python3 -m py_compile` 通过；诊断测试 `3/3 OK`，覆盖 P6.30 真实序列分类、异常消息机器可读标记和五敌低伤害平衡成功路径。边界：本阶段只解释失败，不修复攻击调度，也不把五敌常规伤害标成通过。

## 2026-07-10 P6.29 same-service two-round five-enemy retention ECS 验证记录

P6.29 不新增 Unity runtime 代码、不产出新 Linux 包，复用 P6.23 包，并为 P6.27 通用工具加入参数化多轮执行：`--rounds N` 默认 `1`、上限 `10`。工具只安装一次临时 service；每轮前后读取 systemd `MainPID`，要求与初始临时 PID 相同；每轮 smoke 成功后再执行 health-only，要求连接与玩家计数归零。

验证命令：

```bash
Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh --enemy-count 5 --tick-damage 5 --death-delay-seconds 90 --retention-attacks 4 --rounds 2 --client1-seconds 100 --client2-seconds 90 <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网结果：

- 临时 service 只安装一次，两轮前后的 `MainPID` 均为 `109706`。
- 第 1 轮：`serverTickAttackCount=40`；`enemyTargets=1->1,2->2,3->2,4->1,5->2`；`enemyAttackCounts=1:13,2:9,3:6,4:7,5:5`；`retainedAttackCounts=1:4,2:4,3:4,4:4,5:4`。
- 第 2 轮：`serverTickAttackCount=40`；`enemyTargets=1->3,2->4,3->4,4->3,5->4`；`enemyAttackCounts=1:14,2:8,3:6,4:6,5:6`；`retainedAttackCounts=1:4,2:4,3:4,4:4,5:4`。
- 每轮都输出 `P6_MULTIPLAYER_OK` 和 `TEMP_SERVICE_ROUND_OK`；每轮后的 health-only 均为 `connected=0 spawned=0`。
- 最终输出：`ECS_RETENTION_EXPERIMENT_OK ... completedRounds=2 requestedRounds=2 temporaryServicePid=109706 persistentEnemyCount=3 persistentTickDamage=10 persistentDeathDelaySeconds=90`。

恢复后又独立核验：effective `ExecStart` 已回到三敌人、damage `10`、delay `90`，新常驻 PID 为 `110418`；公网 health 为 `connected=0 spawned=0`；`/var/tmp/ty-new-server-retention.lock` 不存在。

本地合同同步扩展为两轮成功路径、逐轮 PID/health 断言和非法 `--rounds 11` 拒绝，并在 sh/dash 下通过。边界：这里只证明五敌人低伤害的两轮可复现与同进程会话清理，不证明五敌人常规伤害、长时间稳定、正式平衡或容量。

## 2026-07-10 P6.28 five-enemy low-damage retention ECS 验证记录

P6.28 不新增 Unity runtime 代码、不产出新 Linux 包，复用 P6.23 已部署包和 P6.27 通用工具，临时配置为五敌人、tick damage `5`、death delay `90`、每敌至少 4 次 retained attacks。

验证命令：

```bash
Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh --enemy-count 5 --tick-damage 5 --death-delay-seconds 90 --retention-attacks 4 --client1-seconds 100 --client2-seconds 90 <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网结果：

- health 与 UDP 入站通过；连接中 `networkConnectedClients=2 networkSpawnedPlayers=2`，退出后回到 `0/0`。
- 双客户端都观察到五只网络敌人：`client1EnemyIds=1,2,3,4,5 client2EnemyIds=1,2,3,4,5`。
- NavMesh 与 server tick 通过：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=40`。
- 目标覆盖两个玩家：`enemyTargets=1->1,2->2,3->2,4->1,5->2`，攻击计数 `enemyAttackCounts=1:13,2:9,3:6,4:7,5:5`。
- 五只敌人都完成四击保持：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK ... retainedAttackCounts=1:4,2:4,3:4,4:4,5:4`。
- 总结：`P6_MULTIPLAYER_OK ... networkEnemyCount=true networkEnemyServerTick=true networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0`。

恢复证据：effective `ExecStart` 回到三敌人、tick damage `10`、death delay `90`；恢复后 `P1.5_HEALTH_OK ... connected=0 spawned=0`；最终输出 `SERVICE_RESTORE_OK`、`SERVICE_RESTORE_HEALTH_OK` 和 `ECS_RETENTION_EXPERIMENT_OK ... persistentEnemyCount=3 persistentTickDamage=10 persistentDeathDelaySeconds=90`。

边界：本结果只证明五敌人、低伤害 `5`、单轮约 100 秒双客户端 smoke；不证明五敌人 `damage 10`、多轮/长时间稳定、正式平衡、并发容量、断线重连、掉落或受击动画。

## 2026-07-10 P6.27 generic retention experiment tool ECS 验证记录

P6.27 不新增 Unity runtime 代码、不产出 Linux 包；通用工具先完成本地合同，再在 ECS 上用已知四敌人、tick damage `5` 配置复验：

- 新脚本：`Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh`
- 参数化：enemy count、tick damage、death delay、retention attacks、client1/client2 seconds，以及预期 baseline 三项参数。
- baseline 保护：同时核对 `/etc/systemd/system/ty-new-server.service` 与 systemd effective `ExecStart`；每个相关 option token 必须唯一，值必须匹配预期。
- 并发/恢复保护：使用持久 root-only 实验锁保存 owner token 与完整 service 备份；临时配置验证完成后先恢复、检查 effective baseline 和公网 health，最后才删除备份与锁。
- 本地无副作用入口：`--dry-run` 只校验并打印配置，不连接 ECS。
- 离线测试：`Deploy/DedicatedServer/tests/verify_ecs_network_enemy_retention_with_temp_service_test.sh` 覆盖成功、验证失败后的恢复、安装失败后的恢复、恢复失败告警和危险 SSH 用户名拒绝。

本地验证已通过：

```bash
sh -n Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh
dash -n Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh
Deploy/DedicatedServer/tests/verify_ecs_network_enemy_retention_with_temp_service_test.sh
dash Deploy/DedicatedServer/tests/verify_ecs_network_enemy_retention_with_temp_service_test.sh
Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh --dry-run <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

ECS 结果：四敌可见性、`serverTickAttackCount=40`、两个目标分布和 `retainedAttackCounts=1:4,2:4,3:4,4:4` 全部通过；随后 effective `ExecStart` 恢复三敌人、damage `10`、delay `90`，health 回到 `connected=0 spawned=0`，持久备份与实验锁已清理。这证明 P6.27 的临时覆盖/恢复合同可在真实 ECS 路径运行，但仍不是正式容量结论。

## 2026-07-10 P6.26 scripted four-enemy retention ECS 验证记录

P6.26 不新增 Unity runtime 代码、不产出新 Linux 包，复用 P6.23 已部署包，并把 P6.25 的手工 ECS systemd 参数探索收敛为可复跑脚本：

- 脚本：`Deploy/DedicatedServer/verify_p625_ecs_four_enemy_retention.sh`
- 默认基线：`--network-enemy-count 3 --network-enemy-server-tick-damage 10 --network-enemy-server-tick-death-delay-seconds 90`
- 默认临时验证配置：`--network-enemy-count 4 --network-enemy-server-tick-damage 5 --network-enemy-server-tick-death-delay-seconds 90`
- 保护行为：先检查远端 service 是否符合基线，再备份 `/etc/systemd/system/ty-new-server.service` 到 `/tmp/ty-new-server.service.p625-backup.$$`；验证结束或中断时恢复原 service、重启并做 health-only 复查。

脚本化验证命令：

```bash
Deploy/DedicatedServer/verify_p625_ecs_four_enemy_retention.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

临时切换证据：

- ECS service 临时启动参数包含：`--network-enemy-count 4 --enable-network-enemy-server-tick --network-enemy-server-tick-damage 5 --network-enemy-server-tick-death-delay-seconds 90`
- 临时切换后 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`

脚本化 smoke 结果：

- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 四只网络敌人可见性：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=4 client1ObservedEnemyCount=4 client2ObservedEnemyCount=4 client1EnemyIds=1,2,3,4 client2EnemyIds=1,2,3,4`
- NavMesh server tick：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- server tick 攻击计数：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=40`
- 目标分配：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=4 uniqueTargetCount=2 enemyTargets=1->1,2->2,3->2,4->1 enemyAttackCounts=1:11,2:11,3:8,4:10`
- 目标保持：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=4 minRetainedAttacks=4 uniqueTargetCount=2 enemyTargets=1->1,2->2,3->2,4->1 retainedAttackCounts=1:4,2:4,3:4,4:4`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0`

恢复证据：

- 脚本恢复后 ECS service 参数回到：`--network-enemy-count 3 --enable-network-enemy-server-tick --network-enemy-server-tick-damage 10 --network-enemy-server-tick-death-delay-seconds 90`
- 恢复后 health-only：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- 脚本最终输出：`P6.25 scripted ECS four-enemy retention verification passed and service was restored.`

已知边界：

- P6.26 证明的是 P6.25 的四敌人低伤害 smoke 已有一键临时覆盖/恢复合同，减少手工改 systemd 的风险。
- 脚本会重启 ECS `ty-new-server.service`，只能在允许短暂服务重启的验证窗口执行。
- 这仍没有改变线上常驻配置；当前常驻配置仍是三敌人、tick damage `10`。
- 这仍没有证明四敌人 tick damage `10`、超过四只敌人、长时间压测、并发容量、正式平衡、断线重连、掉落或受击动画。

## 2026-07-09 P6.25 four-enemy retention ECS 探索记录

P6.25 不新增代码、不产出新包，复用 P6.23 已部署的 Linux 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p623-three-enemy-retention.tar.gz`（SHA256 `29ab4ba9ea03251be40ba92d756838aec050e3aebf71eeeec8264df200b92edf`）。本阶段临时调整 ECS systemd 参数，把 `--network-enemy-count` 从 `3` 提升到 `4`，验证 2 核 2G ECS 是否能跑通四只 server tick 敌人的四击目标保持 smoke。探索完成后，ECS 已恢复到稳定参数 `--network-enemy-count 3 --network-enemy-server-tick-damage 10 --network-enemy-server-tick-death-delay-seconds 90`。

公网 P6.25 四敌人验证命令模板：

```bash
TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_RETENTION=1 \
TY_NEW_MIN_NETWORK_ENEMY_TARGET_RETENTION_ATTACKS=4 \
TY_NEW_MIN_NETWORK_ENEMY_COUNT=4 \
TY_NEW_CLIENT1_SECONDS=100 \
TY_NEW_CLIENT2_SECONDS=90 \
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_DEATH_SYNC=0 \
TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS=1 \
TY_NEW_SKIP_CLIENT_DESPAWN_CHECK=1 \
TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK=1 \
TY_NEW_SKIP_HEALTH_SYNC_CHECK=1 \
TY_NEW_ECS_SERVER_LOG=/tmp/TY_NEW_p625_ecs_4enemy_retention_damage5.log \
TY_NEW_ECS_SERVER_TAIL_ERR=/tmp/TY_NEW_p625_ecs_4enemy_retention_damage5_tail.err \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

第一次探索：四敌人 + tick damage `10`

- 临时 ECS 参数：`--network-enemy-count 4 --enable-network-enemy-server-tick --network-enemy-server-tick-damage 10 --network-enemy-server-tick-death-delay-seconds 90`
- health、UDP 入站、四只敌人可见性、NavMesh server tick 都通过。
- target retention 失败：`server tick target retention did not observe enough attacks for enemy: enemyId=3 expected>=4 actual=3`
- 服务端日志统计：enemy 1 共 6 次攻击，enemy 2 共 6 次攻击，enemy 3 共 3 次攻击，enemy 4 共 5 次攻击。
- 初步结论：四只敌人使用 `10` 点 tick damage 时，两名 `100` HP 玩家过早接近死亡/死亡，第三只敌人无法稳定积累 4 次 retained attacks；这不是 UDP、visibility 或 NavMesh 失败。

第二次探索：四敌人 + tick damage `5`

- 临时 ECS 参数：`--network-enemy-count 4 --enable-network-enemy-server-tick --network-enemy-server-tick-damage 5 --network-enemy-server-tick-death-delay-seconds 90`
- ECS health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 四只网络敌人可见性：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=4 client1ObservedEnemyCount=4 client2ObservedEnemyCount=4 client1EnemyIds=1,2,3,4 client2EnemyIds=1,2,3,4`
- NavMesh server tick：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- server tick 攻击计数：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=40`
- 目标分配：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=4 uniqueTargetCount=2 enemyTargets=1->1,2->2,3->2,4->1 enemyAttackCounts=1:11,2:11,3:8,4:10`
- 目标保持：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=4 minRetainedAttacks=4 uniqueTargetCount=2 enemyTargets=1->1,2->2,3->2,4->1 retainedAttackCounts=1:4,2:4,3:4,4:4`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0`

恢复与复查：

- 探索完成后 ECS service 已恢复为 `--network-enemy-count 3 --network-enemy-server-tick-damage 10 --network-enemy-server-tick-death-delay-seconds 90`。
- 恢复后 health-only：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- health 行：`uptimeSeconds=20.3`、`networkConnectedClients=0 networkSpawnedPlayers=0`

已知边界：

- P6.25 证明的是：在 P6.23 包上，2 核 2G ECS 可以用四只敌人和较低验证伤害 `5` 跑通四击目标保持 smoke。
- P6.25 没有证明四只敌人在 `--network-enemy-server-tick-damage 10` 下可通过；该配置本次明确失败。
- 当前 ECS 常驻服务仍恢复为三只敌人、tick damage `10`，不是四只敌人、tick damage `5`。
- 这仍不是长时间压测、并发压测、正式容量、正式平衡、超过四只敌人、断线重连、掉落或受击动画验证。

## 2026-07-09 P6.24 repeated three-enemy retention ECS smoke 记录

P6.24 不新增代码、不产出新包，复用 P6.23 已部署的 Linux 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p623-three-enemy-retention.tar.gz`（SHA256 `29ab4ba9ea03251be40ba92d756838aec050e3aebf71eeeec8264df200b92edf`）和当前 ECS systemd 参数 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --network-enemy-count 3 --enable-network-enemy-server-tick --network-enemy-server-tick-damage 10 --network-enemy-server-tick-death-delay-seconds 90`。本阶段目标是确认 P6.23 不是一次性通过：同一 ECS server 进程不重启，连续两轮公网双客户端 smoke 都能通过三敌人四击目标保持，并在每轮结束后回到 `networkConnectedClients=0 networkSpawnedPlayers=0`。

公网 P6.24 重复验证命令模板：

```bash
TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_RETENTION=1 \
TY_NEW_MIN_NETWORK_ENEMY_TARGET_RETENTION_ATTACKS=4 \
TY_NEW_MIN_NETWORK_ENEMY_COUNT=3 \
TY_NEW_CLIENT1_SECONDS=90 \
TY_NEW_CLIENT2_SECONDS=80 \
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_DEATH_SYNC=0 \
TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS=1 \
TY_NEW_SKIP_CLIENT_DESPAWN_CHECK=1 \
TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK=1 \
TY_NEW_SKIP_HEALTH_SYNC_CHECK=1 \
TY_NEW_ECS_SERVER_LOG=/tmp/TY_NEW_p624_ecs_repeat1.log \
TY_NEW_ECS_SERVER_TAIL_ERR=/tmp/TY_NEW_p624_ecs_repeat1_tail.err \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

第 1 轮结果：

- 启动前 health：`uptimeSeconds=977.8`、`networkConnectedClients=0 networkSpawnedPlayers=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 退出后 health：`uptimeSeconds=1087.1`、`networkConnectedClients=0 networkSpawnedPlayers=0`
- 三只网络敌人可见性：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=3 client1ObservedEnemyCount=3 client2ObservedEnemyCount=3 client1EnemyIds=1,2,3 client2EnemyIds=1,2,3`
- server tick 攻击计数：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=20`
- 目标分配：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=3 uniqueTargetCount=2 enemyTargets=1->3,2->4,3->4 enemyAttackCounts=1:10,2:6,3:4`
- 目标保持：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=3 minRetainedAttacks=4 uniqueTargetCount=2 enemyTargets=1->3,2->4,3->4 retainedAttackCounts=1:4,2:4,3:4`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0`

第 2 轮结果：

- 启动前 health：`uptimeSeconds=1104.9`、`networkConnectedClients=0 networkSpawnedPlayers=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 退出后 health：`uptimeSeconds=1213.7`、`networkConnectedClients=0 networkSpawnedPlayers=0`
- 三只网络敌人可见性：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=3 client1ObservedEnemyCount=3 client2ObservedEnemyCount=3 client1EnemyIds=1,2,3 client2EnemyIds=1,2,3`
- server tick 攻击计数：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=20`
- 目标分配：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=3 uniqueTargetCount=2 enemyTargets=1->5,2->6,3->6 enemyAttackCounts=1:10,2:6,3:4`
- 目标保持：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=3 minRetainedAttacks=4 uniqueTargetCount=2 enemyTargets=1->5,2->6,3->6 retainedAttackCounts=1:4,2:4,3:4`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0`

重复验证后的 health-only 复查：

- `P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- health 行：`uptimeSeconds=1235.8`、`networkConnectedClients=0 networkSpawnedPlayers=0`

已知边界：

- P6.24 证明的是同一 ECS server 进程连续两轮公网 P6.23 smoke 可复现，并且每轮结束后 NGO 连接/生成计数回落。
- 这仍不是长时间压测、并发压测、正式容量结论，也没有新增超过三只敌人、断线重连、掉落、受击动画或正式平衡验证。

## 2026-07-09 P6.23 three-enemy four-hit target retention 本机与 ECS 验证记录

P6.23 把 P6.22 的“两只 server tick 敌人四击目标保持”扩到三只网络敌人。P6.22 的 `25` 点 tick 伤害在三敌人场景下会让两个 `100` HP 玩家最多承受 8 次攻击，无法满足 `3 * 4 = 12` 次保留窗口；本阶段新增服务端启动参数 `--network-enemy-server-tick-damage`，P6.23 smoke 使用 `10` 点 tick 伤害，并把 server tick 结算路径改为优先保留上一轮攻击的 live `targetOwner`，避免 formal AI 在近距离/clear-shot 抖动时让第三只敌人在 retained window 内换目标。

代码与测试：

- `ServerRuntimeBootstrap` / `MultiplayerNetworkSessionService` / `NetworkEnemyAvatar`：新增 `--network-enemy-server-tick-damage` / `-networkEnemyServerTickDamage`，默认仍为 `25`，P6.23 service 使用 `10`。
- `NetworkEnemyAvatar.ResolveServerBrainAttackTarget(..., preferRetainedServerTickTarget)`：server gameplay tick 下若上次攻击目标仍存活，优先向同一 `OwnerClientId` 结算网络伤害。
- `NetworkPlayerAvatar.TryFindLiveServerAvatarByOwner()`：提供服务端按 owner 查找 live avatar 的窄 helper。
- 定向 EditMode：`DedicatedServerBuildUtilityTests = 32/32 Passed`，结果文件 `/tmp/unity_editmode_p623_retain_owner_results.xml`。

本机 P6.23 验证：

- MacLocal server build：`/tmp/unity_build_macos_server_p623_retain_owner.log`，`Build Finished, Result: Success.`
- 本机 smoke：`host=127.0.0.1 gamePort=8127 healthPort=9187`，参数包含 `--network-enemy-count 3 --network-enemy-server-tick-damage 10 --network-enemy-server-tick-death-delay-seconds 90`。
- 三只网络敌人可见性：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=3 client1ObservedEnemyCount=3 client2ObservedEnemyCount=3 client1EnemyIds=1,2,3 client2EnemyIds=1,2,3`
- server tick 攻击计数：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=20`
- 目标分配：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=3 uniqueTargetCount=2 enemyTargets=1->1,2->2,3->2 enemyAttackCounts=1:10,2:6,3:4`
- 目标保持：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=3 minRetainedAttacks=4 uniqueTargetCount=2 enemyTargets=1->1,2->2,3->2 retainedAttackCounts=1:4,2:4,3:4`
- 总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8127 healthPort=9187 ... networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0`

Linux/ECS 部署：

- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p623_linux_build.log`，`Build Finished, Result: Success.`
- 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p623-three-enemy-retention.tar.gz`，约 `79M`
- SHA256：`29ab4ba9ea03251be40ba92d756838aec050e3aebf71eeeec8264df200b92edf`
- 远端 SHA256 校验一致；`ty-new-server.service` 当前启动参数包含 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --network-enemy-count 3 --enable-network-enemy-server-tick --network-enemy-server-tick-damage 10 --network-enemy-server-tick-death-delay-seconds 90`。
- 部署脚本完成 ECS 本机 health、UDP listener 和 P1 TCP gameplay 探针；部署后 systemd 为 `active`。

公网 P6.23 验证命令：

```bash
TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_RETENTION=1 \
TY_NEW_MIN_NETWORK_ENEMY_TARGET_RETENTION_ATTACKS=4 \
TY_NEW_MIN_NETWORK_ENEMY_COUNT=3 \
TY_NEW_CLIENT1_SECONDS=75 \
TY_NEW_CLIENT2_SECONDS=65 \
TY_NEW_ECS_SERVER_LOG=/tmp/TY_NEW_p623_ecs_3enemy_retention.log \
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_DEATH_SYNC=0 \
TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS=1 \
TY_NEW_SKIP_CLIENT_DESPAWN_CHECK=1 \
TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK=1 \
TY_NEW_SKIP_HEALTH_SYNC_CHECK=1 \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.23 验证结果：

- ECS health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 三只网络敌人可见性：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=3 client1ObservedEnemyCount=3 client2ObservedEnemyCount=3 client1EnemyIds=1,2,3 client2EnemyIds=1,2,3`
- NavMesh server tick：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- server tick 攻击计数：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=20`
- 目标分配：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=3 uniqueTargetCount=2 enemyTargets=1->1,2->2,3->2 enemyAttackCounts=1:10,2:6,3:4`
- 目标保持：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=3 minRetainedAttacks=4 uniqueTargetCount=2 enemyTargets=1->1,2->2,3->2 retainedAttackCounts=1:4,2:4,3:4`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0`
- ECS smoke 后 health 回落：`networkConnectedClients=0 networkSpawnedPlayers=0`

已知边界：

- P6.23 证明的是三只 server tick 敌人在四击窗口内保持目标，且 retained targets 覆盖两个 live players。
- `--network-enemy-server-tick-damage 10` 是 P6.23 smoke 为了避免两名玩家过早死亡而设置的验证参数，不是正式平衡数值。
- 当前目标保留锁只约束 server gameplay tick 的网络伤害结算；它不是完整仇恨系统，也没有验证超过三只敌人、长时间运行、并发攻击平衡、受击动画、掉落、复活、断线重连或正式容量。

## 2026-07-09 P6.22 four-hit target retention 本机与 ECS 验证记录

P6.22 修复 P6.21 暴露的四击目标保持缺口：当前 formal `EnemyAttackController.TryAttack` 在极近距离、Attack/Chase 快速切换时可能不再产出第 4 次 attack commit，导致玩家 HP 停在 `25`。本阶段新增 server gameplay tick 专用 fallback：当 formal `TryAttack` 未提交，但服务端确认 cooldown ready、formal target 在攻击范围内且 clear shot 成立时，由 server-authoritative fallback 消耗 `EnemyAttackController` cooldown 并写入网络玩家 HP。该 fallback 只在 `serverEnemyGameplayTickEnabled && serverEnemyGameplayTickActive` 路径内生效，不改变客户端本地敌人驱动 suppressed 边界。

代码与测试：

- `EnemyAttackController.RegisterServerAuthoritativeCommit()`：只推进攻击序列并消耗 cooldown，不触发本地 damage/event。
- `NetworkEnemyAvatar.TryApplyServerGameplayTickFallbackAttack()`：只在 server gameplay tick 下补上网络权威伤害，并继续输出 `Server tick enemy attack applied` 探针日志。
- `NetworkEnemyAvatar.TryFaceServerBrainAttackTarget()`：在 server tick 桥接提交前让 formal enemy 面向当前目标，降低近距离角度滞后。
- 定向 EditMode：`DedicatedServerBuildUtilityTests = 32/32 Passed`，结果文件 `/tmp/TY_NEW_editmode_tests.xml`。

本机 P6.22 验证：

- MacLocal server build：`/tmp/unity_build_macos_server_p622_fallback.log`，`Build Finished, Result: Success.`
- Mac client build：`/tmp/unity_build_macos_client_p622_fallback.log`，`Build Finished, Result: Success.`
- 本机 smoke：`host=127.0.0.1 gamePort=8109 healthPort=8110`。
- 结果：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=8`
- 目标分配：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=2 uniqueTargetCount=2 enemyTargets=1->1,2->2 enemyAttackCounts=1:4,2:4`
- 目标保持：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=2 minRetainedAttacks=4 uniqueTargetCount=2 enemyTargets=1->1,2->2 retainedAttackCounts=1:4,2:4`
- 第 4 击证据：`attackId=ServerGameplayTickFallback health=25->0 targetDead=True`，enemy 1/2 均出现。

Linux/ECS 部署：

- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p622_linux_build.log`，`Build Finished, Result: Success.`
- 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p622-target-retention.tar.gz`，约 `80M`
- SHA256：`df6617f7963d7ec229b52ccb721d7db1dd072ee43b5db5a9aff5864586d3ebb8`
- 远端 SHA256 校验一致；`ty-new-server.service` 保持 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --network-enemy-count 2 --enable-network-enemy-server-tick --network-enemy-server-tick-death-delay-seconds 24`。
- 部署脚本完成 ECS 本机 health、UDP listener 和 P1 TCP gameplay 探针；部署后 systemd 为 `active`。

公网 P6.22 验证命令：

```bash
TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_RETENTION=1 \
TY_NEW_MIN_NETWORK_ENEMY_TARGET_RETENTION_ATTACKS=4 \
TY_NEW_CLIENT1_SECONDS=55 \
TY_NEW_CLIENT2_SECONDS=45 \
TY_NEW_ECS_SERVER_LOG=/tmp/TY_NEW_p622_ecs_server_live.log \
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_DEATH_SYNC=0 \
TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS=1 \
TY_NEW_SKIP_CLIENT_DESPAWN_CHECK=1 \
TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK=1 \
TY_NEW_SKIP_HEALTH_SYNC_CHECK=1 \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.22 验证结果：

- ECS health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 两只网络敌人可见性：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=2 client1ObservedEnemyCount=2 client2ObservedEnemyCount=2 client1EnemyIds=1,2 client2EnemyIds=1,2`
- NavMesh server tick：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- server tick 攻击计数：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=8`
- 目标分配：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=2 uniqueTargetCount=2 enemyTargets=1->1,2->2 enemyAttackCounts=1:4,2:4`
- 目标保持：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=2 minRetainedAttacks=4 uniqueTargetCount=2 enemyTargets=1->1,2->2 retainedAttackCounts=1:4,2:4`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0`
- ECS smoke 后 health 回落：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 远端日志第 4 击：enemy 1/2 均记录 `attackId=ServerGameplayTickFallback health=25->0 targetDead=True`。

已知边界：

- P6.22 证明的是两只 server tick 敌人在四击窗口内保持各自目标，并能通过 server-authoritative fallback 完成击杀击。
- `ServerGameplayTickFallback` 是当前 server gameplay tick 的保守兜底，不等于完整正式敌人攻击动画、受击硬直、仇恨系统或并发平衡已经完成。
- 尚未验证更多敌人、更长时间运行、掉落、复活、断线重连或正式容量。

## 2026-07-09 P6.21 four-hit target retention ECS 探索记录

P6.21 尝试把 P6.20 的短窗口目标保持从每只敌人连续 3 次攻击提升到 4 次攻击，目的是验证目标保持能覆盖击杀前后的更长窗口。本阶段没有产出新的 Linux 包，复用当前 ECS 已部署的 P6.16 包和 P6.20 服务参数起点。

探针语义补充：

- `probe_p15_multiplayer.py` 的 target retention 检查允许 retained window 的最后一击是击杀击，避免把合理的 `25->0 targetDead=True` 误判为提前掉目标。
- retained window 中更早的攻击如果已经出现 `targetDead=True` 或 `nextHealth<=0`，仍会判定为失败。

公网 P6.21 探索命令要点：

```bash
TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_RETENTION=1 \
TY_NEW_MIN_NETWORK_ENEMY_TARGET_RETENTION_ATTACKS=4 \
TY_NEW_CLIENT1_SECONDS=55 \
TY_NEW_CLIENT2_SECONDS=45 \
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_DEATH_SYNC=0 \
TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS=1 \
TY_NEW_SKIP_CLIENT_DESPAWN_CHECK=1 \
TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK=1 \
TY_NEW_SKIP_HEALTH_SYNC_CHECK=1 \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

探索结果：

- ECS health 和 UDP `7777` 入站均通过。
- 第一次使用 P6.20 线上参数 `--network-enemy-server-tick-death-delay-seconds 24`，失败为：`expected>=8 actual=6`。
- 第二次临时把 ECS systemd 参数改为 `--network-enemy-server-tick-death-delay-seconds 45` 后复跑，仍失败为：`expected>=8 actual=6`。
- 服务端日志只出现每只敌人 3 次 `Server tick enemy attack applied`：`100->75`、`75->50`、`50->25`，没有出现第 4 次 `25->0`。
- 临时 ECS systemd 参数已恢复到 `--network-enemy-server-tick-death-delay-seconds 24` 并重启；恢复后 health 输出 `status=ok`、`networkListening=true`。

结论：

- P6.21 未通过，不能作为已完成里程碑。
- 当前最后一个通过的 ECS 多敌人目标保持里程碑仍是 P6.20。
- 失败原因不是 smoke death delay 过短，而是当前 formal AI/bridge 在该公网 smoke 中自然只产出每只敌人 3 次 server tick attack commit；继续推进需要 runtime 层修复攻击循环或新增受控的服务端重复攻击提交路径。

## 2026-07-08 P6.20 multi network enemy target retention ECS 验证记录

P6.20 把 P6.19 的短窗口目标保持/去重合同推进到 2 核 2G ECS 公网路径。本阶段没有产出新的 Linux 包，复用当前 ECS 已部署的 P6.16 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p616-multi-enemy-count.tar.gz`（SHA256 `f87ecea9e33584722d04aa8c010c511d14f90e7ece4711abaa23b65a5f0f1fe8`）和当前 systemd 参数 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --network-enemy-count 2 --enable-network-enemy-server-tick --network-enemy-server-tick-death-delay-seconds 24`。

验证脚本补充：

- `verify_p15_ecs_multiplayer.sh` 新增 `TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_RETENTION=1`。
- `TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_RETENTION=1` 会自动启用 target distribution、server tick、NavMesh server log 和至少两只 network enemy。
- `TY_NEW_MIN_NETWORK_ENEMY_TARGET_RETENTION_ATTACKS` 默认 `3`。
- wrapper 会把 `TY_NEW_MIN_NETWORK_ENEMY_SERVER_TICK_ATTACKS` 至少提升到 `enemy_count * retention_attacks`，确保 ECS 日志中有足够攻击样本。

公网 P6.20 验证命令：

```bash
TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_RETENTION=1 \
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_DEATH_SYNC=0 \
TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS=1 \
TY_NEW_SKIP_CLIENT_DESPAWN_CHECK=1 \
TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK=1 \
TY_NEW_SKIP_HEALTH_SYNC_CHECK=1 \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.20 验证结果：

- ECS health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 两只网络敌人可见性：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=2 client1ObservedEnemyCount=2 client2ObservedEnemyCount=2 client1EnemyIds=1,2 client2EnemyIds=1,2`
- NavMesh server tick：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- server tick 攻击计数：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=6`
- 目标分配：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=2 uniqueTargetCount=2 enemyTargets=1->1,2->2 enemyAttackCounts=1:3,2:3`
- 目标保持：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=2 minRetainedAttacks=3 uniqueTargetCount=2 enemyTargets=1->1,2->2 retainedAttackCounts=1:3,2:3`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=false remotePositionSync=false healthSync=false deathSync=false formalDeathSync=false formalAttackSync=false formalHitSync=false networkEnemySync=false networkEnemyCount=true networkEnemyChaseSync=false networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true networkEnemyTargetSwitch=false networkEnemyTargetDistribution=true networkEnemyTargetRetention=true formalNetworkEnemySync=false networkEnemyAttackSync=false disconnected=0`

已知边界：

- P6.20 证明的是 ECS 短窗口内两只 server tick 敌人各自连续 3 次攻击保持不同 live target。
- P6.20 没有新增 Linux 包；若服务端运行参数或部署包变化，需要重新验证。
- 尚未验证更长窗口仇恨、并发攻击平衡、敌人之间更复杂的目标去重、受击动画、掉落或长时间稳定性。

## 2026-07-08 P6.19 multi network enemy target retention local smoke 记录

P6.19 在 P6.18 的多敌人首次目标分配基础上，新增短窗口目标保持/去重合同。本阶段只做本机 Mac server/client 验证，没有构建新的 Linux 包，也没有替换 ECS 远端服务。

新增本地合同：

- `probe_p15_multiplayer.py` 新增 `--require-network-enemy-target-retention`。
- target retention 会自动要求 `--require-network-enemy-target-distribution`、server tick、NavMesh server log 和至少两只 network enemy。
- 探针读取服务端 `Server tick enemy attack applied` 日志中的 `enemyId`、`targetOwner`、`health` 和 `targetDead`。
- 合同要求每只 enemy 在 retained window 内连续攻击同一个仍存活的 target，且多只 enemy 的 retained target 覆盖至少两个不同玩家。
- smoke 输出 `P6_NETWORK_ENEMY_TARGET_RETENTION_OK` 和总结字段 `networkEnemyTargetRetention=true`。

本机 P6.19 smoke 命令：

```bash
Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --host 127.0.0.1 \
  --server-bind-address 127.0.0.1 \
  --game-port 8095 \
  --health-port 8096 \
  --server-log /tmp/TY_NEW_p619_target_retention_server.log \
  --client1-log /tmp/TY_NEW_p619_target_retention_client1.log \
  --client2-log /tmp/TY_NEW_p619_target_retention_client2.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --network-enemy-count 2 \
  --min-network-enemy-count 2 \
  --require-network-enemy-target-retention \
  --min-network-enemy-target-retention-attacks 3 \
  --skip-auto-formal-sync-requirements \
  --skip-client-despawn-check \
  --skip-remote-movement-check \
  --skip-health-sync-check \
  --client1-quit-after-seconds 20 \
  --client2-quit-after-seconds 18 \
  --server-quit-after-seconds 40 \
  --connected-timeout 60
```

本机 P6.19 smoke 结果：

- 多敌人可见性：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=2 client1ObservedEnemyCount=2 client2ObservedEnemyCount=2 client1EnemyIds=1,2 client2EnemyIds=1,2`
- NavMesh server tick：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- server tick 攻击计数：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=6`
- 目标分配：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=2 uniqueTargetCount=2 enemyTargets=1->1,2->2 enemyAttackCounts=1:3,2:3`
- 目标保持：`P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=2 minRetainedAttacks=3 uniqueTargetCount=2 enemyTargets=1->1,2->2 retainedAttackCounts=1:3,2:3`
- 总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8095 healthPort=8096 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=false remotePositionSync=false healthSync=false deathSync=false formalDeathSync=false formalAttackSync=false formalHitSync=false networkEnemySync=false networkEnemyCount=true networkEnemyChaseSync=false networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true networkEnemyTargetSwitch=false networkEnemyTargetDistribution=true networkEnemyTargetRetention=true formalNetworkEnemySync=false networkEnemyAttackSync=false disconnected=0`

已知边界：

- P6.19 证明的是短窗口内两只 server tick 敌人各自连续 3 次攻击保持不同 live target。
- P6.19 当阶段尚未构建新的 Linux 包或做 ECS 公网复验；该缺口已由 P6.20 补上。
- 尚未验证更长窗口仇恨、并发攻击平衡、敌人之间更复杂的目标去重、受击动画、掉落或长时间稳定性。

## 2026-07-08 P6.18 multi network enemy target distribution ECS 验证记录

P6.18 把 P6.17 的多敌人目标分配合同推进到 2 核 2G ECS 公网路径。本阶段没有产出新的 Linux 包，复用当前 ECS 已部署的 P6.16 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p616-multi-enemy-count.tar.gz`（SHA256 `f87ecea9e33584722d04aa8c010c511d14f90e7ece4711abaa23b65a5f0f1fe8`）和当前 systemd 参数 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --network-enemy-count 2 --enable-network-enemy-server-tick --network-enemy-server-tick-death-delay-seconds 24`。

验证脚本补充：

- `verify_p15_ecs_multiplayer.sh` 新增/整理 `TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_DISTRIBUTION=1` 路径。
- target distribution 模式会自动要求 server tick、NavMesh server log 和至少 2 个 network enemy。
- target distribution 模式不会再隐式强制旧的客户端敌人 HP/death/chase 同步断言，避免把目标分配验证和旧 smoke 伤害窗口混在一起。
- 探针总结字段允许 `networkEnemySync=false`，只要 `networkEnemyCount=true`、`networkEnemyNavMeshChaseSync=true`、`networkEnemyServerTick=true`、`networkEnemyTargetDistribution=true` 成立即可通过 P6.18。

公网 P6.18 验证命令：

```bash
TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_DISTRIBUTION=1 \
TY_NEW_MIN_NETWORK_ENEMY_SERVER_TICK_ATTACKS=2 \
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_DEATH_SYNC=0 \
TY_NEW_SKIP_AUTO_FORMAL_SYNC_REQUIREMENTS=1 \
TY_NEW_SKIP_CLIENT_DESPAWN_CHECK=1 \
TY_NEW_SKIP_REMOTE_MOVEMENT_CHECK=1 \
TY_NEW_SKIP_HEALTH_SYNC_CHECK=1 \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.18 验证结果：

- ECS health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 两只网络敌人可见性：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=2 client1ObservedEnemyCount=2 client2ObservedEnemyCount=2 client1EnemyIds=1,2 client2EnemyIds=1,2`
- NavMesh server tick：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- server tick 攻击计数：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=6`
- 目标分配：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=2 uniqueTargetCount=2 enemyTargets=1->1,2->2 enemyAttackCounts=1:3,2:3`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=false remotePositionSync=false healthSync=false deathSync=false formalDeathSync=false formalAttackSync=false formalHitSync=false networkEnemySync=false networkEnemyCount=true networkEnemyChaseSync=false networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true networkEnemyTargetSwitch=false networkEnemyTargetDistribution=true formalNetworkEnemySync=false networkEnemyAttackSync=false disconnected=0`

ECS server 日志证据：

- `Server tick enemy attack applied enemyId=1 targetOwner=1`
- `Server tick enemy attack applied enemyId=2 targetOwner=2`
- 目标分配聚合：`enemyTargets=1->1,2->2 enemyAttackCounts=1:3,2:3`

已知边界：

- P6.18 证明的是短时 smoke 中两只 server tick 敌人的首次攻击目标覆盖两个玩家。
- P6.18 没有新增 Linux 包；若服务端运行参数或部署包变化，需要重新验证。
- 尚未验证长期仇恨、目标去重策略、并发攻击平衡、敌人受击动画、掉落、长时间稳定性或正式容量。

## 2026-07-08 P6.17 multi network enemy target distribution local smoke 记录

P6.17 在 P6.16 两只网络敌人生成/身份/可见性基础上，新增多敌人最小目标分配合同。本阶段只做本机 Mac server/client 验证，尚未构建新的 Linux 包或部署 ECS。

新增本地合同：

- `probe_p15_multiplayer.py` 新增 `--require-network-enemy-target-distribution`。
- 探针读取服务端 `Server tick enemy attack applied` 日志中的 `enemyId` 与 `targetOwner`。
- 合同要求多只 enemy 的首次攻击目标覆盖至少两个不同玩家，避免把死亡后切换误判为初始目标分配成功。
- smoke 输出 `P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK` 和总结字段 `networkEnemyTargetDistribution=true`。

验证结果：

- Python 探针静态检查：`python3 -m py_compile Deploy/DedicatedServer/probe_p15_multiplayer.py` 通过。
- 本机 P6.17 smoke：`P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=2 uniqueTargetCount=2 enemyTargets=1->1,2->2 enemyAttackCounts=1:3,2:3`。
- 同轮服务端 tick 证据：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=6`。
- 同轮多敌人可见性：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=2 client1ObservedEnemyCount=2 client2ObservedEnemyCount=2 client1EnemyIds=1,2 client2EnemyIds=1,2`。
- 同轮总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8085 healthPort=8086 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=false remotePositionSync=true healthSync=true deathSync=false formalDeathSync=false formalAttackSync=false formalHitSync=false networkEnemySync=true networkEnemyCount=true networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true networkEnemyTargetSwitch=false networkEnemyTargetDistribution=true formalNetworkEnemySync=false networkEnemyAttackSync=true disconnected=0`。

本机 P6.17 smoke 命令：

```bash
Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --host 127.0.0.1 \
  --server-bind-address 127.0.0.1 \
  --game-port 8085 \
  --health-port 8086 \
  --server-log /tmp/TY_NEW_p617_target_distribution_server.log \
  --client1-log /tmp/TY_NEW_p617_target_distribution_client1.log \
  --client2-log /tmp/TY_NEW_p617_target_distribution_client2.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --network-enemy-count 2 \
  --min-network-enemy-count 2 \
  --require-network-enemy-target-distribution \
  --min-network-enemy-server-tick-attacks 2 \
  --skip-auto-formal-sync-requirements \
  --skip-client-despawn-check \
  --client1-quit-after-seconds 18 \
  --client2-quit-after-seconds 16 \
  --server-quit-after-seconds 35 \
  --connected-timeout 60
```

已知边界：

- P6.17 本机证明的是两只 server tick 敌人在同一轮 smoke 中分别首次攻击不同玩家。
- 本阶段尚未构建新的 Linux 包或做 ECS 公网 P6.17 复验。
- 尚未验证长期仇恨、目标去重策略、并发攻击平衡、敌人受击动画、掉落或长时间稳定性。

## 2026-07-08 P6.16 multi network enemy count 本机与 ECS 记录

P6.16 在 P6.15 单只敌人目标死亡后切换的基础上，先补齐多敌人的最小服务端生成、稳定身份和客户端可见性合同。本阶段已完成本机 Mac server/client 验证，并构建 Linux Dedicated Server 包部署到 2 核 2G ECS 完成公网双客户端 count-only smoke。

新增本地合同：

- `MultiplayerNetworkSessionSettings` 新增 `NetworkEnemyCount`，默认仍为 `1`，兼容此前所有单敌人 smoke。
- `ServerRuntimeBootstrap` 新增 `--network-enemy-count` / `-networkEnemyCount`，取值 clamp 到 `0..16`，启动日志输出 `networkEnemyCount=...`。
- `MultiplayerNetworkSessionService` 从单个 `spawnedEnemy` 改为多敌人列表，按 count 生成多只 `NetworkEnemyAvatar`，并为每只敌人写入稳定 `enemyId` 与分散 `spawnPosition`。
- `NetworkEnemyAvatar` 新增带 index 的 spawn position helper；index `0` 保持旧位置，index `1` 生成第二只敌人位置 `2.00,0.00,3.00`。server tick / brain chase 模式下也有对应的第二只敌人 chase 起点。
- `probe_p15_multiplayer.py` 新增 `--network-enemy-count` 和 `--min-network-enemy-count`；数量校验独立于 HP/death/attack smoke，输出 `P6_NETWORK_ENEMY_COUNT_OK`。

验证结果：

- Python 探针静态检查：`python3 -m py_compile Deploy/DedicatedServer/probe_p15_multiplayer.py` 通过。
- ECS wrapper 语法检查：`sh -n Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh` 通过。
- 定向 EditMode：`/tmp/unity_editmode_p616_multi_enemy_results.xml = 30/30 Passed`。
- Mac server build：`/tmp/unity_p616_multi_enemy_macos_server_build.log`，`Build Finished, Result: Success.`
- Mac client build：`/tmp/unity_p616_multi_enemy_mac_client_build.log`，`Build Finished, Result: Success.`
- Linux Dedicated Server build：`/tmp/unity_p616_multi_enemy_linux_build.log`，`Build Finished, Result: Success.`
- P6.16 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p616-multi-enemy-count.tar.gz`，约 `79M`
- P6.16 部署包 SHA256：`f87ecea9e33584722d04aa8c010c511d14f90e7ece4711abaa23b65a5f0f1fe8`
- ECS 远端 SHA256：`f87ecea9e33584722d04aa8c010c511d14f90e7ece4711abaa23b65a5f0f1fe8`
- ECS systemd：`ty-new-server.service` 为 `active`
- ECS 当前启动参数包含：`--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --network-enemy-count 2 --enable-network-enemy-server-tick --network-enemy-server-tick-death-delay-seconds 24`

本机 P6.16 smoke 命令：

```bash
Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --host 127.0.0.1 \
  --server-bind-address 127.0.0.1 \
  --game-port 8081 \
  --health-port 8082 \
  --server-log /tmp/TY_NEW_p616_multi_enemy_server.log \
  --client1-log /tmp/TY_NEW_p616_multi_enemy_client1.log \
  --client2-log /tmp/TY_NEW_p616_multi_enemy_client2.log \
  --network-enemy-count 2 \
  --min-network-enemy-count 2 \
  --skip-client-despawn-check \
  --skip-remote-movement-check \
  --skip-health-sync-check \
  --smoke-attack-count 0 \
  --client1-quit-after-seconds 12 \
  --client2-quit-after-seconds 10 \
  --server-quit-after-seconds 25 \
  --connected-timeout 60
```

本机 P6.16 smoke 结果：

- 服务端启动参数确认：`networkEnemyCount=2`
- 服务端生成敌人 1：`[MultiplayerNetwork] Spawned server network enemy ... enemyId=1 spawnPosition=0.00,0.00,3.00`
- 服务端生成敌人 2：`[MultiplayerNetwork] Spawned server network enemy ... enemyId=2 spawnPosition=2.00,0.00,3.00`
- 双客户端接入：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 双客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 客户端数量硬校验：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=2 client1ObservedEnemyCount=2 client2ObservedEnemyCount=2 client1EnemyIds=1,2 client2EnemyIds=1,2`
- 总结：`P3_MULTIPLAYER_OK host=127.0.0.1 gamePort=8081 healthPort=8082 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=false remotePositionSync=false healthSync=false deathSync=false networkEnemyCount=true disconnected=0`

公网 P6.16 smoke 命令：

```bash
Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --skip-server-start \
  --host <ECS_HOST> \
  --game-port 7777 \
  --health-port 7778 \
  --server-log /tmp/TY_NEW_p616_ecs_server_live.log \
  --client1-log /tmp/TY_NEW_p616_ecs_client1.log \
  --client2-log /tmp/TY_NEW_p616_ecs_client2.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --skip-auto-formal-sync-requirements \
  --min-network-enemy-count 2 \
  --skip-client-despawn-check \
  --skip-remote-movement-check \
  --skip-health-sync-check \
  --smoke-attack-count 0 \
  --client1-quit-after-seconds 16 \
  --client2-quit-after-seconds 14 \
  --connected-timeout 60
```

公网 P6.16 smoke 结果：

- 服务端启动参数确认：`networkEnemyCount=2`
- 服务端生成敌人 1：`[MultiplayerNetwork] Spawned server network enemy ... enemyId=1 spawnPosition=-1.00,0.00,5.00`
- 服务端生成敌人 2：`[MultiplayerNetwork] Spawned server network enemy ... enemyId=2 spawnPosition=1.00,0.00,5.00`
- 双客户端接入：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 双客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 客户端数量硬校验：`P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=2 client1ObservedEnemyCount=2 client2ObservedEnemyCount=2 client1EnemyIds=1,2 client2EnemyIds=1,2`
- 总结：`P3_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=false remotePositionSync=false healthSync=false deathSync=false networkEnemyCount=true disconnected=0`

已知边界：

- P6.16 证明的是两只网络敌人的生成、稳定 `enemyId`、分散位置和双客户端可见性。
- P6.16 当阶段尚未验证多敌人目标分配；该缺口已由 P6.18 公网 smoke 补上。
- 仍未验证长期仇恨、并发攻击节奏、敌人之间的目标去重、受击动画、掉落或长时间稳定性。

## 2026-07-08 P6.15 network enemy target switch 本机与 ECS 部署记录

P6.15 在 P6.14 ECS 连续 server tick 攻击基线上，补齐单只网络敌人的最小目标选择/死亡后切换合同。本阶段已完成本机 Mac server/client 验证，并构建 Linux Dedicated Server 包部署到 2 核 2G ECS 完成公网双客户端验证。

新增本地合同：

- `NetworkPlayerAvatar.ApplyServerEnemyDamage()` 同步 formal 玩家子树 `HealthComponent`，使服务端正式 `EnemyBrain` 能在网络 HP 归零后通过 living-target 检查清除死亡目标。
- `NetworkEnemyAvatar` 新增 `Server tick enemy target acquired` 和 `Server tick enemy target switched` 日志；切换日志必须带 `previousTargetDead=True`。
- `ServerRuntimeBootstrap` 新增 `--network-enemy-server-tick-death-delay-seconds`，用于 P6.15 本机 smoke 拉长 enemy death smoke 窗口，默认仍保持 `9` 秒。
- `probe_p15_multiplayer.py` 新增 `--require-network-enemy-target-switch`、`--min-network-enemy-initial-target-attacks` 和总结字段 `networkEnemyTargetSwitch=`；该合同要求初始目标死亡前不切换，死亡后切到另一名 live target，并要求服务端 switch 日志证明 `previousTargetDead=true`。

验证结果：

- Python 探针静态检查：`python3 -m py_compile Deploy/DedicatedServer/probe_p15_multiplayer.py` 通过。
- ECS wrapper 语法检查：`sh -n Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh` 通过。
- 定向 EditMode：`/tmp/unity_editmode_p615_dedicated_results.xml = 30/30 Passed`。
- Mac server build：`/tmp/unity_p615_macos_server_build.log`，`Build Finished, Result: Success.`
- Mac client build：`/tmp/unity_p615_mac_client_build.log`，`Build Finished, Result: Success.`

本机 P6.15 smoke 命令：

```bash
Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --host 127.0.0.1 \
  --server-bind-address 127.0.0.1 \
  --game-port 8079 \
  --health-port 8080 \
  --server-log /tmp/TY_NEW_p615_target_switch_server.log \
  --client1-log /tmp/TY_NEW_p615_target_switch_client1.log \
  --client2-log /tmp/TY_NEW_p615_target_switch_client2.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --require-network-enemy-sync \
  --require-formal-network-enemy-sync \
  --require-network-enemy-attack-sync \
  --require-network-enemy-chase-sync \
  --require-network-enemy-navmesh-chase \
  --require-network-enemy-server-tick \
  --require-network-enemy-target-switch \
  --min-network-enemy-server-tick-attacks 5 \
  --min-network-enemy-initial-target-attacks 3 \
  --min-network-enemy-chase-distance 2.0 \
  --smoke-attack-count 1 \
  --smoke-attack-interval-seconds 0.75 \
  --smoke-move-delay-seconds 8 \
  --client1-quit-after-seconds 85 \
  --client2-quit-after-seconds 65 \
  --server-quit-after-seconds 110 \
  --connected-timeout 60
```

本机 P6.15 smoke 结果：

- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 双客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 网络敌人追击同步：`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=3.62 client2ObservedEnemyMoveDistance=3.62`
- 服务端 NavMesh 追击硬校验：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- 服务端连续 tick 硬校验：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=7`
- 目标切换硬校验：`P6_NETWORK_ENEMY_TARGET_SWITCH_OK initialTargetOwner=1 initialTargetAttackCount=3 switchedTargetOwner=2 previousTargetDead=true`
- 网络敌人 HP/death 同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthDrop=50 client2ObservedEnemyHealthDrop=50`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- server tick 敌人攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=local client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=remote client2ObservedTargetHealthDrop=25`
- 总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8079 healthPort=8080 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=false formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true networkEnemyTargetSwitch=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`

本机 server 日志证据：

- death delay 覆盖：`[ServerRuntime] Startup ... networkEnemyServerTickDeathDelaySeconds=24 ...`
- 初始目标获得：`[MultiplayerEnemy] Server tick enemy target acquired ... targetOwner=1 targetDead=False`
- 初始目标连续命中直到死亡：`Server tick enemy attack applied ... targetOwner=1 ... health=75->50`、`50->25`、`25->0 targetDead=True`
- 死亡后切换：`[MultiplayerEnemy] Server tick enemy target switched ... previousTargetOwner=1 previousTargetDead=True nextTargetOwner=2 nextTargetDead=False`
- 切换后命中另一名玩家：`Server tick enemy attack applied ... targetOwner=2 ... health=100->75`

ECS P6.15 构建与部署：

- Linux Dedicated Server build：`/tmp/unity_p615_ecs_linux_build.log`，`Build Finished, Result: Success.`
- 构建目录：`Builds/DedicatedServer/Linux`，约 `157M`
- P6.15 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p615-target-switch.tar.gz`
- 包大小：约 `79M`
- 本地 SHA256：`8ef303b46694a1757e81decba93bcc513dd09a857ca2a36cdcda7cc2671b7b1f`
- ECS 远端 SHA256：`8ef303b46694a1757e81decba93bcc513dd09a857ca2a36cdcda7cc2671b7b1f`
- systemd：`ty-new-server.service` 为 `active`
- systemd 启动参数：`--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --enable-network-enemy-server-tick --network-enemy-server-tick-death-delay-seconds 24`

部署命令：

```bash
TY_NEW_SERVER_PACKAGE=<PROJECT_ROOT>/Builds/DedicatedServer/TYServer-linux-x86_64-p615-target-switch.tar.gz \
  Deploy/DedicatedServer/deploy_p1_gameplay.sh \
  <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

部署脚本验证：

- ECS 本机 health：`P1.5_HEALTH_OK host=127.0.0.1 healthPort=7778 networkPort=7777 connected=0 spawned=0`
- ECS 本机 UDP `7777` 监听：通过。
- ECS 本机 P1 TCP gameplay：`JOINED ... playerName=ECSSmoke`、`PONG ... joined=true`。

公网 P6.15 smoke 命令：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_NETWORK_ENEMY_SYNC=1 \
TY_NEW_REQUIRE_FORMAL_NETWORK_ENEMY_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_ATTACK_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_CHASE_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_NAVMESH_CHASE=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_SERVER_TICK=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_TARGET_SWITCH=1 \
TY_NEW_MIN_NETWORK_ENEMY_SERVER_TICK_ATTACKS=5 \
TY_NEW_MIN_NETWORK_ENEMY_INITIAL_TARGET_ATTACKS=3 \
TY_NEW_MIN_NETWORK_ENEMY_CHASE_DISTANCE=2.0 \
TY_NEW_SMOKE_ATTACK_COUNT=1 \
TY_NEW_SMOKE_ATTACK_INTERVAL_SECONDS=0.75 \
TY_NEW_SMOKE_MOVE_DELAY_SECONDS=8 \
TY_NEW_CLIENT1_SECONDS=85 \
TY_NEW_CLIENT2_SECONDS=65 \
TY_NEW_CONNECTED_TIMEOUT=60 \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh \
  <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.15 smoke 结果：

- 公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- 公网 UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 双客户端接入：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 双客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 网络敌人追击同步：`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=2.77 client2ObservedEnemyMoveDistance=2.77`
- 服务端 NavMesh 追击硬校验：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- 服务端连续 tick 硬校验：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=7`
- 目标切换硬校验：`P6_NETWORK_ENEMY_TARGET_SWITCH_OK initialTargetOwner=1 initialTargetAttackCount=3 switchedTargetOwner=2 previousTargetDead=true`
- 网络敌人 HP/death 同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthDrop=50 client2ObservedEnemyHealthDrop=50`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- server tick 敌人攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=local client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=remote client2ObservedTargetHealthDrop=25`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true networkEnemyTargetSwitch=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`

ECS server 日志证据：

- 初始目标获得：`[MultiplayerEnemy] Server tick enemy target acquired ... targetOwner=1 targetDead=False`
- 初始目标连续命中直到死亡：`Server tick enemy attack applied ... targetOwner=1 ... health=100->75`、`50->25`、`25->0 targetDead=True`
- 死亡后切换：`[MultiplayerEnemy] Server tick enemy target switched ... previousTargetOwner=1 previousTargetDead=True nextTargetOwner=2 nextTargetDead=False`
- 切换后命中另一名玩家：`Server tick enemy attack applied ... targetOwner=2 ... health=100->75`、`75->50`、`50->25`、`25->0 targetDead=True`

已知边界：

- P6.15 证明的是单只网络敌人在 server tick 下的最小死亡后切换规则，且已完成 ECS 公网双客户端 smoke。
- 仍没有验证多敌人仇恨表、抢仇恨、远离脱战、复活后重新纳入目标、敌人受击动画、掉落或长时间稳定性。

## 2026-07-08 P6.14 continuous network enemy server tick ECS 部署记录

P6.14 把 P6.13 的连续 server tick 敌人攻击节奏构建为 Linux Dedicated Server 包，并部署到 2 核 2G ECS 完成公网双客户端验证。该阶段验证 P6.13 的 `--min-network-enemy-server-tick-attacks` 合同可以在远端 Linux Dedicated Server 上通过，服务端连续多次接收正式 `EnemyAttackController.AttackCommitted` 并写入网络 HP；仍不是多敌人、掉落、受击动画、目标切换或正式容量结论。

构建与部署：

- Linux Dedicated Server build：`/tmp/unity_p614_server_tick_repeat_linux_build.log`，`Build Finished, Result: Success.`
- P6.14 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p614-server-tick-repeat.tar.gz`
- 包大小：约 `80M`
- 本地 SHA256：`edb24e7644d5a7687b235ae53698613a10f7d3993d92b3b4f23cc7fed61b2625`
- ECS 远端 SHA256：`edb24e7644d5a7687b235ae53698613a10f7d3993d92b3b4f23cc7fed61b2625`
- systemd：`ty-new-server.service` 为 `active`
- systemd 启动参数：`--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --enable-network-enemy-server-tick`

部署命令：

```bash
TY_NEW_SERVER_PACKAGE=<PROJECT_ROOT>/Builds/DedicatedServer/TYServer-linux-x86_64-p614-server-tick-repeat.tar.gz \
  Deploy/DedicatedServer/deploy_p1_gameplay.sh \
  <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.14 smoke 命令：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_NETWORK_ENEMY_SYNC=1 \
TY_NEW_REQUIRE_FORMAL_NETWORK_ENEMY_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_ATTACK_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_CHASE_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_NAVMESH_CHASE=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_SERVER_TICK=1 \
TY_NEW_MIN_NETWORK_ENEMY_SERVER_TICK_ATTACKS=2 \
TY_NEW_MIN_NETWORK_ENEMY_CHASE_DISTANCE=2.0 \
TY_NEW_SMOKE_MOVE_DELAY_SECONDS=8 \
TY_NEW_CLIENT1_SECONDS=45 \
TY_NEW_CLIENT2_SECONDS=28 \
TY_NEW_CONNECTED_TIMEOUT=60 \
TY_NEW_SMOKE_ATTACK_COUNT=4 \
TY_NEW_SMOKE_ATTACK_INTERVAL_SECONDS=0.75 \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh \
  <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.14 smoke 结果：

- ECS health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- ECS UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- client2 退出后 health：`networkConnectedClients=1 networkSpawnedPlayers=1`
- 双客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=2.13`
- 玩家 formal 回归：`P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`
- 网络敌人追击同步：`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=2.07 client2ObservedEnemyMoveDistance=3.62`
- 服务端 NavMesh 追击硬校验：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- 服务端连续 tick 硬校验：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=5`
- 网络敌人 HP/death 同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthDrop=50 client2ObservedEnemyHealthDrop=50`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- server tick 敌人攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=local client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=remote client2ObservedTargetHealthDrop=25 client1ObservedFormalEnemyAttackLater=true client2ObservedFormalEnemyAttackLater=true`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`

ECS server 日志证据：

- 日志来源：`/tmp/TY_NEW_p614_ecs_server_live.log`，由 `verify_p15_ecs_multiplayer.sh` 实时 tail 远端 `/var/log/ty-new/server.log`。
- NavMesh ready：`[MultiplayerEnemy] Server tick enemy status ... state=EnemyIdleGuardState ... navMeshAgentEnabled=True navMeshReady=True serverTick=True`
- 追击：`[MultiplayerEnemy] Server tick enemy status ... state=EnemyChaseState currentTarget=FormalPlayer_CombatTest currentTargetDistance=3.63 ... navMeshReady=True serverTick=True`
- 第 1 次网络伤害：`[MultiplayerEnemy] Server tick enemy attack applied ... targetOwner=1 ... health=100->75 ... enemyPosition=-1.00,0.00,2.00`
- 后续同目标网络伤害：`[MultiplayerEnemy] Server tick enemy attack applied ... targetOwner=1 ... health=25->0 ... enemyPosition=-1.00,0.00,1.38`
- 切到另一名玩家后的连续伤害：`[MultiplayerEnemy] Server tick enemy attack applied ... targetOwner=2 ... health=100->75`、`75->50`、`50->25`

收尾健康检查：

- smoke 后公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- smoke 后 systemd：`ty-new-server.service` 仍为 `active`

已知边界：

- P6.14 证明的是单只网络敌人的连续 server tick 攻击能在 ECS 公网双客户端路径跑通。
- 当前没有验证多敌人仇恨、稳定目标选择、受击动画、掉落、长时间稳定性或 4 人上限。
- 敌人 death 事实仍按 P6 smoke 合同触发，用于维持现有 HP/death 探针闭环。

## 2026-07-07 P6.13 continuous network enemy server tick local smoke 记录

P6.13 在 P6.12 ECS server tick 基线上，把网络敌人从“只发布首个正式攻击 commit”推进到本机可验证的连续攻击节奏。本阶段只做本机 Mac server/client 验证，尚未构建 Linux 包或部署 ECS。

新增本地合同：

- `NetworkEnemyAvatar.ShouldAcceptServerBrainAttackCommit()` 明确区分 server tick 和 smoke bridge：server tick 可在正式 `EnemyAttackController` 冷却结束后重复接收 `AttackCommitted`，旧 brain smoke bridge 仍保持一次性攻击，敌人死亡后不再接收。
- `TryCommitServerBrainAttackFromFormalState()` 在 server gameplay tick 模式下不再被第一次攻击后的 `serverEnemyAttackApplied` 闸门拦住。
- `probe_p15_multiplayer.py` 新增 `--min-network-enemy-server-tick-attacks`，要求服务端日志至少出现 N 条 `Server tick enemy attack applied`。
- P6.13 本机 smoke 使用 `--min-network-enemy-server-tick-attacks 2`，实际服务端日志出现 3 次。

验证结果：

- Unity 编译：`/tmp/unity_compile_p613_server_tick_repeat.log`，退出码 `0`。
- Python 探针静态检查：`python3 -m py_compile Deploy/DedicatedServer/probe_p15_multiplayer.py` 通过。
- 定向 EditMode：`/tmp/unity_editmode_p613_dedicated_results.xml = 29/29 Passed`。
- wrapper `Tools/unity-cli/unity-run-tests` 的临时 clone 曾两次卡在 Unity 启动层数据库/超时问题；改用 Unity 原生 `-runTests -testFilter CampusRPG.Tests.EditMode.DedicatedServerBuildUtilityTests` 在当前工程完成验证并退出码 `0`。
- Mac server build：`/tmp/unity_p613_macos_server_build.log`，`Build Finished, Result: Success.`
- Mac client build：`/tmp/unity_p613_mac_client_build.log`，`Build Finished, Result: Success.`

本机 P6.13 smoke 命令：

```bash
python3 Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --game-port 8073 \
  --health-port 8074 \
  --server-log /tmp/TY_NEW_p613_server_tick_repeat_server.log \
  --client1-log /tmp/TY_NEW_p613_server_tick_repeat_client1.log \
  --client2-log /tmp/TY_NEW_p613_server_tick_repeat_client2.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --require-death-sync \
  --require-network-enemy-sync \
  --require-formal-network-enemy-sync \
  --require-network-enemy-attack-sync \
  --require-network-enemy-chase-sync \
  --require-network-enemy-navmesh-chase \
  --require-network-enemy-server-tick \
  --min-network-enemy-server-tick-attacks 2 \
  --min-network-enemy-chase-distance 2.0 \
  --smoke-attack-count 4 \
  --smoke-attack-interval-seconds 0.75 \
  --smoke-move-delay-seconds 8 \
  --client1-quit-after-seconds 45 \
  --client2-quit-after-seconds 28
```

本机 P6.13 smoke 结果：

- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- client2 退出后 health：`networkConnectedClients=1 networkSpawnedPlayers=1`
- 双客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=2.80`
- 玩家 formal 回归：`P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`
- 网络敌人追击同步：`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=2.08 client2ObservedEnemyMoveDistance=2.08`
- 服务端 NavMesh 追击硬校验：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- 服务端连续 tick 硬校验：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=3`
- 网络敌人 HP/death 同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthDrop=50 client2ObservedEnemyHealthDrop=50`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- server tick 敌人攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=local client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=remote client2ObservedTargetHealthDrop=25 client1ObservedFormalEnemyAttackLater=true client2ObservedFormalEnemyAttackLater=true`
- 总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8073 healthPort=8074 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`

本机 server 日志证据：

- NavMesh ready：`[MultiplayerEnemy] Server tick enemy status ... state=EnemyIdleGuardState ... navMeshAgentEnabled=True navMeshReady=True serverTick=True`
- 追击：`[MultiplayerEnemy] Server tick enemy status ... state=EnemyChaseState currentTarget=FormalPlayer_CombatTest currentTargetDistance=3.63 ... navMeshReady=True serverTick=True`
- 第 1 次网络伤害：`[MultiplayerEnemy] Server tick enemy attack applied ... health=100->75 ... enemyPosition=-1.00,0.00,1.97`
- 第 2 次网络伤害：`[MultiplayerEnemy] Server tick enemy attack applied ... health=75->50 ... enemyPosition=-1.00,0.00,1.35`
- 第 3 次网络伤害：`[MultiplayerEnemy] Server tick enemy attack applied ... health=50->25 ... enemyPosition=-1.00,0.00,1.00`

已知边界：

- P6.13 本机证明连续 server tick 攻击节奏可以重复写入网络 HP；尚未构建 Linux 包或部署 ECS。
- 仍只有单只网络敌人，尚未验证多敌人仇恨、目标切换、多目标、受击动画、掉落或长时间稳定性。
- 敌人 death 事实仍按 P6 smoke 合同触发，用于维持当前 HP/death 探针闭环。

## 2026-07-07 P6.12 network enemy server tick ECS 部署记录

P6.12 把 P6.11 的最小非 smoke 服务端敌人 gameplay tick 构建为 Linux Dedicated Server 包，并部署到 2 核 2G ECS 完成公网双客户端验证。该阶段验证 P6.11 的 `--enable-network-enemy-server-tick` 路径可以在远端 Linux Dedicated Server 上保持 `navMeshReady=True` 追击、正式 `EnemyAttackController.AttackCommitted` 提交和网络敌人同步；仍不是多敌人、掉落、受击动画或正式容量结论。

构建与部署：

- Linux Dedicated Server build：`/tmp/unity_p612_server_tick_linux_build.log`，`Build Finished, Result: Success.`
- P6.12 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p612-server-tick.tar.gz`
- 包大小：约 `79M`
- 本地 SHA256：`b9cf2bfe4bd33662e2bcbbe78cdefad5c1009287f235174c5ec546be74e726e7`
- ECS 远端 SHA256：`b9cf2bfe4bd33662e2bcbbe78cdefad5c1009287f235174c5ec546be74e726e7`
- systemd：`ty-new-server.service` 为 `active`
- systemd 启动参数：`--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --enable-network-enemy-server-tick`

部署命令：

```bash
TY_NEW_SERVER_PACKAGE=<PROJECT_ROOT>/Builds/DedicatedServer/TYServer-linux-x86_64-p612-server-tick.tar.gz \
  Deploy/DedicatedServer/deploy_p1_gameplay.sh \
  <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.12 smoke 命令：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_NETWORK_ENEMY_SYNC=1 \
TY_NEW_REQUIRE_FORMAL_NETWORK_ENEMY_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_ATTACK_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_CHASE_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_NAVMESH_CHASE=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_SERVER_TICK=1 \
TY_NEW_MIN_NETWORK_ENEMY_CHASE_DISTANCE=2.0 \
TY_NEW_SMOKE_MOVE_DELAY_SECONDS=8 \
TY_NEW_CLIENT1_SECONDS=40 \
TY_NEW_CLIENT2_SECONDS=22 \
TY_NEW_CONNECTED_TIMEOUT=60 \
TY_NEW_SMOKE_ATTACK_COUNT=4 \
TY_NEW_SMOKE_ATTACK_INTERVAL_SECONDS=0.75 \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh \
  <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.12 smoke 结果：

- ECS health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- ECS UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- client2 退出后 health：`networkConnectedClients=1 networkSpawnedPlayers=1`
- 双客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=0.40`
- 玩家 formal 回归：`P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`
- 网络敌人追击同步：`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=3.46 client2ObservedEnemyMoveDistance=3.62`
- 服务端 NavMesh 追击硬校验：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- 服务端非 smoke tick 硬校验：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true`
- 网络敌人 HP/death 同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthDrop=50 client2ObservedEnemyHealthDrop=50`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- server tick 敌人攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=remote client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=local client2ObservedTargetHealthDrop=50 client1ObservedFormalEnemyAttackLater=true client2ObservedFormalEnemyAttackLater=true`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`

ECS server 日志证据：

- 日志来源：`/tmp/TY_NEW_p612_ecs_server_live.log`，由 `verify_p15_ecs_multiplayer.sh` 实时 tail 远端 `/var/log/ty-new/server.log`。
- NavMesh ready：`[MultiplayerEnemy] Server tick enemy status ... state=EnemyIdleGuardState ... navMeshAgentEnabled=True navMeshReady=True serverTick=True`
- NavMesh chase：`[MultiplayerEnemy] Server tick enemy status ... state=EnemyChaseState currentTarget=FormalPlayer_CombatTest currentTargetDistance=3.63 ... navMeshAgentEnabled=True navMeshReady=True serverTick=True`
- 进入攻击态：`[MultiplayerEnemy] Server tick enemy status ... state=EnemyAttackState currentTarget=FormalPlayer_CombatTest currentTargetDistance=1.88 ... navMeshAgentEnabled=True navMeshReady=True serverTick=True`
- 正式 attack commit：`[MultiplayerEnemy] Server tick enemy attack committed enemyId=1 targetOwner=1 targetHealth=100 targetDead=False formalDamage=10 attackId=Enemy_Melee`
- 服务端权威写 HP：`[MultiplayerEnemy] Server tick enemy attack applied enemyId=1 targetOwner=1 damage=25 formalDamage=10 attackId=Enemy_Melee health=100->75 targetDead=False enemyPosition=-1.00,0.00,2.00 targetPosition=-1.00,0.00,0.00`

收尾健康检查：

- smoke 后公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- smoke 后 systemd：`ty-new-server.service` 仍为 `active`

已知边界：

- P6.12 证明的是单只网络敌人的最小非 smoke server tick 能在 ECS 公网双客户端路径跑通。
- 当前网络伤害仍只发布首个正式敌人 `AttackCommitted`，避免 smoke 中连续敌人攻击反复写玩家 HP；正式攻击频率、连续攻击、目标切换和多目标规则还未设计。
- 敌人 death 事实仍按 P6 smoke 合同触发，用于维持现有 HP/death 探针闭环。
- 仍未验证多敌人仇恨、受击动画、掉落、长时间稳定性、4 人上限或正式公网容量。

## 2026-07-07 P6.11 network enemy server tick local smoke 记录

P6.11 在 P6.10 ECS baked NavMesh chase smoke 基础上，把网络敌人推进到最小非 smoke 服务端 gameplay tick。本阶段只做本机 Mac server/client 验证，尚未构建 Linux 包或部署 ECS。

新增本地合同：

- 新服务端启动参数：`--enable-network-enemy-server-tick` / `--network-enemy-server-tick`。
- `ServerRuntimeBootstrap.ShouldEnableNetworkEnemyGameplayTick()` 解析该参数，并经 `MultiplayerNetworkSessionService.ConfigureServerEnemyGameplayTick()` 注入 `NetworkEnemyAvatar`。
- `NetworkEnemyAvatar` 在双客户端接入后延迟 `2.0s` 放开 server formal 敌人驱动；客户端 formal 敌人驱动继续 suppressed。
- server tick 路径复用正式 `EnemyBrain.Update()`、`EnemyStateMachine`、`EnemyMotor` 的 baked NavMesh 追击，以及 `EnemyAttackController.AttackCommitted`。
- 网络层只提交 formal driver 位姿到 `NetworkEnemyAvatar`，并把第一次正式敌人 attack commit 转成 server-write 玩家 HP 与 formal 敌人攻击表现事实。
- `probe_p15_multiplayer.py` 新增 `--require-network-enemy-server-tick` 和总结字段 `networkEnemyServerTick=`；该开关要求 server log 出现 `Server tick enemy status ... navMeshReady=True serverTick=True` 和 `Server tick enemy attack applied`。

验证结果：

- Unity 编译：`/tmp/unity_compile.log`，退出码 `0`
- Python 探针静态检查：`python3 -m py_compile Deploy/DedicatedServer/probe_p15_multiplayer.py` 通过。
- EditMode 全量：`/tmp/unity_editmode_results.xml = 492/508 Passed, 16 Failed`；失败项均为既有资源/动画/预览基线或 release dependency 检查，新增相关用例 `ServerRuntimeBootstrap_CommandLineSettingsUseDefaultsAndClampValues` 与 `NetworkEnemyAvatar_UsesServerOwnedSpawnAndDeathFacts` 均为 Passed。
- Mac server build：`/tmp/unity_p611_macos_server_build.log`，`Build Finished, Result: Success.`
- Mac client build：`/tmp/unity_p611_mac_client_build.log`，`Build Finished, Result: Success.`
- 首次未提升权限本机 smoke 被 macOS sandbox 拦截，server app abort 且 health 报 `Operation not permitted`；提升权限重跑同一命令通过。

本机 P6.11 smoke 命令：

```bash
python3 Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --game-port 8071 \
  --health-port 8072 \
  --server-log /tmp/TY_NEW_p611_server_tick_server.log \
  --client1-log /tmp/TY_NEW_p611_server_tick_client1.log \
  --client2-log /tmp/TY_NEW_p611_server_tick_client2.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --require-death-sync \
  --require-network-enemy-sync \
  --require-formal-network-enemy-sync \
  --require-network-enemy-attack-sync \
  --require-network-enemy-chase-sync \
  --require-network-enemy-navmesh-chase \
  --require-network-enemy-server-tick \
  --min-network-enemy-chase-distance 2.0 \
  --smoke-attack-count 4 \
  --smoke-attack-interval-seconds 0.75 \
  --smoke-move-delay-seconds 8 \
  --client1-quit-after-seconds 40 \
  --client2-quit-after-seconds 22
```

本机 P6.11 smoke 结果：

- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- client2 退出后 health：`networkConnectedClients=1 networkSpawnedPlayers=1`
- 双客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=2.86`
- 玩家 formal 回归：`P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`
- 网络敌人追击同步：`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=3.62 client2ObservedEnemyMoveDistance=3.62`
- 服务端 NavMesh 追击硬校验：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- 服务端非 smoke tick 硬校验：`P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true`
- 网络敌人 HP/death 同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthDrop=50 client2ObservedEnemyHealthDrop=50`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- server tick 敌人攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=local client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=remote client2ObservedTargetHealthDrop=25 client1ObservedFormalEnemyAttackLater=true client2ObservedFormalEnemyAttackLater=true`
- 总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8071 healthPort=8072 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true networkEnemyServerTick=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`

本机 server 日志证据：

- 服务端没有 `Failed to create agent because there is no valid NavMesh` fallback warning。
- server tick armed：`[MultiplayerEnemy] Armed server enemy gameplay tick enemyId=1 connectedClients=2 delaySeconds=2.0 spawnPosition=-1.00,0.00,5.00`
- NavMesh ready：`[MultiplayerEnemy] Server tick enemy status ... state=EnemyIdleGuardState ... navMeshAgentEnabled=True navMeshReady=True serverTick=True`
- NavMesh chase：`[MultiplayerEnemy] Server tick enemy status ... state=EnemyChaseState currentTarget=FormalPlayer_CombatTest currentTargetDistance=3.63 ... navMeshAgentEnabled=True navMeshReady=True serverTick=True`
- 进入攻击态：`[MultiplayerEnemy] Server tick enemy status ... state=EnemyAttackState currentTarget=FormalPlayer_CombatTest currentTargetDistance=1.88 ... navMeshAgentEnabled=True navMeshReady=True serverTick=True`
- 正式 attack commit：`[MultiplayerEnemy] Server tick enemy attack committed enemyId=1 targetOwner=1 targetHealth=100 targetDead=False formalDamage=10 attackId=Enemy_Melee`
- 服务端权威写 HP：`[MultiplayerEnemy] Server tick enemy attack applied enemyId=1 targetOwner=1 damage=25 formalDamage=10 attackId=Enemy_Melee health=100->75 targetDead=False enemyPosition=-1.00,0.00,2.00 targetPosition=-1.00,0.00,0.00`
- 敌人死亡事实仍按 P6 smoke 合同触发：`[MultiplayerEnemy] Smoke enemy death applied enemyId=1 health=50->0 enemyDead=True`

已知边界：

- P6.11 的追击与攻击提交已走 server formal enemy tick，但敌人 death 事实仍是 P6 smoke 合同，用于维持现有 HP/death 探针闭环。
- 当前网络伤害只发布首个正式敌人 `AttackCommitted`，避免测试期间连续敌人攻击反复写玩家 HP；后续要设计正式攻击频率、目标选择和多目标规则。
- P6.11 本阶段尚未构建 Linux 包或部署 ECS；后续 P6.12 已补上 `--enable-network-enemy-server-tick` 的 ECS 部署和公网双客户端复验。

## 2026-07-07 P6.10 ServerBoot baked NavMesh enemy chase ECS 部署记录

P6.10 把 P6.9 ServerBoot baked NavMesh 敌人追击 smoke 构建为 Linux Dedicated Server 包，并部署到 2 核 2G ECS 完成公网双客户端验证。该阶段验证的是 P6.9 的 `navMeshReady=True` 追击路径可以在远端 Linux Dedicated Server 上跑通；仍不代表完整非 smoke 敌人 gameplay tick、多敌人仇恨、受击动画、掉落或正式容量已经完成。

部署/验证入口更新：

- `Deploy/DedicatedServer/ty-new-server.service` 当前启动参数包含：`--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --enable-network-enemy-brain-chase-attack-smoke`
- `Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh` 新增 `TY_NEW_REQUIRE_NETWORK_ENEMY_CHASE_SYNC=1`、`TY_NEW_REQUIRE_NETWORK_ENEMY_NAVMESH_CHASE=1`、`TY_NEW_MIN_NETWORK_ENEMY_CHASE_DISTANCE` 和 `TY_NEW_USE_BRAIN_CHASE_NETWORK_ENEMY_ATTACK_SMOKE` 支持。
- 当 `TY_NEW_REQUIRE_NETWORK_ENEMY_NAVMESH_CHASE=1` 时，验证脚本会通过 SSH 把远端 `/var/log/ty-new/server.log` 实时 tail 到本机 `/tmp/TY_NEW_p610_ecs_server_live.log`，让 `probe_p15_multiplayer.py --require-network-enemy-navmesh-chase` 能在同一轮公网 smoke 中输出 `networkEnemyNavMeshChaseSync=true`。

包与构建：

- Linux Dedicated Server build：`/tmp/unity_p610_navmesh_linux_build.log`
- Linux build 结果：`Build Finished, Result: Success.`
- 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p610-navmesh-chase.tar.gz`
- 包大小：约 `79M`
- 本地 SHA256：`0367f23c10a10647a3e6ea12a51395cb43d0771347ed3f7d706f8c10e2573fbe`
- ECS 远端 SHA256：`0367f23c10a10647a3e6ea12a51395cb43d0771347ed3f7d706f8c10e2573fbe`
- systemd：`ty-new-server.service` 为 `active`
- 部署后 health：`P1.5_HEALTH_OK host=127.0.0.1 healthPort=7778 networkPort=7777 connected=0 spawned=0`
- 部署后 P1 TCP gameplay probe：`ECSSmoke` join/ping/room/bye 通过。

部署命令：

```bash
TY_NEW_SERVER_PACKAGE=<PROJECT_ROOT>/Builds/DedicatedServer/TYServer-linux-x86_64-p610-navmesh-chase.tar.gz \
  Deploy/DedicatedServer/deploy_p1_gameplay.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.10 验证命令：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_NETWORK_ENEMY_SYNC=1 \
TY_NEW_REQUIRE_FORMAL_NETWORK_ENEMY_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_ATTACK_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_CHASE_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_NAVMESH_CHASE=1 \
TY_NEW_MIN_NETWORK_ENEMY_CHASE_DISTANCE=2.0 \
TY_NEW_SMOKE_MOVE_DELAY_SECONDS=8 \
TY_NEW_CLIENT1_SECONDS=38 \
TY_NEW_CLIENT2_SECONDS=20 \
TY_NEW_CONNECTED_TIMEOUT=60 \
TY_NEW_SMOKE_ATTACK_COUNT=4 \
TY_NEW_SMOKE_ATTACK_INTERVAL_SECONDS=0.45 \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.10 smoke 结果：

- public health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- client2 退出后 health：`networkConnectedClients=1 networkSpawnedPlayers=1`
- 双客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=4.13`
- 玩家 formal 回归：`P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`
- 网络敌人追击同步：`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=3.27 client2ObservedEnemyMoveDistance=3.62`
- 服务端 NavMesh 追击硬校验：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- 网络敌人 HP/death 同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthDrop=50 client2ObservedEnemyHealthDrop=50`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- EnemyBrain NavMesh 追击后攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=remote client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=local client2ObservedTargetHealthDrop=50 client1ObservedFormalEnemyAttackLater=true client2ObservedFormalEnemyAttackLater=true`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`
- smoke 后公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`

远端 server 日志证据：

- 服务端没有 `Failed to create agent because there is no valid NavMesh` fallback warning。
- chase smoke armed：`[MultiplayerEnemy] Armed brain chase smoke enemy attack enemyId=1 connectedClients=2 delaySeconds=2.0 spawnPosition=-1.00,0.00,5.00`
- NavMesh ready：`[MultiplayerEnemy] Brain smoke enemy attack status ... state=EnemyIdleGuardState ... navMeshAgentEnabled=True navMeshReady=True`
- NavMesh chase：`[MultiplayerEnemy] Brain smoke enemy attack status ... state=EnemyChaseState currentTarget=FormalPlayer_CombatTest currentTargetDistance=3.63 ... navMeshAgentEnabled=True navMeshReady=True`
- 进入攻击态：`[MultiplayerEnemy] Brain smoke enemy attack status ... state=EnemyAttackState currentTarget=FormalPlayer_CombatTest currentTargetDistance=1.88 ... navMeshAgentEnabled=True navMeshReady=True`
- 服务端权威写 HP：`[MultiplayerEnemy] Brain smoke enemy attack applied enemyId=1 targetOwner=1 damage=25 formalDamage=10 attackId=Enemy_Melee health=100->75 targetDead=False enemyPosition=-1.00,0.00,2.00 targetPosition=-1.00,0.00,0.00`
- 敌人死亡事实仍按 P6 smoke 合同触发：`[MultiplayerEnemy] Smoke enemy death applied enemyId=1 health=50->0 enemyDead=True`

已知边界：

- `--enable-network-enemy-brain-chase-attack-smoke` 仍是专项验证开关，不应被误认为完整服务端敌人 gameplay tick 已接入。
- P6.10 只证明单只网络敌人的 baked NavMesh chase smoke、HP/death、formal 表现和攻击事实能在 ECS 公网双客户端路径跑通；尚未验证非 smoke 循环、多敌人仇恨目标、掉落、长时间稳定性或 2-4 人容量。
- 当前 UDP 7777 已临时放行 `0.0.0.0/0` 用于公网验证；长期 playtest 前建议收窄来源。

## 2026-07-07 P6.9 ServerBoot baked NavMesh enemy chase local smoke 记录

P6.9 在 P6.8 fallback chase 基础上，为 ServerBoot 增加 server-only baked NavMesh 基线，并用本机 Mac server/client 验证服务端 formal `EnemyBrain` / `EnemyMotor` 在 `navMeshReady=True` 下从距离外追击到攻击范围后提交攻击。该阶段尚未构建或部署 Linux/ECS 包。

新增本地合同：

- `DedicatedServerBuildUtility.CreateOrRepairServerBootScene()` 会创建 `ServerNavMeshGround` 并 bake `Assets/_Game/Scenes/ServerBoot/NavMesh.asset`。
- `DedicatedServerBuildUtility.ValidateBuildInputs()` 会校验 ServerBoot 场景包含 `ServerNavMeshGround`，且 `m_NavMeshData` 不再是 `{fileID: 0}`。
- `NetworkEnemyAvatar` 的 Brain smoke 状态日志新增 `navMeshAgentEnabled=` 和 `navMeshReady=`。
- `probe_p15_multiplayer.py` 新增 `--require-network-enemy-navmesh-chase`；该开关会要求 chase smoke、网络敌人追击位移同步、服务端日志 `navMeshReady=True`，并拒绝服务端日志中的 `Failed to create agent because there is no valid NavMesh` fallback warning。

验证结果：

- ServerBoot rebuild：`/tmp/unity_p69_rebuild_serverboot_navmesh.log`，退出码 `0`
- ServerBoot scene：`Assets/_Game/Scenes/ServerBoot.unity` 中 `m_NavMeshData: {fileID: 23800000, guid: bcd24031822b64d70811e1b370079146, type: 2}`
- ServerBoot NavMesh asset：`Assets/_Game/Scenes/ServerBoot/NavMesh.asset`
- 定向 EditMode：`/tmp/unity_editmode_p69_navmesh_results.xml = 29/29 Passed`
- 新增测试：`ServerBootScene_ContainsBakedNavMeshForEnemyChaseSmoke`
- Python 探针静态检查：`python3 -m py_compile Deploy/DedicatedServer/probe_p15_multiplayer.py` 通过。
- Mac server build：`/tmp/unity_p69_macos_server_build.log`，`Build Finished, Result: Success.`
- Mac client build：`/tmp/unity_p69_mac_client_build.log`，`Build Finished, Result: Success.`

本机 P6.9 smoke 命令：

```bash
python3 Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --game-port 8061 \
  --health-port 8062 \
  --server-log /tmp/TY_NEW_p69_navmesh_enemy_chase_server.log \
  --client1-log /tmp/TY_NEW_p69_navmesh_enemy_chase_client1.log \
  --client2-log /tmp/TY_NEW_p69_navmesh_enemy_chase_client2.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --require-death-sync \
  --require-network-enemy-sync \
  --require-formal-network-enemy-sync \
  --require-network-enemy-attack-sync \
  --require-network-enemy-chase-sync \
  --require-network-enemy-navmesh-chase \
  --use-brain-chase-network-enemy-attack-smoke \
  --min-network-enemy-chase-distance 2.0 \
  --smoke-attack-count 4 \
  --smoke-attack-interval-seconds 0.75 \
  --smoke-move-delay-seconds 8 \
  --client1-quit-after-seconds 38 \
  --client2-quit-after-seconds 20
```

本机 P6.9 smoke 结果：

- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- client2 退出后 health：`networkConnectedClients=1 networkSpawnedPlayers=1`
- 双客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=2.80`
- 玩家 formal 回归：`P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`
- 网络敌人追击同步：`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=3.67 client2ObservedEnemyMoveDistance=3.67`
- 服务端 NavMesh 追击硬校验：`P6_NETWORK_ENEMY_NAVMESH_CHASE_OK navMeshReady=true`
- 网络敌人 HP/death 同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthDrop=50 client2ObservedEnemyHealthDrop=50`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- EnemyBrain NavMesh 追击后攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=local client1ObservedTargetHealthStart=100 client1ObservedTargetHealthLater=75 client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=remote client2ObservedTargetHealthStart=100 client2ObservedTargetHealthLater=75 client2ObservedTargetHealthDrop=25 client1ObservedFormalEnemyAttackStart=false client1ObservedFormalEnemyAttackLater=true client2ObservedFormalEnemyAttackStart=false client2ObservedFormalEnemyAttackLater=true`
- 总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8061 healthPort=8062 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true networkEnemyChaseSync=true networkEnemyNavMeshChaseSync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`

本机 server 日志证据：

- 服务端没有 `Failed to create agent because there is no valid NavMesh` fallback warning。
- NavMesh ready：`[MultiplayerEnemy] Brain smoke enemy attack status ... state=EnemyIdleGuardState ... navMeshAgentEnabled=True navMeshReady=True`
- NavMesh chase：`[MultiplayerEnemy] Brain smoke enemy attack status ... state=EnemyChaseState currentTarget=FormalPlayer_CombatTest currentTargetDistance=3.61 ... navMeshAgentEnabled=True navMeshReady=True`
- 进入攻击态：`[MultiplayerEnemy] Brain smoke enemy attack status ... state=EnemyAttackState currentTarget=FormalPlayer_CombatTest currentTargetDistance=1.84 ... navMeshAgentEnabled=True navMeshReady=True`
- 服务端权威写 HP：`[MultiplayerEnemy] Brain smoke enemy attack applied enemyId=1 targetOwner=1 damage=25 formalDamage=10 attackId=Enemy_Melee health=100->75 targetDead=False enemyPosition=-1.00,0.00,1.95 targetPosition=-1.00,0.00,0.00`

已知边界：

- `--require-network-enemy-navmesh-chase` 是本机探针硬校验，不是 gameplay 模式开关；server 仍通过 `--enable-network-enemy-brain-chase-attack-smoke` 启用 P6.8/P6.9 chase smoke。
- 客户端日志仍可能出现一次 NavMeshAgent warning，因为客户端显示场景没有 baked NavMesh 且客户端 formal 敌人驱动被 suppressed；P6.9 只要求服务端导航路径 `navMeshReady=True`。
- P6.9 尚未构建 Linux 包或部署 ECS；下一步要验证 Linux Dedicated Server 包在 ECS 上也能加载 ServerBoot NavMesh 并保持同样的 `navMeshReady=True` 追击路径。

## 2026-07-07 P6.8 EnemyBrain chase enemy attack local smoke 记录

P6.8 在 P6.7 EnemyBrain 攻击提交基础上，把本机验证推进到距离外追击后再进入攻击范围提交。该阶段只做本机 Mac server/client 验证，尚未构建或部署新的 Linux/ECS 包。

新增本地合同：

- 新服务端启动参数：`--enable-network-enemy-brain-chase-attack-smoke` / `--network-enemy-brain-chase-attack-smoke`。
- `probe_p15_multiplayer.py` 新增 `--use-brain-chase-network-enemy-attack-smoke`、`--require-network-enemy-chase-sync` 和 `--min-network-enemy-chase-distance`。
- chase smoke 会把 formal 网络敌人生成在 `-1,0,5`，等待两个客户端连接后放开 formal 敌人驱动。
- `EnemyBrain` / `EnemySensing` 负责选中 formal 玩家；ServerBoot 当前没有 baked NavMesh，`EnemyMotor` 会记录 `Failed to create agent because there is no valid NavMesh` 并走 fallback 追击。
- `NetworkEnemyPresentationBridge` 在服务端把 formal 子树位姿提交回 `NetworkEnemyAvatar` 网络根节点，让客户端只观察 network enemy 位移、formal 敌人攻击表现和玩家 HP 变化。
- 最终权威 HP 仍由 `NetworkPlayerAvatar.ApplyServerEnemyDamage()` 写入 `NetworkVariable`；客户端不提交敌人命中或伤害。

验证结果：

- Python 探针静态检查：`python3 -m py_compile Deploy/DedicatedServer/probe_p15_multiplayer.py` 通过。
- Unity 编译：`/tmp/unity_compile_p68_chase.log`，退出码 `0`
- Mac server build：`/tmp/unity_p68_macos_server_build.log`，`Build Finished, Result: Success.`
- Mac client build：`/tmp/unity_p68_mac_client_build.log`，`Build Finished, Result: Success.`

本机 P6.8 smoke 命令：

```bash
python3 Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --game-port 8059 \
  --health-port 8060 \
  --server-log /tmp/TY_NEW_p68_brain_chase_enemy_attack_rerun_server.log \
  --client1-log /tmp/TY_NEW_p68_brain_chase_enemy_attack_rerun_client1.log \
  --client2-log /tmp/TY_NEW_p68_brain_chase_enemy_attack_rerun_client2.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --require-death-sync \
  --require-network-enemy-sync \
  --require-formal-network-enemy-sync \
  --require-network-enemy-attack-sync \
  --require-network-enemy-chase-sync \
  --use-brain-chase-network-enemy-attack-smoke \
  --min-network-enemy-chase-distance 2.0 \
  --smoke-attack-count 4 \
  --smoke-attack-interval-seconds 0.75 \
  --smoke-move-delay-seconds 8 \
  --client1-quit-after-seconds 38 \
  --client2-quit-after-seconds 20
```

本机 P6.8 smoke 结果：

- 连接中 health：`networkConnectedClients=2 networkSpawnedPlayers=2`
- client2 退出后 health：`networkConnectedClients=1 networkSpawnedPlayers=1`
- 双客户端退出后 health：`networkConnectedClients=0 networkSpawnedPlayers=0`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=3.01`
- 玩家 formal 回归：`P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`
- 网络敌人追击同步：`P6_NETWORK_ENEMY_CHASE_SYNC_OK client1ObservedEnemyMoveDistance=2.57 client2ObservedEnemyMoveDistance=2.57`
- 网络敌人 HP/death 同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthDrop=50 client2ObservedEnemyHealthDrop=50`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- EnemyBrain 追击后攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=local client1ObservedTargetHealthStart=100 client1ObservedTargetHealthLater=75 client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=remote client2ObservedTargetHealthStart=100 client2ObservedTargetHealthLater=75 client2ObservedTargetHealthDrop=25 client1ObservedFormalEnemyAttackStart=false client1ObservedFormalEnemyAttackLater=true client2ObservedFormalEnemyAttackStart=false client2ObservedFormalEnemyAttackLater=true`
- 总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8059 healthPort=8060 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true networkEnemyChaseSync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`

本机 server 日志证据：

- NavMesh fallback：`Failed to create agent because there is no valid NavMesh`
- chase smoke armed：`[MultiplayerEnemy] Armed brain chase smoke enemy attack enemyId=1 connectedClients=2 delaySeconds=2.0 spawnPosition=-1.00,0.00,5.00`
- 追击状态：`[MultiplayerEnemy] Brain smoke enemy attack status enemyId=1 state=EnemyChaseState currentTarget=FormalPlayer_CombatTest currentTargetDistance=3.37 canAttackCurrentTarget=True currentTargetHasClearShot=True sensedTarget=FormalPlayer_CombatTest networkTargetOwner=1`
- formal 提交事件：`[MultiplayerEnemy] Brain smoke enemy attack committed enemyId=1 targetOwner=1 targetHealth=100 targetDead=False formalDamage=10 attackId=Enemy_Melee`
- 服务端权威写 HP：`[MultiplayerEnemy] Brain smoke enemy attack applied enemyId=1 targetOwner=1 damage=25 formalDamage=10 attackId=Enemy_Melee health=100->75 targetDead=False enemyPosition=-1.00,0.00,1.97 targetPosition=-1.00,0.00,0.00`
- 敌人死亡事实仍按 P6 smoke 合同触发：`[MultiplayerEnemy] Smoke enemy death applied enemyId=1 health=50->0 enemyDead=True`

已知边界：

- `--enable-network-enemy-brain-chase-attack-smoke` 是 P6.8 专项验证开关，不应被误认为完整服务端敌人 gameplay tick 已接入。
- P6.8 验证的是 ServerBoot 无有效 NavMesh 时的 `EnemyMotor` fallback 追击、network enemy 位移同步和攻击提交；尚未验证 baked NavMesh 路径追击、非 smoke 攻击循环、多敌人仇恨目标、敌人受击动画、掉落或 ECS 公网部署。

## 2026-07-07 P6.7 EnemyBrain enemy attack ECS 部署记录

P6.7 把 P6.6 EnemyBrain 敌人攻击 smoke 构建为 Linux Dedicated Server 包，并部署到 ECS 完成公网双客户端验证。该阶段验证 P6.6 的 Brain smoke bridge 在 2 核 2G ECS 公网路径成立；仍不代表完整 NavMesh 距离外追击、非 smoke 攻击循环、多敌人仇恨、受击动画或掉落已经完成。

包与构建：

- Linux Dedicated Server build：`/tmp/unity_p67_brain_enemy_linux_build_direct.log`
- Linux build 结果：`Build Finished, Result: Success.`
- 首次使用 `Tools/unity-cli/ty-new-build-dedicated-server linux --hub-licensing --licensing-ipc LicenseClient-don` 时卡在 Unity licensing IPC，已中断失败进程，改用直接 Unity batchmode `DedicatedServerBuildUtility.BuildLinuxDedicatedServer` 成功构建。
- 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p67-brain-enemy.tar.gz`
- 包大小：约 `79M`
- 本地 SHA256：`6c46f5dca899128aaf09abcab122278d8de791529410e3b75579ce8a9a58eaf4`
- ECS 远端 SHA256：`6c46f5dca899128aaf09abcab122278d8de791529410e3b75579ce8a9a58eaf4`
- systemd：`ty-new-server.service` 为 `active`
- 当前启动参数包含：`--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest --enable-network-enemy-brain-attack-smoke`
- ECS 进程观察：`Memory: 144.0M (peak: 144.5M)`，该值只是 smoke 后短时观察，不是容量结论。

部署命令：

```bash
TY_NEW_SERVER_PACKAGE=<PROJECT_ROOT>/Builds/DedicatedServer/TYServer-linux-x86_64-p67-brain-enemy.tar.gz \
  Deploy/DedicatedServer/deploy_p1_gameplay.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.7 验证命令：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
TY_NEW_REQUIRE_NETWORK_ENEMY_SYNC=1 \
TY_NEW_REQUIRE_FORMAL_NETWORK_ENEMY_SYNC=1 \
TY_NEW_REQUIRE_NETWORK_ENEMY_ATTACK_SYNC=1 \
TY_NEW_SMOKE_MOVE_DELAY_SECONDS=8 \
TY_NEW_CLIENT2_SECONDS=18 \
TY_NEW_CONNECTED_TIMEOUT=60 \
  Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网 P6.7 验证结果：

- ECS health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=3.07`
- 玩家 formal 回归：`P5_FORMAL_ATTACK_SYNC_OK`、`P5_FORMAL_HIT_SYNC_OK`、`P5_FORMAL_DEATH_SYNC_OK`
- 网络敌人同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthDrop=50 client2ObservedEnemyHealthDrop=50`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- EnemyBrain 敌人攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=remote client1ObservedTargetHealthStart=100 client1ObservedTargetHealthLater=75 client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=local client2ObservedTargetHealthStart=100 client2ObservedTargetHealthLater=75 client2ObservedTargetHealthDrop=25 client1ObservedFormalEnemyAttackStart=false client1ObservedFormalEnemyAttackLater=true client2ObservedFormalEnemyAttackStart=false client2ObservedFormalEnemyAttackLater=true`
- 总结：`P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`
- smoke 后公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`

ECS server 日志证据：

- active prefab：`Network player prefab command-line path overrides scenePath=Multiplayer/PF_NetworkPlayerAvatar activePath=Multiplayer/PF_NetworkPlayerCombatTest`
- 网络敌人生成：`[MultiplayerNetwork] Spawned server network enemy prefab=Multiplayer/PF_NetworkEnemyAvatar networkObjectId=1`
- Brain 自动感知目标：`[MultiplayerEnemy] Brain smoke enemy attack status ... sensedTarget=FormalPlayer_CombatTest ... formalTargetDistance=1.25 brainEnabled=True sensingEnabled=True attackControllerEnabled=True`
- formal 提交事件：`[MultiplayerEnemy] Brain smoke enemy attack committed enemyId=1 targetOwner=1 targetHealth=100 targetDead=False formalDamage=10 attackId=Enemy_Melee`
- 服务端权威写 HP：`[MultiplayerEnemy] Brain smoke enemy attack applied enemyId=1 targetOwner=1 damage=25 formalDamage=10 attackId=Enemy_Melee health=100->75 targetDead=False`
- 敌人死亡事实仍按 P6 smoke 合同触发：`[MultiplayerEnemy] Smoke enemy death applied enemyId=1 health=50->0 enemyDead=True`
- smoke 后回落：heartbeat 记录 `networkConnectedClients=0 networkSpawnedPlayers=0`

已知边界：

- `--enable-network-enemy-brain-attack-smoke` 是 P6.7 专项验证开关，不应被误认为完整服务端敌人 gameplay tick 已接入。
- 该阶段验证的是服务端 formal `EnemyBrain` 自动选中目标并进入攻击态后的 smoke bridge 提交；尚未验证 NavMesh 距离外追击、非 smoke 攻击循环、多敌人仇恨目标、敌人受击动画或掉落。

## 2026-07-07 P6.6 EnemyBrain enemy attack local smoke 记录

P6.6 在 P6.5 formal `EnemyAttackController.TryAttack` 基础上，把本机验证推进到服务端 formal `EnemyBrain` 自动感知/选中目标并进入攻击态后的提交路径。本阶段只做本机 Mac server/client 验证，尚未构建或部署新的 Linux/ECS 包。

新增本地合同：

- `EnemyAttackController` 新增 `AttackCommitted` 事件，服务端网络层只消费提交事实，不让客户端决定敌人伤害。
- `NetworkEnemyAvatar.ConfigureServerBrainEnemyAttackSmoke()` 会启用 P6.6 专项模式；服务端在两个客户端连接后放开 formal 敌人驱动，让 `EnemyBrain` / `EnemySensing` 先自动选中 formal `PlayerCharacter`。
- P6.6 smoke bridge 只在 `EnemyBrain.CurrentTarget` 已存在且状态进入 `EnemyAttackState` 后，调用正式 `EnemyAttackController.TryAttack(CurrentTarget)` 产生提交事件；最终权威 HP 仍由 `NetworkPlayerAvatar.ApplyServerEnemyDamage()` 写入 `NetworkVariable`。
- 新服务端启动参数：`--enable-network-enemy-brain-attack-smoke` / `--network-enemy-brain-attack-smoke`。
- `probe_p15_multiplayer.py` 新增 `--use-brain-network-enemy-attack-smoke`。探针现在接受被敌人命中的玩家在 client1/client2 视角中分别表现为 local/remote 或 remote/local，避免 ownerId 分配竞态误判。

验证结果：

- Unity 编译：`/tmp/unity_compile_p66_brain.log`，退出码 `0`
- Mac server build：`/tmp/unity_p66_macos_server_build_commit_adapter.log`，`Build Finished, Result: Success.`
- Mac client build：`/tmp/unity_p66_mac_client_build.log`，`Build Finished, Result: Success.`
- Python 探针静态检查：`python3 -m py_compile Deploy/DedicatedServer/probe_p15_multiplayer.py` 通过。
- 全量 EditMode 在当前本地 asset 工作树下仍有既有 local-preview/asset wiring 失败；P6 相关新增用例在该轮结果中通过，失败项不属于本阶段网络敌人代码。

本机 P6.6 smoke 命令：

```bash
python3 Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --game-port 8047 \
  --health-port 8048 \
  --server-log /tmp/TY_NEW_p66_brain_enemy_attack_server_commit_adapter.log \
  --client1-log /tmp/TY_NEW_p66_brain_enemy_attack_client1_commit_adapter.log \
  --client2-log /tmp/TY_NEW_p66_brain_enemy_attack_client2_commit_adapter.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --require-death-sync \
  --require-network-enemy-sync \
  --require-formal-network-enemy-sync \
  --require-network-enemy-attack-sync \
  --use-brain-network-enemy-attack-smoke \
  --smoke-attack-count 4 \
  --smoke-attack-interval-seconds 0.75 \
  --smoke-move-delay-seconds 8 \
  --client1-quit-after-seconds 35 \
  --client2-quit-after-seconds 18
```

本机 P6.6 smoke 结果：

- 敌人攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedTargetRole=local client1ObservedTargetHealthStart=100 client1ObservedTargetHealthLater=75 client1ObservedTargetHealthDrop=25 client2ObservedTargetRole=remote client2ObservedTargetHealthStart=100 client2ObservedTargetHealthLater=75 client2ObservedTargetHealthDrop=25 client1ObservedFormalEnemyAttackStart=false client1ObservedFormalEnemyAttackLater=true client2ObservedFormalEnemyAttackStart=false client2ObservedFormalEnemyAttackLater=true`
- 总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=8047 healthPort=8048 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`

本机 server 日志证据：

- Brain smoke armed：`[MultiplayerEnemy] Armed brain smoke enemy attack enemyId=1 connectedClients=2 delaySeconds=2.0`
- 自动感知目标：`[MultiplayerEnemy] Brain smoke enemy attack status ... sensedTarget=FormalPlayer_CombatTest ... formalTargetDistance=1.25 brainEnabled=True sensingEnabled=True attackControllerEnabled=True`
- formal 提交事件：`[MultiplayerEnemy] Brain smoke enemy attack committed enemyId=1 targetOwner=1 targetHealth=100 targetDead=False formalDamage=10 attackId=Enemy_Melee`
- 服务端权威写 HP：`[MultiplayerEnemy] Brain smoke enemy attack applied enemyId=1 targetOwner=1 damage=25 formalDamage=10 attackId=Enemy_Melee health=100->75 targetDead=False`
- 敌人死亡事实仍按 P6 smoke 合同触发：`[MultiplayerEnemy] Smoke enemy death applied enemyId=1 health=50->0 enemyDead=True`

已知边界：

- `--enable-network-enemy-brain-attack-smoke` 是 P6.6 专项验证开关，不应被误认为完整服务端敌人 gameplay tick 已接入。
- P6.6 验证了 `EnemyBrain` 自动选中目标并进入攻击态后的本机 smoke bridge；尚未验证 NavMesh 距离外追击、非 smoke 攻击循环、多敌人仇恨目标、敌人受击动画、掉落或 ECS 公网部署。
- 该阶段当时 ECS 仍是 P6.4 包；后续 P6.7 已构建 Linux 包并完成公网验证。

## 2026-07-07 P6.5 formal enemy attack local smoke 记录

P6.5 把 P6.4 的 timer-style 敌人攻击 smoke 推进为 formal `EnemyAttackController.TryAttack` 提交验证。本阶段只做本机 Mac server/client 验证，尚未构建或部署新的 Linux/ECS 包。

新增本地资源和合同：

- `NetworkPlayerAvatar.TryResolveFormalPlayerTarget()` 暴露 formal CombatTest 玩家子树的 target transform，供服务端敌人攻击验证使用。
- `NetworkEnemyAvatar` 新增 `ConfigureServerFormalEnemyAttackSmoke()` 和 `BuildFormalAttackSmokeSpawnPosition()`。P6.5 验证模式下敌人生成在 `(-1, 0, 1.25)`，处于 `SO_Attack_Enemy_Melee` range `1.55` 内。
- 新服务端启动参数：`--enable-network-enemy-formal-attack-smoke` / `--network-enemy-formal-attack-smoke`。
- `probe_p15_multiplayer.py` 新增 `--use-formal-network-enemy-attack-smoke`，只影响本机启动 server；默认 P6.4 老路径不变。
- formal `EnemyAttackController.TryAttack()` 只作为服务端攻击提交门槛；最终权威 HP 仍由 `NetworkPlayerAvatar.ApplyServerEnemyDamage()` 写入 `NetworkVariable`，客户端只观察同步结果。

验证结果：

- Unity 编译：`/tmp/unity_compile_p65_formal_enemy_attack.log`，退出码 `0`
- 定向 EditMode：`/tmp/unity_editmode_p65_formal_enemy_attack_results.xml = 27/27 Passed`
- Mac server build：`/tmp/unity_p65_macos_server_build.log`，`Build Finished, Result: Success.`
- Mac client build：`/tmp/unity_p65_mac_client_build.log`，`Build Finished, Result: Success.`

本机 P6.5 smoke 命令：

```bash
python3 Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --game-port 7997 \
  --health-port 7998 \
  --server-log /tmp/TY_NEW_p65_formal_enemy_attack_server.log \
  --client1-log /tmp/TY_NEW_p65_formal_enemy_attack_client1.log \
  --client2-log /tmp/TY_NEW_p65_formal_enemy_attack_client2.log \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --require-death-sync \
  --require-network-enemy-sync \
  --require-formal-network-enemy-sync \
  --require-network-enemy-attack-sync \
  --use-formal-network-enemy-attack-smoke \
  --smoke-attack-count 4 \
  --smoke-attack-interval-seconds 0.75 \
  --smoke-move-delay-seconds 8 \
  --client1-quit-after-seconds 35 \
  --client2-quit-after-seconds 18
```

本机 P6.5 smoke 结果：

- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=2.94`
- 网络敌人位置：`enemies=1:network:-1.00,0.00,1.25`
- 敌人攻击事实同步：`P6_NETWORK_ENEMY_ATTACK_SYNC_OK client1ObservedLocalHealthStart=100 client1ObservedLocalHealthLater=75 client1ObservedLocalHealthDrop=25 client2ObservedRemoteHealthStart=100 client2ObservedRemoteHealthLater=75 client2ObservedRemoteHealthDrop=25 client1ObservedFormalEnemyAttackStart=false client1ObservedFormalEnemyAttackLater=true client2ObservedFormalEnemyAttackStart=false client2ObservedFormalEnemyAttackLater=true`
- 网络敌人同步：`P6_NETWORK_ENEMY_SYNC_OK client1ObservedEnemyHealthDrop=50 client2ObservedEnemyHealthDrop=50`
- formal 网络敌人同步：`P6_FORMAL_NETWORK_ENEMY_SYNC_OK ... client1ObservedFormalEnemyDriver=suppressed client2ObservedFormalEnemyDriver=suppressed`
- 总结：`P6_MULTIPLAYER_OK host=127.0.0.1 gamePort=7997 healthPort=7998 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true networkEnemySync=true formalNetworkEnemySync=true networkEnemyAttackSync=true disconnected=0`

本机 server 日志证据：

- active prefab：`Network player prefab command-line path overrides scenePath=Multiplayer/PF_NetworkPlayerAvatar activePath=Multiplayer/PF_NetworkPlayerCombatTest`
- formal 攻击提交路径：`[MultiplayerEnemy] Formal smoke enemy attack applied enemyId=1 targetOwner=1 damage=25 health=100->75 targetDead=False enemyPosition=-1.00,0.00,1.25 targetPosition=-1.00,0.00,0.00`
- 敌人死亡事实仍按 P6 smoke 合同触发：`[MultiplayerEnemy] Smoke enemy death applied enemyId=1 health=50->0 enemyDead=True`

已知边界：

- `--enable-network-enemy-formal-attack-smoke` 是 P6.5 专项验证开关，不应被误认为完整服务端敌人 AI tick 已接入。
- 当前 formal 敌人子树仍被网络根节点约束，P6.5 只验证 `EnemyAttackController.TryAttack` 的提交门槛；尚未验证 `EnemyBrain` 自动感知目标、NavMesh 追击、距离外追击后攻击、非 smoke 攻击循环、敌人受击动画、掉落或仇恨目标同步。
- 该阶段当时 ECS 仍是 P6.4 包；后续 P6.7 已构建 Linux 包并完成 EnemyBrain smoke 公网验证。

## 2026-07-07 P5.9 ECS AppleDouble cleanup 回归记录

P5.9 清理了 P5.8 部署后 ECS `/opt/ty-new-server` 下历史遗留的 AppleDouble `._*` 文件，并确认 formal smoke 不回退。

清理前观察：

- `/opt/ty-new-server` 下存在历史 `._*` 普通文件，包括 `TYServer_Data/Plugins/._lib_burst_generated.so`。
- P5.8 服务本身为 `active`，formal attack/hit/death smoke 已通过。
- 清理前启动日志曾出现：`Failed to open plugin: /opt/ty-new-server/TYServer_Data/Plugins/._lib_burst_generated.so`。

清理命令：

```bash
ssh -i <SSH_KEY_PATH> <ECS_USER>@<ECS_HOST> \
  "sudo find /opt/ty-new-server -name '._*' -type f -print -delete"
```

清理后验证：

- `sudo find /opt/ty-new-server -name '._*' -print` 无输出。
- `sudo systemctl restart ty-new-server` 后服务为 `active`。
- 新启动日志不再出现 `Failed to open plugin: ... ._*`。
- 启动路径仍为 `networkPlayerPrefabResourcePath=Multiplayer/PF_NetworkPlayerCombatTest`。
- 命令行覆盖仍为 `scenePath=Multiplayer/PF_NetworkPlayerAvatar activePath=Multiplayer/PF_NetworkPlayerCombatTest`。

脚本维护：

- `Deploy/DedicatedServer/deploy_p1_gameplay.sh` 已在远端 `sudo tar -xzf "$REMOTE_PACKAGE" -C /opt/ty-new-server` 后加入：

```bash
sudo find /opt/ty-new-server -name '._*' -type f -delete
```

- 静态检查：`sh -n Deploy/DedicatedServer/deploy_p1_gameplay.sh` 通过。

公网回归命令：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网回归结果：

- 公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=2.00`
- HP 同步：`P3_ATTACK_HIT_OK attackId=Light_01 client2ObservedLocalHealthStart=100 client2ObservedLocalHealthLater=0 client2ObservedLocalHealthDrop=100 clientRequestedDamage=9999 serverAppliedDamage=100`
- formal 攻击表现同步：`P5_FORMAL_ATTACK_SYNC_OK attackId=Light_01 client2ObservedRemoteFormalAttackStart=false client2ObservedRemoteFormalAttackLater=true`
- formal 受击表现同步：`P5_FORMAL_HIT_SYNC_OK attackId=Light_01 client2ObservedLocalFormalHitStart=false client2ObservedLocalFormalHitLater=true`
- 网络死亡事实同步：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`
- formal 子树死亡状态同步：`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathStart=false client2ObservedLocalFormalDeathLater=true`
- 总结：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true disconnected=0`
- smoke 后公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`

远端日志证据：

- 服务端权威命中：4 次 `Light_01 damage=25`
- 最终死亡：`health=25->0 targetDead=True`
- smoke 后回落：`networkConnectedClients=0 networkSpawnedPlayers=0`

该阶段是运维卫生和部署脚本加固，不改变 gameplay/network 协议。P5.8 的 formal attack/hit/death 能力保持有效。

## 2026-07-07 P5.8 formal attack presentation ECS 部署记录

P5.8 把 P5.7 formal attack presentation 改动构建为新的 Linux Dedicated Server 包，并部署到 ECS 完成公网 formal smoke。

包与构建：

- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p58_formal_attack_linux_build.log`
- Linux build 结果：`Build Finished, Result: Success.`
- 构建日志确认包含：`Assets/_Game/Resources/Multiplayer/PF_NetworkPlayerCombatTest.prefab`
- 构建日志确认包含：`Assets/_Game/Scripts/Runtime/Multiplayer/NetworkPlayerAvatar.cs`
- 构建日志确认包含：`Assets/_Game/Scripts/Runtime/Multiplayer/NetworkPlayerPresentationBridge.cs`
- 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p58-formal-attack.tar.gz`
- 包大小：约 `80M`
- SHA256：`a3134726feece31b3fa43de0f7feeaaca7ee4dac61f918e73a0ca69feb7ba812`
- 本地包检查：当前 P5.8 tar 不包含 `._*` AppleDouble 条目。

部署命令：

```bash
TY_NEW_SERVER_PACKAGE=<PROJECT_ROOT>/Builds/DedicatedServer/TYServer-linux-x86_64-p58-formal-attack.tar.gz \
Deploy/DedicatedServer/deploy_p1_gameplay.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

部署结果：

- 远端 SHA256：`a3134726feece31b3fa43de0f7feeaaca7ee4dac61f918e73a0ca69feb7ba812`
- systemd：`active`
- systemd 启动命令继续包含：`--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest`
- ECS 本机 health：`P1.5_HEALTH_OK host=127.0.0.1 healthPort=7778 networkPort=7777 connected=0 spawned=0`
- ECS 本机 P1 TCP gameplay probe：`TY_NEW_GAME protocol=1`、`JOINED`、`PONG`、`ROOM`、`BYE`

公网 formal prefab 验证命令：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网验证结果：

- 公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 连接与 spawn：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 互相可见：client1/client2 均输出 `avatarCount=2 owned=1 remote=1`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=1.73`
- HP 同步：`P3_ATTACK_HIT_OK attackId=Light_01 client2ObservedLocalHealthStart=100 client2ObservedLocalHealthLater=0 client2ObservedLocalHealthDrop=100 clientRequestedDamage=9999 serverAppliedDamage=100`
- formal 攻击表现同步：`P5_FORMAL_ATTACK_SYNC_OK attackId=Light_01 client2ObservedRemoteFormalAttackStart=false client2ObservedRemoteFormalAttackLater=true`
- formal 受击表现同步：`P5_FORMAL_HIT_SYNC_OK attackId=Light_01 client2ObservedLocalFormalHitStart=false client2ObservedLocalFormalHitLater=true`
- 网络死亡事实同步：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`
- formal 子树死亡状态同步：`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathStart=false client2ObservedLocalFormalDeathLater=true`
- 总结：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true disconnected=0`
- smoke 后公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`

远端日志证据：

- 启动路径：`networkPlayerPrefabResourcePath=Multiplayer/PF_NetworkPlayerCombatTest`
- 命令行覆盖确认：`scenePath=Multiplayer/PF_NetworkPlayerAvatar activePath=Multiplayer/PF_NetworkPlayerCombatTest`
- 服务端权威命中：4 次 `Light_01 damage=25`
- 最终死亡：`health=25->0 targetDead=True`
- smoke 后回落：`networkConnectedClients=0 networkSpawnedPlayers=0`

备注：

- 远端日志启动时出现一次历史残留 AppleDouble 插件文件警告：`Failed to open plugin: /opt/ty-new-server/TYServer_Data/Plugins/._lib_burst_generated.so`。当前 P5.8 本地构建目录和 tar 包均不包含 `._*` 条目，且 ECS health、UDP 入站和 formal smoke 均通过；该历史残留已在 P5.9 清理并完成公网回归。

该阶段说明 formal CombatTest 玩家子树已经能在 ECS 公网环境中根据服务端认可的攻击事实进入正式 `PlayerAttackState`，并继续根据权威 HP/death 事实进入 `PlayerHitState` 和 `PlayerDeathState`。下一阶段仍应保持小步：复活 UI、敌人 AI、完整连招预测、客户端插值/预测分别拆开做。

## 2026-07-07 P5.7 第一阶段 formal attack presentation 本机记录

P5.7 第一阶段在 P5.6 formal hit/death presentation bridge 基础上新增服务端认可攻击事实到 formal 攻击表现的最小桥接：

- `NetworkPlayerAvatar` 新增 server-write `AttackPresentationSequence` / `AttackPresentationCode`。
- 服务端只在 attackId 白名单、序号和冷却检查通过后发布攻击表现事实；客户端仍不能决定命中、目标或伤害。
- `NetworkPlayerPresentationBridge` 观察新攻击表现序号后让 formal 子树进入 `PlayerAttackState`，并立即清理本地 hitbox 准备态，避免表现层产生本地判定。
- formal `PlayerHitState` / `PlayerDeathState` 优先于攻击表现。
- `MultiplayerClientSmokeReporter` 新增 `formalAttacks=` sticky 观测字段。
- `probe_p15_multiplayer.py` 在 formal prefab smoke 中要求 client2 观察远端攻击者 `formalAttacks=false->true`，并输出 `P5_FORMAL_ATTACK_SYNC_OK`。

本机验证：

- 定向 EditMode：`/tmp/unity_editmode_p57_dedicated_results.xml`
- 定向 EditMode 结果：`24/24 Passed`
- 更新测试：`FormalNetworkPlayerPrefab_PresentationBridgeDrivesFormalPlayerState`
- Mac local server build：`/tmp/unity_p57_macos_server_build.log`
- Mac release client build：`/tmp/unity_p57_mac_client_build.log`
- 本机 formal smoke 服务端日志：`/tmp/TY_NEW_p57_formal_attack_local_rerun_server.log`
- 本机 formal smoke client1 日志：`/tmp/TY_NEW_p57_formal_attack_local_rerun_client1.log`
- 本机 formal smoke client2 日志：`/tmp/TY_NEW_p57_formal_attack_local_rerun_client2.log`
- 本机 formal attack：`P5_FORMAL_ATTACK_SYNC_OK attackId=Light_01 client2ObservedRemoteFormalAttackStart=false client2ObservedRemoteFormalAttackLater=true`
- 本机 formal hit：`P5_FORMAL_HIT_SYNC_OK attackId=Light_01 client2ObservedLocalFormalHitStart=false client2ObservedLocalFormalHitLater=true`
- 本机 formal death：`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathStart=false client2ObservedLocalFormalDeathLater=true`
- 本机 summary：`P4_MULTIPLAYER_OK host=127.0.0.1 gamePort=7927 healthPort=7928 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalAttackSync=true formalHitSync=true disconnected=0`

本机 smoke 命令：

```bash
python3 Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --host 127.0.0.1 \
  --server-bind-address 127.0.0.1 \
  --game-port 7927 \
  --health-port 7928 \
  --server-log /tmp/TY_NEW_p57_formal_attack_local_rerun_server.log \
  --client1-log /tmp/TY_NEW_p57_formal_attack_local_rerun_client1.log \
  --client2-log /tmp/TY_NEW_p57_formal_attack_local_rerun_client2.log \
  --client1-quit-after-seconds 35 \
  --client2-quit-after-seconds 15 \
  --connected-timeout 60 \
  --disconnect-timeout 30 \
  --min-remote-move-distance 0.25 \
  --smoke-move-delay-seconds 5.5 \
  --smoke-attack-count 4 \
  --smoke-attack-interval-seconds 0.45 \
  --min-remote-health-drop 100 \
  --require-death-sync
```

ECS 状态：

- 本阶段新增 NGO NetworkVariables，因此未使用旧 ECS P5.5 服务端做兼容回归。
- 当前 ECS 仍运行 P5.5 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p55-presentation.tar.gz`。
- 下一步需要构建新的 Linux Dedicated Server 包、上传 ECS、以 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest` 启动后复跑公网 P5.7 smoke。

该阶段说明 formal CombatTest 玩家子树已经能根据服务端认可的攻击事实进入正式 `PlayerAttackState`，同时 hit/death 仍由权威 HP/death 事实驱动并保持更高优先级。下一阶段可以把这套 P5.7 改动部署到 ECS；仍不要把敌人 AI、复活 UI 或完整连招预测合并到同一个同步点。

## 2026-07-07 P5.6 第一阶段 formal hit presentation 回归记录

P5.6 第一阶段在 P5.5 formal presentation bridge 基础上新增非致死受击表现同步：

- `NetworkPlayerPresentationBridge` 从权威 HP 下降且未死亡推导一次 formal `PlayerHitState`。
- 该阶段不新增网络变量、RPC、服务端命中规则或客户端伤害决定权。
- `MultiplayerClientSmokeReporter` 新增 `formalHits=` sticky 观测字段。
- `probe_p15_multiplayer.py` 在 formal prefab smoke 中要求 `formalHits` 从 `false` 变为 `true`，并输出 `P5_FORMAL_HIT_SYNC_OK`。

本机验证：

- 定向 EditMode：`/tmp/unity_editmode_p56_formal_hit_rerun_results.xml`
- 定向 EditMode 结果：`24/24 Passed`
- 更新测试：`FormalNetworkPlayerPrefab_PresentationBridgeDrivesFormalPlayerState`
- Mac local server build：`/tmp/unity_p56_macos_server_build_rerun.log`
- Mac release client build：`/tmp/unity_p56_mac_client_build_rerun.log`
- 本机 formal smoke 服务端日志：`/tmp/TY_NEW_p56_formal_hit_local_rerun_server.log`
- 本机 formal smoke client1 日志：`/tmp/TY_NEW_p56_formal_hit_local_rerun_client1.log`
- 本机 formal smoke client2 日志：`/tmp/TY_NEW_p56_formal_hit_local_rerun_client2.log`
- 本机 formal hit：`P5_FORMAL_HIT_SYNC_OK attackId=Light_01 client2ObservedLocalFormalHitStart=false client2ObservedLocalFormalHitLater=true`
- 本机 formal death：`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathStart=false client2ObservedLocalFormalDeathLater=true`
- 本机 summary：`P4_MULTIPLAYER_OK host=127.0.0.1 gamePort=7895 healthPort=7896 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalHitSync=true disconnected=0`

ECS 回归：

- 本阶段未重新部署 Linux 服务端包；ECS 仍运行 P5.5 包 `Builds/DedicatedServer/TYServer-linux-x86_64-p55-presentation.tar.gz`。
- P5.5 包 SHA256：`f21ee3976b927bc05e2349e7c7e37cb867f01a7dfd61f7e425c8f1a7e77aaac9`
- 回归方式：使用本地 P5.6 Mac 客户端和最新探针连接当前 ECS 服务。

公网 formal prefab 回归命令：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网验证结果：

- 公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 连接与 spawn：`networkConnectedClients=2 networkSpawnedPlayers=2`
- formal hit：`P5_FORMAL_HIT_SYNC_OK attackId=Light_01 client2ObservedLocalFormalHitStart=false client2ObservedLocalFormalHitLater=true`
- formal death：`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathStart=false client2ObservedLocalFormalDeathLater=true`
- 总结：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true formalHitSync=true disconnected=0`

远端服务状态：

- smoke 后公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- 服务端日志记录第 4 次命中：`health=25->0 targetDead=True`
- smoke 后 health 回落：`networkConnectedClients=0 networkSpawnedPlayers=0`

该阶段说明 formal CombatTest 玩家子树已经能根据服务端权威 HP/death 事实进入正式 `PlayerHitState` 和 `PlayerDeathState`。下一阶段仍应保持边界：继续拆分动画过渡、受击硬直时长、攻击表现、复活 UI 和敌人 AI，不要把它们一次性塞进同一个同步点。

## 2026-07-07 P5.5 第一阶段 formal presentation bridge 部署记录

P5.5 第一阶段让 formal CombatTest 玩家子树的表现层稳定跟随 `NetworkPlayerAvatar` 权威状态，并完成本机与 ECS 公网验证。

代码/表现桥接变更：

- 新增 `NetworkPlayerPresentationBridge`。
- `DedicatedServerBuildUtility.CreateOrRepairFormalNetworkPlayerPrefab()` 会在 `FormalPlayer_CombatTest` 子节点上挂接该桥，并序列化引用根 `NetworkPlayerAvatar`、正式 `PlayerCharacter`、`HealthComponent` 和 `PlayerStateMachine`。
- 桥接行为：约束 formal 子节点 local pose，镜像权威 HP 到正式 `HealthComponent`，权威死亡时通过 `NetworkPlayerDeathStateBridge` 进入正式 `PlayerDeathState`，并持续 suppress 正式子节点 `PlayerCharacter` 的本地单机驱动。
- `MultiplayerClientSmokeReporter` 新增 `formalDeaths=` 与 `formalDrivers=`。
- `probe_p15_multiplayer.py` 在使用 `Multiplayer/PF_NetworkPlayerCombatTest` 时会要求 formal death sync，并输出 `P5_FORMAL_DEATH_SYNC_OK`。

本机验证：

- prefab 生成：`/tmp/unity_p55_create_formal_presentation_prefab.log`
- 定向 EditMode：`/tmp/unity_editmode_p55_presentation_results.xml`
- 定向 EditMode 结果：`24/24 Passed`
- 新增/更新测试：`FormalNetworkPlayerPrefab_PresentationBridgeDrivesFormalPlayerState`
- Mac local server build：`/tmp/unity_p55_macos_server_build.log`
- Mac release client build：`/tmp/unity_p55_mac_client_build.log`
- 本机 formal smoke 服务端日志：`/tmp/TY_NEW_p55_formal_local_server.log`
- 本机 formal smoke client1 日志：`/tmp/TY_NEW_p55_formal_local_client1.log`
- 本机 formal smoke client2 日志：`/tmp/TY_NEW_p55_formal_local_client2.log`
- 本机 formal smoke 通过：`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathStart=false client2ObservedLocalFormalDeathLater=true`
- 本机 summary：`P4_MULTIPLAYER_OK host=127.0.0.1 gamePort=7891 healthPort=7892 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true disconnected=0`

包与构建：

- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p55_presentation_linux_build.log`
- Linux build 结果：`Build Finished, Result: Success.`
- 构建日志确认包含：`Assets/_Game/Resources/Multiplayer/PF_NetworkPlayerCombatTest.prefab`
- 构建日志确认包含：`Assets/_Game/Scripts/Runtime/Multiplayer/NetworkPlayerPresentationBridge.cs`
- 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p55-presentation.tar.gz`
- 包大小：约 `80M`
- SHA256：`f21ee3976b927bc05e2349e7c7e37cb867f01a7dfd61f7e425c8f1a7e77aaac9`

部署备注：

- 首次 `scp` 上传在约 39M 时出现 `Connection reset by peer` / `Broken pipe`。
- 使用低速 rsync 从远端部分文件续传成功：

```bash
rsync -a --append --partial --bwlimit=128 --timeout=90 --quiet \
  -e 'ssh -o ConnectTimeout=10 -o ServerAliveInterval=15 -o ServerAliveCountMax=6 -i <SSH_KEY_PATH>' \
  Builds/DedicatedServer/TYServer-linux-x86_64-p55-presentation.tar.gz \
  <ECS_USER>@<ECS_HOST>:/tmp/TYServer-linux-x86_64-p55-presentation.tar.gz
```

部署结果：

- 远端 SHA256：`f21ee3976b927bc05e2349e7c7e37cb867f01a7dfd61f7e425c8f1a7e77aaac9`
- systemd：`active`
- systemd 启动命令继续包含：`--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest`
- 公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`

公网 formal prefab 验证命令：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网验证结果：

- 公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 连接与 spawn：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 互相可见：client1/client2 均输出 `avatarCount=2 owned=1 remote=1`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=0.67`
- HP 同步：`P3_ATTACK_HIT_OK attackId=Light_01 client2ObservedLocalHealthStart=100 client2ObservedLocalHealthLater=0 client2ObservedLocalHealthDrop=100 clientRequestedDamage=9999 serverAppliedDamage=100`
- 网络死亡事实同步：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`
- formal 子树死亡状态同步：`P5_FORMAL_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalFormalDeathStart=false client2ObservedLocalFormalDeathLater=true`
- formal 本地驱动抑制：客户端 smoke 输出 `formalDrivers=1:local:suppressed|2:remote:suppressed` / `formalDrivers=1:remote:suppressed|2:local:suppressed`
- 总结：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true formalDeathSync=true disconnected=0`

远端日志证据：

- 启动路径：`networkPlayerPrefabResourcePath=Multiplayer/PF_NetworkPlayerCombatTest`
- 服务端权威命中：4 次 `Light_01 damage=25`
- 最终死亡：`health=25->0 targetDead=True`
- smoke 后 health 回落：`networkConnectedClients=0 networkSpawnedPlayers=0`

该阶段说明 formal CombatTest 玩家子树已经能跟随最小权威网络骨架进入正式 `PlayerDeathState`，且 formal 子节点本地单机驱动已被 smoke 观测为 suppressed。下一阶段仍应把动画过渡、受击硬直、复活 UI、敌人 AI 和完整 CombatTest 战斗状态机分开做专项网络化。

## 2026-07-06 P5 第四阶段 ECS formal prefab 部署记录

P5 第四阶段把 formal network player prefab 包部署到 ECS，并完成公网专项 smoke。

包与构建：

- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p5_formal_ecs_linux_build.log`
- Linux build 结果：`Build Finished, Result: Success.`
- 构建日志确认包含：`Assets/_Game/Resources/Multiplayer/PF_NetworkPlayerCombatTest.prefab`
- 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p5-formal-prefab.tar.gz`
- 包大小：约 `81M`
- SHA256：`0072b7853325fbdd064e4497eaba000a804b9321cacbb1455ddeb566fc05f2b5`

部署变更：

- `Deploy/DedicatedServer/ty-new-server.service` 当前使用 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest`。
- 部署命令：

```bash
TY_NEW_SERVER_PACKAGE=<PROJECT_ROOT>/Builds/DedicatedServer/TYServer-linux-x86_64-p5-formal-prefab.tar.gz \
Deploy/DedicatedServer/deploy_p1_gameplay.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

部署结果：

- 远端 SHA256：`0072b7853325fbdd064e4497eaba000a804b9321cacbb1455ddeb566fc05f2b5`
- systemd：`active (running)`
- systemd 启动命令包含：`--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest`
- ECS 本机 health：`P1.5_HEALTH_OK host=127.0.0.1 healthPort=7778 networkPort=7777 connected=0 spawned=0`
- ECS 本机 P1 TCP gameplay 探针通过。

公网 formal prefab 验证命令：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

公网验证结果：

- 公网 health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- 连接与 spawn：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 互相可见：client1/client2 均输出 `avatarCount=2 owned=1 remote=1`
- 断开同步：client2 退出后 client1 输出 `avatarCount=1 owned=1 remote=0`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=2.00`
- HP 同步：`P3_ATTACK_HIT_OK attackId=Light_01 client2ObservedLocalHealthStart=100 client2ObservedLocalHealthLater=0 client2ObservedLocalHealthDrop=100 clientRequestedDamage=9999 serverAppliedDamage=100`
- 死亡同步：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`
- 总结：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`

远端日志证据：

- 启动路径：`networkPlayerPrefabResourcePath=Multiplayer/PF_NetworkPlayerCombatTest`
- override 记录：`Network player prefab command-line path overrides scene prefab reference scenePath=Multiplayer/PF_NetworkPlayerAvatar activePath=Multiplayer/PF_NetworkPlayerCombatTest`
- 服务端权威命中：4 次 `Light_01 damage=25`
- 最终死亡：`health=25->0 targetDead=True`

该阶段说明 formal CombatTest 玩家子树已能承载当前 NetworkPlayerAvatar 最小同步骨架并通过 ECS 公网验证。下一阶段仍应把正式玩家动画/受击表现/复活 UI/敌人 AI 分开做专项网络化，不要一次性扩大范围。

## 2026-07-06 P5 第三阶段本机 formal prefab smoke 记录

P5 第三阶段完成了 formal network player prefab 的本机 Mac server/client 专项验证。

先前第一次专项 smoke 失败在 NGO 握手阶段：

- 服务器 health 正常，`networkStarted=true networkListening=true`。
- 两个客户端都在连接 `127.0.0.1:7891`。
- 服务端日志出现 `NetworkConfig mismatch`，`networkConnectedClients` 一直为 `0`。

根因：

- `ServerBoot.unity` 序列化字段仍引用默认 `Multiplayer/PF_NetworkPlayerAvatar`。
- 命令行虽然传入 `--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest`，但服务端启动时优先使用了场景里的 prefab 引用。
- 客户端从 Resources 加载 formal prefab，服务端实际仍用默认 prefab，导致双方 `NetworkConfig` 哈希不一致。

修复：

- `ServerRuntimeBootstrap` 新增命令行 prefab 路径优先逻辑。
- 当 active prefab 路径不同于场景序列化 prefab 路径时，服务端不再使用场景 prefab 引用，而是按 active Resources 路径加载。
- 新增测试 `ServerRuntimeBootstrap_CommandLinePrefabPathOverridesSerializedScenePrefab`。

本机验证：

- 定向 EditMode：`/tmp/unity_editmode_p5_formal_runtime_override_results.xml`
- 定向 EditMode 结果：`24/24 Passed`
- Mac local server build：`/tmp/unity_p5_formal_override_macos_server_build.log`
- Mac local server build 结果：`Build Finished, Result: Success.`
- Mac release client build：`/tmp/unity_p5_formal_override_mac_client_build.log`
- Mac release client build 结果：`Build Finished, Result: Success.`
- 服务端日志确认 active path：`Network player prefab command-line path overrides scene prefab reference scenePath=Multiplayer/PF_NetworkPlayerAvatar activePath=Multiplayer/PF_NetworkPlayerCombatTest`
- 专项 smoke 服务端日志：`/tmp/TY_NEW_p5_formal_local_server.log`
- 专项 smoke client1 日志：`/tmp/TY_NEW_p5_formal_local_client1.log`
- 专项 smoke client2 日志：`/tmp/TY_NEW_p5_formal_local_client2.log`

通过命令：

```bash
Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --host 127.0.0.1 \
  --server-bind-address 127.0.0.1 \
  --game-port 7891 \
  --health-port 7892 \
  --server-log /tmp/TY_NEW_p5_formal_local_server.log \
  --client1-log /tmp/TY_NEW_p5_formal_local_client1.log \
  --client2-log /tmp/TY_NEW_p5_formal_local_client2.log \
  --client1-quit-after-seconds 35 \
  --client2-quit-after-seconds 15 \
  --connected-timeout 60 \
  --disconnect-timeout 30 \
  --min-remote-move-distance 0.25 \
  --smoke-move-delay-seconds 5.5 \
  --smoke-attack-count 4 \
  --smoke-attack-interval-seconds 0.45 \
  --min-remote-health-drop 100 \
  --require-death-sync
```

通过结果：

- 连接与 spawn：`networkConnectedClients=2 networkSpawnedPlayers=2`
- 双客户端可见：`client1-visible ... remote=1`，`client2-visible ... remote=1`
- 断开同步：`client1-despawn ... remote=0`
- 远端移动同步：`P1.5_REMOTE_POSITION_SYNC_OK client2ObservedRemoteMoveDistance=0.82`
- HP 同步：`P3_ATTACK_HIT_OK attackId=Light_01 ... client2ObservedLocalHealthDrop=100 ... serverAppliedDamage=100`
- 死亡同步：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`
- 总结：`P4_MULTIPLAYER_OK host=127.0.0.1 gamePort=7891 healthPort=7892 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`

第四阶段已构建新的 Linux Dedicated Server 包并部署到 ECS，公网 formal prefab 专项 smoke 已通过。复跑公网验证使用：

```bash
TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest \
Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

## 2026-07-06 P5 第二阶段 formal network player prefab 记录

P5 第二阶段新增一个不替换默认线上服务的正式网络玩家 prefab：

- 新增资源：`Assets/_Game/Resources/Multiplayer/PF_NetworkPlayerCombatTest.prefab`
- Resources 路径：`Multiplayer/PF_NetworkPlayerCombatTest`
- 根节点：`NetworkObject` + `NetworkPlayerAvatar`
- 子节点：`FormalPlayer_CombatTest`，为 unpack 后的正式 CombatTest 玩家组件树。
- 子节点保留 `PlayerCharacter`、`PlayerStateMachine`、`HealthComponent`、`PlayerCombatController` 等正式玩家组件。
- 子节点 `NetworkPlayerDeathStateBridge` 已接到根 `NetworkPlayerAvatar`、正式 `HealthComponent` 和 `PlayerStateMachine`。
- 生成时剥离 local-preview-only 视觉依赖，只保留 proxy baseline；`ValidateFormalNetworkPlayerPrefab()` 会阻止 `Assets/Free medieval weapons`、`Assets/JC_LP_MedievalCharacters_LITE` 等预览目录进入该 Resources prefab。

代码/工具变更：

- `DedicatedServerBuildUtility.CreateOrRepairFormalNetworkPlayerPrefab()`：生成 formal network player prefab。
- `DedicatedServerBuildUtility.ValidateFormalNetworkPlayerPrefab()`：校验正式网络玩家 prefab 的 NGO 组件、正式玩家子树、死亡桥接引用和 local-preview 依赖边界。
- `ValidateBuildInputs()` 已纳入 formal prefab 校验。
- `probe_p15_multiplayer.py` 新增 `--network-player-prefab`，可让本地 server/client 使用 `Multiplayer/PF_NetworkPlayerCombatTest` 做专项 smoke。
- `verify_p15_ecs_multiplayer.sh` 新增可选环境变量 `TY_NEW_NETWORK_PLAYER_PREFAB`；默认不传，仍验证当前线上默认 prefab。

本机验证：

- prefab 生成：`/tmp/unity_create_formal_network_player_prefab_v2.log`
- Unity 编译：`/tmp/unity_compile_p5_formal_network_prefab.log`，退出码 `0`
- 定向 EditMode：`/tmp/unity_editmode_p5_formal_network_prefab_runtime_results.xml`
- 定向 EditMode 结果：`23/23 Passed`
- 新增测试：`FormalNetworkPlayerPrefab_EmbedsCombatTestPlayerAndDeathBridge`
- 新增测试：`FormalNetworkPlayerPrefab_PresentationBridgeDrivesFormalPlayerState`
- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p5_formal_network_prefab_linux_build.log`
- Linux build 结果：`Build Finished, Result: Success.`

ECS 回归验证：

- 本阶段未重新部署 ECS 包，也未把默认服务切到 formal prefab；当前远端服务仍是 P4.5/P5 前置包。
- ECS health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- ECS UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- ECS 公网死亡同步证据：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`
- ECS 公网 P4 回归 smoke：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`

第三阶段已构建本地 Mac server/client 并完成 formal prefab 专项 smoke，使用：

```bash
Deploy/DedicatedServer/probe_p15_multiplayer.py \
  --network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest \
  --require-death-sync \
  --smoke-attack-count 4
```

formal prefab 本机双客户端 smoke 已在第三阶段通过；Linux 包构建、ECS 部署和 `TY_NEW_NETWORK_PLAYER_PREFAB=Multiplayer/PF_NetworkPlayerCombatTest` 公网专项验证已在第四阶段完成。

## 2026-07-06 P5 第一阶段正式玩家 prefab 桥接记录

P5 第一阶段把 P4.5/P5 前置的死亡桥接推进到正式 CombatTest 玩家 prefab：

- `PF_Player_CombatTest` 已挂接 `NetworkPlayerDeathStateBridge`。
- 桥接组件序列化引用正式玩家自身的 `HealthComponent` 和 `PlayerStateMachine`。
- `CombatTestSceneBuilder.BuildPlayerPrefab()` 会在重建正式玩家 prefab 时添加桥接组件。
- `CombatTestSceneBuilder.RepairPlayerPrefab()` 会在修复正式玩家 prefab 时确保桥接组件存在，并重连 `health` / `stateMachine`。

本机验证：

- Unity 编译：`/tmp/unity_compile_p5_prefab_bridge.log`，退出码 `0`
- 定向 EditMode：`/tmp/unity_editmode_p5_prefab_bridge_results.xml`
- 定向 EditMode 结果：`21/21 Passed`
- 新增测试：`CombatTestPlayerPrefab_HasNetworkDeathStateBridgeWiredToFormalPlayerState`，验证正式玩家 prefab 本体存在 `NetworkPlayerDeathStateBridge`，且桥接引用指向同一 prefab 上的 `HealthComponent` 和 `PlayerStateMachine`。
- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p5_prefab_bridge_linux_build.log`
- Linux build 结果：`Build Finished, Result: Success.`

ECS 回归验证：

- 本阶段未重新部署 ECS 包，因为服务端运行时代码未变；当前远端服务仍是 P4.5/P5 前置包。
- ECS health：`P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0`
- ECS UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- ECS 公网死亡同步证据：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`
- ECS 公网 P4 回归 smoke：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`

该阶段只是把正式 CombatTest 玩家 prefab 纳入网络死亡事实入口。下一阶段仍需要把 `NetworkPlayerAvatar` 与正式玩家移动、攻击、动画、复活 UI 和输入/表现边界逐步合并。

## 2026-07-06 P4.5/P5 前置死亡桥接记录

P4.5/P5 前置在 P4 服务端权威死亡事实基础上新增：

- 新增 `NetworkPlayerDeathStateBridge`，用于把 `NetworkPlayerAvatar.IsDead` 桥接到正式 CombatTest 玩家侧的 `HealthComponent` 和 `PlayerStateMachine`。
- 桥接应用后，正式玩家血量会归零，并切入 `PlayerDeathState`；后续正式网络玩家 prefab 可以复用这条薄桥，不需要客户端决定死亡。
- `CampusRPG.Runtime.Multiplayer` 增加对 `CampusRPG.Runtime` 的 asmdef 引用，以便桥接访问正式玩家死亡链路。

本机验证：

- Unity 编译：`/tmp/unity_compile_p5_death_bridge.log`，退出码 `0`
- 定向 EditMode：`/tmp/unity_editmode_p5_death_bridge_results.xml`
- 定向 EditMode 结果：`20/20 Passed`
- 新增测试：`NetworkPlayerDeathStateBridge_AppliesAuthoritativeDeathToCombatPlayerState`，验证桥接会把正式玩家 `HealthComponent` 归零并切入 `PlayerDeathState`。

Linux/ECS 回归验证：

- Linux Dedicated Server build：`/tmp/TY_NEW_dedicated_p5_death_bridge_linux_build.log`
- 当前部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p3-attack.tar.gz`，约 `30M`；文件名暂沿用 P3 攻击包名，内容已包含 P4.5/P5 前置死亡桥接代码。
- 当前部署包 SHA256：`f79ba95fabc3eb8c4feddad30807b09454d68bd95f90cd73f9a51ca51e36d90f`
- ECS 远端 SHA256：`f79ba95fabc3eb8c4feddad30807b09454d68bd95f90cd73f9a51ca51e36d90f`
- ECS systemd：`ty-new-server.service` 为 `active`
- ECS UDP 入站：`P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777`
- ECS 公网 P4 回归 smoke：`P4_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 connected=2 spawned=2 mutualVisibility=true clientDespawnObserved=true remotePositionSync=true healthSync=true deathSync=true disconnected=0`
- ECS 公网死亡同步证据：`P4_DEATH_SYNC_OK attackId=Light_01 smokeAttackCount=4 client2ObservedLocalDeathStart=false client2ObservedLocalDeathLater=true`
- ECS 服务端命中日志：4 次 `Light_01 damage=25`，最终 `health=25->0 targetDead=True`。

该阶段只是给正式 CombatTest 玩家死亡链路准备网络死亡事实入口，还没有把 `PF_Player_CombatTest` 替换为正式网络玩家 prefab，也没有完成死亡动画、复活 UI、敌人 AI 或完整 CombatTest 战斗网络化。

## 2026-07-06 P1 本地构建记录

已新增最小 gameplay TCP 协议，并完成 Linux Dedicated Server 本地构建：

- 构建命令：`Tools/unity-cli/ty-new-build-dedicated-server linux --hub-licensing --licensing-ipc LicenseClient-don --wall-timeout 1800 --log /tmp/TY_NEW_dedicated_p1_gameplay_linux_build_escalated.log`
- 构建结果：`Build Finished, Result: Success.`
- 构建目录：`Builds/DedicatedServer/Linux`
- 构建目录大小：约 `79M`
- 部署包：`Builds/DedicatedServer/TYServer-linux-x86_64-p1-gameplay.tar.gz`
- 部署包大小：约 `29M`
- 部署包 SHA256：`1c18615d47aa3b15a433e0585fe322aab99f9c3838b532702a07afed0737e040`
- 注意：首次构建时 Unity Package Manager 需要下载 `com.unity.toolchain.macos-arm64-linux@1.1.0`，本机实测大小约 `194 MB`，耗时约 `4m 52s`。构建脚本超时时间不要低于 `1800s`。

本地验证：

- Dedicated Server smoke：`Tools/unity-cli/ty-new-build-dedicated-server smoke --hub-licensing --licensing-ipc LicenseClient-don --wall-timeout 600 --log /tmp/TY_NEW_dedicated_p1_gameplay_smoke_final.log`
- smoke 结果：`Dedicated server smoke verification passed.`
- 定向 EditMode：`/tmp/unity_editmode_p1_gameplay_dedicated_final_results.xml`
- 定向 EditMode 结果：`7/7 Passed`
- 覆盖项包括：Linux Server build options、ServerBoot 无 Camera/Audio/Input、health 行格式、gameplay 行协议格式、`ServerGameConnectionService_AcceptsHelloPingAndStateOverTcp`

当前 ECS 部署状态：

- P1 包已上传到 `<ECS_HOST>` 并通过远端 SHA256 校验。
- 远端 SHA256：`1c18615d47aa3b15a433e0585fe322aab99f9c3838b532702a07afed0737e040`
- systemd `ty-new-server.service` 已重启，服务状态为 `active`。
- ECS `ss -lntp` 显示 `0.0.0.0:7777` 与 `0.0.0.0:7778` 均由 Unity server 监听。
- ECS 本机 `127.0.0.1:7778` health 已通过。
- ECS 本机 `127.0.0.1:7777` gameplay 探针已通过。
- 公网 `<ECS_HOST>:7778` health 已通过。
- 公网 `<ECS_HOST>:7777` gameplay 探针已通过。
- 公网 health 已返回 gameplay 计数：`gameConnectionsAccepted=6 gamePlayersJoined=5 gameMessagesReceived=20`。
- 公网 gameplay 响应已验证：`TY_NEW_GAME -> JOINED -> PONG -> ROOM -> BYE`。
- ECS 元数据：实例 `i-uf6bjykk3xaut9e5lg33`，区域 `cn-shanghai`。

## 2026-07-05 ECS 实机验证记录

目标机器：

- IP：`<ECS_HOST>`
- SSH 管理用户：`<ECS_USER>`
- 系统：Ubuntu 24.04.4 LTS
- CPU：2 core
- 内存：约 1.6GiB
- 服务用户：`tyserver`
- 安装目录：`/opt/ty-new-server`
- 日志目录：`/var/log/ty-new`
- systemd 服务：`ty-new-server.service`

已验证：

- 上传包 SHA256：`d8e8744e985c280a551dc83385a53bfaeb0893e880f044a482086f50b22a6dc5`
- ECS 本机前台启动成功，`127.0.0.1:7778` 返回 `TY_NEW_SERVER status=ok`
- systemd 托管启动成功，服务状态为 `active (running)`
- systemd 托管后 `127.0.0.1:7778` 持续返回 `status=ok`
- 约 2 分钟空跑观察中 RSS 约 90MiB，systemd memory 约 67-68MiB，CPU 约 1-2%
- 日志出现 `Forcing GfxDevice: Null` 和 `NullGfxDevice`，符合 headless 运行预期

公网入口排查记录：

- 2026-07-05：从本机公网侧连接 `<ECS_HOST>:7778` 的 TCP 握手可建立，但读取 health 内容超时，且 ECS `ss -antp` 未观察到对应公网 `ESTABLISHED` 连接。
- ECS 本机 `ufw` 为 inactive，iptables 默认 ACCEPT，nftables 无规则，本机防火墙没有拦截 `7778`。
- ECS 本机通过私网 IP `172.24.54.177:7778` 可读取 health，说明服务已绑定到网卡地址。
- 抓包 `sudo timeout 15 tcpdump -n -i any tcp port 7778` 期间，只观察到 ECS 内部私网 IP 探活流量，没有观察到公网侧 `<ECS_HOST>:7778` 的连接进入实例网卡。
- 2026-07-06：从本机公网侧连接 `<ECS_HOST>:7778` 已可直接读取 `TY_NEW_SERVER status=ok`，`nc -vz -w 3 <ECS_HOST> 7778` 成功。
- 当前结论是 ECS 内部 health 和公网 health 均已通过。
- P1 已实现最小 gameplay 协议，ECS 本机 `7777` 验证已通过。
- 2026-07-06：P1 部署后首次公网 `7777` gameplay 探针超时；抓包对照显示公网访问 `7778` 可在 ECS `eth0` 捕获 SYN、响应 payload 和 FIN，公网访问 `7777` 在 ECS `tcpdump -n -i any tcp port 7777` 中为 `0 packets captured`。
- 2026-07-06：随后公网 `7777` 入口规则生效，`Deploy/DedicatedServer/probe_p1_gameplay.py <ECS_HOST> --player-name PublicRetest --timeout 15` 通过，完整返回 `TY_NEW_GAME`、`JOINED`、`PONG`、`ROOM`、`BYE`。

## 本机构建与打包

```bash
Tools/unity-cli/ty-new-build-dedicated-server smoke --hub-licensing --licensing-ipc LicenseClient-don --wall-timeout 300
Tools/unity-cli/ty-new-build-dedicated-server linux --hub-licensing --licensing-ipc LicenseClient-don --wall-timeout 1800
COPYFILE_DISABLE=1 tar --exclude '._*' -czf Builds/DedicatedServer/TYServer-linux-x86_64-p3-attack.tar.gz -C Builds/DedicatedServer/Linux .
```

构建完成后确认：

```bash
file Builds/DedicatedServer/Linux/TYServer.x86_64
du -sh Builds/DedicatedServer/Linux
shasum -a 256 Builds/DedicatedServer/TYServer-linux-x86_64-p3-attack.tar.gz
```

期望 `file` 显示 Linux x86-64 ELF。当前 macOS 本机不能直接运行该 ELF，需要在 Linux ECS 或 Linux 容器内做实跑验证。

## ECS 初始部署

以下命令假设 ECS 是 Ubuntu 22.04/24.04 或 Debian 系发行版，目标目录为 `/opt/ty-new-server`。

```bash
sudo apt-get update
sudo apt-get install -y netcat-openbsd
sudo useradd --system --create-home --shell /usr/sbin/nologin tyserver
sudo mkdir -p /opt/ty-new-server /var/log/ty-new
sudo chown -R tyserver:tyserver /opt/ty-new-server /var/log/ty-new
```

上传并解包：

```bash
scp Builds/DedicatedServer/TYServer-linux-x86_64-p15-ngo.tar.gz <user>@<ecs-ip>:/tmp/
ssh <user>@<ecs-ip>
sudo tar -xzf /tmp/TYServer-linux-x86_64-p15-ngo.tar.gz -C /opt/ty-new-server
sudo chmod +x /opt/ty-new-server/TYServer.x86_64
sudo chown -R tyserver:tyserver /opt/ty-new-server
```

先前台空跑：

```bash
sudo -u tyserver /opt/ty-new-server/TYServer.x86_64 -batchmode -nographics --port 7777 --network-port 7777 --network-bind-address 0.0.0.0 --health-port 7778 --health-bind-address 0.0.0.0 --target-fps 30 --tick-rate 30 --log-interval 10 -logFile /var/log/ty-new/server.log
```

另开一个 SSH 窗口探活：

```bash
printf '' | nc -w 2 127.0.0.1 7778
tail -f /var/log/ty-new/server.log
```

探活应返回类似：

```text
TY_NEW_SERVER status=ok uptimeSeconds=... frame=... managedMemoryMb=... port=7777 healthEnabled=true healthBindAddress=0.0.0.0 healthPort=7778 targetFrameRate=30 tickRate=30 connectionsAccepted=... activeConnections=... gameplayEnabled=true gameplayBindAddress=0.0.0.0 gameplayPort=7777 room=combat-test maxPlayers=16 gameConnectionsAccepted=... gameActiveConnections=... gamePlayersJoined=... gameActivePlayers=... gameMessagesReceived=... networkEnabled=true networkStarted=true networkListening=true networkIsServer=true networkIsClient=false networkListenAddress=0.0.0.0 networkConnectAddress=127.0.0.1 networkPort=7777 networkMaxPlayers=16 networkConnectedClients=... networkSpawnedPlayers=...
```

## P1 gameplay 协议验证

P1 gameplay 是面向探针和后续客户端接入的最小行协议，不是最终同步协议。

服务端启动后会在 TCP `7777` 写入欢迎行：

```text
TY_NEW_GAME protocol=1 connectionId=<id> room=combat-test maxPlayers=16
```

支持命令：

```text
HELLO playerName=CodexSmoke
PING
STATE
QUIT
```

期望响应类似：

```text
JOINED connectionId=1 playerId=1 playerName=CodexSmoke room=combat-test players=1 maxPlayers=16
PONG connectionId=1 playerId=1 joined=true serverTimeMs=...
ROOM room=combat-test players=1 maxPlayers=16 activeConnections=1 connectionsAccepted=1 playersJoined=1 messagesReceived=3
BYE connectionId=1
```

ECS 本机验证：

```bash
python3 -u -c 'import socket; s=socket.create_connection(("127.0.0.1",7777),5); f=s.makefile("rw", encoding="utf-8", newline="\n"); print(f.readline().strip()); f.write("HELLO playerName=LocalSmoke\n"); f.flush(); print(f.readline().strip()); f.write("PING\n"); f.flush(); print(f.readline().strip()); f.write("STATE\n"); f.flush(); print(f.readline().strip()); f.write("QUIT\n"); f.flush(); print(f.readline().strip())'
```

公网验证：

```bash
python3 -u -c 'import socket; s=socket.create_connection(("<ECS_HOST>",7777),5); f=s.makefile("rw", encoding="utf-8", newline="\n"); print(f.readline().strip()); f.write("HELLO playerName=PublicSmoke\n"); f.flush(); print(f.readline().strip()); f.write("PING\n"); f.flush(); print(f.readline().strip()); f.write("STATE\n"); f.flush(); print(f.readline().strip()); f.write("QUIT\n"); f.flush(); print(f.readline().strip())'
```

如果 ECS 本机 `127.0.0.1:7777` 成功但公网失败，优先检查阿里云安全组是否放行 TCP `7777`。

也可以使用项目内探针脚本：

```bash
Deploy/DedicatedServer/probe_p1_gameplay.py <ECS_HOST> --player-name PublicSmoke
```

一键部署脚本会上传当前 P3 包、校验本地/远端 SHA256、安装 systemd service、重启服务，并在 ECS 本机执行 P1.5/P3 health、UDP `7777` 监听和 P1 TCP gameplay 探针：

```bash
Deploy/DedicatedServer/deploy_p1_gameplay.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

脚本内置远端检查：

- `probe_p15_multiplayer.py --health-only` 必须看到 `networkStarted=true networkListening=true networkIsServer=true`
- `ss -lunp` 必须看到 UDP `7777` 监听
- `probe_p1_gameplay.py 127.0.0.1 --player-name ECSSmoke` 必须通过旧 P1 TCP 行协议探针

## systemd 管理

把模板上传到 ECS：

```bash
scp Deploy/DedicatedServer/ty-new-server.service <user>@<ecs-ip>:/tmp/
ssh <user>@<ecs-ip>
sudo cp /tmp/ty-new-server.service /etc/systemd/system/ty-new-server.service
sudo systemctl daemon-reload
sudo systemctl enable ty-new-server
sudo systemctl start ty-new-server
sudo systemctl status ty-new-server --no-pager
```

常用运维命令：

```bash
sudo journalctl -u ty-new-server -f
tail -f /var/log/ty-new/server.log
sudo systemctl restart ty-new-server
sudo systemctl stop ty-new-server
```

## 安全组

P0/P1 空跑阶段建议：

- SSH `22/tcp`：只允许自己的公网 IP
- health `7778/tcp`：优先只允许自己的公网 IP或内网探测源
- gameplay `7777/tcp`：P1 包部署后再开放；playtest 前只允许自己的公网 IP
- NGO/UTP gameplay `7777/udp`：P1.5 双客户端 playtest 前开放；优先只允许自己的公网 IP

公网 playtest 前再开放 gameplay 端口，并记录开放来源、回滚办法和测试时间窗口。

当前实例信息：

- 区域：`cn-shanghai`
- 实例 ID：`i-uf6bjykk3xaut9e5lg33`
- 公网 IP：`<ECS_HOST>`
- 主网卡 ID：`eni-uf6bjykk3xaut9e7rqxm`
- VPC ID：`vpc-uf6px3zcji3u4wvcmin2w`
- vSwitch ID：`vsw-uf6snnt29tpe6vtnnjsm5`

当前已验证生效的 P1 TCP 入方向规则要求：

- 协议类型：TCP
- 端口范围：`7777/7777`
- 授权对象：优先填写自己的公网 IP `/32`；临时测试才考虑更宽范围
- 目标：绑定到实例 `i-uf6bjykk3xaut9e5lg33` 所属安全组

P1.5/P3 双客户端验证当前已确认：

- 协议类型：UDP
- 端口范围：`7777/7777`
- 授权对象：当前测试已临时放行 `0.0.0.0/0`；长期 playtest 前建议收窄到自己的公网 IP `/32`
- 目标：绑定到实例 `i-uf6bjykk3xaut9e5lg33` 所属安全组

复跑完整 P3 公网验证：

```bash
Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

也可以分步运行：

```bash
Deploy/DedicatedServer/probe_udp_ingress.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
Deploy/DedicatedServer/probe_p15_multiplayer.py --skip-server-start --host <ECS_HOST> --game-port 7777 --health-port 7778 --connected-timeout 60
```

公网复测：

```bash
Deploy/DedicatedServer/probe_p1_gameplay.py <ECS_HOST> --player-name PublicSmoke --timeout 15
```

## 2 核 2G 观察项

空跑至少观察 30-60 分钟：

```bash
top -p $(pgrep TYServer.x86_64)
ps -o pid,rss,vsz,pcpu,pmem,etime,command -p $(pgrep TYServer.x86_64)
tail -f /var/log/ty-new/server.log
```

继续下一阶段的最低门槛：

- 进程不崩溃
- health 端口持续响应
- 常驻内存明显低于 1.2G
- CPU 空跑稳定，不长期超过 20%
- 日志没有 graphics/input/audio/server bootstrap 错误

这些结果只能证明 P0/P1 服务端空跑可行，不能证明多人容量。
