using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

public sealed record MahjongScoringContext(
    RecognizedMahjongTile WinningTile,
    bool IsTsumo = false,
    bool IsParent = false,
    bool IsMenzen = true,
    int RiichiSticks = 0);

