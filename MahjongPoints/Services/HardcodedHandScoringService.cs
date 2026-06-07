using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;
using MahjongPoints.Services.Scoring;

namespace MahjongPoints.Services;

/// <summary>
/// 演示用手牌算点服务，直接接收 14 张胡牌状态手牌并串联四层算点框架。
/// </summary>
public sealed class HardcodedHandScoringService : IHandScoringService
{
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
    /// 把识别出的 14 张胡牌状态手牌依次送入拆牌、判役、算符和算点流程。
    /// </summary>
    /// <param name="recognizedTiles">识别出的手牌列表。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>演示算点结果。</returns>
    public Task<MahjongScoringResult> CalculateAsync(
        IReadOnlyList<RecognizedMahjongTile> recognizedTiles,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // ONNX 或 demo 识别器应直接返回 14 张胡牌状态手牌，这里直接使用识别结果。
        var calculationTiles = recognizedTiles.ToArray();

        // 目前没有单独识别和牌张，界面显示暂时取 14 张牌中的最后一张作为和牌张。
        var winningTile = calculationTiles.Length > 0
            ? calculationTiles[^1]
            : new RecognizedMahjongTile("unknown", "未知", 0);

        // 算点上下文保存是否自摸、是否亲家等环境信息；当前 demo 使用默认环境。
        var context = new MahjongScoringContext();

        // 四层算点流水线：先拆牌，再判役，再算符，最后把番符换算成点数。
        var splits = _handSplitter.Split(calculationTiles);
        var yakuResult = _yakuDetector.Detect(calculationTiles, splits, context);
        var fuResult = _fuCalculator.Calculate(calculationTiles, yakuResult, context);
        var pointResult = _scoreCalculator.Calculate(yakuResult, fuResult, context);

        // 当前 demo 先用“能拆牌、有役、有点数”作为是否和牌的判断条件。
        var isWinningHand = splits.Count > 0 && yakuResult.Yakus.Count > 0 && pointResult.TotalPoints > 0;
        var winningShape = yakuResult.SelectedSplit?.DisplayText ?? "No valid 4 melds + 1 pair split.";
        var message = isWinningHand
            ? "Scoring pipeline completed: split hand, detected yaku, calculated fu, calculated points."
            : "Scoring pipeline completed, but the hand has no valid yaku yet.";

        // 把四层流水线的结果转换成 ViewModel 和界面使用的统一算点结果模型。
        var result = new MahjongScoringResult(
            calculationTiles,
            winningTile,
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
