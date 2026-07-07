using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;
using MahjongPoints.Services.Scoring;

namespace MahjongPoints.Services;

/// <summary>
/// 演示用手牌算点服务，直接接收 14 张胡牌状态手牌并串联四层算点框架。
/// </summary>
public sealed class MahjongHandScoringService : IHandScoringService
{
    /// <summary>
    /// 手牌拆解器。
    /// </summary>
    private readonly IHandSplitter _handSplitter;

    /// <summary>
    /// 役种检测器。
    /// </summary>
    private readonly IYakuDetector _yakuDetector;

    /// <summary>
    /// 符数计算器。
    /// </summary>
    private readonly IFuCalculator _fuCalculator;

    /// <summary>
    /// 点数计算器。
    /// </summary>
    private readonly IScoreCalculator _scoreCalculator;

    /// <summary>
    /// 使用默认四层算点组件创建演示算点服务。
    /// </summary>
    public MahjongHandScoringService()
        : this(
            new DefaultHandSplitter(),
            new DefaultYakuDetector(),
            new DefaultFuCalculator(),
            new DefaultScoreCalculator())
    {
    }

    /// <summary>
    /// 使用指定四层算点组件创建演示算点服务。
    /// </summary>
    /// <param name="handSplitter">手牌拆解器。</param>
    /// <param name="yakuDetector">役种检测器。</param>
    /// <param name="fuCalculator">符数计算器。</param>
    /// <param name="scoreCalculator">点数计算器。</param>
    public MahjongHandScoringService(
        IHandSplitter handSplitter,
        IYakuDetector yakuDetector,
        IFuCalculator fuCalculator,
        IScoreCalculator scoreCalculator)
    {
        _handSplitter = handSplitter;
        _yakuDetector = yakuDetector;
        _fuCalculator = fuCalculator;
        _scoreCalculator = scoreCalculator;
    }

    /// <summary>
    /// 把识别出的 14 张胡牌状态手牌依次送入拆牌、判役、算符和算点流程。
    /// </summary>
    /// <param name="recognizedTiles">识别出的手牌列表。</param>
    /// <param name="context">用户选择的胡牌状态和算点环境。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>演示算点结果。</returns>
    public Task<MahjongScoringResult> CalculateAsync(
        IReadOnlyList<RecognizedMahjongTile> recognizedTiles,
        MahjongScoringContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // ONNX 或 demo 识别器应直接返回 14 张胡牌状态手牌，这里直接使用识别结果。
        var calculationTiles = recognizedTiles.ToArray();

        // 胡牌张来自用户在界面选择后写入的算点上下文。
        var winningTile = context.WinningTile;

        // 四层算点流水线：先拆牌，再判役，再算符，最后把番符换算成点数。
        
        //拆牌
        var splits = ApplySelectedOpenMelds(
            _handSplitter.Split(calculationTiles),
            context.SelectedOpenMelds);
        WriteHandSplitsToConsole(calculationTiles, winningTile, splits);//控制台输出不用管
        if (splits.Count == 0)
        {
            var noSplitResult = new MahjongScoringResult(
                calculationTiles,
                winningTile,
                false,
                context.SelectedOpenMelds.Count == 0
                    ? "No valid 4 melds + 1 pair split."
                    : "No split contains all selected open melds.",
                context.SelectedOpenMelds.Count == 0
                    ? "No valid hand split."
                    : "No split contains all selected open melds.",
                0,
                0,
                0,
                [],
                "Scoring pipeline completed, but the hand could not be split.");
            return Task.FromResult(noSplitResult);
        }

        // 每一个分割的番
        var yakuResults = _yakuDetector.Detect(calculationTiles, splits, context);
       
        // 计算每一个切割的符数
        var fuResult = _fuCalculator.Calculate(calculationTiles, yakuResults[0], context);
        var pointResult = _scoreCalculator.Calculate(yakuResults[0], fuResult, context);

        // 当前 demo 先用“能拆牌、有役、有点数”作为是否和牌的判断条件。
        var isWinningHand = yakuResults[0].Yakus.Count > 0 && pointResult.TotalPoints > 0;
        var winningShape = yakuResults[0].SelectedSplit?.DisplayText ?? "No valid 4 melds + 1 pair split.";
        var message = isWinningHand
            ? "Scoring pipeline completed: split hand, detected yaku, calculated fu, calculated points."
            : "Scoring pipeline completed, but the hand has no valid yaku yet.";

        // 把四层流水线的结果转换成 ViewModel 和界面使用的统一算点结果模型。
        var result = new MahjongScoringResult(
            calculationTiles,
            winningTile,
            isWinningHand,
            winningShape,
            pointResult.Summary,
            pointResult.TotalFan,
            pointResult.Fu,
            pointResult.TotalPoints,
            pointResult.Items,
            message);

        return Task.FromResult(result);
    }

    #region 控制台输出及所有辅助函数

    /// <summary>
    /// 根据用户选择的副露面子过滤候选拆法，并把匹配到的面子标记为副露。
    /// </summary>
    /// <param name="splits">拆牌器枚举出的候选拆法。</param>
    /// <param name="selectedOpenMelds">用户确认的副露面子。</param>
    /// <returns>保留并标记副露后的拆法。</returns>
    private static IReadOnlyList<MahjongHandSplitResult> ApplySelectedOpenMelds(
        IReadOnlyList<MahjongHandSplitResult> splits,
        IReadOnlyList<MahjongMeld> selectedOpenMelds)
    {
        if (selectedOpenMelds.Count == 0)
        {
            return splits;
        }

        var selectedKeys = selectedOpenMelds
            .Select(GetMeldKey)
            .GroupBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var results = new List<MahjongHandSplitResult>();
        foreach (var split in splits.Where(split => split.Shape == MahjongHandShape.Standard))
        {
            var remainingSelectedKeys = new Dictionary<string, int>(selectedKeys, StringComparer.OrdinalIgnoreCase);
            var melds = new List<MahjongMeld>(split.Melds.Count);

            foreach (var meld in split.Melds)
            {
                var meldKey = GetMeldKey(meld);
                if (remainingSelectedKeys.TryGetValue(meldKey, out var remainingCount) && remainingCount > 0)
                {
                    melds.Add(meld with { IsOpen = true });
                    if (remainingCount == 1)
                    {
                        remainingSelectedKeys.Remove(meldKey);
                    }
                    else
                    {
                        remainingSelectedKeys[meldKey] = remainingCount - 1;
                    }

                    continue;
                }

                melds.Add(meld);
            }

            if (remainingSelectedKeys.Count == 0)
            {
                results.Add(split with { Melds = melds });
            }
        }

        return results;
    }

    /// <summary>
    /// 获取面子的匹配键，用于副露面子和拆牌结果之间做内容匹配。
    /// </summary>
    /// <param name="meld">面子。</param>
    /// <returns>稳定匹配键。</returns>
    private static string GetMeldKey(MahjongMeld meld)
    {
        return $"{meld.Type}:{string.Join(",", meld.Tiles.Select(tile => tile.Code).Order(StringComparer.OrdinalIgnoreCase))}";
    }
    
    /// <summary>
    /// 把当前手牌拆解结果输出到控制台，方便先观察拆牌流程是否正确。
    /// </summary>
    /// <param name="calculationTiles">参与算点的 14 张牌。</param>
    /// <param name="winningTile">用户选择的胡牌张。</param>
    /// <param name="splits">拆牌器返回的所有拆解结果。</param>
    private static void WriteHandSplitsToConsole(
        IReadOnlyList<RecognizedMahjongTile> calculationTiles,
        RecognizedMahjongTile winningTile,
        IReadOnlyList<MahjongHandSplitResult> splits)
    {
        var orderedSplits = OrderHandSplits(splits).ToArray();

        Console.WriteLine();
        Console.WriteLine("======================================================================");
        Console.WriteLine(" Hand split combinations");
        Console.WriteLine("======================================================================");
        Console.WriteLine($" Hand          : {FormatTiles(calculationTiles)}");
        Console.WriteLine($" Winning tile  : {FormatTile(winningTile)}");
        Console.WriteLine($" Total splits  : {orderedSplits.Length}");
        Console.WriteLine("----------------------------------------------------------------------");

        if (orderedSplits.Length == 0)
        {
            Console.WriteLine(" No standard 4 melds + 1 pair split found.");
            Console.WriteLine("======================================================================");
            Console.WriteLine();
            return;
        }

        for (var splitIndex = 0; splitIndex < orderedSplits.Length; splitIndex++)
        {
            var split = orderedSplits[splitIndex];
            var orderedMelds = OrderMelds(split.Melds).ToArray();

            Console.WriteLine($" Split {splitIndex + 1:D2}/{orderedSplits.Length:D2}");

            if (split.Shape == MahjongHandShape.SevenPairs)
            {
                Console.WriteLine("   Shape : Seven pairs");
                Console.WriteLine($"   Pairs : {string.Join(" | ", split.Pairs.Select(pair => $"{FormatTile(pair)}  {FormatTile(pair)}"))}");

                if (splitIndex < orderedSplits.Length - 1)
                {
                    Console.WriteLine("----------------------------------------------------------------------");
                }

                continue;
            }

            Console.WriteLine($"   Pair  : {FormatTile(split.Pair)}  {FormatTile(split.Pair)}");
            Console.WriteLine("   Melds :");

            for (var meldIndex = 0; meldIndex < orderedMelds.Length; meldIndex++)
            {
                var meld = orderedMelds[meldIndex];
                Console.WriteLine(
                    $"     {meldIndex + 1}. {GetMeldTypeName(meld.Type),-2} | {FormatTiles(meld.Tiles)}");
            }

            Console.WriteLine($"   Shape : {FormatHandSplit(split, orderedMelds)}");

            if (splitIndex < orderedSplits.Length - 1)
            {
                Console.WriteLine("----------------------------------------------------------------------");
            }
        }

        Console.WriteLine("======================================================================");
        Console.WriteLine();
    }

    /// <summary>
    /// 按雀头和面子内容对拆牌组合排序，保证控制台输出稳定有序。
    /// </summary>
    /// <param name="splits">拆牌器返回的所有拆解结果。</param>
    /// <returns>排序后的拆牌组合。</returns>
    private static IEnumerable<MahjongHandSplitResult> OrderHandSplits(IEnumerable<MahjongHandSplitResult> splits)
    {
        return splits
            .OrderBy(split => split.Shape)
            .ThenBy(split => GetTileSortKey(split.Pair.Code), StringComparer.Ordinal)
            .ThenBy(split => string.Join("|", OrderMelds(split.Melds).Select(GetMeldSortKey)), StringComparer.Ordinal);
    }

    /// <summary>
    /// 按面子类型和牌编码对单个拆法中的面子排序。
    /// </summary>
    /// <param name="melds">一个拆法中的面子列表。</param>
    /// <returns>排序后的面子列表。</returns>
    private static IEnumerable<MahjongMeld> OrderMelds(IEnumerable<MahjongMeld> melds)
    {
        return melds
            .OrderBy(meld => meld.Type)
            .ThenBy(GetMeldSortKey, StringComparer.Ordinal);
    }

    /// <summary>
    /// 把多张牌格式化为控制台展示文本。
    /// </summary>
    /// <param name="tiles">要展示的牌列表。</param>
    /// <returns>格式化后的牌列表文本。</returns>
    private static string FormatTiles(IEnumerable<RecognizedMahjongTile> tiles)
    {
        return string.Join("  ", tiles.Select(FormatTile));
    }

    /// <summary>
    /// 把单张牌格式化为控制台展示文本。
    /// </summary>
    /// <param name="tile">要展示的牌。</param>
    /// <returns>格式化后的单张牌文本。</returns>
    private static string FormatTile(RecognizedMahjongTile tile)
    {
        return $"{GetTileDisplayName(tile.Code)}({tile.Code})";
    }

    /// <summary>
    /// 把完整拆法格式化为一行控制台展示文本。
    /// </summary>
    /// <param name="splitResult">完整拆法。</param>
    /// <param name="orderedMelds">已经排序后的面子列表。</param>
    /// <returns>格式化后的完整拆法文本。</returns>
    private static string FormatHandSplit(
        MahjongHandSplitResult splitResult,
        IReadOnlyList<MahjongMeld> orderedMelds)
    {
        var meldParts = orderedMelds.Select(meld => $"[{GetMeldTypeName(meld.Type)} {FormatTiles(meld.Tiles)}]");
        return string.Join(" + ", meldParts) + $" + [Pair {FormatTile(splitResult.Pair)}  {FormatTile(splitResult.Pair)}]";
    }

    /// <summary>
    /// 获取控制台输出中使用的英文牌名。
    /// </summary>
    /// <param name="code">牌编码。</param>
    /// <returns>英文牌名。</returns>
    private static string GetTileDisplayName(string code)
    {
        if (code.Length != 2)
        {
            return code;
        }

        var rank = code[0];
        var suit = char.ToLowerInvariant(code[1]);

        if (!char.IsDigit(rank))
        {
            return code;
        }

        return suit switch
        {
            'm' => $"{rank} Characters",
            'p' => $"{rank} Dots",
            's' => $"{rank} Bamboo",
            'z' => GetHonorTileDisplayName(rank),
            _ => code
        };
    }

    /// <summary>
    /// 获取字牌编码对应的英文牌名。
    /// </summary>
    /// <param name="rank">字牌编码中的数字。</param>
    /// <returns>英文牌名。</returns>
    private static string GetHonorTileDisplayName(char rank)
    {
        return rank switch
        {
            '1' => "East",
            '2' => "South",
            '3' => "West",
            '4' => "North",
            '5' => "White Dragon",
            '6' => "Green Dragon",
            '7' => "Red Dragon",
            _ => rank.ToString()
        };
    }

    /// <summary>
    /// 获取面子的排序键。
    /// </summary>
    /// <param name="meld">面子。</param>
    /// <returns>面子排序键。</returns>
    private static string GetMeldSortKey(MahjongMeld meld)
    {
        return $"{(int)meld.Type}:{string.Join(",", meld.Tiles.Select(tile => GetTileSortKey(tile.Code)))}";
    }

    /// <summary>
    /// 获取牌编码的排序键，让控制台里的牌按万、筒、索、字牌顺序展示。
    /// </summary>
    /// <param name="code">牌编码。</param>
    /// <returns>牌排序键。</returns>
    private static string GetTileSortKey(string code)
    {
        if (code.Length == 2 && char.IsDigit(code[0]))
        {
            return code[1] switch
            {
                'm' or 'M' => $"0{code[0]}",
                'p' or 'P' => $"1{code[0]}",
                's' or 'S' => $"2{code[0]}",
                'z' or 'Z' => $"3{code[0]}",
                _ => $"9{code}"
            };
        }

        return $"9{code}";
    }

    /// <summary>
    /// 获取面子类型的中文名称。
    /// </summary>
    /// <param name="type">面子类型。</param>
    /// <returns>面子类型中文名称。</returns>
    private static string GetMeldTypeName(MahjongMeldType type)
    {
        return type switch
        {
            MahjongMeldType.Sequence => "Sequence",
            MahjongMeldType.Triplet => "Triplet",
            MahjongMeldType.Quad => "Quad",
            _ => "Unknown"
        };
    }
    
    #endregion
}
