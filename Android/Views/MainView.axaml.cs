using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MahjongPoints.Android.Services;
using MahjongPoints.Models;
using MahjongPoints.ViewModels;

namespace MahjongPoints.Android.Views;

public partial class MainView : UserControl
{
    private readonly MainWindowViewModel viewModel;

    public MainView()
    {
        AvaloniaXamlLoader.Load(this);
        viewModel = new MainWindowViewModel();
        DataContext = viewModel;

        viewModel.OpenMeldSelectionRequested += (_, _) =>
        {
            viewModel.StatusMessage = "Android 版暂未提供副露面子弹窗。";
        };
        viewModel.KanSelectionRequested += async (_, e) =>
        {
            await viewModel.ApplyKanSelectionAsync(e.Kind, e.Candidates.FirstOrDefault());
        };
    }

    private async void CapturePhotoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        viewModel.StatusMessage = "正在打开摄像头...";
        await LoadImageAsync(ImageInput.Current.CapturePhotoAsync());
    }

    private async void PickPhotoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        viewModel.StatusMessage = "正在打开相册...";
        await LoadImageAsync(ImageInput.Current.PickPhotoAsync());
    }

    private async Task LoadImageAsync(Task<ImageInputResult?> imageTask)
    {
        var image = await imageTask;
        if (image is null)
        {
            viewModel.StatusMessage = "未选择图片。";
            return;
        }

        try
        {
            await viewModel.LoadAndRecognizeAsync(image.ImagePath);
        }
        finally
        {
            if (image.DeleteAfterLoad && File.Exists(image.ImagePath))
            {
                File.Delete(image.ImagePath);
            }
        }
    }

    private async void TenpaiDiscardWinningOption_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: TenpaiDiscardWinningOption option })
        {
            return;
        }

        e.Handled = true;
        await viewModel.SelectTenpaiDiscardWinningOptionCommand.ExecuteAsync(option);
    }

    private async void RemoveDeclaredKan_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: DeclaredKanItem item })
        {
            return;
        }

        await viewModel.RemoveDeclaredKanCommand.ExecuteAsync(item);
    }
}
