using System;
using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 默认手牌拆解器，枚举标准 4 面子加 1 雀头的拆法。
/// </summary>
public sealed class DefaultHandSplitter : IHandSplitter
{
    /// <summary>
    /// 支持组成顺子的数牌花色。
    /// </summary>
    private static readonly char[] Suits = ['m', 'p', 's'];

    /// <summary>
    /// 把 14 张牌拆解为所有可能的 4 面子加 1 雀头结构。
    /// </summary>
    /// <param name="tiles">参与拆解的完整牌列表。</param>
    /// <returns>所有可用的手牌拆解结果。</returns>
    public IReadOnlyList<MahjongHandSplit> Split(IReadOnlyList<RecognizedMahjongTile> tiles)
    {
        if (tiles.Count != 14)
        {
            return [];
        }

        var tileByCode = tiles
            .GroupBy(tile => tile.Code)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var counts = tiles
            .GroupBy(tile => tile.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var results = new List<MahjongHandSplit>();

        foreach (var pairCode in counts.Where(pair => pair.Value >= 2).Select(pair => pair.Key).ToArray())
        {
            var remainingCounts = new Dictionary<string, int>(counts, StringComparer.OrdinalIgnoreCase);
            RemoveTiles(remainingCounts, pairCode, 2);

            foreach (var melds in FindMelds(remainingCounts, tileByCode, []))
            {
                if (melds.Count == 4)
                {
                    results.Add(new MahjongHandSplit(melds, tileByCode[pairCode]));
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 在剩余牌计数中递归查找所有可用面子组合。
    /// </summary>
    /// <param name="counts">剩余牌计数字典。</param>
    /// <param name="tileByCode">牌编码到牌对象的映射。</param>
    /// <param name="current">当前已经拆出的面子列表。</param>
    /// <returns>所有递归得到的面子组合。</returns>
    private static IReadOnlyList<IReadOnlyList<MahjongMeld>> FindMelds(
        IReadOnlyDictionary<string, int> counts,
        IReadOnlyDictionary<string, RecognizedMahjongTile> tileByCode,
        IReadOnlyList<MahjongMeld> current)
    {
        if (counts.Values.All(count => count == 0))
        {
            return current.Count == 4 ? [current] : [];
        }

        if (current.Count >= 4)
        {
            return [];
        }

        var firstCode = counts
            .Where(pair => pair.Value > 0)
            .Select(pair => pair.Key)
            .OrderBy(GetSortKey)
            .First();

        var results = new List<IReadOnlyList<MahjongMeld>>();

        if (counts[firstCode] >= 3)
        {
            var nextCounts = new Dictionary<string, int>(counts, StringComparer.OrdinalIgnoreCase);
            RemoveTiles(nextCounts, firstCode, 3);
            var tile = tileByCode[firstCode];
            var meld = new MahjongMeld(MahjongMeldType.Triplet, [tile, tile, tile]);
            results.AddRange(FindMelds(nextCounts, tileByCode, [.. current, meld]));
        }

        if (TryGetSuitedTile(firstCode, out var value, out var suit) && value <= 7)
        {
            var secondCode = $"{value + 1}{suit}";
            var thirdCode = $"{value + 2}{suit}";

            if (HasTile(counts, secondCode) && HasTile(counts, thirdCode))
            {
                var nextCounts = new Dictionary<string, int>(counts, StringComparer.OrdinalIgnoreCase);
                RemoveTiles(nextCounts, firstCode, 1);
                RemoveTiles(nextCounts, secondCode, 1);
                RemoveTiles(nextCounts, thirdCode, 1);

                var meld = new MahjongMeld(
                    MahjongMeldType.Sequence,
                    [tileByCode[firstCode], tileByCode[secondCode], tileByCode[thirdCode]]);

                results.AddRange(FindMelds(nextCounts, tileByCode, [.. current, meld]));
            }
        }

        return results;
    }

    /// <summary>
    /// 判断剩余牌计数中是否还有指定编码的牌。
    /// </summary>
    /// <param name="counts">剩余牌计数字典。</param>
    /// <param name="code">麻将牌编码。</param>
    /// <returns>如果指定牌仍有剩余，则返回 <c>true</c>。</returns>
    private static bool HasTile(IReadOnlyDictionary<string, int> counts, string code) =>
        counts.TryGetValue(code, out var count) && count > 0;

    /// <summary>
    /// 从剩余牌计数中移除指定数量的牌。
    /// </summary>
    /// <param name="counts">剩余牌计数字典。</param>
    /// <param name="code">麻将牌编码。</param>
    /// <param name="amount">要移除的数量。</param>
    private static void RemoveTiles(IDictionary<string, int> counts, string code, int amount)
    {
        counts[code] -= amount;
        if (counts[code] <= 0)
        {
            counts.Remove(code);
        }
    }

    /// <summary>
    /// 尝试把牌编码解析为数牌点数和花色。
    /// </summary>
    /// <param name="code">麻将牌编码。</param>
    /// <param name="value">解析出的点数。</param>
    /// <param name="suit">解析出的花色。</param>
    /// <returns>如果编码是合法数牌，则返回 <c>true</c>。</returns>
    private static bool TryGetSuitedTile(string code, out int value, out char suit)
    {
        value = 0;
        suit = '\0';

        if (code.Length != 2 || !char.IsDigit(code[0]) || !Suits.Contains(code[1]))
        {
            return false;
        }

        value = code[0] - '0';
        suit = code[1];
        return value is >= 1 and <= 9;
    }

    /// <summary>
    /// 获取牌编码的稳定排序键，用于递归拆牌时固定处理顺序。
    /// </summary>
    /// <param name="code">麻将牌编码。</param>
    /// <returns>排序键。</returns>
    private static string GetSortKey(string code)
    {
        if (TryGetSuitedTile(code, out var value, out var suit))
        {
            return $"{suit}{value}";
        }

        return $"z{code}";
    }
}
