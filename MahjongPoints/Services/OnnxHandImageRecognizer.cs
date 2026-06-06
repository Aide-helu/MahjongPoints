using System;
using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;

namespace MahjongPoints.Services;

public sealed class OnnxHandImageRecognizer : IHandImageRecognizer
{
    private readonly string _modelPath;

    public OnnxHandImageRecognizer(string modelPath)
    {
        _modelPath = string.IsNullOrWhiteSpace(modelPath)
            ? throw new ArgumentException("Model path cannot be empty.", nameof(modelPath))
            : modelPath;
    }

    public Task<MahjongHandRecognitionResult> RecognizeAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            $"ONNX inference is not wired yet. Model path: {_modelPath}");
    }
}
