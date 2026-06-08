using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;
using MahjongPoints.Services.Scoring;

namespace MahjongPoints.Services;

/// <summary>
/// 定义手牌算点服务。
/// </summary>
public interface IHandScoringService
{
    /// <summary>
    /// 根据识别出的手牌计算和牌结果与点数。
    /// </summary>
    /// <param name="recognizedTiles">识别出的手牌列表。</param>
    /// <param name="context">用户选择的胡牌状态和算点环境。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>算点结果。</returns>
    Task<MahjongScoringResult> CalculateAsync(
        IReadOnlyList<RecognizedMahjongTile> recognizedTiles,
        MahjongScoringContext context,
        CancellationToken cancellationToken = default);
}
