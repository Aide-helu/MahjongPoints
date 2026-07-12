using Android.App;
using Android.Content;
using Android.Provider;
using Bitmap = global::Android.Graphics.Bitmap;

namespace MahjongPoints.Android.Services;

public sealed class AndroidImageInputService(MainActivity activity) : IImageInputService
{
    public Task<ImageInputResult?> CapturePhotoAsync() => activity.CapturePhotoAsync();

    public Task<ImageInputResult?> PickPhotoAsync() => activity.PickPhotoAsync();

    internal static ImageInputResult FromAndroidBitmap(Activity activity, Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Compress(Bitmap.CompressFormat.Png!, 100, stream);
        var path = CreateCacheImagePath(activity, "camera");
        File.WriteAllBytes(path, stream.ToArray());
        return new ImageInputResult(path, "摄像头拍摄", DeleteAfterLoad: true);
    }

    internal static ImageInputResult? FromContentUri(Activity activity, Intent? data)
    {
        var uri = data?.Data;
        if (uri is null)
        {
            return null;
        }

        using var input = activity.ContentResolver?.OpenInputStream(uri);
        if (input is null)
        {
            return null;
        }

        using var stream = new MemoryStream();
        input.CopyTo(stream);
        var path = CreateCacheImagePath(activity, "album");
        File.WriteAllBytes(path, stream.ToArray());
        return new ImageInputResult(path, "相册图片", DeleteAfterLoad: true);
    }

    internal static Intent CreatePickImageIntent()
    {
        var intent = new Intent(Intent.ActionPick, MediaStore.Images.Media.ExternalContentUri);
        intent.SetType("image/*");
        return Intent.CreateChooser(intent, "选择图片")!;
    }

    internal static Intent CreateCaptureImageIntent() => new(MediaStore.ActionImageCapture);

    private static string CreateCacheImagePath(Activity activity, string prefix)
    {
        var cacheDirectory = activity.CacheDir?.AbsolutePath ?? Path.GetTempPath();
        Directory.CreateDirectory(cacheDirectory);
        return Path.Combine(cacheDirectory, $"mahjongpoints-{prefix}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png");
    }
}
