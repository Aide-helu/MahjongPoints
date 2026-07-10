using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MahjongPoints.Models;
using MahjongPoints.ViewModels;
using MahjongPoints.Services.Scoring;

namespace MahjongPoints.Views;

/// <summary>
/// 应用主窗口，负责承载图片选择和识别结果展示界面。
/// </summary>
public partial class MainWindow : Window
{
    private MainWindowViewModel? _subscribedViewModel;
    private bool _isShowingOpenMeldDialog;

    /// <summary>
    /// 初始化主窗口组件。
    /// </summary>
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 窗口关闭时释放 DataContext 中持有的资源。
    /// </summary>
    /// <param name="e">窗口关闭事件参数。</param>
    protected override void OnClosed(EventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.OpenMeldSelectionRequested -= ViewModel_OpenMeldSelectionRequested;
            _subscribedViewModel = null;
        }

        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }

    /// <summary>
    /// 窗口打开后订阅 ViewModel 发出的副露面子选择请求。
    /// </summary>
    /// <param name="e">事件参数。</param>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is MainWindowViewModel viewModel && !ReferenceEquals(_subscribedViewModel, viewModel))
        {
            if (_subscribedViewModel is not null)
            {
                _subscribedViewModel.OpenMeldSelectionRequested -= ViewModel_OpenMeldSelectionRequested;
            }

            _subscribedViewModel = viewModel;
            viewModel.OpenMeldSelectionRequested += ViewModel_OpenMeldSelectionRequested;
        }
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

    private async void LoadClipboardImageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var clipboard = Clipboard;
        if (clipboard is null)
        {
            viewModel.StatusMessage = "当前窗口无法访问剪贴板。";
            return;
        }

        try
        {
            using var bitmap = await clipboard.TryGetBitmapAsync();
            if (bitmap is null)
            {
                viewModel.StatusMessage = "剪贴板里没有图片。";
                return;
            }

            var tempImagePath = Path.Combine(
                Path.GetTempPath(),
                $"MahjongPointsClipboard_{DateTime.Now:yyyyMMddHHmmssfff}.png");
            bitmap.Save(tempImagePath);

            await viewModel.LoadAndRecognizeAsync(tempImagePath);
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = ex.Message;
        }
    }

    private async void TenpaiDiscardWinningOption_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: TenpaiDiscardWinningOption option } ||
            DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        e.Handled = true;
        await viewModel.SelectTenpaiDiscardWinningOptionCommand.ExecuteAsync(option);
    }

    private async void ViewModel_OpenMeldSelectionRequested(object? sender, EventArgs e)
    {
        if (_isShowingOpenMeldDialog || sender is not MainWindowViewModel viewModel)
        {
            return;
        }

        _isShowingOpenMeldDialog = true;
        try
        {
            var dialogViewModel = viewModel.CreateOpenMeldSelectionViewModel();
            if (dialogViewModel.MeldOptions.Count == 0)
            {
                await viewModel.CancelOpenMeldSelectionAsync();
                return;
            }

            var dialog = new OpenMeldSelectionWindow
            {
                DataContext = dialogViewModel
            };

            var selectedOpenMelds = await dialog.ShowDialog<IReadOnlyList<MahjongMeld>?>(this);
            if (selectedOpenMelds is null || selectedOpenMelds.Count == 0)
            {
                await viewModel.CancelOpenMeldSelectionAsync();
                return;
            }

            viewModel.ApplyOpenMeldSelection(selectedOpenMelds);
        }
        finally
        {
            _isShowingOpenMeldDialog = false;
        }
    }
}
