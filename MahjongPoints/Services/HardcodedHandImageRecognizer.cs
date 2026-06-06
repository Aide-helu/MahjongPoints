using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;

namespace MahjongPoints.Services;

public sealed class HardcodedHandImageRecognizer : IHandImageRecognizer
{
    private static readonly RecognizedMahjongTile[] DemoTiles =
    [
        new("2m", "二万", 0.99),
        new("3m", "三万", 0.98),
        new("4m", "四万", 0.98),
        new("3m", "三万", 0.98),
        new("4m", "四万", 0.97),
        new("5m", "五万", 0.97),
        new("4p", "四筒", 0.97),
        new("5p", "五筒", 0.96),
        new("6p", "六筒", 0.96),
        new("6s", "六条", 0.95),
        new("7s", "七条", 0.95),
        new("8s", "八条", 0.94),
        new("5p", "五筒", 0.93)
    ];

    public Task<MahjongHandRecognitionResult> RecognizeAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new MahjongHandRecognitionResult(
            DemoTiles,
            "Hardcoded demo",
            "Stub",
            "当前未接入 ONNX 模型，返回固定的 13 张手牌。");

        return Task.FromResult(result);
    }
}
