using System;
using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 默认符数计算器，按照符数计算规则.md 中记录的规则计算符数。
/// </summary>
public sealed class DefaultFuCalculator : IFuCalculator
{
    /// <summary>
    /// 根据役种结果计算符数。
    /// </summary>
    /// <param name="tiles">参与算点的完整牌列表。</param>
    /// <param name="yakuResult">役种检测结果。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>符数计算结果。</returns>
    public FuCalculationResult Calculate(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        YakuDetectionResult yakuResult,
        MahjongScoringContext context)
    {
        var split = yakuResult.SelectedSplit;
        if (split is null)
        {
            return new FuCalculationResult(0, ["No valid hand split."], null);
        }

        var breakdown = new List<string>();

        if (split.Shape == MahjongHandShape.SevenPairs)
        {
            breakdown.Add("七对子固定 25 符。");
            return new FuCalculationResult(25, breakdown, split);
        }

        if (IsPingHuTsumo(yakuResult, context))
        {
            breakdown.Add("平和自摸固定 20 符。");
            return new FuCalculationResult(20, breakdown, split);
        }

        if (IsOpenPinFuShape(split, context))
        {
            breakdown.Add("副露平和形固定 30 符。");
            return new FuCalculationResult(30, breakdown, split);
        }

        var rawFu = 20;
        breakdown.Add("底符 +20。");

        var pairFu = CalculateValuePairFu(split.Pair, context);
        if (pairFu > 0)
        {
            rawFu += pairFu;
            breakdown.Add($"役牌雀头 {split.Pair.Code} +{pairFu}。");
        }

        foreach (var meld in split.Melds)
        {
            var meldFu = CalculateMeldFu(meld, split, context, out var meldDescription);
            if (meldFu <= 0)
            {
                continue;
            }

            rawFu += meldFu;
            breakdown.Add(meldDescription);
        }

        var waitFu = CalculateWaitFu(split, context, out var waitDescription);
        if (waitFu > 0)
        {
            rawFu += waitFu;
            breakdown.Add(waitDescription);
        }

        if (context.IsTsumo)
        {
            rawFu += 2;
            breakdown.Add("自摸 +2。");
        }

        if (IsMenzenRon(context))
        {
            rawFu += 10;
            breakdown.Add("门前清荣和 +10。");
        }

        var roundedFu = RoundUpToTen(rawFu);
        breakdown.Add(roundedFu == rawFu
            ? $"合计 {roundedFu} 符。"
            : $"合计 {rawFu} 符，向上取整为 {roundedFu} 符。");

        return new FuCalculationResult(
            roundedFu,
            breakdown,
            split);
    }

    /// <summary>
    /// 判断是否为平和自摸。
    /// </summary>
    /// <param name="yakuResult">役种检测结果。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>如果满足平和自摸固定 20 符，则返回 <c>true</c>。</returns>
    private static bool IsPingHuTsumo(YakuDetectionResult yakuResult, MahjongScoringContext context)
    {
        return context.IsTsumo &&
               yakuResult.Yakus.Any(yaku =>
                   string.Equals(yaku.Id, "pinghu", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 判断是否为副露平和形。
    /// </summary>
    /// <param name="split">手牌拆解结果。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>如果是副露后的全顺子、非役牌雀头且无等待符形，则返回 <c>true</c>。</returns>
    private static bool IsOpenPinFuShape(MahjongHandSplitResult split, MahjongScoringContext context)
    {
        if (split.Shape != MahjongHandShape.Standard ||
            !context.IsOpenHand ||
            split.Melds.Any(meld => meld.Type != MahjongMeldType.Sequence) ||
            IsValuePair(split.Pair, context))
        {
            return false;
        }

        return CalculateWaitFu(split, context, out _) == 0;
    }

    /// <summary>
    /// 计算役牌雀头符。
    /// </summary>
    /// <param name="pair">雀头牌。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>役牌雀头符数。</returns>
    private static int CalculateValuePairFu(RecognizedMahjongTile pair, MahjongScoringContext context)
    {
        var fu = 0;
        var pairCode = pair.Code;

        if (IsDragon(pairCode))
        {
            fu += 2;
        }

        if (string.Equals(pairCode, context.SelfWindTileCode, StringComparison.OrdinalIgnoreCase))
        {
            fu += 2;
        }

        if (string.Equals(pairCode, context.RoundWindTileCode, StringComparison.OrdinalIgnoreCase))
        {
            fu += 2;
        }

        return fu;
    }

    /// <summary>
    /// 判断雀头是否为役牌雀头。
    /// </summary>
    /// <param name="pair">雀头牌。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>如果雀头为三元牌、自风或场风，则返回 <c>true</c>。</returns>
    private static bool IsValuePair(RecognizedMahjongTile pair, MahjongScoringContext context) =>
        CalculateValuePairFu(pair, context) > 0;

    /// <summary>
    /// 计算刻子或杠子的符数。
    /// </summary>
    /// <param name="meld">面子。</param>
    /// <param name="split">手牌拆解结果。</param>
    /// <param name="context">算点上下文。</param>
    /// <param name="description">符数明细描述。</param>
    /// <returns>该面子的符数。</returns>
    private static int CalculateMeldFu(
        MahjongMeld meld,
        MahjongHandSplitResult split,
        MahjongScoringContext context,
        out string description)
    {
        description = string.Empty;

        if (meld.Type == MahjongMeldType.Sequence)
        {
            return 0;
        }

        var isTerminalOrHonor = IsTerminalOrHonor(meld.Tiles[0].Code);
        var isConcealed = IsConcealedMeld(meld, split, context);

        var fu = meld.Type switch
        {
            MahjongMeldType.Triplet => 2,
            MahjongMeldType.Quad => 8,
            _ => 0
        };

        if (isConcealed)
        {
            fu *= 2;
        }

        if (isTerminalOrHonor)
        {
            fu *= 2;
        }

        var visibilityText = (meld.Type, isConcealed) switch
        {
            (MahjongMeldType.Triplet, true) => "暗刻",
            (MahjongMeldType.Triplet, false) => "明刻",
            (MahjongMeldType.Quad, true) => "暗杠",
            (MahjongMeldType.Quad, false) => "明杠",
            _ => "面子"
        };

        var tileTypeText = isTerminalOrHonor ? "幺九牌/字牌" : "中张牌";
        description = $"{visibilityText}（{tileTypeText}）{meld.DisplayText} +{fu}。";
        return fu;
    }

    /// <summary>
    /// 判断面子是否按暗刻或暗杠计算。
    /// </summary>
    /// <param name="meld">面子。</param>
    /// <param name="split">手牌拆解结果。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>如果该面子按暗面子计算，则返回 <c>true</c>。</returns>
    private static bool IsConcealedMeld(
        MahjongMeld meld,
        MahjongHandSplitResult split,
        MahjongScoringContext context)
    {
        if (meld.IsOpen)
        {
            return false;
        }

        if (meld.Type == MahjongMeldType.Quad || context.IsTsumo)
        {
            return true;
        }

        if (meld.Type != MahjongMeldType.Triplet || IsUnknownTile(context.WinningTile.Code))
        {
            return true;
        }

        var winningCode = context.WinningTile.Code;
        var ronCompletedThisTriplet = meld.Tiles.Any(tile =>
            string.Equals(tile.Code, winningCode, StringComparison.OrdinalIgnoreCase)) &&
            !CanAssignWinningTileOutsideMeld(split, meld, winningCode);

        return !ronCompletedThisTriplet;
    }

    /// <summary>
    /// 判断和牌张是否能解释到当前刻子外的雀头或顺子中。
    /// </summary>
    /// <param name="split">手牌拆解结果。</param>
    /// <param name="currentMeld">当前刻子。</param>
    /// <param name="winningCode">和牌张编码。</param>
    /// <returns>如果和牌张能解释为其他部分，则返回 <c>true</c>。</returns>
    private static bool CanAssignWinningTileOutsideMeld(
        MahjongHandSplitResult split,
        MahjongMeld currentMeld,
        string winningCode)
    {
        if (string.Equals(split.Pair.Code, winningCode, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return split.Melds.Any(meld =>
            !ReferenceEquals(meld, currentMeld) &&
            meld.Type == MahjongMeldType.Sequence &&
            meld.Tiles.Any(tile => string.Equals(tile.Code, winningCode, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// 计算等待形符。
    /// </summary>
    /// <param name="split">手牌拆解结果。</param>
    /// <param name="context">算点上下文。</param>
    /// <param name="description">符数明细描述。</param>
    /// <returns>等待形符数。</returns>
    private static int CalculateWaitFu(
        MahjongHandSplitResult split,
        MahjongScoringContext context,
        out string description)
    {
        description = string.Empty;

        var waitType = DetectWaitFuType(split, context);
        description = waitType switch
        {
            WaitFuType.Pair => "单骑听牌 +2。",
            WaitFuType.Edge => "边张听牌 +2。",
            WaitFuType.Closed => "嵌张听牌 +2。",
            _ => string.Empty
        };

        return waitType == WaitFuType.None ? 0 : 2;
    }

    /// <summary>
    /// 检测当前拆法下的等待形。
    /// </summary>
    /// <param name="split">手牌拆解结果。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>等待形类型。</returns>
    private static WaitFuType DetectWaitFuType(MahjongHandSplitResult split, MahjongScoringContext context)
    {
        if (split.Shape != MahjongHandShape.Standard || IsUnknownTile(context.WinningTile.Code))
        {
            return WaitFuType.None;
        }

        var winningCode = context.WinningTile.Code;
        var valuedSequenceWait = WaitFuType.None;

        foreach (var meld in split.Melds.Where(meld => meld.Type == MahjongMeldType.Sequence))
        {
            if (!meld.Tiles.Any(tile => string.Equals(tile.Code, winningCode, StringComparison.OrdinalIgnoreCase)) ||
                !TryGetSequenceStart(meld, out var start, out var suit) ||
                !TryGetSuitedTile(winningCode, out var winningValue, out var winningSuit) ||
                suit != winningSuit)
            {
                continue;
            }

            if (IsTwoSidedWait(start, winningValue))
            {
                return WaitFuType.None;
            }

            if (winningValue == start + 1)
            {
                valuedSequenceWait = WaitFuType.Closed;
                continue;
            }

            if (start == 1 && winningValue == 3 ||
                start == 7 && winningValue == 7)
            {
                valuedSequenceWait = WaitFuType.Edge;
            }
        }

        if (string.Equals(split.Pair.Code, winningCode, StringComparison.OrdinalIgnoreCase))
        {
            return WaitFuType.Pair;
        }

        return valuedSequenceWait;
    }

    /// <summary>
    /// 判断顺子和牌张是否为两面听。
    /// </summary>
    /// <param name="sequenceStart">顺子起始数字。</param>
    /// <param name="winningValue">和牌张数字。</param>
    /// <returns>如果该顺子解释为两面听，则返回 <c>true</c>。</returns>
    private static bool IsTwoSidedWait(int sequenceStart, int winningValue)
    {
        return winningValue == sequenceStart && sequenceStart != 7 ||
               winningValue == sequenceStart + 2 && sequenceStart != 1;
    }

    /// <summary>
    /// 判断是否为门前清荣和。
    /// </summary>
    /// <param name="context">算点上下文。</param>
    /// <returns>如果满足门前清荣和加符条件，则返回 <c>true</c>。</returns>
    private static bool IsMenzenRon(MahjongScoringContext context) =>
        !context.IsTsumo && !context.IsOpenHand;

    /// <summary>
    /// 判断是否为三元牌。
    /// </summary>
    /// <param name="code">牌编码。</param>
    /// <returns>如果是白、发、中，则返回 <c>true</c>。</returns>
    private static bool IsDragon(string code) =>
        string.Equals(code, "5z", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "6z", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "7z", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 判断是否为幺九牌或字牌。
    /// </summary>
    /// <param name="code">牌编码。</param>
    /// <returns>如果是 1、9 或字牌，则返回 <c>true</c>。</returns>
    private static bool IsTerminalOrHonor(string code)
    {
        if (code.Length != 2)
        {
            return false;
        }

        return code[1] == 'z' || code[0] is '1' or '9';
    }

    /// <summary>
    /// 尝试读取顺子的起始数字和花色。
    /// </summary>
    /// <param name="meld">顺子面子。</param>
    /// <param name="start">顺子起始数字。</param>
    /// <param name="suit">顺子花色。</param>
    /// <returns>如果该面子是合法顺子，则返回 <c>true</c>。</returns>
    private static bool TryGetSequenceStart(MahjongMeld meld, out int start, out char suit)
    {
        start = 0;
        suit = '\0';

        var suitedTiles = new List<(int Value, char Suit)>();
        foreach (var tile in meld.Tiles)
        {
            if (!TryGetSuitedTile(tile.Code, out var value, out var tileSuit))
            {
                return false;
            }

            suitedTiles.Add((value, tileSuit));
        }

        if (suitedTiles.Count != 3 || suitedTiles.Select(tile => tile.Suit).Distinct().Count() != 1)
        {
            return false;
        }

        var values = suitedTiles.Select(tile => tile.Value).Order().ToArray();
        if (values[1] != values[0] + 1 || values[2] != values[0] + 2)
        {
            return false;
        }

        start = values[0];
        suit = suitedTiles[0].Suit;
        return true;
    }

    /// <summary>
    /// 尝试把牌编码解析为数牌。
    /// </summary>
    /// <param name="code">牌编码。</param>
    /// <param name="value">数牌数字。</param>
    /// <param name="suit">数牌花色。</param>
    /// <returns>如果牌编码是合法数牌，则返回 <c>true</c>。</returns>
    private static bool TryGetSuitedTile(string code, out int value, out char suit)
    {
        value = 0;
        suit = '\0';

        if (code.Length != 2 || !char.IsDigit(code[0]) || code[1] is not ('m' or 'p' or 's'))
        {
            return false;
        }

        value = code[0] - '0';
        suit = code[1];
        return value is >= 1 and <= 9;
    }

    /// <summary>
    /// 判断是否为未知和牌张占位。
    /// </summary>
    /// <param name="code">牌编码。</param>
    /// <returns>如果是未知牌，则返回 <c>true</c>。</returns>
    private static bool IsUnknownTile(string code) =>
        string.Equals(code, "unknown", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 向上取整到十位。
    /// </summary>
    /// <param name="fu">原始符数。</param>
    /// <returns>取整后的符数。</returns>
    private static int RoundUpToTen(int fu) =>
        (int)Math.Ceiling(fu / 10.0) * 10;

    /// <summary>
    /// 表示会加 2 符的等待形。
    /// </summary>
    private enum WaitFuType
    {
        /// <summary>
        /// 无等待形加符。
        /// </summary>
        None,

        /// <summary>
        /// 单骑。
        /// </summary>
        Pair,

        /// <summary>
        /// 边张。
        /// </summary>
        Edge,

        /// <summary>
        /// 嵌张。
        /// </summary>
        Closed
    }
}
