using System;
using System.Collections.Concurrent;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

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
    private static readonly ConcurrentDictionary<string, Bitmap> _imageCache = new(StringComparer.Ordinal);

    public string ImagePath => GetImagePath(Code);

    public Bitmap TileImage => _imageCache.GetOrAdd(ImagePath, LoadImage);

    private static string GetImagePath(string code)
    {
        if (code.Length == 2 && char.IsDigit(code[0]))
        {
            return code[1] switch
            {
                'm' or 'M' => $"avares://MahjongPoints/Images/manzu/m_{code[0]}.png",
                'p' or 'P' => $"avares://MahjongPoints/Images/pinzu/p_{code[0]}.png",
                's' or 'S' => $"avares://MahjongPoints/Images/sozu/s_{code[0]}.png",
                'z' or 'Z' => $"avares://MahjongPoints/Images/tupai/z_{code[0]}.png",
                _ => "avares://MahjongPoints/Images/tupai/z_5.png"
            };
        }

        return "avares://MahjongPoints/Images/tupai/z_5.png";
    }

    private static Bitmap LoadImage(string path)
    {
        using var stream = AssetLoader.Open(new Uri(path));
        return new Bitmap(stream);
    }
}
