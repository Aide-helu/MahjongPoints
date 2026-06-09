using System;
using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 默认役种检测器，当前先提供演示用断幺九检测。
/// </summary>
public sealed class DefaultYakuDetector : IYakuDetector
{
    /// <summary>
    /// 演示实现中支持的断幺九役种定义。
    /// </summary>
    private static readonly MahjongYaku _duanyao = new(
        "duanyao",
        "Duanyao",
        1,
        "All tiles are suited tiles from 2 through 8.");

    private static readonly IReadOnlyDictionary<string, MahjongYaku> _dragonYakus =
        new Dictionary<string, MahjongYaku>(StringComparer.OrdinalIgnoreCase)
        {
            ["5z"] = new(
                "yakuhai-white-dragon",
                "役牌（白板）",
                1,
                "白板刻子。"),
            ["6z"] = new(
                "yakuhai-green-dragon",
                "役牌（发）",
                1,
                "发财刻子。"),
            ["7z"] = new(
                "yakuhai-red-dragon",
                "役牌（中）",
                1,
                "红中刻子。")
        };

    /// <summary>
    /// 根据完整手牌和拆牌结果检测满足的役种。
    /// </summary>
    /// <param name="tiles">参与算点的完整牌列表。</param>
    /// <param name="splits">手牌拆解结果列表。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>役种检测结果。</returns>
    public YakuDetectionResult Detect(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        IReadOnlyList<MahjongHandSplit> splits,
        MahjongScoringContext context)
    {
        var yakus = new List<MahjongYaku>();
        var selectedSplit = splits.FirstOrDefault();

        if (splits.Count > 0 && IsTanyao(tiles))
        {
            yakus.Add(_duanyao);
        }

        if (selectedSplit is not null)
        {
            yakus.AddRange(DetectDragonYakuhai(selectedSplit));
        }

        return new YakuDetectionResult(yakus, selectedSplit);
    }

    private static IEnumerable<MahjongYaku> DetectDragonYakuhai(MahjongHandSplit split)
    {
        foreach (var meld in split.Melds)
        {
            if (meld.Type is not (MahjongMeldType.Triplet or MahjongMeldType.Quad) || meld.Tiles.Count == 0)
            {
                continue;
            }

            var code = meld.Tiles[0].Code;
            if (!meld.Tiles.All(tile => string.Equals(tile.Code, code, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (_dragonYakus.TryGetValue(code, out var yaku))
            {
                yield return yaku;
            }
        }
    }

    /// <summary>
    /// 判断所有牌是否都是 2 到 8 的数牌。
    /// </summary>
    /// <param name="tiles">待检查的牌列表。</param>
    /// <returns>如果满足断幺九条件，则返回 <c>true</c>。</returns>
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
