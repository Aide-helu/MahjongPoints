namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 表示一个麻将役种。
/// </summary>
/// <param name="Id">役种唯一标识。</param>
/// <param name="Name">役种名称。</param>
/// <param name="Fan">役种番数。</param>
/// <param name="Description">役种说明。</param>
public sealed record MahjongYaku(
    string Id,
    string Name,
    int Fan,
    string Description);
