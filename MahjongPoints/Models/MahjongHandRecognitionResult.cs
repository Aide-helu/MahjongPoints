using System.Collections.Generic;

namespace MahjongPoints.Models;

/// <summary>
/// 表示图片识别服务返回的手牌识别结果。
/// </summary>
/// <param name="Tiles">识别出的麻将牌列表。</param>
/// <param name="ModelName">识别模型名称。</param>
/// <param name="InferenceMode">推理模式说明。</param>
/// <param name="Message">识别流程提示信息。</param>
public sealed record MahjongHandRecognitionResult(
    IReadOnlyList<RecognizedMahjongTile> Tiles,
    string ModelName,
    string InferenceMode,
    string Message);
