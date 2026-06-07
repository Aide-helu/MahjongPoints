using System;
using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

public sealed class DefaultScoreCalculator : IScoreCalculator
{
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

    private static int RoundUpToHundred(int points) =>
        (int)Math.Ceiling(points / 100.0) * 100;
}

