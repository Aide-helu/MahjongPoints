using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MahjongPoints.Models;

public sealed record TenpaiDiscardOption(
    RecognizedMahjongTile DiscardTile,
    IReadOnlyList<RecognizedMahjongTile> WinningTiles,
    IReadOnlyList<RecognizedMahjongTile> RemainingTiles)
{
    public IReadOnlyList<TenpaiDiscardWinningOption> WinningOptions { get; } = WinningTiles
        .Select(tile => new TenpaiDiscardWinningOption(DiscardTile, tile, RemainingTiles))
        .ToArray();

    public string DiscardText => $"打出 {DiscardTile.DisplayName} ({DiscardTile.Code})";

    public string DisplayText =>
        $"打出 {DiscardTile.DisplayName} ({DiscardTile.Code})，可听：{string.Join(" ", WinningTiles.Select(tile => $"{tile.DisplayName} ({tile.Code})"))}";
}

public sealed partial class TenpaiDiscardWinningOption(
    RecognizedMahjongTile discardTile,
    RecognizedMahjongTile winningTile,
    IReadOnlyList<RecognizedMahjongTile> remainingTiles) : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public IBrush SelectionBorderBrush => IsSelected ? Brushes.DodgerBlue : Brushes.Transparent;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(4) : new Thickness(0);

    public RecognizedMahjongTile DiscardTile { get; } = discardTile;

    public RecognizedMahjongTile WinningTile { get; } = winningTile;

    public IReadOnlyList<RecognizedMahjongTile> RemainingTiles { get; } = remainingTiles;

    public string DisplayText => $"打出 {DiscardTile.DisplayName} ({DiscardTile.Code})，胡 {WinningTile.DisplayName} ({WinningTile.Code})";

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(SelectionBorderBrush));
        OnPropertyChanged(nameof(SelectionBorderThickness));
    }
}
