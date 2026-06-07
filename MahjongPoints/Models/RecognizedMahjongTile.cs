namespace MahjongPoints.Models;

/// <summary>
/// 表示从图片中识别出的一张麻将牌。
/// </summary>
/// <param name="Code">麻将牌编码，例如 <c>2m</c>、<c>5p</c>。</param>
/// <param name="DisplayName">界面显示名称。</param>
/// <param name="Confidence">识别置信度，取值通常为 0 到 1。</param>
public sealed record RecognizedMahjongTile(
    string Code,
    string DisplayName,
    double Confidence)
{
    /// <summary>
    /// 识别置信度的百分比显示文本。
    /// </summary>
    public string ConfidenceText => $"{Confidence:P0}";
}
