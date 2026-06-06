namespace MahjongPoints.Models;

public sealed record RecognizedMahjongTile(
    string Code,
    string DisplayName,
    double Confidence)
{
    public string ConfidenceText => $"{Confidence:P0}";
}
