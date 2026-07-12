using System;
using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 表示麻将面子类型。
/// </summary>
public enum MahjongMeldType
{
    /// <summary>
    /// 顺子。
    /// </summary>
    Sequence,

    /// <summary>
    /// 刻子。
    /// </summary>
    Triplet,

    /// <summary>
    /// 杠子。
    /// </summary>
    Quad
}

/// <summary>
/// 表示一个麻将面子。
/// </summary>
/// <param name="Type">面子类型。</param>
/// <param name="Tiles">组成面子的牌列表。</param>
/// <param name="IsOpen">是否为明面子。</param>
public sealed record MahjongMeld(
    MahjongMeldType Type,
    IReadOnlyList<RecognizedMahjongTile> Tiles,
    bool IsOpen = false)
{
    /// <summary>
    /// 供界面展示的面子文本。
    /// </summary>
    public string DisplayText => string.Join(" ", Tiles.Select(tile => tile.Code));

    /// <summary>
    /// 用于比较面子的稳定键，包含类型和排序后的牌编码。
    /// </summary>
    public string Key => $"{Type}:{string.Join(",", Tiles.Select(tile => tile.Code).Order(StringComparer.OrdinalIgnoreCase))}";
}
