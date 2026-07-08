using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 表示一次算点需要的环境信息。
/// </summary>
public sealed class MahjongScoringContext : ObservableObject
{
    /// <summary>
    /// 是否为亲家。
    /// </summary>
    private bool _isParent;

    /// <summary>
    /// 是否立直。
    /// </summary>
    private bool _isRiichi;

    /// <summary>
    /// 是否双立直。
    /// </summary>
    private bool _isDoubleRiichi;
    
    /// <summary>
    /// 是否副露。
    /// </summary>
    private bool _isOpenHand;

    /// <summary>
    /// 是否一发。
    /// </summary>
    private bool _isIppatsu;

    /// <summary>
    /// 是否自摸。
    /// </summary>
    private bool _isTsumo;

    /// <summary>
    /// 自风。
    /// </summary>
    private MahjongWind _selfWind = MahjongWind.East;

    /// <summary>
    /// 场风。
    /// </summary>
    private MahjongWind _roundWind = MahjongWind.East;

    /// <summary>
    /// 是否海底捞月
    /// </summary>
    private bool _isHaiDi;

    /// <summary>
    /// 是否河底捞鱼
    /// </summary>
    private bool _isHeDi;

    /// <summary>
    /// 是否抢杠
    /// </summary>
    private bool _isRobKong;

    /// <summary>
    /// 是否是岭上开花
    /// </summary>
    private bool _isRidgeBlossom;

    /// <summary>
    /// 是否为亲家。亲家由用户手动选择，不从自风推导。
    /// </summary>
    [MahjongScoringOption("是否亲家", 10)]
    public bool IsParent
    {
        get => _isParent;
        set => SetProperty(ref _isParent, value);
    }

    /// <summary>
    /// 是否立直。
    /// </summary>
    [MahjongScoringOption("是否立直", 20)]
    public bool IsRiichi
    {
        get => _isRiichi;
        set => SetProperty(ref _isRiichi, value);
    }

    /// <summary>
    /// 是否双立直
    /// </summary>
    [MahjongScoringOption("是否双立直", 30)]
    public bool IsDoubleRiichi
    {
        get => _isDoubleRiichi;
        set => SetProperty(ref _isDoubleRiichi, value);
    }
    

    /// <summary>
    /// 是否副露。
    /// </summary>
    [MahjongScoringOption("是否副露", 40)]
    public bool IsOpenHand
    {
        get => _isOpenHand;
        set
        {
            if (SetProperty(ref _isOpenHand, value))
            {
                OnPropertyChanged(nameof(IsMenzen));
            }
        }
    }

    /// <summary>
    /// 是否一发。
    /// </summary>
    [MahjongScoringOption("是否一发", 50)]
    public bool IsIppatsu
    {
        get => _isIppatsu;
        set => SetProperty(ref _isIppatsu, value);
    }

    /// <summary>
    /// 是否自摸。
    /// </summary>
    [MahjongScoringOption("是否自摸", 60)]
    public bool IsTsumo
    {
        get => _isTsumo;
        set => SetProperty(ref _isTsumo, value);
    }
    
    /// <summary>
    /// 用户选择的当前自风，用于判定役牌（自风）和平和雀头。
    /// </summary>
    public MahjongWind SelfWind
    {
        get => _selfWind;
        set
        {
            if (SetProperty(ref _selfWind, value))
            {
                OnPropertyChanged(nameof(SelfWindTileCode));
            }
        }
    }

    /// <summary>
    /// 用户选择的当前场风，用于判定役牌（场风）和平和雀头。
    /// </summary>
    public MahjongWind RoundWind
    {
        get => _roundWind;
        set
        {
            if (SetProperty(ref _roundWind, value))
            {
                OnPropertyChanged(nameof(RoundWindTileCode));
            }
        }
    }

    /// <summary>
    /// 当前自风对应的实际风牌编码。
    /// </summary>
    public string SelfWindTileCode => SelfWind.ToTileCode();

    /// <summary>
    /// 当前场风对应的实际风牌编码。
    /// </summary>
    public string RoundWindTileCode => RoundWind.ToTileCode();


    [MahjongScoringOption("是否海底捞月", 90)]
    public bool IsHaiDi
    {
        get => _isHaiDi;
        set => SetProperty(ref _isHaiDi, value);
    }

    [MahjongScoringOption("是否河底捞鱼", 100)]
    public bool IsHeDi
    {
        get => _isHeDi;
        set => SetProperty(ref _isHeDi, value);
    }

    [MahjongScoringOption("是否抢杠", 110)]
    public bool IsRobKong
    {
        get => _isRobKong;
        set => SetProperty(ref _isRobKong, value);
    }

    [MahjongScoringOption("是否岭上开花", 120)]
    public bool IsRidgeBlossom
    {
        get => _isRidgeBlossom;
        set => SetProperty(ref _isRidgeBlossom, value);
    }
    
    /// <summary>
    /// 是否门前清。（互斥关系）
    /// </summary>
    public bool IsMenzen => !IsOpenHand;
    
    
    /// <summary>
    /// 使用默认胡牌张占位值创建算点上下文。
    /// </summary>
    public MahjongScoringContext()
        : this(_defaultWinningTile)
    {
    }

    /// <summary>
    /// 使用指定胡牌张创建算点上下文。
    /// </summary>
    /// <param name="winningTile">用户选择的胡牌张。</param>
    public MahjongScoringContext(RecognizedMahjongTile winningTile)
    {
        _winningTile = winningTile;
    }
    
    
    /// <summary>
    /// 未选择胡牌张时使用的默认占位牌。
    /// </summary>
    private static readonly RecognizedMahjongTile _defaultWinningTile = new("unknown", "未知", 0);

    /// <summary>
    /// 用户当前选择的胡牌张。
    /// </summary>
    private RecognizedMahjongTile _winningTile;
    
    /// <summary>
    /// 可收入的立直棒数量。
    /// </summary>
    private int _riichiSticks;

    private int _doraCount;

    /// <summary>
    /// 用户确认过的副露面子列表；为空时由算点服务自行使用全部拆牌结果。
    /// </summary>
    private IReadOnlyList<MahjongMeld> _selectedOpenMelds = [];
    
    /// <summary>
    /// 用户选择的胡牌张。
    /// </summary>
    public RecognizedMahjongTile WinningTile
    {
        get => _winningTile;
        set => SetProperty(ref _winningTile, value ?? _defaultWinningTile);
    }
    
    /// <summary>
    /// 可收入的立直棒数量。
    /// </summary>
    public int RiichiSticks
    {
        get => _riichiSticks;
        set => SetProperty(ref _riichiSticks, value);
    }

    public int DoraCount
    {
        get => _doraCount;
        set => SetProperty(ref _doraCount, Math.Max(0, value));
    }

    /// <summary>
    /// 用户确认过的副露面子列表。
    /// </summary>
    public IReadOnlyList<MahjongMeld> SelectedOpenMelds
    {
        get => _selectedOpenMelds;
        set => SetProperty(ref _selectedOpenMelds, value ?? []);
    }
    
    /// <summary>
    /// 将胡牌张恢复为默认占位值。
    /// </summary>
    public void ResetWinningTile()
    {
        WinningTile = _defaultWinningTile;
    }
}
