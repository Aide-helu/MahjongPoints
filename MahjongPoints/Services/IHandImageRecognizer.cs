using System.Threading;
using System.Threading.Tasks;
using MahjongPoints.Models;

namespace MahjongPoints.Services;

public interface IHandImageRecognizer
{
    Task<MahjongHandRecognitionResult> RecognizeAsync(
        string imagePath,
        CancellationToken cancellationToken = default);
}
