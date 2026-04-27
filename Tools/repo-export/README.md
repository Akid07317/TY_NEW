# Repo Export For GPT-5.4 Pro

这个目录下的脚本用于把当前 Git 仓库导出成“适合发给 GPT-5.4 Pro 分析”的代码包。

默认目标：

- 只导出当前 Git 提交中的文件，而不是工作区未提交内容
- 保留代码、文档、配置、工具脚本
- 排除 Unity 二进制/内容资产，例如 `.fbx`、`.png`、`.anim`、`.prefab`、`.meta`
- 默认聚焦项目自有内容，避免把第三方素材包整包塞进模型上下文

## 推荐用法

```bash
python3 Tools/repo-export/export_github_analysis_bundle.py
```

这会基于 `HEAD` 生成：

- 一个 `.zip`
- 一个 `MANIFEST.json`
- 一个 `PROMPT_FOR_GPT54PRO.md`

默认输出目录：

```bash
Exports/github-analysis/
```

## 常用参数

导出指定提交：

```bash
python3 Tools/repo-export/export_github_analysis_bundle.py --revision origin/main
```

把 Unity YAML 文本资产也带上：

```bash
python3 Tools/repo-export/export_github_analysis_bundle.py --include-unity-yaml
```

指定输出目录：

```bash
python3 Tools/repo-export/export_github_analysis_bundle.py --output-dir /tmp/repo-bundle
```

## 默认包含

- 根目录文本配置：例如 `.gitignore`、`.gitattributes`、`AGENT.md`、`TY_NEW.slnx`
- `Docs/`
- `Packages/`
- `ProjectSettings/`
- `Tools/`
- `.vscode/`
- `Assets/_Game/` 下的代码/文本文件，例如：
  - `.cs`
  - `.asmdef`
  - `.asmref`
  - `.json`
  - `.md`
  - `.txt`
  - `.shader`
  - `.compute`
  - `.hlsl`
  - `.cginc`

## 默认排除

- Unity 内容资产和素材：
  - `.fbx`
  - `.png/.jpg/.tga/.psd`
  - `.wav/.mp3/.ogg`
  - `.anim`
  - `.prefab`
  - `.unity`
  - `.mat`
  - `.meta`
- 临时目录和构建目录
- 第三方大素材目录

如果后续你要给模型做“场景接线 / prefab 引用 / ScriptableObject 数据”级别的深分析，再加 `--include-unity-yaml` 会更合适。

## 动画诊断追问包（无视频版）

当问题已经不是“代码结构对不对”，而是“动作看起来为什么别扭、下一步该换武器还是改时序”时，不要继续重传整仓代码包，改用一个小增量包更高效。

适用场景：

- 已经在同一条 ChatGPT 对话里上传过一次完整诊断包
- 后续追问只需要补“更连续的画面证据”和少量关键资源
- 当前模型端不能直接输入视频

推荐内容：

- `8 fps` 连续帧序列，至少覆盖当前最可疑的两招
- 对应的压缩 contact sheet，便于横向比动作语义和出手节奏
- Inspector 等价摘要卡，例如 `SO_Attack_*` 和生成后 `.anim` 的关键时序值
- 本次真正想比较的替代武器 prefab 与 `.meta`
- `prompt.md`
- `notes.md`
- `manifest.json`

这次已验证可用的增量包结构参考：

- `Exports/animation-diagnosis/ty_new_animation_incremental_20260422T110022Z.zip`
- `Light_02/` 和 `Counter/` 的 `8 fps` 连续帧
- `light_02_contact_sheet_8fps.jpg`
- `counter_contact_sheet_8fps.jpg`
- `so_attack_light_02_summary_card.jpg`
- `an_player_light_02_summary_card.jpg`
- `weapon/Sword_OH.prefab`

## 上传方式

推荐分工如下：

- 把 `zip` 作为附件上传给 ChatGPT Pro
- 把 `prompt.md` 的正文直接粘到聊天框里
- `notes.md` 和 `manifest.json` 保留在压缩包里，不要整段贴进聊天框

这样做的原因是：

- 聊天框里的任务要求权重最高，更容易让模型按当前问题作答
- `manifest.json` 适合作为附件里的结构索引，不适合作为聊天正文
- `notes.md` 适合作为上下文补充，而不是新的主指令

如果模型仍然追问更多视觉证据，优先继续补关键帧拼图和摘要卡，不要先扩成整包第三方资产。

## 生成入口

本地已经有一条可复用的 Unity 导出入口：

- 菜单：`CampusRPG/Setup/Local Preview/Export Animation Diagnosis Incremental Package`
- 脚本：`Assets/LocalPreviewTools/AnimationDiagnosisIncrementalCapture.cs`

这条导出链的目标不是做正式资源生产，而是给模型追问准备一个“足够连贯、但体积仍小”的视觉诊断包。
