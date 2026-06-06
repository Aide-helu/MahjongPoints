using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using MahjongPoints.Models;
using MahjongPoints.Services;

namespace MahjongPoints.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IHandImageRecognizer _recognizer;
    private readonly IHandScoringService _scoringService;

    [ObservableProperty]
    private string? selectedImagePath;

    [ObservableProperty]
    private string selectedFileName = "未选择图片";

    [ObservableProperty]
    private Bitmap? previewImage;

    [ObservableProperty]
    private string statusMessage = "请选择一张图片，demo 会返回固定的胡牌手牌并计算点数。";

    [ObservableProperty]
    private string recognitionSummary = "等待识别";

    [ObservableProperty]
    private string scoreSummary = "等待算点";

    [ObservableProperty]
    private string winningTileText = "胡牌张：未计算";

    [ObservableProperty]
    private string winningShape = "牌型：未计算";

    [ObservableProperty]
    private string scoringMessage = "当前结果会在识别后自动送入算点逻辑。";

    [ObservableProperty]
    private bool isWinningHand;

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<RecognizedMahjongTile> RecognizedTiles { get; } = [];

    public ObservableCollection<RecognizedMahjongTile> CalculationTiles { get; } = [];

    public ObservableCollection<MahjongScoreItem> ScoreItems { get; } = [];

    public MainWindowViewModel()
        : this(new HardcodedHandImageRecognizer(), new HardcodedHandScoringService())
    {
    }

    public MainWindowViewModel(
        IHandImageRecognizer recognizer,
        IHandScoringService scoringService)
    {
        _recognizer = recognizer;
        _scoringService = scoringService;
    }

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
            foreach (var tile in recognitionResult.Tiles)
            {
                RecognizedTiles.Add(tile);
            }

            RecognitionSummary =
                $"{recognitionResult.Tiles.Count} 张手牌 | {recognitionResult.InferenceMode} | {recognitionResult.ModelName}";

            StatusMessage = "正在算点...";
            var scoringResult = await _scoringService.CalculateAsync(recognitionResult.Tiles, cancellationToken);
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

    public void Dispose()
    {
        PreviewImage?.Dispose();
    }

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

    private void ReplacePreviewImage(Bitmap bitmap)
    {
        var oldPreview = PreviewImage;
        PreviewImage = bitmap;
        oldPreview?.Dispose();
    }
}
