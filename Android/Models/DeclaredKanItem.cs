using System;
using System.Collections.Generic;
using MahjongPoints.Services.Scoring;

namespace MahjongPoints.Models;

/// <summary>
/// 用户手动声明的杠类型。
/// </summary>
public enum DeclaredKanKind
{
    /// <summary>
    /// 暗杠；识别模型通常只能看到两张同牌。
    /// </summary>
    Concealed,

    /// <summary>
    /// 明杠；识别结果中应能看到四张同牌。
    /// </summary>
    Open
}

/// <summary>
/// 界面上已声明的一组杠，并提供算点层使用的面子模型。
/// </summary>
public sealed class DeclaredKanItem(DeclaredKanKind kind, RecognizedMahjongTile tile)
{
    /// <summary>
    /// 杠的来源类型。
    /// </summary>
    public DeclaredKanKind Kind { get; } = kind;

    /// <summary>
    /// 被声明为杠的牌。
    /// </summary>
    public RecognizedMahjongTile Tile { get; } = tile;

    /// <summary>
    /// 算点层使用的四张相同牌面子。
    /// </summary>
    public MahjongMeld Meld { get; } = new(
        MahjongMeldType.Quad,
        [tile, tile, tile, tile],
        kind == DeclaredKanKind.Open);

    /// <summary>
    /// 列表展示文本。
    /// </summary>
    public string DisplayText => $"{KindText} {Tile.DisplayName} ({Tile.Code})";

    private string KindText => Kind switch
    {
        DeclaredKanKind.Concealed => "暗杠",
        DeclaredKanKind.Open => "明杠",
        _ => "杠"
    };
}

/// <summary>
/// 请求界面弹出杠候选选择窗口的事件参数。
/// </summary>
public sealed class KanSelectionRequestedEventArgs(
    DeclaredKanKind kind,
    IReadOnlyList<RecognizedMahjongTile> candidates) : EventArgs
{
    /// <summary>
    /// 本次要声明的杠类型。
    /// </summary>
    public DeclaredKanKind Kind { get; } = kind;

    /// <summary>
    /// 根据识别牌数筛出的候选牌。
    /// </summary>
    public IReadOnlyList<RecognizedMahjongTile> Candidates { get; } = candidates;
}
