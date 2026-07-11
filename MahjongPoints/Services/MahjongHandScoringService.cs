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
    private static readonly string[] _allTileCodes =
    [
        "1m", "2m", "3m", "4m", "5m", "6m", "7m", "8m", "9m",
        "1p", "2p", "3p", "4p", "5p", "6p", "7p", "8p", "9p",
        "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s",
        "1z", "2z", "3z", "4z", "5z", "6z", "7z"
    ];

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
    /// 为 13 张手牌查找所有能组成可拆牌型的听牌张。
    /// </summary>
    /// <param name="recognizedTiles">当前识别出的 13 张手牌。</param>
    /// <returns>可补成有效拆牌形状的候选胡牌张。</returns>
    public IReadOnlyList<RecognizedMahjongTile> FindTenpaiTiles(IReadOnlyList<RecognizedMahjongTile> recognizedTiles)
    {
        if (recognizedTiles.Count != 13)
        {
            return [];
        }

        var counts = recognizedTiles
            .GroupBy(tile => tile.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var results = new List<RecognizedMahjongTile>();
        foreach (var code in _allTileCodes)
        {
            if (counts.GetValueOrDefault(code) >= 4)
            {
                continue;
            }

            var tile = CreateTile(code);
            if (_handSplitter.Split(recognizedTiles.Concat([tile]).ToArray()).Count > 0)
            {
                results.Add(tile);
            }
        }

        return results;
    }

    /// <summary>
    /// 为 14 张手牌查找所有“打出一张后可听”的候选。
    /// </summary>
    /// <param name="recognizedTiles">当前识别出的 14 张手牌。</param>
    /// <returns>按弃牌分组的听牌候选。</returns>
    public IReadOnlyList<TenpaiDiscardOption> FindTenpaiDiscardOptions(IReadOnlyList<RecognizedMahjongTile> recognizedTiles)
    {
        if (recognizedTiles.Count != 14)
        {
            return [];
        }

        var results = new List<TenpaiDiscardOption>();
        foreach (var discardTile in recognizedTiles.DistinctBy(tile => tile.Code, StringComparer.OrdinalIgnoreCase))
        {
            var remainingTiles = RemoveOneTile(recognizedTiles, discardTile.Code);
            var winningTiles = FindTenpaiTiles(remainingTiles);
            if (winningTiles.Count > 0)
            {
                results.Add(new TenpaiDiscardOption(discardTile, winningTiles, remainingTiles));
            }
        }

        return results;
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
        var calculationTiles = recognizedTiles.Count == 13 && IsKnownTile(context.WinningTile)
            ? recognizedTiles.Concat([context.WinningTile]).ToArray()
            : recognizedTiles.ToArray();
        if (context.DeclaredKans.Count > 0)
        {
            var normalizedTiles = NormalizeDeclaredKans(recognizedTiles, context.DeclaredKans);
            calculationTiles = normalizedTiles.Count == 13 && IsKnownTile(context.WinningTile)
                ? normalizedTiles.Concat([context.WinningTile]).ToArray()
                : normalizedTiles.ToArray();
        }

        // 胡牌张来自用户在界面选择后写入的算点上下文。
        var winningTile = context.WinningTile;

        // 四层算点流水线：先拆牌，再判役，再算符，最后把番符换算成点数。
        var splits = ApplyDeclaredMelds(
            _handSplitter.Split(calculationTiles),
            context.SelectedOpenMelds,
            context.DeclaredKans);
        if (splits.Count == 0)
        {
            var noSplitResult = new MahjongScoringResult(
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
                0);
            return Task.FromResult(noSplitResult);
        }

        var scoredResults = _yakuDetector
            .Detect(calculationTiles, splits, context)
            .Select(yakuResult =>
            {
                var fuResult = _fuCalculator.Calculate(calculationTiles, yakuResult, context);
                var pointResult = _scoreCalculator.Calculate(yakuResult, fuResult, context);
                return new { YakuResult = yakuResult, FuResult = fuResult, PointResult = pointResult };
            })
            .ToArray();

        var selected = scoredResults
            .OrderByDescending(result => result.PointResult.TotalPoints)
            .First();

        // 当前 demo 先用“能拆牌、有役、有点数”作为是否和牌的判断条件。
        var isWinningHand = selected.YakuResult.Yakus.Count > 0 && selected.PointResult.TotalPoints > 0;
        var winningShape = selected.YakuResult.SelectedSplit?.DisplayText ?? "No valid 4 melds + 1 pair split.";

        
        // 把四层流水线的结果转换成 ViewModel 和界面使用的统一算点结果模型。
        var result = new MahjongScoringResult(
            winningTile,
            isWinningHand,
            winningShape,
            selected.PointResult.Summary,
            selected.PointResult.TotalFan,
            selected.PointResult.Fu,
            selected.PointResult.TotalPoints);

        return Task.FromResult(result);
    }

    #region 辅助函数

    /// <summary>
    /// 把声明杠转换成拆牌器能处理的三张刻子形态。
    /// </summary>
    /// <param name="tiles">识别出的物理牌列表。</param>
    /// <param name="declaredKans">用户声明的杠。</param>
    /// <returns>用于拆牌的归一化牌列表。</returns>
    private static IReadOnlyList<RecognizedMahjongTile> NormalizeDeclaredKans(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        IReadOnlyList<MahjongMeld> declaredKans)
    {
        var results = tiles.ToList();
        foreach (var kan in declaredKans.Where(meld => meld.Type == MahjongMeldType.Quad))
        {
            var code = kan.Tiles[0].Code;
            var count = results.Count(tile => string.Equals(tile.Code, code, StringComparison.OrdinalIgnoreCase));
            while (count > 3)
            {
                var index = results.FindLastIndex(tile => string.Equals(tile.Code, code, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                {
                    break;
                }

                results.RemoveAt(index);
                count--;
            }

            while (count < 3)
            {
                results.Add(CreateTile(code));
                count++;
            }
        }

        return results;
    }

    /// <summary>
    /// 过滤必须包含用户声明杠的拆法，并把对应刻子替换回四张杠面子。
    /// </summary>
    /// <param name="splits">拆牌器枚举出的候选拆法。</param>
    /// <param name="selectedOpenMelds">用户确认的副露面子。</param>
    /// <param name="declaredKans">用户声明的杠。</param>
    /// <returns>保留并标记杠和副露后的拆法。</returns>
    private static IReadOnlyList<MahjongHandSplitResult> ApplyDeclaredMelds(
        IReadOnlyList<MahjongHandSplitResult> splits,
        IReadOnlyList<MahjongMeld> selectedOpenMelds,
        IReadOnlyList<MahjongMeld> declaredKans)
    {
        if (declaredKans.Count == 0)
        {
            return ApplySelectedOpenMelds(splits, selectedOpenMelds);
        }

        var results = new List<MahjongHandSplitResult>();
        foreach (var split in splits.Where(split => split.Shape == MahjongHandShape.Standard))
        {
            var melds = split.Melds.ToList();
            var containsAllDeclaredKans = true;
            foreach (var kan in declaredKans.Where(meld => meld.Type == MahjongMeldType.Quad))
            {
                var kanCode = kan.Tiles[0].Code;
                var index = melds.FindIndex(meld =>
                    meld.Type is MahjongMeldType.Triplet or MahjongMeldType.Quad &&
                    meld.Tiles.All(tile => string.Equals(tile.Code, kanCode, StringComparison.OrdinalIgnoreCase)));

                if (index < 0)
                {
                    containsAllDeclaredKans = false;
                    break;
                }

                melds[index] = kan;
            }

            if (containsAllDeclaredKans)
            {
                results.Add(split with { Melds = melds });
            }
        }

        return ApplySelectedOpenMelds(results, selectedOpenMelds);
    }

    /// <summary>
    /// 过滤必须包含用户确认副露面子的拆法，并把对应面子标记为副露。
    /// </summary>
    /// <param name="splits">候选拆牌结果。</param>
    /// <param name="selectedOpenMelds">用户确认的副露面子。</param>
    /// <returns>保留并标记副露后的拆牌结果。</returns>
    private static IReadOnlyList<MahjongHandSplitResult> ApplySelectedOpenMelds(
        IReadOnlyList<MahjongHandSplitResult> splits,
        IReadOnlyList<MahjongMeld> selectedOpenMelds)
    {
        if (selectedOpenMelds.Count == 0)
        {
            return splits;
        }

        var selectedKeys = selectedOpenMelds
            .Select(meld => meld.Key)
            .GroupBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var results = new List<MahjongHandSplitResult>();
        foreach (var split in splits.Where(split => split.Shape == MahjongHandShape.Standard))
        {
            var remainingSelectedKeys = new Dictionary<string, int>(selectedKeys, StringComparer.OrdinalIgnoreCase);
            var melds = new List<MahjongMeld>(split.Melds.Count);

            foreach (var meld in split.Melds)
            {
                var meldKey = meld.Key;
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
    /// 根据牌编码创建用于候选计算的识别牌。
    /// </summary>
    /// <param name="code">牌编码。</param>
    /// <returns>识别牌。</returns>
    private static RecognizedMahjongTile CreateTile(string code) =>
        new(code, GetTileDisplayName(code), 1);

    /// <summary>
    /// 从手牌中移除第一张指定编码的牌。
    /// </summary>
    /// <param name="tiles">原始牌列表。</param>
    /// <param name="code">要移除的牌编码。</param>
    /// <returns>移除一张指定牌后的牌列表。</returns>
    private static IReadOnlyList<RecognizedMahjongTile> RemoveOneTile(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        string code)
    {
        var removed = false;
        var results = new List<RecognizedMahjongTile>(tiles.Count - 1);
        foreach (var tile in tiles)
        {
            if (!removed && string.Equals(tile.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                removed = true;
                continue;
            }

            results.Add(tile);
        }

        return results;
    }

    /// <summary>
    /// 判断指定识别牌是否是标准麻将牌编码。
    /// </summary>
    /// <param name="tile">要判断的识别牌。</param>
    /// <returns>如果牌编码存在于标准牌表中，则返回 <c>true</c>。</returns>
    private static bool IsKnownTile(RecognizedMahjongTile tile) =>
        _allTileCodes.Contains(tile.Code, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 获取界面显示用牌名。
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

    #endregion
}
