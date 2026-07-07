using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;

namespace MahjongPoints.Services;

/// <summary>
/// 演示用手牌图片识别服务，始终返回固定的 14 张胡牌状态手牌。
/// </summary>
public sealed class HardcodedHandImageRecognizer : IHandImageRecognizer
{
    /// <summary>
    /// 演示识别结果中固定返回的麻将牌列表。也是ONNX要识别出并转化成的标准格式
    /// </summary>
    private static readonly RecognizedMahjongTile[] _demoTiles =
    [
        new("1m", "一万", 0.99),
        new("2m", "二万", 0.98),
        new("3m", "三万", 0.98),
        new("5m", "五万", 0.99),
        new("5m", "五万", 0.98),
        new("3p", "三筒", 0.98),
        new("4p", "四筒", 0.99),
        new("5p", "五筒", 0.98),
        new("2s", "二条", 0.98),
        new("3s", "三条", 0.98),
        new("4s", "四条", 0.95),
        new("5s", "五条", 0.94),
        new("6s", "六条", 0.94),
        new("7s", "七条", 0.93),
        
    ];

    /// <summary>
    /// 返回固定的演示识别结果。
    /// </summary>
    /// <param name="imagePath">待识别图片路径，当前演示实现不会读取此文件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>固定的手牌识别结果。</returns>
    public Task<MahjongHandRecognitionResult> RecognizeAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new MahjongHandRecognitionResult(
            _demoTiles,
            "Hardcoded demo",
            "Stub",
            "当前未接入 ONNX 模型，返回固定的 14 张胡牌状态手牌。");

        return Task.FromResult(result);
    }
}
