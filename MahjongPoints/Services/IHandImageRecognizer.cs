using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;

namespace MahjongPoints.Services;

/// <summary>
/// 定义麻将图片识别服务。
/// </summary>
public interface IHandImageRecognizer
{
    /// <summary>
    /// 从指定图片中识别麻将牌。
    /// </summary>
    /// <param name="imagePath">待识别图片路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>图片识别结果。</returns>
    Task<MahjongHandRecognitionResult> RecognizeAsync(
        string imagePath,
        CancellationToken cancellationToken = default);
}
