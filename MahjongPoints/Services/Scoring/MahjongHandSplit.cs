using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 表示一组 4 面子加 1 雀头的手牌拆解结果。
/// </summary>
/// <param name="Melds">拆出的面子列表。</param>
/// <param name="Pair">雀头牌。</param>
public sealed record MahjongHandSplit(
    IReadOnlyList<MahjongMeld> Melds,
    RecognizedMahjongTile Pair)
{
    /// <summary>
    /// 供界面展示的拆牌文本。
    /// </summary>
    public string DisplayText =>
        string.Join(" + ", Melds.Select(meld => meld.DisplayText)) + $" + {Pair.Code} {Pair.Code}";
}
