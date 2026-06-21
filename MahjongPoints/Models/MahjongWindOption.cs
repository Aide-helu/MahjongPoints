using System.Collections.Generic;
using MahjongPoints.Services.Scoring;

namespace MahjongPoints.Models;

/// <summary>
/// 表示风位下拉框中的一个候选项。
/// </summary>
/// <param name="Wind">该选项对应的风位。</param>
public sealed record MahjongWindOption(MahjongWind Wind)
{
    /// <summary>
    /// 界面展示的风位名称。
    /// </summary>
    public string DisplayName => Wind.ToDisplayName();

    /// <summary>
    /// 风位对应的风牌编码。
    /// </summary>
    public string TileCode => Wind.ToTileCode();

    /// <summary>
    /// 东、南、西、北四个固定选项，供自风和场风下拉框复用。
    /// </summary>
    public static IReadOnlyList<MahjongWindOption> All { get; } =
    [
        new(MahjongWind.East),
        new(MahjongWind.South),
        new(MahjongWind.West),
        new(MahjongWind.North)
    ];
}
