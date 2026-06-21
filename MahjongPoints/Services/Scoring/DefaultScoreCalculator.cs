using System;
using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 默认点数计算器，根据番数、符数、亲子身份和和牌方式换算最终收入点数。
/// </summary>
public sealed class DefaultScoreCalculator : IScoreCalculator
{
    /// <summary>
    /// 根据役种、符数和算点上下文计算最终点数。
    /// </summary>
    /// <param name="yakuResult">役种检测结果。</param>
    /// <param name="fuResult">符数计算结果。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>点数计算结果。</returns>
    public PointCalculationResult Calculate(
        YakuDetectionResult yakuResult,
        FuCalculationResult fuResult,
        MahjongScoringContext context)
    {
        var fan = yakuResult.TotalFan;
        var fu = fuResult.Fu;
        var breakdown = new List<string>(fuResult.Breakdown);

        if (fan <= 0 || fu <= 0)
        {
            return new PointCalculationResult(
                fan,
                fu,
                0,
                "No yaku or invalid fu.",
                [],
                breakdown);
        }

        var score = CalculateScore(fan, fu, context);
        var riichiStickPoints = context.RiichiSticks * 1000;
        var totalPoints = score.TotalPoints + riichiStickPoints;

        breakdown.Add($"番数：{fan}。");
        breakdown.Add($"符数：{fu}。");
        breakdown.Add(score.BasePointDescription);
        breakdown.Add(score.PaymentDescription);
        if (riichiStickPoints > 0)
        {
            breakdown.Add($"立直棒收入：{context.RiichiSticks} 本，共 +{riichiStickPoints} 点。");
        }

        breakdown.Add($"最终获得总点数：{totalPoints} 点。");

        var items = yakuResult.Yakus
            .Select(yaku => new MahjongScoreItem(
                yaku.Name,
                yaku.Fan,
                fu,
                totalPoints,
                yaku.Description))
            .ToArray();

        var summary = $"{string.Join(", ", yakuResult.Yakus.Select(yaku => yaku.Name))} | {fan}番 {fu}符 | {score.Summary} | 总收入 {totalPoints}点";

        return new PointCalculationResult(
            fan,
            fu,
            totalPoints,
            summary,
            items,
            breakdown);
    }

    /// <summary>
    /// 根据规则计算荣和或自摸时的总收入。
    /// </summary>
    /// <param name="fan">番数。</param>
    /// <param name="fu">符数。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>点数计算明细。</returns>
    private static ScoreCalculation CalculateScore(int fan, int fu, MahjongScoringContext context)
    {
        var basePoint = CalculateBasePoint(fan, fu, out var basePointDescription);

        if (!context.IsTsumo)
        {
            var multiplier = context.IsParent ? 6 : 4;
            var payment = RoundUpToHundred(basePoint * multiplier);
            var winnerText = context.IsParent ? "庄家荣和" : "闲家荣和";
            var paymentDescription = $"{winnerText}：放铳者支付 {multiplier}a = {basePoint * multiplier}，向上取整为 {payment} 点。";

            return new ScoreCalculation(
                payment,
                $"{winnerText} {payment}点",
                basePointDescription,
                paymentDescription);
        }

        if (context.IsParent)
        {
            var paymentPerPlayer = RoundUpToHundred(basePoint * 2);
            var totalPoints = paymentPerPlayer * 3;
            var paymentDescription = $"庄家自摸：每家支付 2a = {basePoint * 2}，每家向上取整为 {paymentPerPlayer} 点，总收入 {paymentPerPlayer} x 3 = {totalPoints} 点。";

            return new ScoreCalculation(
                totalPoints,
                $"庄家自摸 每家{paymentPerPlayer}点",
                basePointDescription,
                paymentDescription);
        }

        var childPayment = RoundUpToHundred(basePoint);
        var parentPayment = RoundUpToHundred(basePoint * 2);
        var childTsumoTotalPoints = childPayment * 2 + parentPayment;
        var childPaymentDescription = $"闲家自摸：闲家各支付 a = {basePoint}，各自向上取整为 {childPayment} 点；庄家支付 2a = {basePoint * 2}，向上取整为 {parentPayment} 点；总收入 {childPayment} x 2 + {parentPayment} = {childTsumoTotalPoints} 点。";

        return new ScoreCalculation(
            childTsumoTotalPoints,
            $"闲家自摸 闲家{childPayment}点/庄家{parentPayment}点",
            basePointDescription,
            childPaymentDescription);
    }

    /// <summary>
    /// 根据番数和符数计算基础点 a。
    /// </summary>
    /// <param name="fan">番数。</param>
    /// <param name="fu">符数。</param>
    /// <param name="description">基础点计算说明。</param>
    /// <returns>基础点 a。</returns>
    private static int CalculateBasePoint(int fan, int fu, out string description)
    {
        if (fan <= 4)
        {
            var rawBasePoint = fu * (int)Math.Pow(2, fan + 2);
            if (rawBasePoint >= 2000)
            {
                description = $"基础点：a = {fu} x 2^({fan} + 2) = {rawBasePoint}，达到满贯，按 a = 2000 计算。";
                return 2000;
            }

            description = $"基础点：a = {fu} x 2^({fan} + 2) = {rawBasePoint}。";
            return rawBasePoint;
        }

        var limitBasePoint = fan switch
        {
            5 => 2000,
            6 or 7 => 3000,
            >= 8 and <= 10 => 4000,
            11 or 12 => 6000,
            _ => 8000
        };

        var limitName = fan switch
        {
            5 => "满贯",
            6 or 7 => "跳满",
            >= 8 and <= 10 => "倍满",
            11 or 12 => "三倍满",
            _ => "役满"
        };

        description = $"基础点：{fan} 番为{limitName}，按 a = {limitBasePoint} 计算。";
        return limitBasePoint;
    }

    /// <summary>
    /// 将点数向上取整到百位。
    /// </summary>
    /// <param name="points">原始点数。</param>
    /// <returns>百位向上取整后的点数。</returns>
    private static int RoundUpToHundred(int points) =>
        (int)Math.Ceiling(points / 100.0) * 100;

    /// <summary>
    /// 表示一次点数计算得到的收入和展示文本。
    /// </summary>
    /// <param name="TotalPoints">不含立直棒的和牌收入。</param>
    /// <param name="Summary">界面摘要。</param>
    /// <param name="BasePointDescription">基础点计算说明。</param>
    /// <param name="PaymentDescription">支付计算说明。</param>
    private sealed record ScoreCalculation(
        int TotalPoints,
        string Summary,
        string BasePointDescription,
        string PaymentDescription);
}
