using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;
using MahjongPoints.Services.Scoring;

namespace MahjongPoints.Services;

public sealed class HardcodedHandScoringService : IHandScoringService
{
    private static readonly RecognizedMahjongTile WinningTile = new("5p", "5 pin", 1.0);

    private readonly IHandSplitter _handSplitter;
    private readonly IYakuDetector _yakuDetector;
    private readonly IFuCalculator _fuCalculator;
    private readonly IScoreCalculator _scoreCalculator;

    public HardcodedHandScoringService()
        : this(
            new DefaultHandSplitter(),
            new DefaultYakuDetector(),
            new DefaultFuCalculator(),
            new DefaultScoreCalculator())
    {
    }

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

    public Task<MahjongScoringResult> CalculateAsync(
        IReadOnlyList<RecognizedMahjongTile> recognizedTiles,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var calculationTiles = recognizedTiles.Concat([WinningTile]).ToArray();
        var context = new MahjongScoringContext(WinningTile);

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
            WinningTile,
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

