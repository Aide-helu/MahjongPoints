using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 默认役种检测器，当前先提供演示用断幺九检测。
/// </summary>
public sealed class DefaultYakuDetector : IYakuDetector
{


    /// <summary>
    /// 三元牌役种
    /// </summary>
    private static readonly IReadOnlyDictionary<string, MahjongYaku> _sanyuanpai = 
        new Dictionary<string, MahjongYaku>(StringComparer.OrdinalIgnoreCase)
    {
        ["5z 5z 5z"] = new("yipai-sanyuanpai-bai","役牌（白）", 1, "白板刻子。"),
        ["6z 6z 6z"] = new("yipai-sanyuanpai-fa", "役牌（发）", 1, "发财刻子。"),
        ["7z 7z 7z"] = new("yipai-sanyuanpai-zhong", "役牌（中）", 1, "红中刻子。")
    };

    /// <summary>
    /// 风牌役种
    /// </summary>
    private static readonly IReadOnlyDictionary<string, MahjongYaku> _fengpai =
        new Dictionary<string, MahjongYaku>(StringComparer.OrdinalIgnoreCase)
    {
        ["1z 1z 1z"] = new("yipai-fengpai-dong", "役牌（东）", 1, "东风刻子。"),
        ["2z 2z 2z"] = new("yipai-fengpai-nan","役牌（南）",1,"南风刻子"),
        ["3z 3z 3z"] = new("yipai-fengpai-xi","役牌（西）",1,"西风刻子"),
        ["4z 4z 4z"] = new("yipai-fengpai-bei","役牌（北）",1,"北风刻子")
    };
    
    
    
    
    /// <summary>
    /// 一杯口：手牌中有两个完全相同的顺子（门清限定）
    /// </summary>
    private static readonly MahjongYaku _yibeikou = new("yibeikou", "一杯口", 1, "两个相同顺子");

    /// <summary>
    /// 平和：手牌由顺子组成，雀头不是役牌，最后一张牌为两面听（门清限定）
    /// </summary>
    private static readonly MahjongYaku _pinghu = new("pinghu", "平和", 1, "顺子+两面听+非役牌雀头");

    /// <summary>
    /// 一发：立直后一巡内胡牌，且期间没有吃碰杠
    /// </summary>
    private static readonly MahjongYaku _yifa = new("yifa", "一发", 1, "立直后一巡内胡牌");

    /// <summary>
    /// 立直：门清状态宣言听牌，支付1000点
    /// </summary>
    private static readonly MahjongYaku _lizhi = new("lizhi", "立直", 1, "门清听牌宣言");

    /// <summary>
    /// 门前清自摸和：门清状态下自摸胡牌
    /// </summary>
    private static readonly MahjongYaku _menqianqingzimohu = new("menqianqingzimohu", "门前清自摸和", 1, "门清自摸");
    
    /// <summary>
    /// 断幺九役种
    /// </summary>
    private static readonly MahjongYaku _duanyao = new("duanyao", "断幺九",1, "手牌中只能由2-8的数牌组成");


    /// <summary>
    /// 抢杠：抢其他玩家加杠的牌胡牌
    /// </summary>
    private static readonly MahjongYaku _qianggang = new("qianggang", "抢杠", 1, "抢加杠胡牌");

    /// <summary>
    /// 岭上开花：杠牌后摸岭上牌胡牌
    /// </summary>
    private static readonly MahjongYaku _lingshangkaihua = new("lingshangkaihua", "岭上开花", 1, "杠后摸岭上牌胡牌");

    /// <summary>
    /// 海底捞月：摸最后一张牌（海底牌）胡牌
    /// </summary>
    private static readonly MahjongYaku _haidilaoyue = new("haidilaoyue", "海底捞月", 1, "摸海底牌胡牌");

    /// <summary>
    /// 河底捞鱼：打最后一张牌（河底牌）被他人胡牌
    /// </summary>
    private static readonly MahjongYaku _hedilaoyu = new("hedilaoyu", "河底捞鱼", 1, "河底牌点炮");

    /// <summary>
    /// 三色同刻：手牌中有三种花色的相同数字的刻子
    /// </summary>
    private static readonly MahjongYaku _sansetongke = new("sansetongke", "三色同刻", 2, "三种花色相同数字刻子");

    /// <summary>
    /// 混全带幺九：所有面子含幺九牌（可以副露，副露减1番）
    /// </summary>
    private static readonly MahjongYaku _hunquandaiyaojiu = new("hunquandaiyaojiu", "混全带幺九", 2, "所有面子含幺九牌");

    /// <summary>
    /// 一气通贯：同花色完成123，456，789三组顺子
    /// </summary>
    private static readonly MahjongYaku _yiqitongguan = new("yiqitongguan", "一气通贯", 1, "同花色123+456+789顺子");

    /// <summary>
    /// 三色同顺：三种花色相同数字的顺子
    /// </summary>
    private static readonly MahjongYaku _sansetongshun = new("sansetongshun", "三色同顺", 1, "三种花色相同数字顺子");

    /// <summary>
    /// 对对和：手牌由四个刻子（杠子）和一对雀头组成
    /// </summary>
    private static readonly MahjongYaku _duiduihu = new("duiduihu", "对对和", 2, "四个刻子+一对雀头");

    /// <summary>
    /// 三暗刻：手牌中有三个暗刻（暗杠算暗刻）
    /// </summary>
    private static readonly MahjongYaku _sananke = new("sananke", "三暗刻", 2, "三个暗刻");

    /// <summary>
    /// 三杠子：手牌中有三个杠子
    /// </summary>
    private static readonly MahjongYaku _sangangzi = new("sangangzi", "三杠子", 2, "三个杠子");

    /// <summary>
    /// 小三元：两个三元牌刻子+一对三元牌雀头
    /// </summary>
    private static readonly MahjongYaku _xiaosanyuan = new("xiaosanyuan", "小三元", 2, "两个三元刻子+三元雀头");

    /// <summary>
    /// 混老头：所有面子均为幺九牌（含字牌），对对和或七对子形态
    /// </summary>
    private static readonly MahjongYaku _hunlaotou = new("hunlaotou", "混老头", 2, "全幺九牌对对和/七对子");

    /// <summary>
    /// 七对子：手牌为七个对子（门清限定）
    /// </summary>
    private static readonly MahjongYaku _qiduizi = new("qiduizi", "七对子", 2, "七个对子");

    /// <summary>
    /// 双立直：在第一巡未摸牌前立直
    /// </summary>
    private static readonly MahjongYaku _shuanglizhi = new("shuanglizhi", "双立直", 2, "第一巡内立直");

    /// <summary>
    /// 清一色：手牌只有同一种花色（不含字牌）
    /// </summary>
    private static readonly MahjongYaku _qingyise = new("qingyise", "清一色", 6, "全同花色数牌");

    /// <summary>
    /// 二杯口：手牌中有两个一杯口（即两组相同顺子），实为七对子的一种特殊形态
    /// </summary>
    private static readonly MahjongYaku _erbeikou = new("erbeikou", "二杯口", 3, "两个一杯口");

    /// <summary>
    /// 纯全带幺九：所有面子含幺九数牌（不含字牌）
    /// </summary>
    private static readonly MahjongYaku _chunquandaiyaojiu = new("chunquandaiyaojiu", "纯全带幺九", 3, "所有面子含幺九数牌");

    /// <summary>
    /// 混一色：手牌由同一种花色的数牌和字牌组成
    /// </summary>
    private static readonly MahjongYaku _hunyise = new("hunyise", "混一色", 3, "同花色数牌+字牌");

    /// <summary>
    /// 根据完整手牌和拆牌结果检测满足的役种。
    /// </summary>
    /// <param name="tiles">参与算点的完整牌列表。</param>
    /// <param name="splits">手牌拆解结果列表。</param>
    /// <param name="context">算点上下文。</param>
    /// <returns>役种检测结果。</returns>
    public IReadOnlyList<YakuDetectionResult> Detect(
        IReadOnlyList<RecognizedMahjongTile> tiles,
        IReadOnlyList<MahjongHandSplitResult> splits,
        MahjongScoringContext context)
    {
        
        var result=new List<YakuDetectionResult>();
        
        foreach (var split in splits)
        {
            var yakus = new List<MahjongYaku>();
            
            //通过用户context直接添加不需要判断的役
            Global(yakus, context);

            //判断是不是七对子
            if (IsQiDuiZi(tiles, split, context))
            {
                yakus.Add(_qiduizi);
            }
            
            //判断役牌三元牌
            if (IsYiPaiSanYuanPai(tiles, split))
            {
                var sanYuanPais = DetectSanYuanPai(split); 
                foreach (var sanYuanPai in sanYuanPais)
                {
                    yakus.Add(sanYuanPai);
                }
            }
            
            //判断役牌风牌
            if (IsYiPaiFengPai(tiles, split))
            {
                var FengPais = DetectFengPai(split,context); 
                foreach (var FengPai in FengPais)
                {
                    yakus.Add(FengPai);
                }
            }
            
            
            //判断断幺
            if (IsDuanYao(tiles))
            {
                yakus.Add(_duanyao);
            }

            //判断平和
            if (IsPingHu(split, context))
            {
                yakus.Add(_pinghu);
            }

            //判断二杯口；二杯口是两组一杯口，成立时不重复计算一杯口
            if (IsErBeiKou(split, context))
            {
                yakus.Add(_erbeikou);
            }
            else if (IsYiBeiKou(split, context))
            {
                yakus.Add(_yibeikou);
            }

            //判断三色同刻
            if (IsSanSeTongKe(split))
            {
                yakus.Add(_sansetongke);
            }

            var isChunQuanDaiYaoJiu = IsChunQuanDaiYaoJiu(tiles, split);
            var isHunLaoTou = IsHunLaoTou(tiles, split);
            var isQingYiSe = IsQingYiSe(tiles);
            var isHunYiSe = IsHunYiSe(tiles);

            //判断混全带幺九；纯全带幺九或混老头成立时不重复计算混全带幺九
            if (!isChunQuanDaiYaoJiu && !isHunLaoTou && IsHunQuanDaiYaoJiu(tiles, split))
            {
                yakus.Add(_hunquandaiyaojiu);
            }

            //判断一气通贯
            if (IsYiQiTongGuan(split))
            {
                yakus.Add(_yiqitongguan);
            }

            //判断三色同顺
            if (IsSanSeTongShun(split))
            {
                yakus.Add(_sansetongshun);
            }

            //判断对对和
            if (IsDuiDuiHe(split))
            {
                yakus.Add(_duiduihu);
            }

            //判断三暗刻
            if (IsSanAnKe(split, context))
            {
                yakus.Add(_sananke);
            }

            //判断三杠子
            if (IsSanGangZi(split))
            {
                yakus.Add(_sangangzi);
            }

            //判断小三元
            if (IsXiaoSanYuan(split))
            {
                yakus.Add(_xiaosanyuan);
            }

            //判断混老头
            if (isHunLaoTou)
            {
                yakus.Add(_hunlaotou);
            }

            //判断清一色
            if (isQingYiSe)
            {
                yakus.Add(_qingyise);
            }

            //判断纯全带幺九
            if (isChunQuanDaiYaoJiu)
            {
                yakus.Add(_chunquandaiyaojiu);
            }

            //判断混一色；清一色成立时不重复计算混一色
            if (!isQingYiSe && isHunYiSe)
            {
                yakus.Add(_hunyise);
            }
            
            
            result.Add(new YakuDetectionResult(yakus, split));
        }

        return result;
    }



    /// <summary>
    /// 上下文通用役种
    /// </summary>
    /// <param name="yakus"></param>
    /// <param name="context"></param>
    private static void Global(List<MahjongYaku> yakus, MahjongScoringContext context)
    {
        if (context.IsMenzen)
        {
            //双立直：前提是门前请
            if (context.IsDoubleRiichi)
            {
                yakus.Add(_shuanglizhi);
            }
            //立直：前提是门前请
            else if (context.IsRiichi)
            {
                yakus.Add(_lizhi);
            }
            //一发：前提是立直或双立直
            if (context.IsIppatsu && (context.IsRiichi || context.IsDoubleRiichi))
            {
                yakus.Add(_yifa);
            }
            //自摸：前提是门前请
            if (context.IsTsumo)
            {
                yakus.Add(_menqianqingzimohu);
            }
        }

        //抢杠：前提是不是自摸
        if (context.IsRobKong && !context.IsTsumo)
        {
            yakus.Add(_qianggang);
        }

        //岭上开花：前提是自摸，不能是抢杠
        if (context.IsRidgeBlossom && context.IsTsumo && !context.IsRobKong)
        {
            yakus.Add(_lingshangkaihua);
        }
        
        //海底捞月：自摸，不能抢杠，不能岭上开花
        if (context.IsHaiDi && context.IsTsumo && !context.IsRobKong && !context.IsRidgeBlossom)
        {
            yakus.Add(_haidilaoyue);
        }
        
        //河底捞鱼：不能自摸，最后一巡不能杠，不能岭上开花，不能海底捞月
        if (context.IsHeDi && !context.IsTsumo && !context.IsRobKong && !context.IsRidgeBlossom && !context.IsHaiDi)
        {
            yakus.Add(_hedilaoyu);
        }
    }

    /// <summary>
    /// 判断是否是三元役牌
    /// </summary>
    /// <param name="tiles"></param>
    /// <param name="splitResult">分割序列。</param>
    /// <returns></returns>
    private static bool IsYiPaiSanYuanPai(IEnumerable<RecognizedMahjongTile> tiles,MahjongHandSplitResult splitResult)
    {
        //找到刻子/杠子的面子
        foreach (var meld in splitResult.Melds)
        {
            if(meld.Type is MahjongMeldType.Sequence)continue;
            if (meld.Type is MahjongMeldType.Triplet or MahjongMeldType.Quad)
            {
                //判断这是不是三元牌
                if (meld.Tiles[0].Code is "5z" or "6z" or "7z")
                {
                    return true;
                }
            }
        }
        return false;
    }
    /// <summary>
    /// 检测是哪种三元牌
    /// </summary>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static IEnumerable<MahjongYaku> DetectSanYuanPai(MahjongHandSplitResult splitResult)
    {
        foreach (var meld in splitResult.Melds)
        {
            if(meld.Type is MahjongMeldType.Sequence)continue;

            if (meld.Type is (MahjongMeldType.Triplet or MahjongMeldType.Quad))
            {
                if (_sanyuanpai.TryGetValue(meld.DisplayText,out var yaku))
                {
                    yield return yaku;
                }
            }
           
        }
    }


    /// <summary>
    /// 判断是否是风役牌
    /// </summary>
    /// <param name="tiles"></param>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static bool IsYiPaiFengPai(IEnumerable<RecognizedMahjongTile> tiles, MahjongHandSplitResult splitResult)
    {
        //找到刻子/杠子的面子
        foreach (var meld in splitResult.Melds)
        {
            if(meld.Type is MahjongMeldType.Sequence)continue;
            if (meld.Type is (MahjongMeldType.Triplet or MahjongMeldType.Quad))
            {
                //判断这是不是风牌
                if (meld.Tiles[0].Code is "1z" or "2z" or "3z" or "4z")
                {
                    return true;
                }
            }
        }
        return false;
    }
    /// <summary>
    /// 检测是哪种风牌
    /// </summary>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static IEnumerable<MahjongYaku> DetectFengPai(MahjongHandSplitResult splitResult,MahjongScoringContext content)
    {
        foreach (var meld in splitResult.Melds)
        {
            
            if(meld.Type is MahjongMeldType.Sequence)continue;

            if (meld.Type is (MahjongMeldType.Triplet or MahjongMeldType.Quad))
            {
                //风牌役种需要联系上下文
                if (_fengpai.TryGetValue(meld.DisplayText,out var yaku))
                {
                    if (content.IsSelfWind)
                    {
                        yield return yaku;
                    }

                    if (content.IsLocalWind)
                    {
                        yield return yaku;
                    }
                }
            }
           
        }
    }

    /// <summary>
    /// 判断是否是一杯口
    /// </summary>
    /// <param name="splitResult"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private static bool IsYiBeiKou(MahjongHandSplitResult splitResult, MahjongScoringContext context)
    {
        //一杯口只适用于门前清的标准型手牌
        if (context.IsMenzen && splitResult.Shape == MahjongHandShape.Standard)
        {
            //搭子记录器
            var meldDictionary =new Dictionary<string, int>();
            //遍历四个搭子
            foreach (var meld in splitResult.Melds)
            {
                if (meld.Type is MahjongMeldType.Sequence)
                {
                    meldDictionary[meld.DisplayText] = meldDictionary.GetValueOrDefault(meld.DisplayText) + 1;
                }
            }
            //统计Value值为2的个数==1 返回true
            return meldDictionary.Count(v => v.Value == 2) == 1;
        }
        return false;
    }

    /// <summary>
    /// 判断是否是平和
    /// </summary>
    /// <param name="splitResult"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private static bool IsPingHu(MahjongHandSplitResult splitResult, MahjongScoringContext context)
    {
        //平和门前限定，并且只适用于标准型
        if (context.IsMenzen && splitResult.Shape == MahjongHandShape.Standard)
        {
            //先判断是不是四顺子
            var melds = splitResult.Melds;
            if (melds.Count(meld => meld.Type == MahjongMeldType.Sequence) != 4) return false;

            //面子中不能存在字牌；雀头可以是数牌或非役牌风牌。
            if (melds.SelectMany(meld => meld.Tiles).Any(tile => tile.Code.Length == 2 && tile.Code[1] == 'z'))
            {
                return false;
            }

            var pairCode = splitResult.Pair.Code;
            if (pairCode.Length == 2 && pairCode[1] == 'z')
            {
                if (pairCode[0] is '5' or '6' or '7')
                {
                    return false;
                }

                //当前上下文只记录雀头风牌是否属于自风/场风，未记录具体东南西北。
                if (pairCode[0] is '1' or '2' or '3' or '4'
                    && (context.IsSelfWind || context.IsLocalWind))
                {
                    return false;
                }
            }

            //两面听只可能是顺子的两端胡牌；中间张是嵌张，直接排除
            var hasTwoSidedWait = false;
            foreach (var meld in melds)
            {
                var firstCode = meld.Tiles[0].Code;
                var lastCode = meld.Tiles[2].Code;

                if (string.Equals(context.WinningTile.Code, firstCode, StringComparison.OrdinalIgnoreCase))
                {
                    // 7-8-9 胡 7 是边张，不算两面听
                    if (firstCode[0] != '7')
                    {
                        hasTwoSidedWait = true;
                    }
                }

                if (string.Equals(context.WinningTile.Code, lastCode, StringComparison.OrdinalIgnoreCase))
                {
                    // 1-2-3 胡 3 是边张，不算两面听
                    if (firstCode[0] != '1')
                    {
                        hasTwoSidedWait = true;
                    }
                }
            }

            return hasTwoSidedWait;

        }
        return false;
    }
    
    /// <summary>
    /// 判断是否是断幺
    /// </summary>
    /// <param name="tiles">待检查的牌列表。</param>
    /// <param name="split">分割序列。</param>
    /// <returns>如果满足断幺九条件，则返回 <c>true</c>。</returns>
    private static bool IsDuanYao(IEnumerable<RecognizedMahjongTile> tiles)
    {
        //遍历每一张牌即可，可复合除役满所有牌
        foreach (var tile in tiles)
        {
            //如果某个编码是字牌直接返回
            if (tile.Code[1].Equals('z')) return false;

            //如果某张数牌是1或9直接返回
            if (tile.Code[0] - '0' is 1 or 9) return false;
        }
        return true;
    }
    
    /// <summary>
    /// 判断是否是三色同刻
    /// </summary>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static bool IsSanSeTongKe(MahjongHandSplitResult splitResult)
    {
        //三色同刻只适用于标准型；七对子等特殊形不能成立
        if (splitResult.Shape != MahjongHandShape.Standard)
        {
            return false;
        }

        //按刻子/杠子的数字分组，记录该数字出现过哪些数牌花色
        var suitsByValue = new Dictionary<int, HashSet<char>>();
        foreach (var meld in splitResult.Melds)
        {
            if (meld.Type is not (MahjongMeldType.Triplet or MahjongMeldType.Quad))
            {
                continue;
            }

            var code = meld.Tiles[0].Code;
            if (code.Length != 2 || code[1] == 'z')
            {
                continue;
            }

            var value = code[0] - '0';
            var suit = code[1];
            if (!suitsByValue.TryGetValue(value, out var suits))
            {
                suits = [];
                suitsByValue[value] = suits;
            }

            suits.Add(suit);
        }

        //任意一个数字同时拥有万、筒、索三种刻子/杠子，即为三色同刻
        return suitsByValue.Values.Any(suits =>
            suits.Contains('m') &&
            suits.Contains('p') &&
            suits.Contains('s'));
    }

    /// <summary>
    /// 判断是否是混全带幺九
    /// </summary>
    /// <param name="tiles"></param>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static bool IsHunQuanDaiYaoJiu(IEnumerable<RecognizedMahjongTile> tiles, MahjongHandSplitResult splitResult)
    {
        
        
        //不是普通型直接返回false，七对子不算混全
        if (splitResult.Shape != MahjongHandShape.Standard)
        {
            return false;
        }

        //如果 所有的牌都不存在字牌直接返回
        if (tiles.All(tile => tile.Code[1] != 'z'))
        {
            return false;
        }
        
        var hasSequence = false;
        foreach (var meld in splitResult.Melds)
        {
            //至少有一个顺子
            if (meld.Type == MahjongMeldType.Sequence)
            {
                hasSequence = true;
            }
        
            //如果当前meld的序列存在幺九直接break当前循环
            var hasTerminalOrHonor = false;
            foreach (var tile in meld.Tiles)
            {
                //序列存在1和9和字，直接
                if (tile.Code[1] == 'z' || tile.Code[0] is '1' or '9')
                {
                    hasTerminalOrHonor = true;
                    break;
                }
            }
            
            //如果某个面子的序列不存在幺九直接false
            if (!hasTerminalOrHonor)
            {
                return false;
            }
        }

        //至少一个顺子，并且每个面子都带幺九
        return hasSequence && (splitResult.Pair.Code[1] == 'z' || splitResult.Pair.Code[0] is '1' or '9');
    }

    /// <summary>
    /// 判断是否是一气通贯
    /// </summary>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static bool IsYiQiTongGuan(MahjongHandSplitResult splitResult)
    {
        //不是普通型直接false
        if (splitResult.Shape != MahjongHandShape.Standard)
        {
            return false;
        }

        //
        var sequenceStartsBySuit = new Dictionary<char, HashSet<int>>();
        //
        foreach (var meld in splitResult.Melds)
        {
            //不是顺子直接下一个面子
            if (meld.Type != MahjongMeldType.Sequence)
            {
                continue;
            }
            
            //获取 1 4 7顺子开头
            var firstCode = meld.Tiles[0].Code;
            //如果是字牌直接下一个面子
            if (firstCode.Length != 2 || firstCode[1] == 'z')
            {
                continue;
            }
            
            //记录万条筒
            var suit = firstCode[1];
            //记录数字
            var start = firstCode[0] - '0';
            
            //如果当前的条筒万还没有创建字典类型就先创建
            if (!sequenceStartsBySuit.TryGetValue(suit, out var starts))
            {
                starts = [];
                sequenceStartsBySuit[suit] = starts;
            }
            
            //按照条筒万添加每一个顺子的首个字母
            starts.Add(start);
        }

        //查找是否存在一个类型，存在147开头的顺子
        return sequenceStartsBySuit.Values.Any(starts =>
            starts.Contains(1) &&
            starts.Contains(4) &&
            starts.Contains(7));
    }

    /// <summary>
    /// 判断是否是三色同顺
    /// </summary>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static bool IsSanSeTongShun(MahjongHandSplitResult splitResult)
    {
        //不是标准型直接false
        if (splitResult.Shape != MahjongHandShape.Standard)
        {
            return false;
        }

        //与一气通贯同理，根据获取到的顺子作为Key：1234567，然后去搜索是否存在一个顺子有m p s
        var sequenceSuitsByStart = new Dictionary<int, HashSet<char>>();
        foreach (var meld in splitResult.Melds)
        {
            if (meld.Type != MahjongMeldType.Sequence)
            {
                continue;
            }

            var firstCode = meld.Tiles[0].Code;
            if (firstCode.Length != 2 || firstCode[1] == 'z')
            {
                continue;
            }

            var start = firstCode[0] - '0';
            var suit = firstCode[1];
            if (!sequenceSuitsByStart.TryGetValue(start, out var suits))
            {
                suits = [];
                sequenceSuitsByStart[start] = suits;
            }

            suits.Add(suit);
        }

        return sequenceSuitsByStart.Values.Any(suits =>
            suits.Contains('m') &&
            suits.Contains('p') &&
            suits.Contains('s'));
    }

    /// <summary>
    /// 判断是否是对对和
    /// </summary>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static bool IsDuiDuiHe(MahjongHandSplitResult splitResult)
    {

        if (splitResult.Shape != MahjongHandShape.Standard)
        {
            return false;
        }

        return splitResult.Melds.All(meld => meld.Type is MahjongMeldType.Triplet or MahjongMeldType.Quad);
    }

    /// <summary>
    /// 判断是否是三暗刻
    /// </summary>
    /// <param name="splitResult"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private static bool IsSanAnKe(MahjongHandSplitResult splitResult, MahjongScoringContext context)
    {
        //不是标准型直接false
        if (splitResult.Shape != MahjongHandShape.Standard)
        {
            return false;
        }

        var winningCode = context.WinningTile.Code;
        var canAssignWinningTileOutsideTriplet =
            string.Equals(splitResult.Pair.Code, winningCode, StringComparison.OrdinalIgnoreCase) ||
            splitResult.Melds.Any(meld =>
                meld.Type == MahjongMeldType.Sequence &&
                meld.Tiles.Any(tile => string.Equals(tile.Code, winningCode, StringComparison.OrdinalIgnoreCase)));

        //记录暗刻数量
        var concealedTripletCount = 0;
        foreach (var meld in splitResult.Melds)
        {
            //不是刻子或杠子读下一个面子
            if (meld.Type is not (MahjongMeldType.Triplet or MahjongMeldType.Quad))
            {
                continue;
            }

            //如果面子是副露的也下一个面子
            if (meld.IsOpen)
            {
                continue;
            }

            //不是自摸，并且荣和牌在当前刻子/杠子里，则该组不算暗刻
            if (!context.IsTsumo &&
                winningCode != "unknown" &&
                !canAssignWinningTileOutsideTriplet &&
                meld.Tiles.Any(meldTile =>
                    string.Equals(meldTile.Code, winningCode, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            concealedTripletCount++;
        }

        return concealedTripletCount >= 3;
    }

    /// <summary>
    /// 判断是否是三杠子
    /// </summary>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static bool IsSanGangZi(MahjongHandSplitResult splitResult)
    {
        if (splitResult.Shape != MahjongHandShape.Standard)
        {
            return false;
        }

        return splitResult.Melds.Count(meld => meld.Type == MahjongMeldType.Quad) >= 3;
    }

    /// <summary>
    /// 判断是否是小三元
    /// </summary>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static bool IsXiaoSanYuan(MahjongHandSplitResult splitResult)
    {
        //小三元只适用于标准型：两组三元牌刻子/杠子 + 剩下一种三元牌作雀头
        if (splitResult.Shape != MahjongHandShape.Standard)
        {
            return false;
        }

        var tripletDragonCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var meld in splitResult.Melds)
        {
            if (meld.Type is not (MahjongMeldType.Triplet or MahjongMeldType.Quad))
            {
                continue;
            }

            var code = meld.Tiles[0].Code;
            if (code is "5z" or "6z" or "7z")
            {
                tripletDragonCodes.Add(code);
            }
        }

        // 必须正好有两种三元牌是刻子/杠子
        if (tripletDragonCodes.Count != 2)
        {
            return false;
        }

        // 第三种三元牌必须是雀头
        return splitResult.Pair.Code is "5z" or "6z" or "7z" &&
               !tripletDragonCodes.Contains(splitResult.Pair.Code);
    }

    /// <summary>
    /// 判断是否是混老头
    /// </summary>
    /// <param name="tiles"></param>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static bool IsHunLaoTou(IEnumerable<RecognizedMahjongTile> tiles, MahjongHandSplitResult splitResult)
    {
        //混老头可以是标准型或七对子；其他特殊形不处理
        if (splitResult.Shape is not (MahjongHandShape.Standard or MahjongHandShape.SevenPairs))
        {
            return false;
        }

        var hasTerminal = false;
        var hasHonor = false;
        foreach (var tile in tiles)
        {
            var code = tile.Code;
            
            if (code[1] == 'z')
            {
                hasHonor = true;
                continue;
            }

            //数牌只能是1或9；出现2-8则不是混老头
            if (code[0] is not ('1' or '9'))
            {
                return false;
            }

            hasTerminal = true;
        }

        //混老头要求同时包含数牌幺九牌和字牌
        return hasTerminal && hasHonor;
    }

    /// <summary>
    /// 判断是否是七对子
    /// </summary>
    /// <param name="tiles"></param>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static bool IsQiDuiZi(IEnumerable<RecognizedMahjongTile> tiles, MahjongHandSplitResult splitResult, MahjongScoringContext context)
    {
        //七对子是门前限定；副露时不成立
        if (!context.IsMenzen || splitResult.Shape != MahjongHandShape.SevenPairs)
        {
            return false;
        }

        //必须正好7种牌，每种2张；四张相同牌不能拆成两个对子
        var pairCounts = tiles
            .GroupBy(tile => tile.Code, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Count())
            .ToArray();

        if (pairCounts.Length == 7 && pairCounts.All(count => count == 2))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 判断是否是清一色
    /// </summary>
    /// <param name="tiles"></param>
    /// <returns></returns>
    private static bool IsQingYiSe(IEnumerable<RecognizedMahjongTile> tiles)
    {
        var suit = '\0';
        foreach (var tile in tiles)
        {
            var code = tile.Code;
            if (code[1] == 'z')
            {
                return false;
            }

            //第一张数牌确定清一色花色，之后所有数牌必须同花色
            if (suit == '\0')
            {
                suit = code[1];
                continue;
            }

            if (code[1] != suit)
            {
                return false;
            }
        }

        return suit != '\0';
    }

    /// <summary>
    /// 判断是否是二杯口
    /// </summary>
    /// <param name="splitResult"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private static bool IsErBeiKou(MahjongHandSplitResult splitResult, MahjongScoringContext context)
    {
        //二杯口门前限定，并且只适用于标准型
        if (!context.IsMenzen || splitResult.Shape != MahjongHandShape.Standard)
        {
            return false;
        }

        var sequenceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var meld in splitResult.Melds)
        {
            if (meld.Type != MahjongMeldType.Sequence)
            {
                continue;
            }

            sequenceCounts[meld.DisplayText] = sequenceCounts.GetValueOrDefault(meld.DisplayText) + 1;
        }

        //需要两种不同的顺子各出现两次；四组完全相同不算二杯口
        return sequenceCounts.Count(pair => pair.Value == 2) == 2;
    }

    /// <summary>
    /// 判断是否是纯全带幺九
    /// </summary>
    /// <param name="tiles"></param>
    /// <param name="splitResult"></param>
    /// <returns></returns>
    private static bool IsChunQuanDaiYaoJiu(IEnumerable<RecognizedMahjongTile> tiles, MahjongHandSplitResult splitResult)
    {
        //纯全带幺九只适用于标准型，且不能含字牌
        if (splitResult.Shape != MahjongHandShape.Standard)
        {
            return false;
        }

        if (tiles.Any(tile => tile.Code.Length != 2 || tile.Code[1] == 'z'))
        {
            return false;
        }

        foreach (var meld in splitResult.Melds)
        {
            if (meld.Type == MahjongMeldType.Sequence)
            {
                var start = meld.Tiles[0].Code[0];
                //顺子必须是123或789，才包含幺九牌
                if (start is not ('1' or '7'))
                {
                    return false;
                }

                continue;
            }

            //刻子/杠子必须本身是1或9
            if (meld.Tiles[0].Code[0] is not ('1' or '9'))
            {
                return false;
            }
        }

        //雀头也必须是1或9数牌
        return splitResult.Pair.Code[0] is '1' or '9';
    }

    /// <summary>
    /// 判断是否是混一色
    /// </summary>
    /// <param name="tiles"></param>
    /// <returns></returns>
    private static bool IsHunYiSe(IEnumerable<RecognizedMahjongTile> tiles)
    {
        var suit = '\0';
        var hasSuitedTile = false;
        var hasHonor = false;

        foreach (var tile in tiles)
        {
            var code = tile.Code;
            if (code.Length != 2)
            {
                return false;
            }

            if (code[1] == 'z')
            {
                hasHonor = true;
                continue;
            }

            hasSuitedTile = true;
            if (suit == '\0')
            {
                suit = code[1];
                continue;
            }

            //出现第二种数牌花色就不是混一色
            if (code[1] != suit)
            {
                return false;
            }
        }

        //混一色必须同时有一种数牌花色和至少一张字牌；清一色不算混一色
        return hasSuitedTile && hasHonor;
    }
    

    
    
    
    
    
}
