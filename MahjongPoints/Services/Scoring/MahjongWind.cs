namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 表示日麻中的东南西北四种风位。
/// </summary>
public enum MahjongWind
{
    /// <summary>
    /// 东风。
    /// </summary>
    East,

    /// <summary>
    /// 南风。
    /// </summary>
    South,

    /// <summary>
    /// 西风。
    /// </summary>
    West,

    /// <summary>
    /// 北风。
    /// </summary>
    North
}

/// <summary>
/// 提供风位到牌编码和界面文本的转换。
/// </summary>
public static class MahjongWindExtensions
{
    /// <summary>
    /// 将风位转换为字牌编码，供判役逻辑和牌面编码比较。
    /// </summary>
    /// <param name="wind">要转换的风位。</param>
    /// <returns>风牌编码，东南西北分别为 <c>1z</c> 到 <c>4z</c>。</returns>
    public static string ToTileCode(this MahjongWind wind)
    {
        return wind switch
        {
            MahjongWind.East => "1z",
            MahjongWind.South => "2z",
            MahjongWind.West => "3z",
            MahjongWind.North => "4z",
            _ => "1z"
        };
    }

    /// <summary>
    /// 将风位转换为界面显示文本。
    /// </summary>
    /// <param name="wind">要转换的风位。</param>
    /// <returns>东、南、西、北中的一个。</returns>
    public static string ToDisplayName(this MahjongWind wind)
    {
        return wind switch
        {
            MahjongWind.East => "东",
            MahjongWind.South => "南",
            MahjongWind.West => "西",
            MahjongWind.North => "北",
            _ => "东"
        };
    }
}
