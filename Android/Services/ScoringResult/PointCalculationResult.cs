using System.Collections.Generic;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 表示最终点数计算结果。
/// </summary>
/// <param name="TotalFan">总番数。</param>
/// <param name="Fu">最终符数。</param>
/// <param name="TotalPoints">最终点数。</param>
/// <param name="Summary">界面展示用摘要。</param>
/// <param name="Items">役种或得分明细。</param>
/// <param name="Breakdown">点数计算过程明细。</param>
public sealed record PointCalculationResult(
    int TotalFan,
    int Fu,
    int TotalPoints,
    string Summary,
    IReadOnlyList<MahjongScoreItem> Items,
    IReadOnlyList<string> Breakdown);
