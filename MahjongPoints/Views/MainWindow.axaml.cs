using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MahjongPoints.ViewModels;

namespace MahjongPoints.Views;

/// <summary>
/// 应用主窗口，负责承载图片选择和识别结果展示界面。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 初始化主窗口组件。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 窗口关闭时释放 DataContext 中持有的资源。
    /// </summary>
    /// <param name="e">窗口关闭事件参数。</param>
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }

    /// <summary>
    /// 处理选择图片按钮点击事件，并把选择的图片路径交给 ViewModel 识别。
    /// </summary>
    /// <param name="sender">触发事件的控件。</param>
    /// <param name="e">路由事件参数。</param>
    private async void SelectImageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择麻将手牌图片",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("图片文件")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"],
                    MimeTypes = ["image/png", "image/jpeg", "image/bmp", "image/webp"]
                }
            ]
        });

        if (files.Count == 0 || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        await viewModel.LoadAndRecognizeAsync(files[0].Path.LocalPath);
    }
}
