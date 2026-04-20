# 素材来源清单

本清单记录当前项目在本地开发时用过、考虑过、或为 `CombatTest` 预览接入过的第三方素材目录，方便后来人判断：

- 哪些素材只是本地预览用
- 哪些素材现在已经真正接进 `_Game`
- 哪些目录不应该直接提交到公开仓库

## 1. 当前原则

- 当前工程会在 **检测到导入角色/动作素材存在时默认优先使用它们**，但同时保留代理链作为缺素材环境下的自动回退。
- `Assets/_Game/` 里的代理角色、代理材质、代理动作，仍然是当前项目的安全兜底基线。
- `Assets/Kevin Iglesias`、`Assets/DoubleL`、`Assets/ithappy`、`Assets/JC_LP_MedievalCharacters_LITE` 等目录现在既是素材候选源，也是玩家正式显示层与 Humanoid 动作的默认输入源。
- 如果仓库未来继续保持公开，仍要注意不要把无授权的第三方原始资源直接推上去；但在当前工作区里，它们已经被当作正式接线来源使用。

## 2. 当前目录清单

| 本地目录 | 当前用途 | 是否为仓库硬依赖 | 当前状态 |
|---|---|---|---|
| `Assets/Kevin Iglesias/` | 玩家正式显示角色、Avatar、Humanoid 动作主候选源 | 否 | 检测到时默认接线 |
| `Assets/DoubleL/` | 玩家待机 / 走跑 / 格挡 / 多段攻击动作候选源 | 否 | 检测到时默认接线 |
| `Assets/ithappy/` | 玩家走跑 / 闪避 / 受击 / 死亡动作候选源 | 否 | 检测到时默认接线 |
| `Assets/JC_LP_MedievalCharacters_LITE/` | 玩家 Humanoid 角色 prefab / Avatar 后备源 | 否 | 检测到时默认后备 |
| `Assets/Free medieval weapons/` | 武器资源候选 | 否 | 当前未接入 `_Game` |
| `Assets/MYFG-Weapon Pack Lite/` | 武器资源候选 | 否 | 当前未接入 `_Game` |
| `Assets/Polytope Studio/` | 场景 / 美术资源候选 | 否 | 当前未接入 `_Game` |

## 3. 已接入代码的位置

当前下面两条编辑器链路会主动读取这些第三方目录，并在素材存在时作为 **玩家正式显示层与动作来源**：

- [Assets/_Game/Scripts/Editor/CombatImportedPlayerVisualUtility.cs](/Users/don/TY_NEW/Assets/_Game/Scripts/Editor/CombatImportedPlayerVisualUtility.cs:6)
  负责给 `PF_Player_CombatTest` 绑定导入的 Humanoid 角色 prefab / Avatar，并在无素材时回退清理。

- [Assets/_Game/Scripts/Editor/CombatTestAssetGenerator.cs](/Users/don/TY_NEW/Assets/_Game/Scripts/Editor/CombatTestAssetGenerator.cs:1149)
  负责在素材存在且偏好开启时，把第三方 Humanoid 动作复制成 `_Game/Animations/Characters/CombatTest/AN_Player_*` 的正式本地副本。

兜底回退仍然在这里：

- [Assets/_Game/Scripts/Editor/CombatTestSceneBuilder.cs](/Users/don/TY_NEW/Assets/_Game/Scripts/Editor/CombatTestSceneBuilder.cs:25)
  修复 / 重建时会优先接导入角色；若当前机器没有素材，则自动恢复成仓库自包含的代理角色。

- [Assets/_Game/Scripts/Tests/EditMode/CombatTestAnimationAssetWiringTests.cs](/Users/don/TY_NEW/Assets/_Game/Scripts/Tests/EditMode/CombatTestAnimationAssetWiringTests.cs:49)
  现在会按当前素材可用性，检查 `PF_Player_CombatTest` 与玩家动作片段是否正确落到“导入角色/导入动作”或“代理角色/代理动作”其中一条链上。

## 4. 如何切换玩家素材来源

当前默认行为是：只要素材存在，就优先用导入角色和 Humanoid 动作。若你要手动切换，可按下面顺序：

1. 把素材导入到当前工程，并保留上面表格中的目录名。
2. 保持菜单 `CampusRPG/Setup/CombatTest/Prefer Imported Player Sources When Available` 为开启状态。
3. 执行 `CampusRPG/Setup/Repair CombatTest Prefab Wiring`。
4. 若只想立即重绑玩家外观，也可执行 `CampusRPG/Setup/Apply Imported Player Visuals To CombatTest Player Prefab`。
5. 若你想临时退回代理链，先关闭偏好菜单，再重跑 `Repair CombatTest Prefab Wiring`。

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

- 现在工程已经变成“有第三方角色/动作素材时默认正式使用，没有时自动回退代理”的双链路结构。
- 第三方素材不再只是本地预览输入源；在当前工作区里，它们已经是玩家正式显示层与 Humanoid 动作的默认来源。
