# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

DeskCat 是一款 Windows 桌面宠物程序，使用 C# + WPF + .NET 9 开发。一只橘色像素风小猫常驻桌面，会自主行走、睡觉、玩耍，用户可以与它互动（拖拽、喂食、抚摸）。

## 常用命令

```bash
# 编译项目
dotnet build DeskCat.sln --configuration Debug

# 运行程序
dotnet run --project src/DeskCat/DeskCat.csproj

# 或直接运行编译后的 exe
src/DeskCat/bin/Debug/net9.0-windows/DeskCat.exe

# 发布单文件版本（无需安装 .NET 运行时）
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true

# 重新生成精灵图资源（如需修改动画）
python tools/generate_sprites_v2.py
```

## 架构概览

```
PetViewModel (协调者)
    ├── StateMachine (状态机，驱动宠物行为)
    ├── AnimationService (动画播放，精灵图帧切换)
    ├── AttributeService (属性系统：饱食度/心情/精力)
    ├── MovementService (行走移动)
    └── TrayService (系统托盘)
```

**核心数据流**：
- `DispatcherTimer` 每 33ms 调用 `PetViewModel.Update()` 驱动状态机
- 状态变化触发 `AnimationService.Play(state)` 播放对应动画
- 精灵图（PNG Sprite Sheet）通过 `CroppedBitmap` 裁剪显示当前帧

**状态机**：
- 状态：Idle, Walk, Sit, Sleep, Eat, Happy, Angry, Curious, Drag, Fall
- 状态转换由时间触发（随机超时）或用户交互触发（点击/拖拽）

## 精灵图资源

位于 `src/DeskCat/Resources/Animations/`，每个 PNG 是横向排列的精灵图条：
- 单帧尺寸：128×128 像素
- 动画配置：`Models/AnimationConfig.cs`

如需修改动画，编辑 `tools/generate_sprites_v2.py` 后重新运行。

## 注意事项

- 单实例检测使用 Mutex，在 `App.xaml.cs` 的 `OnStartup` 中实现
- 窗口透明需要 `AllowsTransparency="True"` + `WindowStyle="None"`
- 宠物尺寸由 `PetViewModel.PetSize` 控制（当前 256）
