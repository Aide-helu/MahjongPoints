using System.Collections.Generic;

namespace MahjongPoints.Models;

public sealed record MahjongHandRecognitionResult(
    IReadOnlyList<RecognizedMahjongTile> Tiles,
    string ModelName,
    string InferenceMode,
    string Message);
