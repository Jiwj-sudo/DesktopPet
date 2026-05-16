# AGENTS.md

本文件为后续 AI Agent / Codex / Claude Code 在本项目中工作的上下文说明。进入项目后请先阅读本文件，再进行代码修改。

## 项目简介

DeskCat 是一个 Windows 桌面宠物程序，目标是一只常驻桌面的橘色像素风小猫。它通过透明置顶 WPF 窗口显示在桌面上，支持自主状态切换、行走、睡觉、拖拽、抚摸、喂食、托盘菜单和属性持久化。

核心技术栈：

- C# / WPF
- .NET 9 Windows 桌面应用
- Windows Forms `NotifyIcon` 用于系统托盘
- PNG sprite sheet 用于帧动画
- 本地 JSON 文件保存宠物属性
- VS2022 / MSBuild 构建

目标平台：

- Windows 10 / Windows 11

## 架构

当前项目结构：

```text
.
├── DeskCat.sln
├── build-vs2022.cmd
├── 项目方案.md
├── AGENTS.md
├── tools/
│   ├── generate_sprites.py
│   ├── generate_sprites_v2.py
│   ├── generate_icon.py
│   └── preview_sprites.py
└── src/
    └── DeskCat/
        ├── DeskCat.csproj
        ├── App.xaml / App.xaml.cs
        ├── MainWindow.xaml / MainWindow.xaml.cs
        ├── app.manifest
        ├── Config/
        │   └── appsettings.json
        ├── Controls/
        │   └── PetControl.xaml / PetControl.xaml.cs
        ├── Converters/
        │   └── StateToAnimationConverter.cs
        ├── Models/
        │   ├── AnimationConfig.cs
        │   ├── PetAttributes.cs
        │   └── PetState.cs
        ├── Services/
        │   ├── AnimationService.cs
        │   ├── AttributeService.cs
        │   ├── MovementService.cs
        │   ├── StartupService.cs
        │   ├── StateMachine.cs
        │   └── TrayService.cs
        ├── ViewModels/
        │   ├── BindableBase.cs
        │   └── PetViewModel.cs
        └── Resources/
            ├── cat_icon.ico
            └── Animations/
                ├── idle.png
                ├── walk.png
                ├── sit.png
                ├── sleep.png
                ├── eat.png
                ├── happy.png
                ├── angry.png
                ├── curious.png
                ├── drag.png
                ├── fall.png
                └── pet.png
```

主要模块职责：

- `App.xaml.cs`：应用入口、单实例互斥、退出清理。
- `MainWindow.xaml(.cs)`：透明无边框主窗口、鼠标交互、右键菜单、托盘服务接线。
- `Controls/PetControl.xaml`：宠物图片渲染控件，绑定当前动画帧和朝向。
- `ViewModels/PetViewModel.cs`：宠物核心协调者，串联状态机、动画、移动、属性、交互命令。
- `Services/AnimationService.cs`：加载 sprite sheet，通过 `CroppedBitmap` 裁剪并播放帧动画。
- `Services/StateMachine.cs`：保存当前状态、进入时间和状态变更事件。
- `Services/MovementService.cs`：行走方向、速度、屏幕边界和落地位置计算。
- `Services/AttributeService.cs`：饱食度、心情、精力的加载、衰减和保存。
- `Services/TrayService.cs`：系统托盘图标和托盘右键菜单。
- `Services/StartupService.cs`：通过当前用户 Run 注册表项控制开机自启。
- `Models/*`：宠物状态、属性和动画配置。
- `tools/*`：素材生成/预览脚本，通常不要在功能开发中改动，除非任务明确涉及素材。

动画资源约定：

- 资源位于 `src/DeskCat/Resources/Animations/`。
- 每个动画是横向排列的 PNG sprite sheet。
- 当前帧尺寸为 `128x128`，显示尺寸由 `PetViewModel.PetSize` 控制。
- 动画帧数和 FPS 在 `AnimationConfig.Defaults` 中维护。

## 开发命令

推荐使用 VS2022 或项目脚本构建。

使用项目脚本：

```cmd
build-vs2022.cmd
```

在 VS2022 Developer Command Prompt 中直接调用 MSBuild：

```cmd
MSBuild.exe DeskCat.sln /m /restore /p:Configuration=Debug
```

如果本机 `dotnet` SDK 可用，也可尝试：

```cmd
dotnet build DeskCat.sln
dotnet run --project src\DeskCat\DeskCat.csproj
```

发布单文件示例：

```cmd
dotnet publish src\DeskCat\DeskCat.csproj -c Release -r win-x64 /p:PublishSingleFile=true
```

素材脚本示例，仅在明确需要重新生成资源时使用：

```cmd
python tools\generate_sprites_v2.py
python tools\preview_sprites.py
python tools\generate_icon.py
```

注意：Codex 沙箱环境可能无法直接运行 VS/MSBuild/dotnet。若权限审核超时，不要误判为项目编译失败；请让用户在本机终端或 VS2022 中运行上述命令，并根据真实编译输出继续处理。

## 代码规范

- 保持 WPF MVVM 风格：窗口层处理 UI 事件和菜单接线，宠物行为逻辑放在 `PetViewModel` 和 `Services`。
- 不要把复杂业务逻辑写进 XAML code-behind，除非它确实是窗口输入、菜单或生命周期逻辑。
- `PetViewModel` 是协调者，不应直接承担大量图像处理、注册表、文件 IO 细节；这些应留在服务类中。
- 所有状态名使用 `PetState` 枚举，不要用裸字符串表示状态。
- 动画新增或修改时，同步更新：
  - `Resources/Animations/*.png`
  - `AnimationConfig.Defaults`
  - 必要时更新 `PetState`
- 属性值必须保持在 `0..100`，修改后调用 `PetAttributes.Clamp()`。
- 文件保存必须容错，不能因为存档文件损坏、权限问题或临时 IO 问题导致桌宠启动/运行失败。
- WPF 图像资源优先使用 pack URI 或项目 Resource，不要依赖运行目录下的相对路径。
- 托盘图标优先使用 `Resources/cat_icon.ico`，避免手写 `GetHicon()` 后忘记释放原生句柄。
- 鼠标交互要区分单击、双击、拖拽，避免一个动作触发多个互相冲突的状态。
- 行走和落地逻辑需要考虑多显示器，不要只假设主屏幕。
- 保持中文 UI 文案简洁，不在桌宠窗口内添加解释性文本。
- 新增代码请开启 nullable 友好写法，避免引入明显空引用风险。
- 只添加必要注释，优先让方法名和类型职责表达意图。

## Agent 工作约束

- 修改前先阅读 `项目方案.md`、本文件和相关源码，不要凭空重构。
- 默认不要改动或删除用户已有文件，尤其不要清理用户未要求清理的素材、预览图和脚本。
- 不要编辑 `src/**/bin/`、`src/**/obj/`、`*.lscache`、`*_wpftmp.*` 等构建产物。
- 不要把 `bin/`、`obj/` 的内容当作源码依据；这些文件可由构建重新生成。
- 不要随意重新生成 sprite PNG。素材脚本可能覆盖现有动画，只有在任务明确要求更新素材时才运行。
- 不要把运行时状态、用户本地存档或机器特定路径写进源码。
- 不要引入大型第三方依赖来解决小型 WPF 逻辑问题。
- 不要将 WPF 主窗口改为普通有边框窗口；桌宠窗口应保持透明、无边框、不显示任务栏。
- 不要移除系统托盘，关闭主窗口时应默认隐藏到托盘而非直接退出。
- 不要破坏单实例逻辑，避免用户启动多个桌宠窗口。
- 涉及开机自启时，只操作当前用户的 Run 注册表项，不要请求管理员权限。
- 修改动画、状态机、保存逻辑后，应尽量构建验证；如果当前 agent 环境无法编译，必须明确说明命令未实际执行，并让用户提供真实编译输出。
- 如果发现现有实现与方案冲突，优先做小范围修复，并在回复中说明行为变化。
- 如果需要大幅重构，请先说明动机和影响范围，避免一次性改动 UI、状态机、资源和构建配置。
