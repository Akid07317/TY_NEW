# 用户自有美术候选档位

## 目标

让内部人工试玩包真正使用人物模型、Humanoid Avatar、武器和动作，同时保持公开仓库的 proxy baseline 可重复、可恢复。

## 当前档位

`ReleaseCandidateArtProfile.UserOwnedGhostSamurai` 只接受下列 GhostSamurai 同源输入：

- 玩家人物与 Avatar：`Assets/GhostSamurai_Animset/Model/Model_Unity_Ver1.FBX`
- 玩家武器：`Assets/GhostSamurai_Animset/Model/Weapon/SM_Katana01.FBX`
- `EnemyMelee` / `EnemyMobile` 人物与 Avatar：同一个已授权 `Model_Unity_Ver1.FBX`
- `EnemyMelee` / `EnemyMobile` 武器：同一个已授权 `SM_Katana01.FBX`，绑定模型自带 `Weapon_r`
- `EnemyRanged` 人物、Avatar 与弓：`Assets/GhostSamurai_Animset/Model/WM_Master_Unity_Bow2.FBX`，使用模型内置 `SK_Bow_02`
- `EnemyRanged` 箭：`Assets/GhostSamurai_Animset/Model/Weapon/SM_Arrow_01.FBX`，绑定模型自带 `arrow`
- 动作：只允许 `Assets/GhostSamurai_Animset/Animation/` 下的 Humanoid clip；严格档位拒绝其他素材根、`__preview__` 临时片段和 proxy 曲线回退

构建时会先把玩家及 melee / mobile / ranged 三类敌人 Prefab 切到 imported Humanoid 链，绑定各自真实武器并重建 imported 动作，再执行与 RC 相同的场景顺序、项目身份和 Built-in Render Pipeline 检查。该过程只在临时克隆中运行；预检或构建结束后恢复主树接线，公开发布基线始终保持 public-safe proxy，不依赖 `GhostSamurai_Animset`。

三类敌人的严格映射如下。`EnemyMelee` 与 `EnemyMobile` 目前共享同一套已授权武士模型与 Katana，通过动作选择和角色调色板区分职责；如果以后要让 mobile 使用独立人物或长柄武器，必须先补充新的授权素材，不能用 primitive 标记冒充真实武器。

| 敌人 | 严格人物 / Avatar | 严格武器 | 动作职责 |
|---|---|---|---|
| `EnemyMelee` | `Model_Unity_Ver1.FBX` | `SM_Katana01.FBX` → `Weapon_r` | 近战攻击、受击、死亡与移动 |
| `EnemyMobile` | `Model_Unity_Ver1.FBX` | `SM_Katana01.FBX` → `Weapon_r` | 机动攻击、受击、死亡与三段移动 |
| `EnemyRanged` | `WM_Master_Unity_Bow2.FBX` | 内置 `SK_Bow_02` + `SM_Arrow_01.FBX` → `arrow` | 远程攻击、Anti-Air、Chase Roll、Guard Break、受击、死亡与移动 |

## 材质事实与内部调色板

`GhostSamurai_Animset` 是动画样机包。当前本地目录没有角色、Katana、Bow 或 Arrow 的 PNG / TGA / JPG 贴图；FBX 和旁置 `.mat` 都只是灰、深灰、红眼或灰阶纯色材质。因此“人物或武器发灰”不是 Renderer、Avatar 或 Shader 丢失，也没有隐藏贴图可恢复。

`UserOwnedGhostSamurai` 档位会在临时克隆中按 FBX 材质槽生成 7 个确定性的 Built-in `Standard` 色块材质：主甲、深色布料、红眼、刀柄、刀刃、刀镡和刀锋。它只提高部件辨识度，不伪装成有贴图的正式角色。如果后续要求真实服装纹理，需要另行提供授权明确、包含 Albedo / Normal 等贴图的 Humanoid 人物资产，GhostSamurai 继续只负责动作。

敌人会生成独立的严格角色调色板：melee 为红系、mobile 为青系、ranged 为金棕系。门禁按实际 FBX 槽位检查身体、Katana、Bow 和 Arrow 的每个 Renderer 材质，要求全部来自 `Assets/_Game/Animations/Characters/CombatTest/LocalPreview/Materials/Enemy/UserOwnedGhostSamurai/`、使用 Built-in `Standard`，并拒绝空材质、原始 FBX 灰模材质或其他素材根的静默回退。

## 技术预检

先运行不出包的隔离预检：

```bash
Tools/unity-cli/ty-new-build-release art-validate --wall-timeout 600
```

当前 macOS / Unity `6000.4.2f1` 环境要求主工程 Unity Editor 已保存并关闭后再运行：同项目 Editor 打开时，第二个 Hub GUI Editor 会在 Domain Reload 休眠，标准 batchmode 则可能触发 Licensing Client `ObjectDisposedException`。临时克隆能隔离文件写入，但不能绕过这一授权进程冲突。

预检会应用后再恢复 proxy baseline，并拒绝以下情况：

- 玩家模型、Humanoid Avatar、Katana 或 7 个语义材质不完整
- 玩家 Katana 未直接使用模型自带 `Weapon_r` 插槽，或挂点 / 刀模型的局部位置、旋转、缩放不是 `zero / identity / one`
- 玩家 `Locomotion` 不是 13 个独立 Idle / Walk / Run motion，或 Block、Dodge、CombatRoll、AirDodge、Hit、GuardBreak、Death、Light 三连、Heavy 缺 state / motion
- 任一敌人没有正确的 GhostSamurai 人物、有效 Humanoid Avatar、Animator Controller、运行时动画 relay，或仍显示 proxy Renderer
- melee / mobile 的 Katana 没有以局部 identity 绑定 `Weapon_r`；ranged 缺少 Bow2 内置 `SK_Bow_02`，或 Arrow 没有以局部 identity 绑定 `arrow`
- 任一敌人身体 / Katana / Bow / Arrow 的材质为空、Shader 不受 Built-in 支持，或没有进入该敌人专属严格调色板
- 任一敌人缺少三段不同 locomotion motion、Hit、Death 及其 archetype 攻击；ranged 还必须具备 Anti-Air、Chase Roll 和 Guard Break attack motion
- 任一核心 motion 不是 GhostSamurai Humanoid clip、名称以 `__preview__` 开头，或仍包含 `CombatProxyVisualRoot` 占位曲线

该预检只证明技术接线和资源完整性，不证明动作自然、过渡舒服或战斗手感通过。

## 构建

```bash
Tools/unity-cli/ty-new-build-release art-mac --wall-timeout 1800
Tools/unity-cli/ty-new-build-release art-windows --wall-timeout 1800
```

如果普通 batchmode 无法初始化授权，使用项目已验证的 Hub GUI 授权链：

```bash
Tools/unity-cli/ty-new-build-release art-mac --editor-mode --hub-licensing --licensing-ipc LicenseClient-don --wall-timeout 1800
```

输出：

- `Builds/ReleaseCandidate/UserOwnedArt/Mac/TY_NEW.app`
- `Builds/ReleaseCandidate/UserOwnedArt/Windows/TY_NEW.exe`

### 2026-07-11 已验证的玩家证据

- `art-validate` 已在主 Unity Editor 关闭后通过，日志明确确认模型、Humanoid Avatar、7 个调色板材质、Katana、Animator Controller 与核心动作图技术完整；对应 EditMode 窄门为 `15/15 Passed`。
- `art-mac` 已成功生成 `180M` universal macOS app；`codesign --verify --deep --strict` 通过，Player 数据可检出全部 7 个 `GhostSamurai_*` 材质。
- 成品已启动并进入 `Campus Chapter 01`，运行画面可见 imported 人形与 Katana；`~/Library/Logs/TY_NEW Team/TY_NEW/Player.log` 未检出 error / exception。
- 项目所有者反馈持刀位置不对后，已改为复用 GhostSamurai 原包示例的 `Weapon_r` socket：刀根与 FBX 实例均保持局部 identity；修正版重新通过 `15/15`、`art-validate`、`art-mac` 与实机静止姿态检查。
- 这组证据只关闭自动化技术门；下方动作自然度与手感表仍由项目所有者填写。

三类敌人的修正版 fresh 定向 EditMode 窄门已达到 `27/27 Passed`（`/tmp/TY_NEW_enemy_art_gate_results_fixed.xml`）。完整 `art-validate` 也已通过（`/tmp/TY_NEW_enemy_art_validate_fixed2.log`），它会在最终 prefab 形态核对各自 FBX 的 Humanoid Avatar、独立 controller、真实 Katana / Bow / Arrow prefab 来源、全槽位严格材质和动作图。随后 `art-mac` 实体构建成功，产出 `256M` universal macOS app，strict adhoc codesign 通过；Player 数据可检出三类敌人的 `ImportedEnemyVisualRoot`、角色调色板、`SM_Katana01`、`SK_Bow_02` 与 `SM_Arrow_01`。新包已启动到 `Campus Chapter 01` 主菜单，fresh Player.log 未见 error / exception；主树三敌 prefab 仍保持 public-safe `CombatProxyVisualRoot`，没有保存 imported 根。

## 验收分工

- 项目所有者负责动作主观验收：自然度、脚滑、手刀对位、起收势、过渡、读招和总体手感。
- 自动化负责模型 / Avatar / 武器 / 材质、Animator state 与 motion 完整性、代理曲线排除、构建、日志和平台边界。
- 自动化不得把“技术可触发”写成“动作手感通过”；只有项目所有者能填写动作验收结论。

## 项目所有者动作验收表

启动 `Builds/ReleaseCandidate/UserOwnedArt/Mac/TY_NEW.app` 后，用 `WASD`、`LMB`、`RMB`、`Left Ctrl`、`Left Shift` 与 `Tab` 逐项检查。结论只填 `PASS` 或 `NEEDS_TUNE`，必要时记录发生场景和观感。

| 动作项 | 操作 | 所有者结论 | 备注 |
|---|---|---|---|
| Idle | 静止 3 秒，锁定 / 非锁定各看一次 | 待验收 | |
| 移动与锁定步态 | `WASD`，再 `Tab` 锁定后走前后左右及斜向 | 待验收 | |
| 轻击三连 | `LMB` 按尾段节奏连续三次；空挥 / 命中各一次 | 待验收 | |
| 重击 | `RMB`；空挥 / 命中各一次 | 待验收 | |
| 格挡与破防 | 按住 `Left Ctrl` 接普通攻击，再尝试硬挡 Boss 慢重击 | 待验收 | |
| Dodge / CombatRoll / AirDodge | 锁定、非锁定移动、空中分别按 `Left Shift` | 待验收 | |
| 普通受击 | 不格挡承受普通攻击 | 待验收 | |
| 死亡 | 让 HP 归零，观察倒地和状态结束 | 待验收 | |

### 敌人动作主观验收表

自动化只保证下面动作绑定到真实 GhostSamurai motion，并且模型、武器和材质在技术上完整。项目所有者仍需在实际战斗画面里确认读招、武器对位、起收势、脚滑和三类敌人的辨识度。

| 敌人动作项 | 观察重点 | 所有者结论 | 备注 |
|---|---|---|---|
| `EnemyMelee` 近战攻击 | 红系武士与 Katana 同屏可见；挥刀方向、手刀对位、命中时序和回收姿态 | 待验收 | |
| `EnemyMobile` 机动攻击 | 青系武士与 Katana 同屏可见；移动攻击、转向、脚滑和与 melee 的辨识度 | 待验收 | |
| `EnemyRanged` 基础远程攻击 | 金棕系 Bow2、弓和 Arrow 同屏可见；搭箭、拉弓、放箭与命中提示一致 | 待验收 | |
| `EnemyRanged` Anti-Air / Chase Roll / Guard Break | 三个专属 attack state 身体语言不同；滚动、弓箭对位和起收势自然 | 待验收 | |
| 三类敌人 Hit / Death / Locomotion | 受击与死亡不复用 proxy，三段移动可辨认且不明显脚滑 | 待验收 | |

## 授权边界

- 项目所有者在本阶段明确将其称为“我的人物和动作资产”。
- 本机 `GhostSamurai_Animset` 的目录元数据包含 Unity Store 来源标记。
- 当前目录未发现独立 EULA / License 文件，因此这些证据只足以建立“用户自有内部候选”工作流，不自动授权公开仓库提交、素材转售或对外分发。
- 对外发布前仍需由项目所有者确认购买记录和完整许可条款；确认后再决定是否制作 self-contained `_Game` 净化输出。
