using System;
using System.Collections.Generic;
using System.Linq;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 默认手牌拆解器，枚举标准 4 面子加 1 雀头的拆法。
/// </summary>
public sealed class DefaultHandSplitter : IHandSplitter
{
    /// <summary>
    /// 支持组成顺子的数牌花色。
    /// </summary>
    private static readonly char[] _suits = ['m', 'p', 's'];

    /// <summary>
    /// 把 14 张牌拆解为所有可能的 4 面子加 1 雀头结构。
    /// </summary>
    /// <param name="tiles">参与拆解的完整牌列表。</param>
    /// <returns>所有可用的手牌拆解结果。</returns>
    public IReadOnlyList<MahjongHandSplit> Split(IReadOnlyList<RecognizedMahjongTile> tiles)
    {
        if (tiles.Count != 14)
        {
            return [];
        }

        //通过编码获取单张牌的对象
        var tileByCode = tiles
            .GroupBy(tile => tile.Code)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        //分组统计，分组统计计算每张牌的总数，然后输出为一个字典
        var counts = tiles
            .GroupBy(tile => tile.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        var results = new List<MahjongHandSplit>();

        //优先进行七对子分割。
        if (SevenPairsSplit(counts, tileByCode, out var sevenPairsSplit))
        {
            results.Add(sevenPairsSplit);
            return results;
        }
        
        //这个循环尝试选择出总数大于等于2的Key
        foreach (var pairCode in counts.Where(pair => pair.Value >= 2).Select(pair => pair.Key).ToArray())
        {
            //记录一个拷贝字典，拷贝counts
            var remainingCounts = new Dictionary<string, int>(counts, StringComparer.OrdinalIgnoreCase);
            
            //去掉去掉，得到12张牌
            RemoveTiles(remainingCounts, pairCode, 2);

            //melds是其中一个分割序列
            //FindMelds：输入12张牌字典类型，
            foreach (var melds in FindMelds(remainingCounts, tileByCode, []))
            {
                //凑齐了四个面子
                if (melds.Count == 4)
                {
                    results.Add(new MahjongHandSplit(melds, tileByCode[pairCode]));
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 尝试创建七对子分割
    /// </summary>
    /// <param name="counts"></param>
    /// <param name="tileByCode"></param>
    /// <param name="split"></param>
    /// <returns></returns>
    private static bool SevenPairsSplit(
        IReadOnlyDictionary<string, int> counts,
        IReadOnlyDictionary<string, RecognizedMahjongTile> tileByCode,
        out MahjongHandSplit split)
    {
        split = new MahjongHandSplit([], tileByCode.Values.First(), MahjongHandShape.SevenPairs, []);

        if (counts.Count != 7 || counts.Values.Any(count => count != 2))
        {
            return false;
        }

        var pairs = counts
            .Keys
            .OrderBy(GetSortKey)
            .Select(code => tileByCode[code])
            .ToArray();

        split = new MahjongHandSplit([], pairs[0], MahjongHandShape.SevenPairs, pairs);
        return true;
    }

    
    
    
    /// <summary>
    /// 在剩余牌计数中递归查找所有可用面子组合。
    /// </summary>
    /// <param name="counts">剩余牌计数字典。</param>
    /// <param name="tileByCode">牌编码到牌对象的映射。</param>
    /// <param name="current">当前已经拆出的面子列表。</param>
    /// <returns>所有递归得到的面子组合。</returns>
    private static IReadOnlyList<IReadOnlyList<MahjongMeld>> FindMelds(
        IReadOnlyDictionary<string, int> counts,
        IReadOnlyDictionary<string, RecognizedMahjongTile> tileByCode,
        IReadOnlyList<MahjongMeld> current)
    {
        // 所有牌都被用完时，只有刚好拆出 4 个面子才算成功拆法。
        if (counts.Values.All(count => count == 0))
        {
            //返回一层包装过的current
            return current.Count == 4 ? [current] : [];
        }

        // 标准胡牌最多只有 4 个面子，已经达到 4 个还剩牌就说明这条拆法失败。
        if (current.Count >= 4)
        {
            return [];
        }

        // 固定从剩余牌里排序最靠前的一张开始拆，避免同一组面子因为顺序不同被重复枚举。
        var firstCode = counts
            .Where(pair => pair.Value > 0)
            .Select(pair => pair.Key)
            .OrderBy(GetSortKey)
            .First();

        var results = new List<IReadOnlyList<MahjongMeld>>();

        // 分支一：如果当前牌至少有 3 张，尝试把它作为刻子移除。
        if (counts[firstCode] >= 3)
        {
            //拷贝counts，当前的counts不执行删改
            var nextCounts = new Dictionary<string, int>(counts, StringComparer.OrdinalIgnoreCase);
            //删除这个面子
            RemoveTiles(nextCounts, firstCode, 3);
            //获取形成面子的单个对象
            var tile = tileByCode[firstCode];
            //构建面子
            var meld = new MahjongMeld(MahjongMeldType.Triplet, [tile, tile, tile]);
            //执行下一次递归
            results.AddRange(FindMelds(nextCounts, tileByCode, [.. current, meld]));
        }

        // 分支二：如果当前牌是 1 到 7 的数牌，尝试和后两张同花色牌组成顺子。
        if (TryGetSuitedTile(firstCode, out var value, out var suit) && value <= 7)
        {

            //第一张，第二张和第三张牌
            var secondCode = $"{value + 1}{suit}";
            var thirdCode = $"{value + 2}{suit}";
            
            //查看这两张续牌是否存在
            if (counts.ContainsKey(secondCode) && counts.ContainsKey(thirdCode))
            {
                //拷贝counts，当前的counts不执行删改
                var nextCounts = new Dictionary<string, int>(counts, StringComparer.OrdinalIgnoreCase);

                
                //两张续牌都存在
                //删除这个面子
                RemoveTiles(nextCounts, firstCode, 1);
                RemoveTiles(nextCounts,secondCode,1);
                RemoveTiles(nextCounts,thirdCode,1);
                
                //获取构建这个面子的两个对象
                var firstTile = tileByCode[firstCode];
                var secondTile=tileByCode[secondCode];
                var thirdTile = tileByCode[thirdCode];
                
                //构建面子
                var meld = new MahjongMeld(MahjongMeldType.Sequence, [firstTile, secondTile, thirdTile]);
                
                //执行下一次递归
                results.AddRange(FindMelds(nextCounts,tileByCode,[.. current, meld]));
            }
        }

        // 汇总当前牌走刻子分支和顺子分支后得到的所有合法拆法。
        return results;
    }
    
    
    

    /// <summary>
    /// 判断剩余牌计数中是否还有指定编码的牌。
    /// </summary>
    /// <param name="counts">剩余牌计数字典。</param>
    /// <param name="code">麻将牌编码。</param>
    /// <returns>如果指定牌仍有剩余，则返回 <c>true</c>。</returns>
    private static bool HasTile(IReadOnlyDictionary<string, int> counts, string code) =>
        counts.TryGetValue(code, out var count) && count > 0;

    /// <summary>
    /// 从剩余牌计数中移除指定数量的牌。
    /// </summary>
    /// <param name="counts">剩余牌计数字典。</param>
    /// <param name="code">麻将牌编码。</param>
    /// <param name="amount">要移除的数量。</param>
    private static void RemoveTiles(IDictionary<string, int> counts, string code, int amount)
    {
        counts[code] -= amount;
        if (counts[code] <= 0)
        {
            counts.Remove(code);
        }
    }

    /// <summary>
    /// 尝试把牌编码解析为数牌点数和花色。
    /// </summary>
    /// <param name="code">麻将牌编码。</param>
    /// <param name="value">解析出的点数。</param>
    /// <param name="suit">解析出的花色。</param>
    /// <returns>如果编码是合法数牌，则返回 <c>true</c>。</returns>
    private static bool TryGetSuitedTile(string code, out int value, out char suit)
    {
        value = 0;
        suit = '\0';

        if (code.Length != 2 || !char.IsDigit(code[0]) || !_suits.Contains(code[1]))
        {
            return false;
        }

        value = code[0] - '0';
        suit = code[1];
        return value is >= 1 and <= 9;
    }

    /// <summary>
    /// 获取牌编码的稳定排序键，用于递归拆牌时固定处理顺序，把牌从 2m->m2m 所有的万字前面都是m ，这样排序就能放一起了
    /// </summary>
    /// <param name="code">麻将牌编码。</param>
    /// <returns>排序键。</returns>
    private static string GetSortKey(string code)
    {
        if (TryGetSuitedTile(code, out var value, out var suit))
        {
            return $"{suit}{value}";
        }

        return $"z{code}";
    }
}
