using System.Collections.Generic;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

public sealed class DefaultFuCalculator : IFuCalculator
{
    public FuCalculationResult Calculate(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        YakuDetectionResult yakuResult,
        MahjongScoringContext context)
    {
        if (yakuResult.SelectedSplit is null)
        {
            return new FuCalculationResult(0, ["No valid hand split."], null);
        }

        return new FuCalculationResult(
            30,
            [
                "Skeleton fu calculation.",
                "Current default keeps the demo result at 30 fu."
            ],
            yakuResult.SelectedSplit);
    }
}

