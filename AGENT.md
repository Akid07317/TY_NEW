# AGENT.md

本文件用于约束后续所有在该工作区内协作的代理、脚本助手和人工开发者，确保项目始终围绕“`1 个月内完成第一章可玩版本`”这一目标推进。

## 1. 项目定位

- 项目类型：`Unity 3D 第三人称动作 RPG`
- 当前目标：只做 `第一章`
- 优先级：`战斗手感` > `章节闭环` > `可测试性` > `包装表现`
- 目标版本：`Windows`、`Mac` 可运行的章节完整体验

## 2. 不可突破的范围边界

以下内容在第一版中默认禁止新增，除非文档与范围同步更新：

- 联机
- 开放世界
- 装备与背包系统
- 商店系统
- 多职业
- 复杂任务树
- 多结局
- 复杂成长树
- 元素反应系统
- 两阶段以上 Boss
- 同伴系统
- 程序生成地图
- 复杂潜行系统

## 3. 核心技术约束

- 必须使用 `Unity Input System`
- 必须使用 `Cinemachine` 做第三人称相机
- 玩家与敌人核心逻辑必须基于 `有限状态机`
- 技能、攻击参数、敌人配置、掉落表必须基于 `ScriptableObject`
- 尽量少依赖第三方插件，优先用 Unity 原生方案
- 逻辑层必须与动画层解耦，动画事件只负责时机通知

## 4. 工作顺序

每次开始实装前，遵守下面顺序：

1. 先阅读 `Docs/Project_Master_Document.md`
2. 再阅读 `Docs/Development_Blueprint_V1.md`
3. 对照 `Docs/Core_Script_Architecture.md` 确认改动落点
4. 仅在必要时新增目录、脚本、SO 类型
5. 修改后补齐测试场景验证或最小测试说明

### 批处理与自动化附加规则

- 在 `macOS` 上执行 Unity `-batchmode`、测试或编辑器工具前，先确认 `Unity Hub` 已退出，避免旧版 `UnityLicensingClient` 抢占授权通信通道。
- 若主工程 `/Users/don/TY_NEW` 已被本地 Unity 编辑器打开，自动化构建优先使用临时克隆目录执行，避免与人工编辑状态互相抢锁。
- 命令行执行 Unity `-runTests` 时不要附带 `-quit`；当前 `com.unity.test-framework@1.6.0` 会在测试结束后自行退出，而 `-quit` 会让测试不启动。
- 本地回归优先使用 `Tools/unity-cli/unity-run-tests`，避免测试参数漂移。
- 自动化严禁裸跑没有墙钟超时的 Unity `-batchmode` / `-executeMethod` 命令。若必须执行编辑器方法，先确认同项目 GUI Editor 已退出或改用隔离副本，并给本次进程设置明确的最大等待时间；若日志在启动阶段长时间只停留在 licensing / Package Manager 之前，记录阻塞并停止等待。
- 批处理失败时先看 `licensing` 与 `Package` 日志，再判断是否属于代码问题；不要把环境阻塞误判成运行时代码回归。
- 若授权/entitlement 问题反复出现，自动化可以按 `Tools/unity-cli/README.md` 热启动 `Unity Hub` 与 Unity Editor 做 warmup；warmup 完成后仍需确认 `Unity Hub` 已退出，再执行 batchmode 测试。

## 5. 代码与工程规范

- 脚本命名要体现职责，避免滥用 `Manager`
- 单个脚本只做一个主要职责
- 所有公开可调参数优先暴露到配置资产，而不是散落在 MonoBehaviour
- FSM 状态切换由代码决定，动画不做状态权威源
- 角色动作尽量以代码驱动位移为主，Root Motion 仅用于少量受控动作
- Debug 功能必须保留：回血、回蓝、加量表、传送检查点、重置遭遇战、直进 Boss

## 6. 目录规则

- 正式内容统一放入 `Assets/_Game/`
- 不要把业务脚本继续堆在 `Assets/` 根目录
- 新增场景统一放在 `Assets/_Game/Scenes/`
- 新增配置资产统一放在 `Assets/_Game/Data/`
- 测试代码统一放在 `Assets/_Game/Scripts/Tests/`

## 7. 文档维护规则

- 若改动了核心范围、战斗规则、开发阶段计划，必须同步更新 `Docs/Project_Master_Document.md`
- 若改动了执行顺序、周计划、砍线策略，必须同步更新 `Docs/Development_Blueprint_V1.md`
- 若改动了目录、输入、层级、场景基线，必须同步更新 `Docs/Unity_Project_Setup_Checklist.md`
- 若改动了主模块或类职责，必须同步更新 `Docs/Core_Script_Architecture.md`

## 8. 交付判定原则

任何新增内容都要回答三个问题：

1. 它是否提升第一章的可通关质量？
2. 它是否会拖慢 1 个月交付？
3. 它是否破坏当前模块化和可测试性？

若第 1 条答案不明确，或第 2、3 条风险偏高，则默认不做。
