using CommunityToolkit.Mvvm.ComponentModel;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 表示一次算点需要的环境信息。
/// </summary>
public sealed class MahjongScoringContext : ObservableObject
{
    /// <summary>
    /// 未选择胡牌张时使用的默认占位牌。
    /// </summary>
    private static readonly RecognizedMahjongTile _defaultWinningTile = new("unknown", "未知", 0);

    /// <summary>
    /// 用户当前选择的胡牌张。
    /// </summary>
    private RecognizedMahjongTile _winningTile;

    /// <summary>
    /// 是否为亲家。
    /// </summary>
    private bool _isParent;

    /// <summary>
    /// 是否立直。
    /// </summary>
    private bool _isRiichi;

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
    /// 可收入的立直棒数量。
    /// </summary>
    private int _riichiSticks;

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
    /// 用户选择的胡牌张。
    /// </summary>
    public RecognizedMahjongTile WinningTile
    {
        get => _winningTile;
        set => SetProperty(ref _winningTile, value ?? _defaultWinningTile);
    }

    /// <summary>
    /// 是否为亲家。
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
    /// 是否副露。
    /// </summary>
    [MahjongScoringOption("是否副露", 30)]
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
    [MahjongScoringOption("是否一发", 40)]
    public bool IsIppatsu
    {
        get => _isIppatsu;
        set => SetProperty(ref _isIppatsu, value);
    }

    /// <summary>
    /// 是否自摸。
    /// </summary>
    [MahjongScoringOption("是否自摸", 50)]
    public bool IsTsumo
    {
        get => _isTsumo;
        set => SetProperty(ref _isTsumo, value);
    }

    /// <summary>
    /// 可收入的立直棒数量。
    /// </summary>
    public int RiichiSticks
    {
        get => _riichiSticks;
        set => SetProperty(ref _riichiSticks, value);
    }

    /// <summary>
    /// 是否门前清。
    /// </summary>
    public bool IsMenzen => !IsOpenHand;

    /// <summary>
    /// 将胡牌张恢复为默认占位值。
    /// </summary>
    public void ResetWinningTile()
    {
        WinningTile = _defaultWinningTile;
    }
}
