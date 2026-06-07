using System.Collections.Generic;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

public interface IHandSplitter
{
    IReadOnlyList<MahjongHandSplit> Split(IReadOnlyList<RecognizedMahjongTile> tiles);
}

