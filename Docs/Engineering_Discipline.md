# 工程纪律

本页用于把日常开发中的改动分层看清楚，避免 Unity 自动生成内容、local preview 素材和真实代码改动混在一起。

## 1. 改动分层

每次准备提交、打包、发给外部模型分析或做发布判断前，先把工作区拆成下面几类：

| 类别 | 例子 | 默认处理 |
|---|---|---|
| Runtime 代码 | `Assets/_Game/Scripts/Runtime/**/*.cs` | 必须有对应测试或运行验证 |
| Editor 工具 | `Assets/_Game/Scripts/Editor/*.cs`、`Tools/unity-cli/*` | 必须说明入口、只读/会写哪些资产 |
| 测试代码 | `Assets/_Game/Scripts/Tests/**/*.cs` | 应和被保护的行为一起提交 |
| 文档 | `Docs/*.md`、`AGENT.md`、工具 README | 跟随规则、工作流、范围变化同步 |
| Unity 基线资产 | `_Game` 下正式 scene/prefab/controller/approved clip/SO | 只在明确改变基线时提交 |
| Local preview | GhostSamurai / imported player / imported enemy / `LocalPreview/` | 默认不作为发布基线提交 |
| 自动生成噪音 | `*.anim` 大量曲线 YAML、`__pycache__`、临时导出 | 先隔离，再判断是否需要清理或忽略 |

## 2. 每轮开始

推荐顺序：

```bash
git status --short
Tools/unity-cli/ty-new-diff-audit
```

这一步只读，不会修改文件。它会给出：

- 当前代码、测试、文档、Unity YAML 的行数分布
- 最大的 Unity 序列化资产 diff
- 未跟踪代码、文档、Unity/meta、临时文件数量
- local-preview-only 素材目录是否存在、是否被 Git 跟踪
- 当前最应该跑的验证门

## 3. 提交前硬规则

- 不把 `__pycache__`、`.pyc`、临时导出、构建产物放进提交。
- 不把第三方 raw asset 目录当成正式默认依赖提交；边界以 `Docs/Asset_Source_List.md` 为准。
- 不把大量 `.anim` 曲线 YAML 和代码一起顺手提交。只有当本轮明确是 approved animation clip promotion，才把这类文件纳入提交说明。
- 不把 local-preview 脏态当作 release-safe baseline。预览结束后先执行对应 repair / baseline check。
- `ProjectSettings/`、`Packages/` 的变更必须能说清楚原因；导入素材顺手产生的改动默认不进提交。
- Runtime 行为改动必须跑相关测试；没有测试时至少记录手动验证路径。

## 4. 常用验证入口

```bash
# 单轮 diff 体检
Tools/unity-cli/ty-new-diff-audit

# 只看 GhostSamurai / imported local-preview 研究线是否自洽
Tools/unity-cli/ty-new-ghostsamurai-preview-check --startup-timeout 90

# 证明 local-preview 脏态可以回到 public-safe proxy baseline
Tools/unity-cli/ty-new-ghostsamurai-baseline-check --startup-timeout 90

# 第一版发布候选最终门
Tools/unity-cli/ty-new-final-gate --startup-timeout 45
```

如果主工程正被 Unity GUI 打开，优先让这些命令使用临时克隆或现有封装脚本，不要裸跑无超时的 Unity batchmode。

## 5. 评审/提交切片

提交说明建议按切片写，不按 Git 噪音写：

- `runtime:` 玩家状态机、战斗判定、敌人 AI、HUD 行为。
- `editor:` 生成器、修复菜单、预览驱动器、批处理入口。
- `tests:` 回归用例、发布守门、场景接线断言。
- `docs:` 工作流、素材边界、路线图、验证说明。
- `assets:` 明确被批准进入 `_Game` 基线的 prefab / scene / controller / clip / ScriptableObject。

当一个切片里同时包含 `runtime + editor + tests + docs` 是正常的；当同一个提交里还混入几百万行 `.anim` 或本地素材目录时，先停下来重新分层。

## 6. 大脏树处理

如果工作区已经混入大量场景、Prefab、动画和外部素材，不要急着 stash、reset 或清理。先做非破坏性判断：

```bash
Tools/unity-cli/ty-new-diff-audit --top 20
```

然后按目的选择：

- 要继续开发：只改当前切片需要的代码/测试/文档。
- 要发布验证：先回到 public-safe baseline，再跑 baseline / final gate。
- 要保存现场：做仓库外 checkpoint，再清理临时内容。
- 要发给外部模型：优先用 `Tools/repo-export/` 从 Git revision 导出，不把本地脏态混进去。
