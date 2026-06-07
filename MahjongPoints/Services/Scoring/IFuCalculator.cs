using System.Collections.Generic;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

public interface IFuCalculator
{
    FuCalculationResult Calculate(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        YakuDetectionResult yakuResult,
        MahjongScoringContext context);
}

