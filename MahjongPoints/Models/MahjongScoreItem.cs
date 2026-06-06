namespace MahjongPoints.Models;

public sealed record MahjongScoreItem(
    string Name,
    int Fan,
    int Fu,
    int Points,
    string Description);
