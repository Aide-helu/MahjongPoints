using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;

namespace MahjongPoints.Services;

public sealed class HardcodedHandScoringService : IHandScoringService
{
    private static readonly RecognizedMahjongTile WinningTile = new("5p", "五筒", 1.0);

    private static readonly MahjongScoreItem[] DemoScoreItems =
    [
        new(
            "断幺九",
            1,
            30,
            1000,
            "硬编码 demo 牌型全部由 2-8 的数牌组成，按 1 番 30 符固定返回。")
    ];

    public Task<MahjongScoringResult> CalculateAsync(
        IReadOnlyList<RecognizedMahjongTile> recognizedTiles,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var calculationTiles = recognizedTiles.Concat([WinningTile]).ToArray();

        var result = new MahjongScoringResult(
            calculationTiles,
            WinningTile,
            IsWinningHand: IsDemoWinningShape(calculationTiles),
            WinningShape: "二三四万 + 三四五万 + 四五六筒 + 六七八条 + 五筒雀头",
            ScoreSummary: "胡牌 | 1 番 30 符 | 1000 点",
            TotalFan: 1,
            Fu: 30,
            TotalPoints: 1000,
            DemoScoreItems,
            "已将硬编码的 13 张识别结果和胡牌张送入算点逻辑。");

        return Task.FromResult(result);
    }

    private static bool IsDemoWinningShape(IReadOnlyList<RecognizedMahjongTile> tiles)
    {
        if (tiles.Count != 14)
        {
            return false;
        }

        var expectedCounts = new Dictionary<string, int>
        {
            ["2m"] = 1,
            ["3m"] = 2,
            ["4m"] = 2,
            ["5m"] = 1,
            ["4p"] = 1,
            ["5p"] = 3,
            ["6p"] = 1,
            ["6s"] = 1,
            ["7s"] = 1,
            ["8s"] = 1
        };

        var actualCounts = tiles
            .GroupBy(tile => tile.Code)
            .ToDictionary(group => group.Key, group => group.Count());

        return expectedCounts.Count == actualCounts.Count
               && expectedCounts.All(expected =>
                   actualCounts.TryGetValue(expected.Key, out var actual)
                   && actual == expected.Value);
    }
}
