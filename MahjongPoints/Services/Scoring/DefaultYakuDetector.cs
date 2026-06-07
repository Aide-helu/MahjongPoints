using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

public sealed class DefaultYakuDetector : IYakuDetector
{
    private static readonly MahjongYaku Tanyao = new(
        "tanyao",
        "Tanyao",
        1,
        "All tiles are suited tiles from 2 through 8.");

    public YakuDetectionResult Detect(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        IReadOnlyList<MahjongHandSplit> splits,
        MahjongScoringContext context)
    {
        var yakus = new List<MahjongYaku>();

        if (splits.Count > 0 && IsTanyao(tiles))
        {
            yakus.Add(Tanyao);
        }

        return new YakuDetectionResult(yakus, splits.FirstOrDefault());
    }

    private static bool IsTanyao(IEnumerable<RecognizedMahjongTile> tiles)
    {
        foreach (var tile in tiles)
        {
            if (tile.Code.Length != 2 || !char.IsDigit(tile.Code[0]))
            {
                return false;
            }

            var value = tile.Code[0] - '0';
            if (value is < 2 or > 8)
            {
                return false;
            }
        }

        return true;
    }
}

