using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MahjongPoints.Models;
using MahjongPoints.Services;
using MahjongPoints.Services.Scoring;

namespace MahjongPoints.ViewModels;

/// <summary>
/// 主窗口的视图模型，负责图片加载、手牌识别、算点调用和界面状态更新。
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    /// <summary>
    /// 手牌图片识别服务。
    /// </summary>
    private readonly IHandImageRecognizer _recognizer;

    /// <summary>
    /// 手牌算点服务。
    /// </summary>
    private readonly IHandScoringService _scoringService;

    /// <summary>
    /// 当前选择图片的文件名。
    /// </summary>
    [ObservableProperty]
    private string _selectedFileName = "未选择图片";

    /// <summary>
    /// 当前图片预览位图。
    /// </summary>
    [ObservableProperty]
    private Bitmap? _previewImage;

    /// <summary>
    /// 界面顶部状态提示文本。
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "请选择一张图片，模型会识别手牌并计算点数。";

    /// <summary>
    /// 算点结果摘要。
    /// </summary>
    [ObservableProperty]
    private string _scoreSummary = "等待算点";

    /// <summary>
    /// 当前和牌张显示文本。
    /// </summary>
    [ObservableProperty]
    private string _winningTileText = "胡牌张：未计算";

    /// <summary>
    /// 当前牌型或拆牌结果显示文本。
    /// </summary>
    [ObservableProperty]
    private string _winningShape = "牌型：未计算";

    /// <summary>
    /// 当前是否正在执行图片加载、识别或算点流程。
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isTenpaiMode;

    [ObservableProperty]
    private bool _isWinningTileMode;

    [ObservableProperty]
    private bool _isDiscardTenpaiMode;

    [ObservableProperty]
    private RecognizedMahjongTile? _selectedTenpaiTile;

    [ObservableProperty]
    private TenpaiDiscardWinningOption? _selectedTenpaiDiscardWinningOption;

    /// <summary>
    /// 界面展示的识别牌集合。
    /// </summary>
    public ObservableCollection<RecognizedMahjongTile> RecognizedTiles { get; } = [];

    /// <summary>
    /// 当前 13 张手牌模式下可选择的听牌胡牌张集合。
    /// </summary>
    public ObservableCollection<RecognizedMahjongTile> TenpaiTiles { get; } = [];

    /// <summary>
    /// 当前 14 张未和牌手牌可选择的弃牌听牌候选集合。
    /// </summary>
    public ObservableCollection<TenpaiDiscardOption> TenpaiDiscardOptions { get; } = [];

    /// <summary>
    /// 用户已经在界面声明的杠集合。
    /// </summary>
    public ObservableCollection<DeclaredKanItem> DeclaredKans { get; } = [];

    /// <summary>
    /// 用户当前选择的胡牌状态和算点环境。
    /// </summary>
    public MahjongScoringContext ScoringContext { get; } = new();

    /// <summary>
    /// 请求界面弹出副露面子选择窗口。
    /// </summary>
    public event EventHandler? OpenMeldSelectionRequested;

    /// <summary>
    /// 请求界面弹出杠候选选择窗口。
    /// </summary>
    public event EventHandler<KanSelectionRequestedEventArgs>? KanSelectionRequested;

    /// <summary>
    /// 界面展示的算点选项集合。
    /// </summary>
    public ObservableCollection<MahjongScoringOptionItem> ScoringOptionItems { get; } = [];

    /// <summary>
    /// 自风和场风下拉框共用的东南西北选项。
    /// </summary>
    public IReadOnlyList<MahjongWindOption> WindOptions { get; } = MahjongWindOption.All;

    /// <summary>
    /// 当前自风下拉框选中的选项。
    /// </summary>
    private MahjongWindOption _selectedSelfWindOption = MahjongWindOption.All[0];

    /// <summary>
    /// 当前场风下拉框选中的选项。
    /// </summary>
    private MahjongWindOption _selectedRoundWindOption = MahjongWindOption.All[0];

    /// <summary>
    /// 用户在界面选择的自风；变更后同步写入算点上下文。
    /// </summary>
    public MahjongWindOption SelectedSelfWindOption
    {
        get => _selectedSelfWindOption;
        set
        {
            if (value is null || EqualityComparer<MahjongWindOption>.Default.Equals(_selectedSelfWindOption, value))
            {
                return;
            }

            if (SetProperty(ref _selectedSelfWindOption, value))
            {
                ScoringContext.SelfWind = value.Wind;
            }
        }
    }

    /// <summary>
    /// 用户在界面选择的场风；变更后同步写入算点上下文。
    /// </summary>
    public MahjongWindOption SelectedRoundWindOption
    {
        get => _selectedRoundWindOption;
        set
        {
            if (value is null || EqualityComparer<MahjongWindOption>.Default.Equals(_selectedRoundWindOption, value))
            {
                return;
            }

            if (SetProperty(ref _selectedRoundWindOption, value))
            {
                ScoringContext.RoundWind = value.Wind;
            }
        }
    }

    /// <summary>
    /// 使用默认演示识别服务和默认演示算点服务创建主窗口视图模型。
    /// </summary>
    public MainWindowViewModel()
        : this(new OnnxHandImageRecognizer(), new MahjongHandScoringService())
    {
    }

    /// <summary>
    /// 使用指定识别服务和算点服务创建主窗口视图模型。
    /// </summary>
    /// <param name="recognizer">手牌图片识别服务。</param>
    /// <param name="scoringService">手牌算点服务。</param>
    public MainWindowViewModel(
        IHandImageRecognizer recognizer,
        IHandScoringService scoringService)
    {
        _recognizer = recognizer;
        _scoringService = scoringService;

        foreach (var option in MahjongScoringOptionItem.CreateItems(ScoringContext))
        {
            ScoringOptionItems.Add(option);
        }

        ScoringContext.PropertyChanged += ScoringContext_OnPropertyChanged;
    }

    /// <summary>
    /// 加载指定图片，执行手牌识别，并将识别结果送入算点服务。
    /// </summary>
    /// <param name="imagePath">待加载和识别的图片路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步操作任务。</returns>
    public async Task LoadAndRecognizeAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            StatusMessage = "图片文件不存在。";
            return;
        }

        IsBusy = true;
        StatusMessage = "正在加载图片...";
        SelectedFileName = Path.GetFileName(imagePath);
        ResetResultState();

        try
        {
            using var imageStream = File.OpenRead(imagePath);
            ReplacePreviewImage(new Bitmap(imageStream));

            StatusMessage = "正在识别手牌...";
            var recognitionResult = await _recognizer.RecognizeAsync(imagePath, cancellationToken);

            RecognizedTiles.Clear();
            
            //在界面塞入牌面
            foreach (var tile in recognitionResult.Tiles)
            {
                RecognizedTiles.Add(tile);
            }

            PrepareTileSelection(recognitionResult.Tiles);

            StatusMessage = "正在算点...";
            
            //算点服务入口
            var scoringResult = await _scoringService.CalculateAsync(
                recognitionResult.Tiles,
                ScoringContext,
                cancellationToken);

            if (recognitionResult.Tiles.Count == 14 && !scoringResult.IsWinningHand)
            {
                PrepareDiscardTenpaiSelection(recognitionResult.Tiles);
                if (IsDiscardTenpaiMode)
                {
                    SetDiscardTenpaiWaitingResult();
                    StatusMessage = "请选择可胡牌进行算点。";
                    return;
                }
            }
            
            //界面结果显示
            ApplyScoringResult(scoringResult);

            StatusMessage = "算点完成。";
        }
        catch (Exception ex)
        {
            ResetResultState();
            ScoreSummary = "算点失败";
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 释放当前图片预览占用的位图资源。
    /// </summary>
    public void Dispose()
    {
        ScoringContext.PropertyChanged -= ScoringContext_OnPropertyChanged;
        if (_recognizer is IDisposable disposableRecognizer)
        {
            disposableRecognizer.Dispose();
        }

        PreviewImage?.Dispose();
    }

    /// <summary>
    /// 处理算点上下文变化，并在用户修改胡牌状态后重新算点。
    /// </summary>
    /// <param name="sender">触发属性变化的对象。</param>
    /// <param name="e">属性变化事件参数。</param>
    private async void ScoringContext_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!IsScoringInputProperty(e.PropertyName))
        {
            return;
        }

        if (string.Equals(e.PropertyName, nameof(MahjongScoringContext.IsOpenHand), StringComparison.Ordinal))
        {
            if (ScoringContext.IsOpenHand &&
                RecognizedTiles.Count > 0 &&
                OpenMeldSelectionRequested is not null)
            {
                OpenMeldSelectionRequested.Invoke(this, EventArgs.Empty);
                return;
            }

            if (!ScoringContext.IsOpenHand)
            {
                ScoringContext.SelectedOpenMelds = [];
            }
        }

        if (string.Equals(e.PropertyName, nameof(MahjongScoringContext.WinningTile), StringComparison.Ordinal))
        {
            RefreshWinningTileText();
        }

        if (IsBusy || RecognizedTiles.Count == 0)
        {
            return;
        }

        await RecalculateCurrentHandAsync();
    }

    /// <summary>
    /// 判断指定属性变化是否需要重新算点。
    /// </summary>
    /// <param name="propertyName">发生变化的属性名称。</param>
    /// <returns>如果该属性会影响算点结果，则返回 <c>true</c>。</returns>
    private static bool IsScoringInputProperty(string? propertyName)
    {
        //类似多个 "||"的写法
        return propertyName is null
            or nameof(MahjongScoringContext.WinningTile)
            or nameof(MahjongScoringContext.IsParent)
            or nameof(MahjongScoringContext.SelfWind)
            or nameof(MahjongScoringContext.RoundWind)
            or nameof(MahjongScoringContext.IsRiichi)
            or nameof(MahjongScoringContext.IsDoubleRiichi)
            or nameof(MahjongScoringContext.IsOpenHand)
            or nameof(MahjongScoringContext.IsIppatsu)
            or nameof(MahjongScoringContext.IsTsumo)
            or nameof(MahjongScoringContext.IsHaiDi)
            or nameof(MahjongScoringContext.IsHeDi)
            or nameof(MahjongScoringContext.IsRobKong)
            or nameof(MahjongScoringContext.IsRidgeBlossom)
            or nameof(MahjongScoringContext.SelectedOpenMelds)
            or nameof(MahjongScoringContext.RiichiSticks)
            or nameof(MahjongScoringContext.DoraCount);
    }

    /// <summary>
    /// 增加当前宝牌数量。
    /// </summary>
    [RelayCommand]
    private void IncrementDoraCount()
    {
        ScoringContext.DoraCount++;
    }

    /// <summary>
    /// 减少当前宝牌数量，最小值限制为零。
    /// </summary>
    [RelayCommand]
    private void DecrementDoraCount()
    {
        ScoringContext.DoraCount = Math.Max(0, ScoringContext.DoraCount - 1);
    }

    /// <summary>
    /// 选择立直、一发、自摸的快捷组合。
    /// </summary>
    [RelayCommand]
    private void SelectRiichiIppatsuTsumo()
    {
        ApplyRiichiShortcut(isIppatsu: true, isTsumo: true);
    }

    /// <summary>
    /// 选择立直、一发、荣和的快捷组合。
    /// </summary>
    [RelayCommand]
    private void SelectRiichiIppatsu()
    {
        ApplyRiichiShortcut(isIppatsu: true, isTsumo: false);
    }

    /// <summary>
    /// 选择立直、自摸且非一发的快捷组合。
    /// </summary>
    [RelayCommand]
    private void SelectRiichiTsumo()
    {
        ApplyRiichiShortcut(isIppatsu: false, isTsumo: true);
    }

    /// <summary>
    /// 选择副露自摸的快捷组合。
    /// </summary>
    [RelayCommand]
    private void SelectOpenTsumo()
    {
        ApplyOpenHandShortcut(isTsumo: true);
    }

    /// <summary>
    /// 选择副露荣和的快捷组合。
    /// </summary>
    [RelayCommand]
    private void SelectOpenRon()
    {
        ApplyOpenHandShortcut(isTsumo: false);
    }

    /// <summary>
    /// 应用立直相关快捷组合，并清除与立直互斥的副露和双立直选项。
    /// </summary>
    /// <param name="isIppatsu">是否勾选一发。</param>
    /// <param name="isTsumo">是否勾选自摸。</param>
    private void ApplyRiichiShortcut(bool isIppatsu, bool isTsumo)
    {
        ScoringContext.IsOpenHand = false;
        ScoringContext.IsDoubleRiichi = false;
        ScoringContext.IsRiichi = true;
        ScoringContext.IsIppatsu = isIppatsu;
        ScoringContext.IsTsumo = isTsumo;
    }

    /// <summary>
    /// 应用副露快捷组合，并清除立直、双立直和一发选项。
    /// </summary>
    /// <param name="isTsumo">是否勾选自摸。</param>
    private void ApplyOpenHandShortcut(bool isTsumo)
    {
        ScoringContext.IsRiichi = false;
        ScoringContext.IsDoubleRiichi = false;
        ScoringContext.IsIppatsu = false;
        ScoringContext.IsOpenHand = true;
        ScoringContext.IsTsumo = isTsumo;
    }

    /// <summary>
    /// 从识别出的对子中声明暗杠。
    /// </summary>
    /// <returns>异步操作任务。</returns>
    [RelayCommand]
    private Task AddConcealedKanAsync() =>
        AddKanAsync(DeclaredKanKind.Concealed);

    /// <summary>
    /// 从识别出的四张同牌中声明明杠。
    /// </summary>
    /// <returns>异步操作任务。</returns>
    [RelayCommand]
    private Task AddOpenKanAsync() =>
        AddKanAsync(DeclaredKanKind.Open);

    /// <summary>
    /// 移除用户已经声明的杠并重新算点。
    /// </summary>
    /// <param name="item">要移除的杠声明项。</param>
    /// <returns>异步操作任务。</returns>
    [RelayCommand]
    private async Task RemoveDeclaredKanAsync(DeclaredKanItem? item)
    {
        if (item is null)
        {
            return;
        }

        DeclaredKans.Remove(item);
        SyncDeclaredKansToContext();
        await RecalculateCurrentHandAsync();
    }

    /// <summary>
    /// 应用杠选择弹窗返回的候选牌。
    /// </summary>
    /// <param name="kind">要声明的杠类型。</param>
    /// <param name="tile">用户选择的候选牌；为空时不处理。</param>
    /// <returns>异步操作任务。</returns>
    public async Task ApplyKanSelectionAsync(DeclaredKanKind kind, RecognizedMahjongTile? tile)
    {
        if (tile is null)
        {
            return;
        }

        await AddDeclaredKanAsync(kind, tile);
    }

    /// <summary>
    /// 根据候选数量决定自动声明或请求界面弹窗选择。
    /// </summary>
    /// <param name="kind">要添加的杠类型。</param>
    /// <returns>异步操作任务。</returns>
    private async Task AddKanAsync(DeclaredKanKind kind)
    {
        var candidates = GetKanCandidates(kind);
        if (candidates.Count == 0)
        {
            StatusMessage = kind == DeclaredKanKind.Concealed
                ? "没有可声明为暗杠的对子。"
                : "没有可声明为杠子的四张相同牌。";
            return;
        }

        if (candidates.Count == 1)
        {
            await AddDeclaredKanAsync(kind, candidates[0]);
            return;
        }

        KanSelectionRequested?.Invoke(this, new KanSelectionRequestedEventArgs(kind, candidates));
    }

    /// <summary>
    /// 添加一组杠；同一张牌只能声明一次杠。
    /// </summary>
    /// <param name="kind">要添加的杠类型。</param>
    /// <param name="tile">要声明为杠的牌。</param>
    /// <returns>异步操作任务。</returns>
    private async Task AddDeclaredKanAsync(DeclaredKanKind kind, RecognizedMahjongTile tile)
    {
        if (DeclaredKans.Any(item => string.Equals(item.Tile.Code, tile.Code, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "这个杠子已经声明过。";
            return;
        }

        DeclaredKans.Add(new DeclaredKanItem(kind, tile));
        SyncDeclaredKansToContext();
        if (kind == DeclaredKanKind.Open)
        {
            ScoringContext.IsOpenHand = true;
        }

        StatusMessage = "已添加杠子。";
        await RecalculateCurrentHandAsync();
    }

    /// <summary>
    /// 暗杠候选取两张同牌，明杠候选取四张同牌。
    /// </summary>
    /// <param name="kind">要查找的杠类型。</param>
    /// <returns>可声明为该类型杠的候选牌列表。</returns>
    private IReadOnlyList<RecognizedMahjongTile> GetKanCandidates(DeclaredKanKind kind)
    {
        var requiredCount = kind == DeclaredKanKind.Concealed ? 2 : 4;
        var declaredCodes = DeclaredKans
            .Select(item => item.Tile.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return RecognizedTiles
            .GroupBy(tile => tile.Code, StringComparer.OrdinalIgnoreCase)
            .Where(group => !declaredCodes.Contains(group.Key))
            .Where(group => group.Count() == requiredCount)
            .Select(group => group.First())
            .OrderBy(tile => tile.Code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// 将界面声明的杠同步到算点上下文。
    /// </summary>
    private void SyncDeclaredKansToContext()
    {
        ScoringContext.DeclaredKans = DeclaredKans.Select(item => item.Meld).ToArray();
    }

    /// <summary>
    /// 同步用户选择的听牌胡牌张到算点上下文。
    /// </summary>
    /// <param name="value">用户选择的听牌胡牌张。</param>
    partial void OnSelectedTenpaiTileChanged(RecognizedMahjongTile? value)
    {
        if (value is not null)
        {
            ScoringContext.WinningTile = value;
        }
    }

    /// <summary>
    /// 当打牌胡牌候选变化时刷新候选选中状态。
    /// </summary>
    /// <param name="value">当前选中的打牌胡牌候选。</param>
    partial void OnSelectedTenpaiDiscardWinningOptionChanged(TenpaiDiscardWinningOption? value)
    {
        RefreshTenpaiDiscardWinningSelection(value);
    }

    /// <summary>
    /// 根据用户选择的打牌胡牌候选重新计算当前手牌。
    /// </summary>
    /// <param name="option">用户选择的打牌胡牌候选。</param>
    /// <returns>异步操作任务。</returns>
    [RelayCommand]
    private async Task SelectTenpaiDiscardWinningOptionAsync(TenpaiDiscardWinningOption? option)
    {
        if (option is null)
        {
            return;
        }

        SelectedTenpaiDiscardWinningOption = option;
        IsBusy = true;
        StatusMessage = "正在根据选择的听牌重新算点...";
        ScoreSummary = "正在算点...";
        WinningTileText = $"胡牌张：{option.WinningTile.DisplayName} ({option.WinningTile.Code})";
        WinningShape = "牌型：计算中";

        try
        {
            ScoringContext.WinningTile = option.WinningTile;
            var scoringResult = await _scoringService.CalculateAsync(
                CreateDiscardWinningCalculationTiles(option),
                ScoringContext);

            ApplyScoringResult(scoringResult);
            StatusMessage = "算点完成。";
        }
        catch (Exception ex)
        {
            ScoreSummary = "算点失败";
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 根据识别出的牌数准备听牌候选或胡牌张选择状态。
    /// </summary>
    /// <param name="recognizedTiles">识别出的手牌列表。</param>
    private void PrepareTileSelection(IReadOnlyList<RecognizedMahjongTile> recognizedTiles)
    {
        TenpaiTiles.Clear();
        TenpaiDiscardOptions.Clear();
        SelectedTenpaiDiscardWinningOption = null;
        IsTenpaiMode = recognizedTiles.Count == 13;
        IsWinningTileMode = recognizedTiles.Count == 14;
        IsDiscardTenpaiMode = false;

        if (IsTenpaiMode)
        {
            foreach (var tile in _scoringService.FindTenpaiTiles(recognizedTiles))
            {
                TenpaiTiles.Add(tile);
            }

            SelectedTenpaiTile = TenpaiTiles.FirstOrDefault();
            if (SelectedTenpaiTile is not null)
            {
                ScoringContext.WinningTile = SelectedTenpaiTile;
            }

            return;
        }

        SelectedTenpaiTile = null;
        SelectDefaultWinningTile(recognizedTiles);
    }

    /// <summary>
    /// 为 14 张非和牌手牌准备弃牌听牌候选。
    /// </summary>
    /// <param name="recognizedTiles">识别出的 14 张手牌。</param>
    private void PrepareDiscardTenpaiSelection(IReadOnlyList<RecognizedMahjongTile> recognizedTiles)
    {
        TenpaiTiles.Clear();
        TenpaiDiscardOptions.Clear();
        SelectedTenpaiTile = null;
        SelectedTenpaiDiscardWinningOption = null;

        foreach (var option in _scoringService.FindTenpaiDiscardOptions(recognizedTiles))
        {
            TenpaiDiscardOptions.Add(option);
        }

        IsWinningTileMode = false;
        IsTenpaiMode = false;
        IsDiscardTenpaiMode = TenpaiDiscardOptions.Any(option => option.WinningOptions.Count > 0);
    }

    /// <summary>
    /// 设置等待用户选择弃牌胡牌候选时的界面结果文本。
    /// </summary>
    private void SetDiscardTenpaiWaitingResult()
    {
        ScoreSummary = "请选择可胡牌算点";
        WinningTileText = "胡牌张：未选择";
        WinningShape = "牌型：未计算";
    }

    /// <summary>
    /// 刷新所有弃牌胡牌候选的选中状态。
    /// </summary>
    /// <param name="selectedOption">当前选中的候选；为空时全部取消选中。</param>
    private void RefreshTenpaiDiscardWinningSelection(TenpaiDiscardWinningOption? selectedOption)
    {
        foreach (var option in TenpaiDiscardOptions.SelectMany(discardOption => discardOption.WinningOptions))
        {
            option.IsSelected = ReferenceEquals(option, selectedOption);
        }
    }

    /// <summary>
    /// 识别完成后为当前手牌选择默认胡牌张。
    /// </summary>
    /// <param name="recognizedTiles">识别出的 14 张胡牌状态手牌。</param>
    private void SelectDefaultWinningTile(IReadOnlyList<RecognizedMahjongTile> recognizedTiles)
    {
        if (recognizedTiles.Count == 0)
        {
            ScoringContext.ResetWinningTile();
            return;
        }

        ScoringContext.WinningTile = recognizedTiles[recognizedTiles.Count - 1];
    }

    /// <summary>
    /// 使用当前识别牌和当前算点上下文重新计算结果。
    /// </summary>
    /// <returns>异步操作任务。</returns>
    private async Task RecalculateCurrentHandAsync()
    {
        StatusMessage = "正在根据当前胡牌状态重新算点...";

        try
        {
            var recognizedTiles = IsDiscardTenpaiMode && SelectedTenpaiDiscardWinningOption is not null
                ? CreateDiscardWinningCalculationTiles(SelectedTenpaiDiscardWinningOption)
                : new List<RecognizedMahjongTile>(RecognizedTiles);
            var scoringResult = await _scoringService.CalculateAsync(
                recognizedTiles,
                ScoringContext);

            ApplyScoringResult(scoringResult);
            StatusMessage = "算点完成。";
        }
        catch (Exception ex)
        {
            ScoreSummary = "算点失败";
            StatusMessage = ex.Message;
        }
    }

    /// <summary>
    /// 根据当前识别牌创建副露面子选择弹窗视图模型。
    /// </summary>
    /// <returns>副露面子选择弹窗视图模型。</returns>
    public OpenMeldSelectionViewModel CreateOpenMeldSelectionViewModel()
    {
        var splitter = new DefaultHandSplitter();
        var splits = splitter.Split(new List<RecognizedMahjongTile>(RecognizedTiles));
        return new OpenMeldSelectionViewModel(splits);
    }

    /// <summary>
    /// 应用用户在副露弹窗中确认的副露面子，并立即重新算点。
    /// </summary>
    /// <param name="selectedOpenMelds">用户确认的副露面子列表。</param>
    /// <returns>异步操作任务。</returns>
    public void ApplyOpenMeldSelection(IReadOnlyList<MahjongMeld> selectedOpenMelds)
    {
        ScoringContext.SelectedOpenMelds = selectedOpenMelds;
    }

    /// <summary>
    /// 取消副露面子选择，同时取消“是否副露”选项。
    /// </summary>
    /// <returns>异步操作任务。</returns>
    public async Task CancelOpenMeldSelectionAsync()
    {
        ScoringContext.SelectedOpenMelds = [];
        if (ScoringContext.IsOpenHand)
        {
            ScoringContext.IsOpenHand = false;
            return;
        }

        await RecalculateCurrentHandAsync();
    }

    /// <summary>
    /// 根据当前上下文里的胡牌张刷新界面显示文本。
    /// </summary>
    private void RefreshWinningTileText()
    {
        var winningTile = ScoringContext.WinningTile;
        WinningTileText = $"胡牌张：{winningTile.DisplayName} ({winningTile.Code})";
    }

    /// <summary>
    /// 使用弃牌后剩余手牌和所选胡牌张组成实际算点用的 14 张牌。
    /// </summary>
    /// <param name="option">用户选择的弃牌胡牌候选。</param>
    /// <returns>用于算点的完整手牌。</returns>
    private static IReadOnlyList<RecognizedMahjongTile> CreateDiscardWinningCalculationTiles(TenpaiDiscardWinningOption option) =>
        option.RemainingTiles.Concat([option.WinningTile]).ToArray();

    /// <summary>
    /// 将算点结果写入界面绑定集合和状态属性。
    /// </summary>
    /// <param name="result">算点服务返回的结果。</param>
    private void ApplyScoringResult(MahjongScoringResult result)
    {
        ScoreSummary = result.ScoreSummary;
        WinningTileText = $"胡牌张：{result.WinningTile.DisplayName} ({result.WinningTile.Code})";
        WinningShape = $"牌型：{result.WinningShape}";
    }

    /// <summary>
    /// 清空当前识别和算点结果，恢复等待状态。
    /// </summary>
    private void ResetResultState()
    {
        RecognizedTiles.Clear();
        TenpaiTiles.Clear();
        TenpaiDiscardOptions.Clear();
        DeclaredKans.Clear();
        ScoringContext.SelectedOpenMelds = [];
        ScoringContext.DeclaredKans = [];
        SelectedTenpaiTile = null;
        SelectedTenpaiDiscardWinningOption = null;
        IsTenpaiMode = false;
        IsWinningTileMode = false;
        IsDiscardTenpaiMode = false;
        ScoreSummary = "等待算点";
        WinningTileText = "胡牌张：未计算";
        WinningShape = "牌型：未计算";
    }

    /// <summary>
    /// 替换当前预览图片，并释放旧的位图资源。
    /// </summary>
    /// <param name="bitmap">新的预览位图。</param>
    private void ReplacePreviewImage(Bitmap bitmap)
    {
        var oldPreview = PreviewImage;
        PreviewImage = bitmap;
        oldPreview?.Dispose();
    }
}
