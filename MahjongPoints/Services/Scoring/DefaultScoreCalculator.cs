using System;
using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 默认点数计算器，根据番数、符数和亲子身份换算荣和点数。
/// </summary>
public sealed class DefaultScoreCalculator : IScoreCalculator
{
    /// <summary>
    /// 根据役种、符数和算点上下文计算最终点数。
    /// </summary>
    /// <param name="yakuResult">役种检测结果。</param>
    /// <param name="fuResult">符数计算结果。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>点数计算结果。</returns>
    public PointCalculationResult Calculate(
        YakuDetectionResult yakuResult,
        FuCalculationResult fuResult,
        MahjongScoringContext context)
    {
        var fan = yakuResult.TotalFan;
        var fu = fuResult.Fu;
        var breakdown = new List<string>(fuResult.Breakdown);

        if (fan <= 0 || fu <= 0)
        {
            return new PointCalculationResult(
                fan,
                fu,
                0,
                "No yaku or invalid fu.",
                [],
                breakdown);
        }

        var totalPoints = CalculateRonPoints(fan, fu, context.IsParent) + context.RiichiSticks * 1000;
        breakdown.Add($"Fan: {fan}");
        breakdown.Add($"Fu: {fu}");
        breakdown.Add($"Total points: {totalPoints}");

        var items = yakuResult.Yakus
            .Select(yaku => new MahjongScoreItem(
                yaku.Name,
                yaku.Fan,
                fu,
                totalPoints,
                yaku.Description))
            .ToArray();

        var summary = $"{string.Join(", ", yakuResult.Yakus.Select(yaku => yaku.Name))} | {fan} fan {fu} fu | {totalPoints} points";

        return new PointCalculationResult(
            fan,
            fu,
            totalPoints,
            summary,
            items,
            breakdown);
    }

    /// <summary>
    /// 计算荣和时的基础收点。
    /// </summary>
    /// <param name="fan">番数。</param>
    /// <param name="fu">符数。</param>
    /// <param name="isParent">是否亲家。</param>
    /// <returns>荣和点数。</returns>
    private static int CalculateRonPoints(int fan, int fu, bool isParent)
    {
        if (fan >= 5 || fan == 4 && fu >= 30 || fan == 3 && fu >= 70)
        {
            return isParent ? 12000 : 8000;
        }

        var basePoints = fu * (int)Math.Pow(2, fan + 2);
        var multiplier = isParent ? 6 : 4;
        return RoundUpToHundred(basePoints * multiplier);
    }

    /// <summary>
    /// 将点数向上取整到百位。
    /// </summary>
    /// <param name="points">原始点数。</param>
    /// <returns>百位向上取整后的点数。</returns>
    private static int RoundUpToHundred(int points) =>
        (int)Math.Ceiling(points / 100.0) * 100;
}
