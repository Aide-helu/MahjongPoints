using System.Collections.Generic;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

public interface IYakuDetector
{
    YakuDetectionResult Detect(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        IReadOnlyList<MahjongHandSplit> splits,
        MahjongScoringContext context);
}

