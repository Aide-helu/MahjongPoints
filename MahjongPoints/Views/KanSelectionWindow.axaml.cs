using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MahjongPoints.Models;

namespace MahjongPoints.Views;

/// <summary>
/// 多个杠候选牌时使用的选择窗口。
/// </summary>
public partial class KanSelectionWindow : Window
{
    /// <summary>
    /// XAML 设计器使用的默认构造函数。
    /// </summary>
    public KanSelectionWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 使用指定杠类型和候选牌初始化选择窗口。
    /// </summary>
    public KanSelectionWindow(
        DeclaredKanKind kind,
        IReadOnlyList<RecognizedMahjongTile> candidates)
        : this()
    {
        MessageText.Text = kind == DeclaredKanKind.Concealed
            ? "选择哪一组对子作为暗杠。"
            : "选择哪一组四张相同牌作为杠。";
        CandidateList.ItemsSource = candidates;
        CandidateList.SelectedIndex = 0;
    }

    /// <summary>
    /// 处理确认按钮点击，并返回当前选中的杠候选牌。
    /// </summary>
    /// <param name="sender">触发点击事件的控件。</param>
    /// <param name="e">路由事件参数。</param>
    private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(CandidateList.SelectedItem as RecognizedMahjongTile);
    }

    /// <summary>
    /// 处理取消按钮点击，并关闭弹窗且不返回候选牌。
    /// </summary>
    /// <param name="sender">触发点击事件的控件。</param>
    /// <param name="e">路由事件参数。</param>
    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
