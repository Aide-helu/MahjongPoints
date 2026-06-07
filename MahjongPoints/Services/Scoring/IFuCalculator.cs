using System.Collections.Generic;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 定义符数计算器。
/// </summary>
public interface IFuCalculator
{
    /// <summary>
    /// 根据手牌、役种检测结果和算点上下文计算符数。
    /// </summary>
    /// <param name="tiles">参与算点的完整牌列表。</param>
    /// <param name="yakuResult">役种检测结果。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>符数计算结果。</returns>
    FuCalculationResult Calculate(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        YakuDetectionResult yakuResult,
        MahjongScoringContext context);
}
