using System.Collections.Generic;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 表示符数计算结果。
/// </summary>
/// <param name="Fu">最终符数。</param>
/// <param name="Breakdown">符数计算明细。</param>
/// <param name="SelectedSplit">用于计算符数的手牌拆解结果。</param>
public sealed record FuCalculationResult(
    int Fu,
    IReadOnlyList<string> Breakdown,
    MahjongHandSplitResult? SelectedSplit);
