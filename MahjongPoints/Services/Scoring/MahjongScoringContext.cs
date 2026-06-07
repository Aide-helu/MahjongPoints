using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 表示一次算点需要的环境信息。
/// </summary>
/// <param name="IsTsumo">是否自摸。</param>
/// <param name="IsParent">是否亲家。</param>
/// <param name="IsMenzen">是否门前清。</param>
/// <param name="RiichiSticks">可收入的立直棒数量。</param>
public sealed record MahjongScoringContext(
    bool IsTsumo = false,
    bool IsParent = false,
    bool IsMenzen = true,
    int RiichiSticks = 0);
