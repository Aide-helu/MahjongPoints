using Avalonia;
using System;

namespace MahjongPoints;

/// <summary>
/// 应用程序入口类，负责启动 Avalonia 桌面应用。
/// </summary>
sealed class Program
{
    /// <summary>
    /// 应用程序主入口，创建 Avalonia 应用并启动经典桌面生命周期。
    /// </summary>
    /// <param name="args">命令行启动参数。</param>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// 创建 Avalonia 应用构建器，同时供运行时和设计器使用。
    /// </summary>
    /// <returns>配置完成的 Avalonia 应用构建器。</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
