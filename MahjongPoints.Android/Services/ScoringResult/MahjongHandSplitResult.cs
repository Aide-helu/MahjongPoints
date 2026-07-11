using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 表示手牌拆解形状。
/// </summary>
public enum MahjongHandShape
{
    /// <summary>
    /// 普通型
    /// </summary>
    Standard,
    /// <summary>
    /// 七对子
    /// </summary>
    SevenPairs,
    /// <summary>
    /// 役满
    /// </summary>
    YaKuMan
}

/// <summary>
/// 表示一组 4 面子加 1 雀头的手牌拆解结果。
/// </summary>
/// <param name="Melds">拆出的面子列表。</param>
/// <param name="Pair">雀头牌。</param>
/// <param name="Shape">分割类型（默认Standard）</param>
/// <param name="PairTiles">七对子等特殊牌型中的对子牌列表。</param>
public sealed record MahjongHandSplitResult(
    IReadOnlyList<MahjongMeld> Melds,
    RecognizedMahjongTile Pair,
    MahjongHandShape Shape = MahjongHandShape.Standard,
    IReadOnlyList<RecognizedMahjongTile>? PairTiles = null)
{
    /// <summary>
    /// 当前拆牌结果中的对子列表；普通型只有雀头，七对子有七个对子。
    /// </summary>
    public IReadOnlyList<RecognizedMahjongTile> Pairs => PairTiles ?? [Pair];

    /// <summary>
    /// 供界面展示的拆牌文本。
    /// </summary>
    public string DisplayText =>
        Shape == MahjongHandShape.SevenPairs
            ? string.Join(" + ", Pairs.Select(pair => $"{pair.Code} {pair.Code}"))
            : string.Join(" + ", Melds.Select(meld => meld.DisplayText)) + $" + {Pair.Code} {Pair.Code}";
}
