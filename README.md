# Unity 3D 动作 RPG 第一章工程

本工程用于制作一个独立开发的 `Unity 3D 第三人称动作 RPG` 第一章可玩版本。当前目标不是做完整商业项目，而是在 `1 个月` 内做出一个可从头打到尾、具备完整战斗闭环、检查点恢复和章节完成结算的可玩章节。

当前工作区状态：

- Unity 版本：`6000.4.2f1`
- 当前渲染基线：`Built-in Render Pipeline（已移除 HDRP 运行依赖）`
- 已启用：`Input System`
- 目标平台：`Windows`、`Mac`
- 开发模式：`独立开发`、`少插件`、`模块化`
- 批处理注意：`macOS` 下执行 Unity `-batchmode` 前先退出 `Unity Hub`，避免旧版授权客户端与编辑器协议冲突

## Unity 初始化问题处理

若出现以下现象：

- Unity 打开工程后一闪而退
- `-batchmode` 长时间卡住后退出
- 日志出现 `Licensing initialization failed`、`Failed to handshake`、`The connection with the Unity Licensing Client has been lost`

优先按下面顺序处理，而不是先怀疑项目代码：

1. 退出所有 Unity Editor 实例。
2. 完全退出 `Unity Hub`，不要只关窗口。
3. 清理残留的 `Unity.Licensing.Client` 进程。
4. 手动重新打开一次主工程，确认编辑器能稳定停留。
5. 若主工程正在本地打开，自动化测试和批处理优先在临时克隆目录执行，避免项目锁和 `Library` 冲突。

详细处理步骤与原因说明见 [Unity 工程初始化清单](Docs/Unity_Project_Setup_Checklist.md)。

## 文档索引

- [项目总文档](Docs/Project_Master_Document.md)
- [第一版开发蓝图](Docs/Development_Blueprint_V1.md)
- [Unity 工程初始化清单](Docs/Unity_Project_Setup_Checklist.md)
- [核心脚本架构与骨架](Docs/Core_Script_Architecture.md)
- [CombatTest 接线清单](Docs/CombatTest_Setup_Guide.md)
- [协作约束与代理说明](AGENT.md)
- [Assets 目录说明](Assets/_Game/README.md)

## 当前执行原则

- 先完成 `战斗手感`，再补 `章节包装`
- 先保证 `可通关`，再追求 `内容体量`
- 先做 `灰盒与调试能力`，再做 `美术替换`
- 所有关键参数必须 `数据化`
- 所有核心系统必须在 `CombatTest` 或 `BossTest` 中单独验证

## 目录约定

- `Docs/`：项目文档、蓝图、清单
- `Assets/_Game/`：正式游戏内容
- `Assets/_Game/Data/`：ScriptableObject 与配置资产
- `Assets/_Game/Prefabs/`：角色、战斗、交互、UI 预制体
- `Assets/_Game/Scenes/`：正式章节场景与测试场景
- `Assets/_Game/Scripts/`：运行时代码、编辑器工具、测试

## 下一步建议

1. 在 Unity 中打开并确认 `Assets/_Game/Scenes/` 下的正式 Scene、`CampusInputActions` 和 asmdef 被正确导入。
2. 按 [核心脚本架构与骨架](Docs/Core_Script_Architecture.md) 补齐 `Player`、`Combat`、`AI`、`Save` 的第二批行为脚本。
3. 先在 `CombatTest` 中接起 `GameBootstrap`、`InputReader`、`PlayerCharacter`、`HealthComponent` 等基础组件，再开始攻击与敌人状态。
