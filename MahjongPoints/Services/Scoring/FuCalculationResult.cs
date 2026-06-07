using System.Collections.Generic;

namespace MahjongPoints.Services.Scoring;

public sealed record FuCalculationResult(
    int Fu,
    IReadOnlyList<string> Breakdown,
    MahjongHandSplit? SelectedSplit);

