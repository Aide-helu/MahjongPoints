using Avalonia;
using System;
using System.IO;
using System.Linq;
using System.Text;
using MahjongPoints.Services;

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
    public static void Main(string[] args)
    {
        ConfigureConsoleOutputEncoding();
        if (TryRunRecognitionCommand(args))
        {
            return;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// 配置控制台输出编码，避免 Rider 或终端中中文日志乱码。
    /// </summary>
    private static void ConfigureConsoleOutputEncoding()
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // 某些无控制台启动方式下无法设置输出编码，忽略后继续启动界面。
        }
    }

    private static bool TryRunRecognitionCommand(string[] args)
    {
        if (args.Length != 2 || !string.Equals(args[0], "--recognize", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        using var recognizer = new OnnxHandImageRecognizer();
        var result = recognizer.RecognizeAsync(args[1]).GetAwaiter().GetResult();
        Console.WriteLine(string.Join(" ", result.Tiles.Select(tile => tile.Code)));
        foreach (var tile in result.Tiles)
        {
            Console.WriteLine($"{tile.Code} {tile.Confidence:F3}");
        }

        return true;
    }

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
