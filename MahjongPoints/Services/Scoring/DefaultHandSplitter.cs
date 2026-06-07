using System;
using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

public sealed class DefaultHandSplitter : IHandSplitter
{
    private static readonly char[] Suits = ['m', 'p', 's'];

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

    private static bool HasTile(IReadOnlyDictionary<string, int> counts, string code) =>
        counts.TryGetValue(code, out var count) && count > 0;

    private static void RemoveTiles(IDictionary<string, int> counts, string code, int amount)
    {
        counts[code] -= amount;
        if (counts[code] <= 0)
        {
            counts.Remove(code);
        }
    }

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

    private static string GetSortKey(string code)
    {
        if (TryGetSuitedTile(code, out var value, out var suit))
        {
            return $"{suit}{value}";
        }

        return $"z{code}";
    }
}

