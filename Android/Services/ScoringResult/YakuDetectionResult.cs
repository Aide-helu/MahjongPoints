using System.Collections.Generic;
using System.Linq;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 表示役种检测结果。
/// </summary>
/// <param name="Yakus">检测到的役种列表。</param>
/// <param name="SelectedSplit">用于判役的手牌拆解结果。</param>
public sealed record YakuDetectionResult(
    IReadOnlyList<MahjongYaku> Yakus,
    MahjongHandSplitResult? SelectedSplit)
{
    /// <summary>
    /// 检测到的役种总番数。
    /// </summary>
    public int TotalFan => Yakus.Sum(yaku => yaku.Fan);
}
