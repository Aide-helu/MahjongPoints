using System.Collections.Generic;

namespace MahjongPoints.Models;

public sealed record MahjongScoringResult(
    IReadOnlyList<RecognizedMahjongTile> CalculationTiles,
    RecognizedMahjongTile WinningTile,
    bool IsWinningHand,
    string WinningShape,
    string ScoreSummary,
    int TotalFan,
    int Fu,
    int TotalPoints,
    IReadOnlyList<MahjongScoreItem> Items,
    string Message);
