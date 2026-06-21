using Avalonia.Controls;
using Avalonia.Interactivity;
using MahjongPoints.ViewModels;

namespace MahjongPoints.Views;

/// <summary>
/// 选择副露面子的弹窗。
/// </summary>
public partial class OpenMeldSelectionWindow : Window
{
    /// <summary>
    /// 初始化副露面子选择弹窗。
    /// </summary>
    public OpenMeldSelectionWindow()
    {
        InitializeComponent();
    }

    private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is OpenMeldSelectionViewModel viewModel)
        {
            Close(viewModel.CreateSelectedSplit());
            return;
        }

        Close(null);
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
