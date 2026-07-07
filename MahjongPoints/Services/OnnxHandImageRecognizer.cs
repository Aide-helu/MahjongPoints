using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MahjongPoints.Services;

public sealed class OnnxHandImageRecognizer : IHandImageRecognizer, IDisposable
{
    private const int InputSize = 640;
    private const float ConfidenceThreshold = 0.5f;
    private const float IouThreshold = 0.5f;

    private static readonly string[] _classNames =
    [
        "1m", "1p", "1s", "2m", "2p", "2s", "3m", "3p", "3s",
        "4m", "4p", "4s", "5m", "5p", "5s", "6m", "6p", "6s",
        "7m", "7p", "7s", "8m", "8p", "8s", "9m", "9p", "9s",
        "chun", "haku", "hatsu", "nan", "pe", "sha", "tou",
    ];

    private static readonly IReadOnlyDictionary<string, string> _codeMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tou"] = "1z",
            ["nan"] = "2z",
            ["sha"] = "3z",
            ["pe"] = "4z",
            ["haku"] = "5z",
            ["hatsu"] = "6z",
            ["chun"] = "7z",
        };

    private static readonly IReadOnlyDictionary<string, string> _displayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["1m"] = "一万", ["2m"] = "二万", ["3m"] = "三万", ["4m"] = "四万", ["5m"] = "五万", ["6m"] = "六万", ["7m"] = "七万", ["8m"] = "八万", ["9m"] = "九万",
            ["1p"] = "一筒", ["2p"] = "二筒", ["3p"] = "三筒", ["4p"] = "四筒", ["5p"] = "五筒", ["6p"] = "六筒", ["7p"] = "七筒", ["8p"] = "八筒", ["9p"] = "九筒",
            ["1s"] = "一索", ["2s"] = "二索", ["3s"] = "三索", ["4s"] = "四索", ["5s"] = "五索", ["6s"] = "六索", ["7s"] = "七索", ["8s"] = "八索", ["9s"] = "九索",
            ["1z"] = "东", ["2z"] = "南", ["3z"] = "西", ["4z"] = "北", ["5z"] = "白", ["6z"] = "发", ["7z"] = "中",
        };

    private readonly string _modelPath;
    private readonly Lazy<InferenceSession> _session;

    public OnnxHandImageRecognizer(string? modelPath = null)
    {
        _modelPath = FirstExisting(
            modelPath,
            Environment.GetEnvironmentVariable("MAHJONG_ONNX_MODEL"),
            Path.Combine(AppContext.BaseDirectory, "Models", "shiranai_shallow_best.onnx"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Models", "shiranai_shallow_best.onnx")));
        _session = new Lazy<InferenceSession>(() => new InferenceSession(_modelPath));
    }

    public Task<MahjongHandRecognitionResult> RecognizeAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("Image file not found.", imagePath);
        }

        var input = CreateInputTensor(imagePath);
        using var results = _session.Value.Run([NamedOnnxValue.CreateFromTensor("images", input)]);
        var tiles = Postprocess(results[0].AsTensor<float>())
            .OrderBy(detection => detection.X1)
            .Select(detection =>
            {
                var rawCode = _classNames[detection.ClassId];
                var code = _codeMap.GetValueOrDefault(rawCode, rawCode);
                return new RecognizedMahjongTile(
                    code,
                    _displayNames.GetValueOrDefault(code, code),
                    detection.Confidence);
            })
            .ToArray();

        return Task.FromResult(new MahjongHandRecognitionResult(
            tiles,
            Path.GetFileName(_modelPath),
            "ONNX Runtime",
            $"识别完成：{tiles.Length} 张。"));
    }

    public void Dispose()
    {
        if (_session.IsValueCreated)
        {
            _session.Value.Dispose();
        }
    }

    private static DenseTensor<float> CreateInputTensor(string imagePath)
    {
        using var source = new Bitmap(imagePath);
        var scale = InputSize / (float)Math.Max(source.Width, source.Height);
        var resizedWidth = (int)(source.Width * scale);
        var resizedHeight = (int)(source.Height * scale);
        var padX = (InputSize - resizedWidth) / 2;
        var padY = (InputSize - resizedHeight) / 2;

        using var graySource = CreateAutocontrastGrayImage(source);
        using var canvas = new Bitmap(InputSize, InputSize);
        using (var graphics = Graphics.FromImage(canvas))
        {
            graphics.Clear(Color.Black);
            graphics.InterpolationMode = InterpolationMode.Bilinear;
            graphics.DrawImage(graySource, padX, padY, resizedWidth, resizedHeight);
        }

        var tensor = new DenseTensor<float>([1, 3, InputSize, InputSize]);
        for (var y = 0; y < InputSize; y++)
        {
            for (var x = 0; x < InputSize; x++)
            {
                var value = canvas.GetPixel(x, y).R / 255f;
                tensor[0, 0, y, x] = value;
                tensor[0, 1, y, x] = value;
                tensor[0, 2, y, x] = value;
            }
        }

        return tensor;
    }

    private static Bitmap CreateAutocontrastGrayImage(Bitmap source)
    {
        var gray = new byte[source.Width * source.Height];
        var min = byte.MaxValue;
        var max = byte.MinValue;
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);
                var value = (byte)Math.Clamp((int)MathF.Round(pixel.R * 0.299f + pixel.G * 0.587f + pixel.B * 0.114f), 0, 255);
                gray[y * source.Width + x] = value;
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }
        }

        var bitmap = new Bitmap(source.Width, source.Height);
        var divisor = Math.Max(1, max - min);
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var stretched = (byte)Math.Clamp((gray[y * source.Width + x] - min) * 255 / divisor, 0, 255);
                bitmap.SetPixel(x, y, Color.FromArgb(stretched, stretched, stretched));
            }
        }

        return bitmap;
    }

    private static IReadOnlyList<Detection> Postprocess(Tensor<float> output)
    {
        var detections = new List<Detection>();
        for (var i = 0; i < output.Dimensions[2]; i++)
        {
            var classId = 0;
            var confidence = output[0, 4, i];
            for (var c = 1; c < _classNames.Length; c++)
            {
                var score = output[0, 4 + c, i];
                if (score > confidence)
                {
                    confidence = score;
                    classId = c;
                }
            }

            if (confidence <= ConfidenceThreshold)
            {
                continue;
            }

            var centerX = output[0, 0, i];
            var centerY = output[0, 1, i];
            var width = output[0, 2, i];
            var height = output[0, 3, i];
            detections.Add(new Detection(
                centerX - width / 2,
                centerY - height / 2,
                centerX + width / 2,
                centerY + height / 2,
                classId,
                confidence));
        }

        return NonMaxSuppress(detections);
    }

    private static IReadOnlyList<Detection> NonMaxSuppress(List<Detection> detections)
    {
        var kept = new List<Detection>();
        foreach (var detection in detections.OrderByDescending(detection => detection.Confidence))
        {
            if (kept.All(keptDetection => CalculateIou(detection, keptDetection) <= IouThreshold))
            {
                kept.Add(detection);
            }
        }

        return kept;
    }

    private static float CalculateIou(Detection a, Detection b)
    {
        var x1 = Math.Max(a.X1, b.X1);
        var y1 = Math.Max(a.Y1, b.Y1);
        var x2 = Math.Min(a.X2, b.X2);
        var y2 = Math.Min(a.Y2, b.Y2);
        var intersection = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        var union = a.Area + b.Area - intersection;
        return union <= 0 ? 0 : intersection / union;
    }

    private static string FirstExisting(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) && File.Exists(value))
            {
                return value;
            }
        }

        throw new FileNotFoundException("ONNX model file not found.");
    }

    private sealed record Detection(
        float X1,
        float Y1,
        float X2,
        float Y2,
        int ClassId,
        float Confidence)
    {
        public float Area => Math.Max(0, X2 - X1) * Math.Max(0, Y2 - Y1);
    }
}
