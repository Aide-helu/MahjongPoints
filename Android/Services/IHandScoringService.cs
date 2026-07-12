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
    /// 为 13 张手牌查找所有能组成可拆牌型的听牌张。
    /// </summary>
    /// <param name="recognizedTiles">当前识别出的 13 张手牌。</param>
    /// <returns>可补成有效拆牌形状的候选胡牌张。</returns>
    IReadOnlyList<RecognizedMahjongTile> FindTenpaiTiles(IReadOnlyList<RecognizedMahjongTile> recognizedTiles);

    /// <summary>
    /// 为 14 张手牌查找所有“打出一张后可听”的候选。
    /// </summary>
    /// <param name="recognizedTiles">当前识别出的 14 张手牌。</param>
    /// <returns>按弃牌分组的听牌候选。</returns>
    IReadOnlyList<TenpaiDiscardOption> FindTenpaiDiscardOptions(IReadOnlyList<RecognizedMahjongTile> recognizedTiles);

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
