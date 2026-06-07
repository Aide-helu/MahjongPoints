using System.Collections.Generic;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 默认符数计算器，当前保持演示结果使用固定 30 符。
/// </summary>
public sealed class DefaultFuCalculator : IFuCalculator
{
    /// <summary>
    /// 根据役种结果计算符数。
    /// </summary>
    /// <param name="tiles">参与算点的完整牌列表。</param>
    /// <param name="yakuResult">役种检测结果。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>符数计算结果。</returns>
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
