using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

public sealed record MahjongHandSplit(
    IReadOnlyList<MahjongMeld> Melds,
    RecognizedMahjongTile Pair)
{
    public string DisplayText =>
        string.Join(" + ", Melds.Select(meld => meld.DisplayText)) + $" + {Pair.Code} {Pair.Code}";
}

