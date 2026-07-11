using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MahjongPoints.Models;

/// <summary>
/// 表示一张可打出的牌，以及打出后可胡的所有牌。
/// </summary>
/// <param name="DiscardTile">建议打出的牌。</param>
/// <param name="WinningTiles">打出后可胡的牌列表。</param>
/// <param name="RemainingTiles">打出该牌后剩余的 13 张牌。</param>
public sealed record TenpaiDiscardOption(
    RecognizedMahjongTile DiscardTile,
    IReadOnlyList<RecognizedMahjongTile> WinningTiles,
    IReadOnlyList<RecognizedMahjongTile> RemainingTiles)
{
    /// <summary>
    /// 展开后的每一个“打出某牌，胡某牌”候选项。
    /// </summary>
    public IReadOnlyList<TenpaiDiscardWinningOption> WinningOptions { get; } = WinningTiles
        .Select(tile => new TenpaiDiscardWinningOption(DiscardTile, tile, RemainingTiles))
        .ToArray();

    /// <summary>
    /// 只显示弃牌的界面文本。
    /// </summary>
    public string DiscardText => $"打出 {DiscardTile.DisplayName} ({DiscardTile.Code})";

    /// <summary>
    /// 显示弃牌和全部可胡牌的界面文本。
    /// </summary>
    public string DisplayText =>
        $"打出 {DiscardTile.DisplayName} ({DiscardTile.Code})，可听：{string.Join(" ", WinningTiles.Select(tile => $"{tile.DisplayName} ({tile.Code})"))}";
}

/// <summary>
/// 表示一个具体的“打出某牌，胡某牌”可选项。
/// </summary>
/// <param name="discardTile">建议打出的牌。</param>
/// <param name="winningTile">打出后选择的胡牌张。</param>
/// <param name="remainingTiles">打出该牌后剩余的 13 张牌。</param>
public sealed partial class TenpaiDiscardWinningOption(
    RecognizedMahjongTile discardTile,
    RecognizedMahjongTile winningTile,
    IReadOnlyList<RecognizedMahjongTile> remainingTiles) : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// 选中状态对应的边框颜色。
    /// </summary>
    public IBrush SelectionBorderBrush => IsSelected ? Brushes.DodgerBlue : Brushes.Transparent;

    /// <summary>
    /// 选中状态对应的边框粗细。
    /// </summary>
    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(4) : new Thickness(0);

    /// <summary>
    /// 建议打出的牌。
    /// </summary>
    public RecognizedMahjongTile DiscardTile { get; } = discardTile;

    /// <summary>
    /// 打出后可胡的牌。
    /// </summary>
    public RecognizedMahjongTile WinningTile { get; } = winningTile;

    /// <summary>
    /// 打出该牌后剩余的 13 张牌。
    /// </summary>
    public IReadOnlyList<RecognizedMahjongTile> RemainingTiles { get; } = remainingTiles;

    /// <summary>
    /// 界面展示用的完整候选文本。
    /// </summary>
    public string DisplayText => $"打出 {DiscardTile.DisplayName} ({DiscardTile.Code})，胡 {WinningTile.DisplayName} ({WinningTile.Code})";

    /// <summary>
    /// 选中状态变化时通知边框相关属性刷新。
    /// </summary>
    /// <param name="value">新的选中状态。</param>
    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(SelectionBorderBrush));
        OnPropertyChanged(nameof(SelectionBorderThickness));
    }
}
