using System;
using System.Collections.ObjectModel;
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

    public MahjongScoringContext ScoringContext { get; } = new();

    public ObservableCollection<MahjongScoringOptionItem> ScoringOptionItems { get; } = [];

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

            RecognitionSummary = $"{recognitionResult.Tiles.Count} 张手牌 | {recognitionResult.InferenceMode} | {recognitionResult.ModelName}";

            StatusMessage = "正在算点...";
            
            //算点服务入口
            var scoringResult = await _scoringService.CalculateAsync(recognitionResult.Tiles, cancellationToken);
            
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
        PreviewImage?.Dispose();
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
