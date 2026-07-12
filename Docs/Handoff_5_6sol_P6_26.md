# 5.6sol 接手交接：TY_NEW 多人 Dedicated Server 线

更新时间：2026-07-10
工作区：`<PROJECT_ROOT>`
接手重点：Dedicated Server / ECS 多人战斗验证线已在 P6.31 正式收口。

> 公开仓库说明：服务器身份和本机密钥路径已替换为 `<ECS_USER>`、`<ECS_HOST>` 和 `<SSH_KEY_PATH>`；运行命令前必须填入你自己的安全运维参数。

## 1. 当前一句话结论

TY_NEW 仍不能被描述为“完整多人动作 RPG 服务端”，但 P6 独立 multiplayer spike 已收口：2 核 2G ECS 上的 Linux Dedicated Server 已完成 formal CombatTest 网络玩家、server-owned 网络敌人、server tick 攻击、三敌稳定常驻四击回归、五敌低伤害双轮探索、五敌常规伤害失败诊断，以及不修改 service 的最终 closure gate。

当前 ECS 常驻稳定配置不是四敌人，而是：

```text
--network-player-prefab Multiplayer/PF_NetworkPlayerCombatTest
--network-enemy-count 3
--enable-network-enemy-server-tick
--network-enemy-server-tick-damage 10
--network-enemy-server-tick-death-delay-seconds 90
```

P6.31 证明的是：当前三敌、tick damage `10`、delay `90` 常驻基线可以在不重启 service 的前提下再次通过三敌各四击目标保持；回归前后 MainPID 均为 `112305`，health 均为 `0/0`，unit/effective 参数和实验锁无漂移。至此 P6 关闭；五敌常规伤害、公平调度、长压测和容量属于后续独立范围。

## 2. 关键路径和文件

- ECS runbook：`Docs/Dedicated_Server_ECS_Runbook.md`
- 约束文档：`Docs/Multiplayer_Server_Constraints.md`
- 当前稳定 systemd 模板：`Deploy/DedicatedServer/ty-new-server.service`
- 一般 ECS 公网验证：`Deploy/DedicatedServer/verify_p15_ecs_multiplayer.sh`
- P6.26 新增脚本化复验：`Deploy/DedicatedServer/verify_p625_ecs_four_enemy_retention.sh`
- P6.27-P6.30 通用实验工具：`Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh`
- P6.27/P6.29 离线契约测试：`Deploy/DedicatedServer/tests/verify_ecs_network_enemy_retention_with_temp_service_test.sh`
- 本地/ECS 探针核心：`Deploy/DedicatedServer/probe_p15_multiplayer.py`
- P6.30 retention 诊断离线测试：`Deploy/DedicatedServer/tests/test_probe_p15_multiplayer_retention.py`
- P6.31 总收口 gate：`Deploy/DedicatedServer/verify_p631_p6_closure.sh`
- P6.31 closure 离线契约：`Deploy/DedicatedServer/tests/verify_p631_p6_closure_test.sh`

远端 ECS：

- Host：`<ECS_HOST>`
- SSH user：`<ECS_USER>`
- SSH key：`<SSH_KEY_PATH>`
- Service：`ty-new-server.service`
- Health：TCP `7778`
- NGO/UTP：UDP `7777`

## 3. 本线程完成的主要工作

### 调研与约束

- 明确初始结论：项目不能直接把现有 Unity 客户端丢到阿里云 2 核 2G 当正式多人服务端。
- 产出并持续更新 `Docs/Multiplayer_Server_Constraints.md`，把“能验证什么、不能宣称什么”作为硬边界。
- 确立路线：只把 2 核 2G ECS 当工程验证环境，不当正式容量结论。

### Dedicated Server / Multiplayer 里程碑推进

本线程/本阶段已推进到 P6.31 并关闭 P6：

- P6.20：ECS 公网两只 server tick 敌人短窗口目标保持。
- P6.22：修复四击目标保持缺口，两只敌人各自 4 次 retained attacks 通过。
- P6.23：三只 server tick 敌人四击目标保持，本机与 ECS 公网通过；产出当前复用包。
- P6.24：同一 ECS server 进程连续两轮三敌人四击目标保持通过。
- P6.25：四敌人探索；tick damage `10` 失败，tick damage `5` 通过。
- P6.26：新增脚本，把 P6.25 的手工 systemd 参数切换变成“备份、临时覆盖、验证、恢复、health 复查”的可复跑流程。
- P6.27：通用参数实验工具完成真实 ECS 四敌低伤害复验，证明 unit/effective baseline 检查、持久备份、恢复 health 与锁清理合同。
- P6.28：五敌人 + tick damage `5` 公网 smoke 通过，五只敌人各自完成 4 次 retained attacks，结束后恢复三敌常驻基线。
- P6.29：通用工具增加 `--rounds` 和临时 service PID 守门；五敌低伤害在同一临时 PID 下连续两轮通过，每轮后 health 回到 `0/0`，最终恢复三敌常驻基线。
- P6.30：五敌 + damage `10` 缺口稳定复现；探针新增结构化诊断，证明 20 次攻击耗尽两名玩家 200 HP，但 per-enemy 分布不均使敌人 3/5 未完成四击。
- P6.31：新增只读式 closure gate；真实 ECS 三敌四击回归通过，前后 PID `112305` 不变，health `0/0`、baseline 参数和锁状态均无漂移，最终输出 `P6_CLOSURE_OK p6Status=closed`。

当前复用的 Linux Dedicated Server 包：

```text
Builds/DedicatedServer/TYServer-linux-x86_64-p623-three-enemy-retention.tar.gz
SHA256: 29ab4ba9ea03251be40ba92d756838aec050e3aebf71eeeec8264df200b92edf
```

## 4. P6.26 验证证据

新增脚本：

```bash
Deploy/DedicatedServer/verify_p625_ecs_four_enemy_retention.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

脚本行为：

- 先检查远端 service 是否处于 P6.23 稳定基线。
- 备份 `/etc/systemd/system/ty-new-server.service` 到 `/tmp/ty-new-server.service.p625-backup.$$`。
- 临时切到四敌人 + tick damage `5`。
- 运行公网 health、UDP ingress、双客户端 smoke。
- 最后恢复原 service，重启，并做 health-only 复查。

P6.26 实跑通过的关键输出：

```text
P1.5_UDP_INGRESS_OK host=<ECS_HOST> port=7777
P6_NETWORK_ENEMY_COUNT_OK minNetworkEnemyCount=4 client1ObservedEnemyCount=4 client2ObservedEnemyCount=4 client1EnemyIds=1,2,3,4 client2EnemyIds=1,2,3,4
P6_NETWORK_ENEMY_SERVER_TICK_OK navMeshReady=true serverTickAttackCount=40
P6_NETWORK_ENEMY_TARGET_DISTRIBUTION_OK minEnemyCount=4 uniqueTargetCount=2 enemyTargets=1->1,2->2,3->2,4->1 enemyAttackCounts=1:11,2:11,3:8,4:10
P6_NETWORK_ENEMY_TARGET_RETENTION_OK minEnemyCount=4 minRetainedAttacks=4 uniqueTargetCount=2 enemyTargets=1->1,2->2,3->2,4->1 retainedAttackCounts=1:4,2:4,3:4,4:4
P6_MULTIPLAYER_OK host=<ECS_HOST> gamePort=7777 healthPort=7778 ... networkEnemyTargetDistribution=true networkEnemyTargetRetention=true disconnected=0
```

恢复证据：

```text
ExecStart ... --network-enemy-count 3 --enable-network-enemy-server-tick --network-enemy-server-tick-damage 10 --network-enemy-server-tick-death-delay-seconds 90 ...
P1.5_HEALTH_OK host=<ECS_HOST> healthPort=7778 networkPort=7777 connected=0 spawned=0
P6.25 scripted ECS four-enemy retention verification passed and service was restored.
```

本地检查：

```bash
sh -n Deploy/DedicatedServer/verify_p625_ecs_four_enemy_retention.sh
```

已通过，脚本也已设置可执行位。

## 5. 当前风险和边界

- 工作区非常脏，包含大量 Unity 资产、local preview、docs、Deploy 目录 untracked/modified；不要贸然提交、重置或清理。
- `Docs/Dedicated_Server_ECS_Runbook.md`、`Docs/Multiplayer_Server_Constraints.md` 和 `Deploy/DedicatedServer/` 当前在 `git status` 下显示为 untracked 或 dirty，需要接手者先审计再决定如何纳入版本控制。
- P6.27-P6.30 通用脚本会重启 ECS `ty-new-server.service`，只能在允许短暂服务中断的窗口运行。
- P6.31 closure gate 不修改或重启 service；任何出现 PID 变化、baseline 漂移、health 非零或锁存在都会失败。
- 四敌人与五敌人 + tick damage `10` 都未通过四击保持；P6.30 已把五敌失败定位为两名玩家 200 HP 被 20 次攻击耗尽时的 per-enemy 调度不均，不要把常规伤害配置当成已通过，也不要误写成 ECS 容量失败。
- 当前 ECS 常驻配置仍是三敌人 + tick damage `10`，不是四敌人 + tick damage `5`。
- 2 核 2G ECS 仍只是工程验证环境，不是正式公网动作联机容量结论。

## 6. P6 收口证据与后续边界

### P6.27 通用工具已完成 ECS 实证

已新增通用 ECS 参数实验工具：

```text
Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh
```

当前能力：

- 参数化 enemy count、tick damage、death delay、retention attacks、client seconds 和 rounds；默认仍为单轮，最多 10 轮。
- 多轮实验只安装一次临时 service，并在每轮前后校验 `MainPID` 不变；每轮验证结束后额外执行 health-only，要求连接与玩家计数归零。
- 同时检查 unit 文件与 systemd effective `ExecStart`，默认拒绝 baseline 不匹配、重复参数或并发实验。
- 使用持久 root-only 锁目录保存 owner 与完整 service 备份，再做临时覆盖、验证、恢复和恢复后 health。
- 成功、验证失败、安装失败与可处理信号都会进入恢复路径；恢复失败时保留现场并明确要求人工介入。
- 输出清晰区分：临时配置验证结果、恢复结果、当前常驻配置。
- 提供 `--dry-run`，不连接远端即可检查参数；离线契约测试覆盖成功、验证失败恢复、安装失败恢复和恢复失败告警。

这样后续试 5 敌人、不同 damage、不同 death delay 时，不需要再新增一次性脚本。

本地已通过 `sh -n`、`dash -n` 以及 sh/dash 双路径契约测试。真实 ECS 首轮复验使用四敌人、tick damage `5`、每敌 4 次 retained attacks：公网 health、UDP 入站、四敌可见性、server tick、目标分配与目标保持全部通过；随后 effective `ExecStart` 恢复三敌人、damage `10`、delay `90`，health 回到 `connected=0 spawned=0`，备份与锁均已清理。

### P6.28 五敌低伤害 smoke 已通过

P6.28 复用 P6.27 工具与 P6.23 已部署包，临时配置为：

```text
--network-enemy-count 5
--network-enemy-server-tick-damage 5
--network-enemy-server-tick-death-delay-seconds 90
```

公网结果：两个客户端都观察到 enemyId `1,2,3,4,5`；`serverTickAttackCount=40`；目标分配为 `1->1,2->2,3->2,4->1,5->2`，覆盖两个玩家；攻击计数为 `1:13,2:9,3:6,4:7,5:5`；目标保持为 `retainedAttackCounts=1:4,2:4,3:4,4:4,5:4`。验证结束后 service 恢复三敌人 + damage `10`，health 为 `connected=0 spawned=0`，锁和备份已清理。

### P6.29 五敌低伤害同进程双轮 smoke 已通过

P6.29 不新增 Unity runtime 或 Linux 包；通用工具新增 `--rounds`，一次临时覆盖后按轮运行 smoke，并在每轮前后检查临时 service `MainPID`。ECS 临时配置为五敌人、tick damage `5`、death delay `90`，两轮均使用 PID `109706`。

- 第 1 轮：`enemyTargets=1->1,2->2,3->2,4->1,5->2`，`enemyAttackCounts=1:13,2:9,3:6,4:7,5:5`，五敌均 `retainedAttackCounts=...:4`。
- 第 2 轮：`enemyTargets=1->3,2->4,3->4,4->3,5->4`，`enemyAttackCounts=1:14,2:8,3:6,4:6,5:6`，五敌均 `retainedAttackCounts=...:4`。
- 两轮各自结束后 health-only 均为 `connected=0 spawned=0`；最终输出 `completedRounds=2 requestedRounds=2 temporaryServicePid=109706`。
- 恢复后独立核验：effective `ExecStart` 为三敌人 + damage `10` + delay `90`，新常驻 PID `110418`，health `0/0`，实验锁不存在。

P6.29 的双轮结果仍只覆盖低伤害 `5`；五敌 `damage 10` 的失败原因已由下方 P6.30 诊断，不要把双轮 smoke 写成长期稳定或正式容量结论。

### P6.30 五敌常规伤害生命预算诊断已完成

P6.30 不新增 Unity runtime 或 Linux 包，只增强 `probe_p15_multiplayer.py` 的 retention 失败路径。失败时保留原始 `enemyId expected/actual`，并额外输出 `P6_NETWORK_ENEMY_TARGET_RETENTION_DIAGNOSTIC`，包含攻击序列、每敌攻击数与缺口、每目标命中/伤害/HP/死亡者以及分类。

真实 ECS 诊断版复验输出：

```text
classification=health_budget_exhausted_with_uneven_enemy_scheduling
observedAttacks=20 requiredAttacks=20
enemyAttackCounts=1:6,2:5,3:3,4:4,5:2
enemyAttackDeficits=3:1,5:2 missingEnemyAttackSlots=3 excessEnemyAttacks=3
targetBudgets=1:hits=10/damage=100/health=100->0/dead=true/killedByEnemy=1|2:hits=10/damage=100/health=100->0/dead=true/killedByEnemy=5
```

这说明总攻击量达到五敌各四击所需的 20 次，但两名玩家各 100 HP 已被完全耗尽，额外的 3 次攻击集中到敌人 1/2，正好对应敌人 3/5 的 3 个缺口。实验失败后工具恢复三敌常驻配置；独立核验 effective `ExecStart` 为三敌 + damage `10` + delay `90`，PID `112305`，health `0/0`，锁不存在。

P6.30 已把五敌常规伤害限制解释清楚；不要通过延长客户端时长掩盖玩家已死亡的事实。

### P6.31 总收口 gate 已通过，P6 CLOSED

新增 `Deploy/DedicatedServer/verify_p631_p6_closure.sh`，它不安装临时 unit、不重启 service，只执行以下收口合同：

- 回归前后同时检查 unit 文件与 effective `ExecStart` 均为三敌 + damage `10` + delay `90`。
- 回归前后要求 `/var/tmp/ty-new-server-retention.lock` 不存在，health 为 `connected=0 spawned=0`。
- 捕获 MainPID 后直接在当前常驻 service 上运行三敌各四击目标保持；完成后要求 PID 不变。
- 输出固定能力矩阵和 `P6_CLOSURE_OK p6Status=closed`。

真实 ECS 最终证据：`serverTickAttackCount=20`；`enemyTargets=1->1,2->2,3->2`；`retainedAttackCounts=1:4,2:4,3:4`；前后 PID 均为 `112305`；最终 health `0/0`，baseline 与锁无漂移。

| P6 收口项 | 结论 | 证据/边界 |
|---|---|---|
| 三敌常驻基线 | 已关闭、可复跑 | damage `10`、delay `90`，P6.31 同 PID 四击回归通过 |
| 五敌低伤害 | 探索通过 | damage `5`，P6.29 同一临时 PID 双轮通过；不作为常驻配置 |
| 五敌常规伤害 | 已知限制 | P6.30 定位为 200 HP 耗尽与攻击调度不均；未通过 |
| 长时间稳定/容量/断线重连 | P6 范围外 | 如需继续，另开 P7，不得从当前 smoke 外推 |

P6 收口后应回到第一章正式开发。若未来明确启动 P7，再单独定义公平调度、长压测、指标和容量门槛。

## 7. 接手者快速命令

查看当前文档状态：

```bash
rg -n 'P6\.31|P6 CLOSED|P0-P6\.31|P6_CLOSURE_OK|health_budget_exhausted' Docs/Dedicated_Server_ECS_Runbook.md Docs/Multiplayer_Server_Constraints.md
```

语法检查脚本：

```bash
sh -n Deploy/DedicatedServer/verify_p625_ecs_four_enemy_retention.sh
sh -n Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh
Deploy/DedicatedServer/tests/verify_ecs_network_enemy_retention_with_temp_service_test.sh
python3 Deploy/DedicatedServer/tests/test_probe_p15_multiplayer_retention.py
Deploy/DedicatedServer/tests/verify_p631_p6_closure_test.sh
Deploy/DedicatedServer/verify_p631_p6_closure.sh --dry-run <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh --dry-run --rounds 2 <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

复跑 P6.31 最终 closure gate：

```bash
Deploy/DedicatedServer/verify_p631_p6_closure.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

复跑 P6.28 五敌低伤害验证：

```bash
Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh --enemy-count 5 --tick-damage 5 --death-delay-seconds 90 --retention-attacks 4 --client1-seconds 100 --client2-seconds 90 <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

复跑 P6.29 五敌低伤害同进程双轮验证：

```bash
Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh --enemy-count 5 --tick-damage 5 --death-delay-seconds 90 --retention-attacks 4 --rounds 2 --client1-seconds 100 --client2-seconds 90 <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

复跑 P6.30 五敌常规伤害诊断（预期 smoke 返回非零，但必须恢复 service）：

```bash
Deploy/DedicatedServer/verify_ecs_network_enemy_retention_with_temp_service.sh --enemy-count 5 --tick-damage 10 --death-delay-seconds 90 --retention-attacks 4 --rounds 1 --client1-seconds 100 --client2-seconds 90 <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

复跑 P6.26 ECS 验证：

```bash
Deploy/DedicatedServer/verify_p625_ecs_four_enemy_retention.sh <ECS_USER> <ECS_HOST> <SSH_KEY_PATH>
```

只做公网 health：

```bash
Deploy/DedicatedServer/probe_p15_multiplayer.py --health-only --host <ECS_HOST> --game-port 7777 --health-port 7778 --startup-timeout 10 --socket-timeout 5
```

检查远端 service 当前参数：

```bash
ssh -i <SSH_KEY_PATH> <ECS_USER>@<ECS_HOST> "systemctl cat ty-new-server.service"
```

## 8. 给 5.6sol 的接手原则

- 先读 `Docs/Dedicated_Server_ECS_Runbook.md` 的 P6.26、P6.25、P6.24、P6.23 段。
- 不要把 smoke 通过写成正式玩法完成。
- 不要把 2 核 2G smoke 写成正式容量。
- 不要在未审计 dirty tree 前提交。
- 每次改 ECS service 都必须有恢复路径和恢复后 health 证据。
- 优先把重复手工动作脚本化，再做更激进的敌人数/伤害/时长探索。
