namespace MahjongPoints.Models;

/// <summary>
/// 表示算点结果中的一条役种或得分明细。
/// </summary>
/// <param name="Name">明细名称。</param>
/// <param name="Fan">该项对应番数。</param>
/// <param name="Fu">该项使用的符数。</param>
/// <param name="Points">该项对应点数。</param>
/// <param name="Description">明细说明。</param>
public sealed record MahjongScoreItem(
    string Name,
    int Fan,
    int Fu,
    int Points,
    string Description);
