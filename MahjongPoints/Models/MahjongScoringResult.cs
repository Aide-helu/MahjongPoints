using System.Collections.Generic;

namespace MahjongPoints.Models;

/// <summary>
/// 表示一手牌的算点结果。
/// </summary>
/// <param name="CalculationTiles">参与算点的完整牌列表。</param>
/// <param name="WinningTile">补入或标记的和牌张。</param>
/// <param name="IsWinningHand">是否满足当前算点逻辑下的和牌条件。</param>
/// <param name="WinningShape">牌型或拆牌结果说明。</param>
/// <param name="ScoreSummary">用于界面展示的点数摘要。</param>
/// <param name="TotalFan">总番数。</param>
/// <param name="Fu">符数。</param>
/// <param name="TotalPoints">总点数。</param>
/// <param name="Items">役种或得分明细。</param>
/// <param name="Message">算点流程提示信息。</param>
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
