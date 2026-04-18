# Assets/_Game 目录说明

`Assets/_Game/` 是本项目的正式生产根目录。后续所有业务内容原则上都应进入这里，而不是继续散落在模板默认目录中。

## 子目录职责

- `Art/`：模型、贴图、外部导入资源的正式落点
- `Audio/`：BGM、SFX、Mixer
- `Animations/`：Animator Controller、动画片段、Avatar Mask
- `Materials/`：正式材质球
- `VFX/`：粒子、Shader Graph VFX、命中特效
- `UI/`：Sprite、UI 预制体、字体、界面素材
- `Data/`：ScriptableObject 配置资产
- `Prefabs/`：角色、战斗、交互、环境、UI 预制体
- `Scenes/`：正式场景与测试场景
- `Scripts/`：运行时、编辑器与测试代码

## 使用规则

- 正式场景统一进入 `Scenes/`
- 正式脚本统一进入 `Scripts/`
- 不在 `Assets/` 根目录继续创建业务文件
- 任何新的配置资产优先放进 `Data/`
- Prefab 命名和分类保持稳定，不要临时散放

## 第一版重点目录

第一版优先使用以下目录：

- `Data/Combat`
- `Data/Skills`
- `Data/Enemies`
- `Data/Chapter`
- `Prefabs/Characters`
- `Prefabs/Gameplay`
- `Scenes`
- `Scripts/Runtime`
- `Scripts/Tests`
