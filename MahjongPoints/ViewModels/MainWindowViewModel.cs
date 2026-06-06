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

    [ObservableProperty]
    private string? selectedImagePath;

    [ObservableProperty]
    private string selectedFileName = "未选择图片";

    [ObservableProperty]
    private Bitmap? previewImage;

    [ObservableProperty]
    private string statusMessage = "请选择一张包含十三张手牌的图片。";

    [ObservableProperty]
    private string recognitionSummary = "等待识别";

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<RecognizedMahjongTile> RecognizedTiles { get; } = [];

    public MainWindowViewModel()
        : this(new HardcodedHandImageRecognizer())
    {
    }

    public MainWindowViewModel(IHandImageRecognizer recognizer)
    {
        _recognizer = recognizer;
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

        try
        {
            using var imageStream = File.OpenRead(imagePath);
            ReplacePreviewImage(new Bitmap(imageStream));

            StatusMessage = "正在识别手牌...";
            var result = await _recognizer.RecognizeAsync(imagePath, cancellationToken);

            RecognizedTiles.Clear();
            foreach (var tile in result.Tiles)
            {
                RecognizedTiles.Add(tile);
            }

            RecognitionSummary = $"{result.Tiles.Count} 张手牌 | {result.InferenceMode} | {result.ModelName}";
            StatusMessage = result.Message;
        }
        catch (Exception ex)
        {
            RecognizedTiles.Clear();
            RecognitionSummary = "识别失败";
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

    private void ReplacePreviewImage(Bitmap bitmap)
    {
        var oldPreview = PreviewImage;
        PreviewImage = bitmap;
        oldPreview?.Dispose();
    }
}
