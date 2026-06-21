using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
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
    /// 当前选择的图片完整路径。
    /// </summary>
    [ObservableProperty]
    private string? _selectedImagePath;

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
    private string _statusMessage = "请选择一张图片，demo 会返回固定的胡牌手牌并计算点数。";

    /// <summary>
    /// 手牌识别摘要。
    /// </summary>
    [ObservableProperty]
    private string _recognitionSummary = "等待识别";

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
    /// 算点流程提示文本。
    /// </summary>
    [ObservableProperty]
    private string _scoringMessage = "当前结果会在识别后自动送入算点逻辑。";

    /// <summary>
    /// 当前识别结果是否被算点服务判定为和牌。
    /// </summary>
    [ObservableProperty]
    private bool _isWinningHand;

    /// <summary>
    /// 当前是否正在执行图片加载、识别或算点流程。
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// 界面展示的识别牌集合。
    /// </summary>
    public ObservableCollection<RecognizedMahjongTile> RecognizedTiles { get; } = [];

    /// <summary>
    /// 界面展示的算点用完整牌集合。
    /// </summary>
    public ObservableCollection<RecognizedMahjongTile> CalculationTiles { get; } = [];

    /// <summary>
    /// 界面展示的役种或得分明细集合。
    /// </summary>
    public ObservableCollection<MahjongScoreItem> ScoreItems { get; } = [];

    /// <summary>
    /// 用户当前选择的胡牌状态和算点环境。
    /// </summary>
    public MahjongScoringContext ScoringContext { get; } = new();

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
        : this(new HardcodedHandImageRecognizer(), new HardcodedHandScoringService())
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
        SelectedImagePath = imagePath;
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

            SelectDefaultWinningTile(recognitionResult.Tiles);

            RecognitionSummary = $"{recognitionResult.Tiles.Count} 张手牌 | {recognitionResult.InferenceMode} | {recognitionResult.ModelName}";

            StatusMessage = "正在算点...";
            
            //算点服务入口
            var scoringResult = await _scoringService.CalculateAsync(
                recognitionResult.Tiles,
                ScoringContext,
                cancellationToken);
            
            //界面结果显示
            ApplyScoringResult(scoringResult);

            StatusMessage = $"{recognitionResult.Message} {scoringResult.Message}";
        }
        catch (Exception ex)
        {
            ResetResultState();
            RecognitionSummary = "识别失败";
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
            or nameof(MahjongScoringContext.RiichiSticks);
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
            var recognizedTiles = new List<RecognizedMahjongTile>(RecognizedTiles);
            var scoringResult = await _scoringService.CalculateAsync(
                recognizedTiles,
                ScoringContext);

            ApplyScoringResult(scoringResult);
            StatusMessage = scoringResult.Message;
        }
        catch (Exception ex)
        {
            ScoreSummary = "算点失败";
            ScoringMessage = ex.Message;
            StatusMessage = ex.Message;
        }
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
    /// 将算点结果写入界面绑定集合和状态属性。
    /// </summary>
    /// <param name="result">算点服务返回的结果。</param>
    private void ApplyScoringResult(MahjongScoringResult result)
    {
        CalculationTiles.Clear();
        foreach (var tile in result.CalculationTiles)
        {
            CalculationTiles.Add(tile);
        }

        ScoreItems.Clear();
        foreach (var item in result.Items)
        {
            ScoreItems.Add(item);
        }

        IsWinningHand = result.IsWinningHand;
        ScoreSummary = result.ScoreSummary;
        WinningTileText = $"胡牌张：{result.WinningTile.DisplayName} ({result.WinningTile.Code})";
        WinningShape = $"牌型：{result.WinningShape}";
        ScoringMessage = result.Message;
    }

    /// <summary>
    /// 清空当前识别和算点结果，恢复等待状态。
    /// </summary>
    private void ResetResultState()
    {
        RecognizedTiles.Clear();
        CalculationTiles.Clear();
        ScoreItems.Clear();
        RecognitionSummary = "等待识别";
        ScoreSummary = "等待算点";
        WinningTileText = "胡牌张：未计算";
        WinningShape = "牌型：未计算";
        ScoringMessage = "当前结果会在识别后自动送入算点逻辑。";
        IsWinningHand = false;
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
