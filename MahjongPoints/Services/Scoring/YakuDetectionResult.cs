using System.Collections.Generic;
using System.Linq;

namespace MahjongPoints.Services.Scoring;

public sealed record YakuDetectionResult(
    IReadOnlyList<MahjongYaku> Yakus,
    MahjongHandSplit? SelectedSplit)
{
    public int TotalFan => Yakus.Sum(yaku => yaku.Fan);
}

