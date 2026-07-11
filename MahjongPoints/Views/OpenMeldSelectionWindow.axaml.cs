using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 处理确认按钮点击，并把用户选择的副露面子作为弹窗结果返回。
    /// </summary>
    /// <param name="sender">触发点击事件的控件。</param>
    /// <param name="e">路由事件参数。</param>
    private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is OpenMeldSelectionViewModel viewModel)
        {
            Close(viewModel.CreateSelectedOpenMelds());
            return;
        }

        Close(null);
    }

    /// <summary>
    /// 处理取消按钮点击，并关闭弹窗且不返回副露面子。
    /// </summary>
    /// <param name="sender">触发点击事件的控件。</param>
    /// <param name="e">路由事件参数。</param>
    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
