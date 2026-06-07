using System;
using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;

namespace MahjongPoints.Services;

/// <summary>
/// ONNX 手牌图片识别服务占位实现。
/// </summary>
public sealed class OnnxHandImageRecognizer : IHandImageRecognizer
{
    /// <summary>
    /// ONNX 模型文件路径。
    /// </summary>
    private readonly string _modelPath;

    /// <summary>
    /// 使用指定模型路径创建 ONNX 手牌图片识别服务。
    /// </summary>
    /// <param name="modelPath">ONNX 模型文件路径。</param>
    /// <exception cref="ArgumentException">当模型路径为空时抛出。</exception>
    public OnnxHandImageRecognizer(string modelPath)
    {
        _modelPath = string.IsNullOrWhiteSpace(modelPath)
            ? throw new ArgumentException("Model path cannot be empty.", nameof(modelPath))
            : modelPath;
    }

    /// <summary>
    /// 使用 ONNX 模型识别图片中的麻将牌。
    /// </summary>
    /// <param name="imagePath">待识别图片路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>图片识别结果。</returns>
    /// <exception cref="NotImplementedException">当前尚未接入真实 ONNX 推理时抛出。</exception>
    public Task<MahjongHandRecognitionResult> RecognizeAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            $"ONNX inference is not wired yet. Model path: {_modelPath}");
    }
}
