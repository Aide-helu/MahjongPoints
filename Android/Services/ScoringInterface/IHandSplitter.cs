using System.Collections.Generic;
using MahjongPoints.Models;

namespace MahjongPoints.Services.Scoring;

/// <summary>
/// 定义手牌拆解器。
/// </summary>
public interface IHandSplitter
{
    /// <summary>
    /// 把完整手牌拆解为所有可能的 4 面子加 1 雀头结构。
    /// </summary>
    /// <param name="tiles">参与拆解的完整牌列表。</param>
    /// <returns>所有可用的手牌拆解结果。</returns>
    IReadOnlyList<MahjongHandSplitResult> Split(IReadOnlyList<RecognizedMahjongTile> tiles);
}
