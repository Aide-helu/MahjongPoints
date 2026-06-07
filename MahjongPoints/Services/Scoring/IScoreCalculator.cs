namespace MahjongPoints.Services.Scoring;

public interface IScoreCalculator
{
    PointCalculationResult Calculate(
        YakuDetectionResult yakuResult,
        FuCalculationResult fuResult,
        MahjongScoringContext context);
}

