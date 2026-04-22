# 素材来源清单

本清单只回答一件事：哪些素材属于公开仓库默认基线，哪些素材只能留在本地预览链。

## 1. 当前原则

- 公开仓库默认基线始终是 `Assets/_Game/` 自包含的 proxy visuals + proxy / approved `_Game` 动画输出。
- `Build CombatTest Scene`、`Repair CombatTest Prefab Wiring`、`Build Chapter01 Combined Scene` 这类标准构建链，必须在有没有第三方素材的机器上都产出同一套 public-repo-safe 结果。
- `Repair Chapter01 Baseline And Traversal Wiring` 也属于标准收口链，职责是先恢复 `CombatTest` prefab 的 proxy baseline，再同步 `Chapter01` 场景接线，不允许把 local preview 结果固化回正式章节。
- `Assets/Kevin Iglesias`、`Assets/DoubleL`、`Assets/ithappy`、`Assets/JC_LP_MedievalCharacters_LITE` 这类目录只允许作为本地 local preview 候选源，不能再被当作正式默认输入源。
- 敌人当前仍固定走 `CombatProxyVisualRoot` 代理外观基线；在补齐独立敌人 Animator / Avatar / 动画链之前，不启用 imported enemy 默认链。
- 第三方原始资源目录不应直接提交到公开仓库；如果后续真要让某套角色或动作成为正式默认资源，应该先把可提交的净化结果落进 `_Game`，再由正式 builder 只读 `_Game`。

## 2. 当前目录清单

| 本地目录 | 当前用途 | 是否为仓库硬依赖 | 当前状态 |
|---|---|---|---|
| `Assets/Kevin Iglesias/` | 玩家 / 敌人 local preview 角色 prefab / Avatar / 动作候选源 | 否 | local preview only |
| `Assets/DoubleL/` | 玩家 local preview 攻击动作候选源 | 否 | local preview only |
| `Assets/ithappy/` | 玩家 local preview 走跑 / 闪避 / 受击 / 死亡动作候选源 | 否 | local preview only |
| `Assets/JC_LP_MedievalCharacters_LITE/` | 玩家 local preview Humanoid 角色 prefab / Avatar 首选源 | 否 | local preview only |
| `Assets/Free medieval weapons/` | 玩家 local preview 武器 prefab 首选源 | 否 | local preview only |
| `Assets/MYFG-Weapon Pack Lite/` | 武器资源候选 | 否 | 当前未接入 `_Game` |
| `Assets/Polytope Studio/` | 场景 / 美术资源候选 | 否 | 当前未接入 `_Game` |

## 3. 代码锚点

- `Assets/_Game/Scripts/Editor/CombatImportedPlayerVisualUtility.cs`
  玩家 imported visuals 的本地预览入口。默认值应为关闭，不能再决定正式 build / repair 的输出。

- `Assets/_Game/Scripts/Editor/CombatImportedEnemyVisualUtility.cs`
  敌人 imported Avatar chain 的 local preview 入口。标准 `Build` / `Repair` 路径不会调用它；只有显式的 local preview 菜单才会生成本地 AnimatorController、给 enemy root 挂 `Animator + EnemyCombatAnimationRelay`，并把 imported humanoid 角色接到单独的 Avatar 链上。

- `Assets/_Game/Scripts/Editor/CombatTestAssetGenerator.cs`
  正式默认职责是生成 proxy / placeholder / approved `_Game` 动画资产。若要重建 imported player 动画，只能走显式的 local preview 菜单。

- `Assets/_Game/Scripts/Editor/CombatTestSceneBuilder.cs`
  标准 `Build` / `Repair` 路径应始终恢复玩家 proxy baseline；local preview 只允许走单独的显式菜单。

- `Assets/_Game/Scripts/Tests/EditMode/CombatTestAnimationAssetWiringTests.cs`
  默认测试集应保护 public baseline，不再按“本机有没有 imported 素材”切换成两套不同断言。

## 4. 本地预览工作流

如果你只是想在本机看 imported 角色 / 动作，按下面顺序：

1. 导入本地素材，并保留上表目录名。
2. 手动开启菜单 `CampusRPG/Setup/CombatTest/Prefer Imported Player Sources When Available`。
3. 执行 `CampusRPG/Setup/Local Preview/Rebuild CombatTest Imported Player Animations`。
4. 如需预览导入角色，再执行 `CampusRPG/Setup/Local Preview/Apply Imported Player Visuals To CombatTest Player Prefab`。
   当前会优先尝试 `Assets/JC_LP_MedievalCharacters_LITE/Prefabs/SM_MedievalMaleLite_01.prefab`；若不存在，再回退到 `Assets/Kevin Iglesias/` 下的兼容 Humanoid 角色。
   如果首选角色材质仍是 HDRP / 不受支持 shader，本地预览会自动在 `Assets/_Game/Animations/Characters/CombatTest/LocalPreview/Materials/Player/` 下生成 built-in 兼容材质，避免玩家预览变成粉紫色。
   如果本机存在 `Assets/Free medieval weapons/Prefabs/Sword_DH.prefab`，这一步还会把本地预览武器挂到 imported 右手骨，并自动隐藏 proxy 剑体，只保留前向标记。
5. 如果你要给敌人单独试 imported humanoid Avatar 链，手动执行 `CampusRPG/Setup/Local Preview/Apply Imported Enemy Avatar Chain To CombatTest Enemy Prefabs`。
   这条链会按 skinned mesh 的最低点自动贴地，避免 enemy 预览模型埋进地面；同时攻击状态每次重新进入时都会从头重播 attack clip，防止看起来像没有攻击动画。
6. 这一步会在 `Assets/_Game/Animations/Characters/CombatTest/LocalPreview/` 下生成本地 AnimatorController；该目录只服务 local preview，不应提交。
7. 预览结束后，执行 `CampusRPG/Setup/Repair CombatTest Prefab Wiring`，把 `PF_Player_CombatTest` 和三类敌人 prefab 一起恢复到 proxy baseline，并拆掉 enemy root 上的 `Animator/EnemyCombatAnimationRelay`。
8. 如果 `CombatTest` 场景里的敌人再次提示 `no valid NavMesh`，执行 `CampusRPG/Setup/Repair CombatTest Scene NavMesh`，把当前场景的导航数据重新烘出来。

注意：

- local preview 只是本机工作流，不是正式默认链。
- 敌人 imported Avatar chain 目前仍是显式 local preview 分支，不是正式默认链。
- 如果 local preview 改脏了 `PF_Player_CombatTest.prefab`、`AC_Player_CombatTest.controller`、`_Game/Animations/Characters/CombatTest/*`、`CombatTest.unity`、`Chapter01_Combined.unity`，提交前必须先回到 proxy baseline 再检查 diff。

## 5. 提交规则

- 可以提交：
  - `_Game` 下与 proxy baseline 相关的脚本、测试、文档、材质、prefab、scene、controller、approved animation clips

- 不应直接提交：
  - `Assets/Kevin Iglesias/`
  - `Assets/DoubleL/`
  - `Assets/ithappy/`
  - `Assets/JC_LP_MedievalCharacters_LITE/`
  - 任何由 local preview 直接带进正式输出、并仍然依赖上述目录的 `_Game` 资产
  - `Assets/_Game/Animations/Characters/CombatTest/LocalPreview/` 下生成的 enemy local-preview AnimatorController
  - `Assets/_Game/Animations/Characters/CombatTest/LocalPreview/Materials/Player/` 下生成的本地 player preview 兼容材质
  - 任何只是为了导入素材而顺手改出来、但当前 `_Game` 没实际使用的 `Packages/` / `ProjectSettings/` 变更

## 6. 当前结论

- 当前正式默认基线应恢复为“proxy visuals + proxy / approved `_Game` animations”。
- 第三方素材目录现在只应作为 local preview 候选源存在，不能再被描述成正式默认来源。
