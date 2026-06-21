namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 定义点数计算器。
/// </summary>
public interface IScoreCalculator
{
    /// <summary>
    /// 根据役种、符数和算点上下文换算最终点数。
    /// </summary>
    /// <param name="yakuResult">役种检测结果。</param>
    /// <param name="fuResult">符数计算结果。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>点数计算结果。</returns>
    PointCalculationResult Calculate(
        YakuDetectionResult yakuResult,
        FuCalculationResult fuResult,
        MahjongScoringContext context);
}
