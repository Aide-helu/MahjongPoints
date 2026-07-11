namespace MahjongPoints.Android.Services;

public sealed record ImageInputResult(string ImagePath, string SourceName);

public interface IImageInputService
{
    Task<ImageInputResult?> CapturePhotoAsync();

    Task<ImageInputResult?> PickPhotoAsync();
}

public static class ImageInput
{
    public static IImageInputService Current { get; set; } = new EmptyImageInputService();

    private sealed class EmptyImageInputService : IImageInputService
    {
        public Task<ImageInputResult?> CapturePhotoAsync() => Task.FromResult<ImageInputResult?>(null);

        public Task<ImageInputResult?> PickPhotoAsync() => Task.FromResult<ImageInputResult?>(null);
    }
}
