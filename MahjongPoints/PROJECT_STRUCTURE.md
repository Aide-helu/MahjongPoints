# MahjongPoints 项目结构

```text
MahjongPoints/
|-- .gitignore
|-- global.json
|-- MahjongPoints.sln
`-- MahjongPoints/
    |-- App.axaml
    |-- App.axaml.cs
    |-- app.manifest
    |-- MahjongPoints.csproj
    |-- Program.cs
    |-- PROJECT_STRUCTURE.md
    |-- ViewLocator.cs
    |-- Assets/
    |   `-- avalonia-logo.ico
    |-- Models/
    |   |-- MahjongHandRecognitionResult.cs
    |   |-- MahjongScoreItem.cs
    |   |-- MahjongScoringResult.cs
    |   `-- RecognizedMahjongTile.cs
    |-- Services/
    |   |-- MahjongHandScoringService.cs
    |   |-- IHandImageRecognizer.cs
    |   |-- IHandScoringService.cs
    |   `-- OnnxHandImageRecognizer.cs
    |-- ViewModels/
    |   |-- MainWindowViewModel.cs
    |   `-- ViewModelBase.cs
    `-- Views/
        |-- MainWindow.axaml
        `-- MainWindow.axaml.cs
```

## 整体架构

本项目是一个 Avalonia 桌面应用，采用简单的 MVVM 风格结构。

当前演示流程：

```text
用户选择图片
-> MainWindow.axaml.cs 接收文件选择结果
-> MainWindowViewModel 加载图片预览
-> IHandImageRecognizer 识别 13 张手牌
-> IHandScoringService 使用识别结果计算分数
-> UI 展示图片、识别出的牌、参与计算的牌以及计分结果
```

当前实现通过 `OnnxHandImageRecognizer` 直接运行项目内的 ONNX 模型。

## 根目录文件

| 文件 | 用途 |
| --- | --- |
| `.gitignore` | 忽略构建产物、IDE 文件、日志、包文件以及其他本地或生成文件。 |
| `global.json` | 固定当前解决方案使用的 .NET SDK 版本。 |
| `MahjongPoints.sln` | Visual Studio/Rider 解决方案文件。 |

## 应用项目

| 文件 | 用途 |
| --- | --- |
| `MahjongPoints.csproj` | 主项目配置文件，包含目标框架、Avalonia 包、MVVM 包和资源配置。 |
| `Program.cs` | 应用入口，负责配置并启动 Avalonia。 |
| `App.axaml` | 应用级 Avalonia XAML，包含样式和数据模板。 |
| `App.axaml.cs` | 创建主窗口，并把 `MainWindowViewModel` 设置为 `DataContext`。 |
| `app.manifest` | Windows 应用清单文件。 |
| `ViewLocator.cs` | Avalonia 模板辅助类，用于根据 ViewModel 解析对应 View。 |
| `PROJECT_STRUCTURE.md` | 项目结构和架构说明文档。 |

## Views

| 文件 | 用途 |
| --- | --- |
| `Views/MainWindow.axaml` | 主界面，包含图片选择按钮、图片预览、识别出的 13 张牌、参与计算的 14 张牌和计分详情。 |
| `Views/MainWindow.axaml.cs` | 只作为 UI 事件桥接层，负责打开文件选择器并调用 ViewModel。 |

View 层应该保持轻量。业务逻辑应放在 `ViewModels/` 或 `Services/` 中。

## ViewModels

| 文件 | 用途 |
| --- | --- |
| `ViewModels/ViewModelBase.cs` | ViewModel 基类，目前继承自 `ObservableObject`。 |
| `ViewModels/MainWindowViewModel.cs` | 保存 UI 状态，加载用户选择的图片，调用识别和计分服务，并暴露可绑定结果。 |

`MainWindowViewModel` 是当前演示流程的协调者。

## Models

| 文件 | 用途 |
| --- | --- |
| `Models/RecognizedMahjongTile.cs` | 表示一张识别出的麻将牌，包含编码、显示名称和置信度。 |
| `Models/MahjongHandRecognitionResult.cs` | 表示图片识别输出，包含识别出的牌、模型名称、推理模式和提示信息。 |
| `Models/MahjongScoreItem.cs` | 表示一条计分、役种或番符明细。 |
| `Models/MahjongScoringResult.cs` | 表示最终计分结果，包含参与计算的 14 张牌、和牌牌、分数摘要和明细。 |

Models 是纯数据结构，不应该依赖 Avalonia UI 类型。

## Services

| 文件 | 用途 |
| --- | --- |
| `Services/IHandImageRecognizer.cs` | 图片识别接口。 |
| `Services/OnnxHandImageRecognizer.cs` | 使用项目内 ONNX 模型识别手牌。 |
| `Services/IHandScoringService.cs` | 麻将计分接口。 |
| `Services/MahjongHandScoringService.cs` | 麻将计分服务，串联拆牌、判役、算符和算点。 |

服务接口让应用中的具体实现可以替换：

```text
OnnxHandImageRecognizer -> IHandImageRecognizer
MahjongHandScoringService -> IHandScoringService
```

## Assets

| 文件 | 用途 |
| --- | --- |
| `Assets/avalonia-logo.ico` | Avalonia 模板自带的窗口和应用图标。 |
