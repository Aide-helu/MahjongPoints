using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

public enum MahjongMeldType
{
    Sequence,
    Triplet,
    Quad
}

public sealed record MahjongMeld(
    MahjongMeldType Type,
    IReadOnlyList<RecognizedMahjongTile> Tiles)
{
    public string DisplayText => string.Join(" ", Tiles.Select(tile => tile.Code));
}

