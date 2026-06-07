using System.Collections.Generic;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 定义役种检测器。
/// </summary>
public interface IYakuDetector
{
    /// <summary>
    /// 根据完整手牌和拆解结果检测满足的役种。
    /// </summary>
    /// <param name="tiles">参与算点的完整牌列表。</param>
    /// <param name="splits">手牌拆解结果列表。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>役种检测结果。</returns>
    YakuDetectionResult Detect(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        IReadOnlyList<MahjongHandSplit> splits,
        MahjongScoringContext context);
}
