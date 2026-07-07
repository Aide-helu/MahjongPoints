using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using MahjongPoints.Services;
using MahjongPoints.ViewModels;
using MahjongPoints.Views;

namespace MahjongPoints;

/// <summary>
/// Avalonia 应用对象，负责加载全局 XAML 并创建主窗口。
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 初始化应用级 XAML 资源。
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 框架初始化完成后创建主窗口并设置主窗口的 ViewModel。
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(
                    new OnnxHandImageRecognizer(),
                    new HardcodedHandScoringService()),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
