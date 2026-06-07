using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;
using MahjongPoints.Services.Scoring;

namespace MahjongPoints.Services;

/// <summary>
/// 演示用手牌算点服务，使用固定和牌张并串联四层算点框架。
/// </summary>
public sealed class HardcodedHandScoringService : IHandScoringService
{
    /// <summary>
    /// 演示流程中补入的固定和牌张。
    /// </summary>
    private static readonly RecognizedMahjongTile _winningTile = new("5p", "5 pin", 1.0);

    /// <summary>
    /// 手牌拆解器。
    /// </summary>
    private readonly IHandSplitter _handSplitter;

    /// <summary>
    /// 役种检测器。
    /// </summary>
    private readonly IYakuDetector _yakuDetector;

    /// <summary>
    /// 符数计算器。
    /// </summary>
    private readonly IFuCalculator _fuCalculator;

    /// <summary>
    /// 点数计算器。
    /// </summary>
    private readonly IScoreCalculator _scoreCalculator;

    /// <summary>
    /// 使用默认四层算点组件创建演示算点服务。
    /// </summary>
    public HardcodedHandScoringService()
        : this(
            new DefaultHandSplitter(),
            new DefaultYakuDetector(),
            new DefaultFuCalculator(),
            new DefaultScoreCalculator())
    {
    }

    /// <summary>
    /// 使用指定四层算点组件创建演示算点服务。
    /// </summary>
    /// <param name="handSplitter">手牌拆解器。</param>
    /// <param name="yakuDetector">役种检测器。</param>
    /// <param name="fuCalculator">符数计算器。</param>
    /// <param name="scoreCalculator">点数计算器。</param>
    public HardcodedHandScoringService(
        IHandSplitter handSplitter,
        IYakuDetector yakuDetector,
        IFuCalculator fuCalculator,
        IScoreCalculator scoreCalculator)
    {
        _handSplitter = handSplitter;
        _yakuDetector = yakuDetector;
        _fuCalculator = fuCalculator;
        _scoreCalculator = scoreCalculator;
    }

    /// <summary>
    /// 把识别出的 13 张牌补入固定和牌张后，依次执行拆牌、判役、算符和算点。
    /// </summary>
    /// <param name="recognizedTiles">识别出的手牌列表。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>演示算点结果。</returns>
    public Task<MahjongScoringResult> CalculateAsync(
        IReadOnlyList<RecognizedMahjongTile> recognizedTiles,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var calculationTiles = recognizedTiles.Concat([_winningTile]).ToArray();
        var context = new MahjongScoringContext(_winningTile);

        var splits = _handSplitter.Split(calculationTiles);
        var yakuResult = _yakuDetector.Detect(calculationTiles, splits, context);
        var fuResult = _fuCalculator.Calculate(calculationTiles, yakuResult, context);
        var pointResult = _scoreCalculator.Calculate(yakuResult, fuResult, context);

        var isWinningHand = splits.Count > 0 && yakuResult.Yakus.Count > 0 && pointResult.TotalPoints > 0;
        var winningShape = yakuResult.SelectedSplit?.DisplayText ?? "No valid 4 melds + 1 pair split.";
        var message = isWinningHand
            ? "Scoring pipeline completed: split hand, detected yaku, calculated fu, calculated points."
            : "Scoring pipeline completed, but the hand has no valid yaku yet.";

        var result = new MahjongScoringResult(
            calculationTiles,
            _winningTile,
            isWinningHand,
            winningShape,
            pointResult.Summary,
            pointResult.TotalFan,
            pointResult.Fu,
            pointResult.TotalPoints,
            pointResult.Items,
            message);

        return Task.FromResult(result);
    }
}
