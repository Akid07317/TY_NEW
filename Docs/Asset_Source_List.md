# 素材来源清单

本清单记录当前项目在本地开发时用过、考虑过、或为 `CombatTest` 预览接入过的第三方素材目录，方便后来人判断：

- 哪些素材只是本地预览用
- 哪些素材现在已经真正接进 `_Game`
- 哪些目录不应该直接提交到公开仓库

## 1. 当前原则

- 当前公开仓库基线必须能在 **不安装任何第三方素材包** 的情况下正常打开、生成 `CombatTest`、并通过核心回归测试。
- `Assets/_Game/` 里的代理角色、代理材质、代理动作，是当前仓库的正式可提交基线。
- `Assets/Kevin Iglesias`、`Assets/DoubleL`、`Assets/ithappy`、`Assets/JC_LP_MedievalCharacters_LITE` 等目录只作为 **本地预览候选素材源**，不作为公开仓库硬依赖。
- 如果仓库未来继续保持公开，不要把 Unity Asset Store 的原始资源目录直接推上去；优先提交 `_Game` 下的自制代理结果、配置和文档。

## 2. 当前目录清单

| 本地目录 | 当前用途 | 是否为仓库硬依赖 | 当前状态 |
|---|---|---|---|
| `Assets/Kevin Iglesias/` | 玩家本地预览模型、Avatar、Humanoid 动作候选源 | 否 | `CombatTest` 本地预览可选 |
| `Assets/DoubleL/` | 玩家待机 / 走跑 / 格挡 / 多段攻击动作候选源 | 否 | `CombatTest` 本地预览可选 |
| `Assets/ithappy/` | 玩家走跑 / 闪避 / 受击 / 死亡动作候选源 | 否 | `CombatTest` 本地预览可选 |
| `Assets/JC_LP_MedievalCharacters_LITE/` | 玩家 Humanoid 角色 prefab / Avatar 后备源 | 否 | `CombatTest` 本地预览后备 |
| `Assets/Free medieval weapons/` | 武器资源候选 | 否 | 当前未接入 `_Game` |
| `Assets/MYFG-Weapon Pack Lite/` | 武器资源候选 | 否 | 当前未接入 `_Game` |
| `Assets/Polytope Studio/` | 场景 / 美术资源候选 | 否 | 当前未接入 `_Game` |

## 3. 已接入代码的位置

当前只有下面两条编辑器链路会主动读取这些第三方目录，但它们都只服务于 **本地预览**：

- [Assets/_Game/Scripts/Editor/CombatImportedPlayerVisualUtility.cs](/Users/don/TY_NEW/Assets/_Game/Scripts/Editor/CombatImportedPlayerVisualUtility.cs:6)
  负责在你本地手动执行菜单时，把导入的 Humanoid 角色 prefab 套到 `PF_Player_CombatTest` 上。

- [Assets/_Game/Scripts/Editor/CombatTestAssetGenerator.cs](/Users/don/TY_NEW/Assets/_Game/Scripts/Editor/CombatTestAssetGenerator.cs:1149)
  负责在你本地打开预览开关后，把第三方 Humanoid 动作复制成 `_Game/Animations/Characters/CombatTest/AN_Player_*` 的本地副本。

仓库正式基线的防线在这里：

- [Assets/_Game/Scripts/Editor/CombatTestSceneBuilder.cs](/Users/don/TY_NEW/Assets/_Game/Scripts/Editor/CombatTestSceneBuilder.cs:25)
  默认修复 / 重建时会清掉 `ImportedVisualRoot`，恢复成仓库自包含的代理角色。

- [Assets/_Game/Scripts/Tests/EditMode/CombatTestAnimationAssetWiringTests.cs](/Users/don/TY_NEW/Assets/_Game/Scripts/Tests/EditMode/CombatTestAnimationAssetWiringTests.cs:49)
  现在会检查 `PF_Player_CombatTest` 不依赖 `Kevin Iglesias` / `JC_LP_MedievalCharacters_LITE`，并检查玩家动作片段包含 `CombatProxyVisualRoot/*` 的代理曲线。

## 4. 如何在本地复现“导入素材预览”

如果你只是想在自己机器上看更接近正式角色的手感，按下面顺序即可：

1. 把本地素材导入到当前工程，并保留上面表格中的目录名。
2. 若想临时把导入角色外观套到玩家 prefab 上，执行菜单：
   `CampusRPG/Setup/Apply Imported Player Visuals To CombatTest Player Prefab (Local Preview)`
3. 若想临时让 `AN_Player_*` 从第三方 Humanoid 动作源重建，先勾上菜单：
   `CampusRPG/Setup/CombatTest/Use Imported Player Sources For Local Preview`
4. 然后再执行：
   `CampusRPG/Setup/Repair CombatTest Prefab Wiring`

## 5. 提交规则

- 可以提交：
  - `Assets/_Game/Animations/Characters/CombatTest/`
  - `Assets/_Game/Materials/M_CombatProxy_*`
  - `Assets/_Game/Prefabs/Characters/PF_*_CombatTest.prefab`
  - `_Game` 下与这些预览流程相关的编辑器脚本、测试、文档

- 不建议直接提交：
  - `Assets/Kevin Iglesias/`
  - `Assets/DoubleL/`
  - `Assets/ithappy/`
  - `Assets/JC_LP_MedievalCharacters_LITE/`
  - 任何只是为了导入素材而顺手改出来、但当前 `_Game` 没实际使用的 `Packages/` / `ProjectSettings/` 变更

## 6. 当前结论

- 现在仓库已经恢复到“无第三方原始素材也能工作”的安全基线。
- 第三方素材仍然有价值，但它们的定位是 **本地预览输入源**，不是公开仓库必须携带的正式内容。
