using CommunityToolkit.Mvvm.ComponentModel;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 表示一次算点需要的环境信息。
/// </summary>
/// <param name="IsTsumo">是否自摸。</param>
/// <param name="IsParent">是否亲家。</param>
/// <param name="IsMenzen">是否门前清。</param>
/// <param name="RiichiSticks">可收入的立直棒数量。</param>
public sealed class MahjongScoringContext : ObservableObject
{
    private static readonly RecognizedMahjongTile DefaultWinningTile = new("5p", "五筒", 1.0);
    private bool _isParent;
    private bool _isRiichi;
    private bool _isOpenHand;
    private bool _isIppatsu;
    private bool _isTsumo;
    private int _riichiSticks;

    public MahjongScoringContext()
        : this(DefaultWinningTile)
    {
    }

    public MahjongScoringContext(RecognizedMahjongTile winningTile)
    {
        WinningTile = winningTile;
    }

    public RecognizedMahjongTile WinningTile { get; }

    [MahjongScoringOption("是否亲家", 10)]
    public bool IsParent
    {
        get => _isParent;
        set => SetProperty(ref _isParent, value);
    }

    [MahjongScoringOption("是否立直", 20)]
    public bool IsRiichi
    {
        get => _isRiichi;
        set => SetProperty(ref _isRiichi, value);
    }

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

    [MahjongScoringOption("是否一发", 40)]
    public bool IsIppatsu
    {
        get => _isIppatsu;
        set => SetProperty(ref _isIppatsu, value);
    }

    [MahjongScoringOption("是否自摸", 50)]
    public bool IsTsumo
    {
        get => _isTsumo;
        set => SetProperty(ref _isTsumo, value);
    }

    public int RiichiSticks
    {
        get => _riichiSticks;
        set => SetProperty(ref _riichiSticks, value);
    }

    public bool IsMenzen => !IsOpenHand;
}
