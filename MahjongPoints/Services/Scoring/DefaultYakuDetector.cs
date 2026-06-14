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
        IReadOnlyList<MahjongHandSplit> splits,
        MahjongScoringContext context)
    {
        
        var result=new List<YakuDetectionResult>();
        
        foreach (var split in splits)
        {
            var yakus = new List<MahjongYaku>();
            
            //通过用户context直接添加不需要判断的役
            Global(yakus, context);

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
            if (IsDuanYao(tiles,split))
            {
                yakus.Add(_duanyao);
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
    /// <param name="split">分割序列。</param>
    /// <returns></returns>
    private static bool IsYiPaiSanYuanPai(IEnumerable<RecognizedMahjongTile> tiles,MahjongHandSplit split)
    {
        //找到刻子/杠子的面子
        foreach (var meld in split.Melds)
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
    /// <param name="split"></param>
    /// <returns></returns>
    private static IEnumerable<MahjongYaku> DetectSanYuanPai(MahjongHandSplit split)
    {
        foreach (var meld in split.Melds)
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
    /// <param name="split"></param>
    /// <returns></returns>
    private static bool IsYiPaiFengPai(IEnumerable<RecognizedMahjongTile> tiles, MahjongHandSplit split)
    {
        //找到刻子/杠子的面子
        foreach (var meld in split.Melds)
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
    /// <param name="split"></param>
    /// <returns></returns>
    private static IEnumerable<MahjongYaku> DetectFengPai(MahjongHandSplit split,MahjongScoringContext content)
    {
        foreach (var meld in split.Melds)
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
    /// <param name="split"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private static bool IsYiBeiKou(MahjongHandSplit split, MahjongScoringContext context)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是平和
    /// </summary>
    /// <param name="split"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private static bool IsPingHu(MahjongHandSplit split, MahjongScoringContext context)
    {
        return false;
    }
    
    /// <summary>
    /// 判断是否是断幺
    /// </summary>
    /// <param name="tiles">待检查的牌列表。</param>
    /// <param name="split">分割序列。</param>
    /// <returns>如果满足断幺九条件，则返回 <c>true</c>。</returns>
    private static bool IsDuanYao(IEnumerable<RecognizedMahjongTile> tiles,MahjongHandSplit split)
    {
        foreach (var tile in tiles)
        {
            if (tile.Code.Length != 2 || !char.IsDigit(tile.Code[0]))
            {
                return false;
            }

            var value = tile.Code[0] - '0';
            if (value is < 2 or > 8)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// 判断是否是三色同刻
    /// </summary>
    /// <param name="split"></param>
    /// <returns></returns>
    private static bool IsSanSeTongKe(MahjongHandSplit split)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是混全带幺九
    /// </summary>
    /// <param name="tiles"></param>
    /// <param name="split"></param>
    /// <returns></returns>
    private static bool IsHunQuanDaiYaoJiu(IEnumerable<RecognizedMahjongTile> tiles, MahjongHandSplit split)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是一气通贯
    /// </summary>
    /// <param name="split"></param>
    /// <returns></returns>
    private static bool IsYiQiTongGuan(MahjongHandSplit split)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是三色同顺
    /// </summary>
    /// <param name="split"></param>
    /// <returns></returns>
    private static bool IsSanSeTongShun(MahjongHandSplit split)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是对对和
    /// </summary>
    /// <param name="split"></param>
    /// <returns></returns>
    private static bool IsDuiDuiHe(MahjongHandSplit split)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是三暗刻
    /// </summary>
    /// <param name="split"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private static bool IsSanAnKe(MahjongHandSplit split, MahjongScoringContext context)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是三杠子
    /// </summary>
    /// <param name="split"></param>
    /// <returns></returns>
    private static bool IsSanGangZi(MahjongHandSplit split)
    {
        return false;
    }

    
    /// <summary>
    /// 判断是否是小三元
    /// </summary>
    /// <param name="split"></param>
    /// <returns></returns>
    private static bool IsXiaoSanYuan(MahjongHandSplit split)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是混老头
    /// </summary>
    /// <param name="tiles"></param>
    /// <param name="split"></param>
    /// <returns></returns>
    private static bool IsHunLaoTou(IEnumerable<RecognizedMahjongTile> tiles, MahjongHandSplit split)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是七对子
    /// </summary>
    /// <param name="tiles"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private static bool IsQiDuiZi(IEnumerable<RecognizedMahjongTile> tiles, MahjongScoringContext context)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是清一色
    /// </summary>
    /// <param name="tiles"></param>
    /// <returns></returns>
    private static bool IsQingYiSe(IEnumerable<RecognizedMahjongTile> tiles)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是二杯口
    /// </summary>
    /// <param name="split"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    private static bool IsErBeiKou(MahjongHandSplit split, MahjongScoringContext context)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是纯全带幺九
    /// </summary>
    /// <param name="tiles"></param>
    /// <param name="split"></param>
    /// <returns></returns>
    private static bool IsChunQuanDaiYaoJiu(IEnumerable<RecognizedMahjongTile> tiles, MahjongHandSplit split)
    {
        return false;
    }

    /// <summary>
    /// 判断是否是混一色
    /// </summary>
    /// <param name="tiles"></param>
    /// <returns></returns>
    private static bool IsHunYiSe(IEnumerable<RecognizedMahjongTile> tiles)
    {
        return false;
    }
    

    
    
    
    
    
}
