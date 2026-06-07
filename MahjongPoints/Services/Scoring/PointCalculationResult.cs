using System.Collections.Generic;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

public sealed record PointCalculationResult(
    int TotalFan,
    int Fu,
    int TotalPoints,
    string Summary,
    IReadOnlyList<MahjongScoreItem> Items,
    IReadOnlyList<string> Breakdown);

