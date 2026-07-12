using Android.App;
using Android.Content;
using Avalonia.Android;
using MahjongPoints.Android.Services;

namespace MahjongPoints.Android;

[global::Android.App.Activity(
    Label = "MahjongPoints",
    MainLauncher = true,
    Theme = "@style/AppTheme",
    ConfigurationChanges =
        global::Android.Content.PM.ConfigChanges.Orientation |
        global::Android.Content.PM.ConfigChanges.ScreenSize |
        global::Android.Content.PM.ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    private const int CapturePhotoRequest = 2001;
    private const int PickPhotoRequest = 2002;

    private TaskCompletionSource<ImageInputResult?>? pendingImage;

    protected override void OnCreate(global::Android.OS.Bundle? savedInstanceState)
    {
        ImageInput.Current = new AndroidImageInputService(this);
        base.OnCreate(savedInstanceState);
    }

    internal Task<ImageInputResult?> CapturePhotoAsync()
    {
        if (pendingImage is not null)
        {
            return pendingImage.Task;
        }

        pendingImage = new TaskCompletionSource<ImageInputResult?>();
        StartActivityForResult(AndroidImageInputService.CreateCaptureImageIntent(), CapturePhotoRequest);
        return pendingImage.Task;
    }

    internal Task<ImageInputResult?> PickPhotoAsync()
    {
        if (pendingImage is not null)
        {
            return pendingImage.Task;
        }

        pendingImage = new TaskCompletionSource<ImageInputResult?>();
        StartActivityForResult(AndroidImageInputService.CreatePickImageIntent(), PickPhotoRequest);
        return pendingImage.Task;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        var completion = pendingImage;
        pendingImage = null;

        if (completion is null)
        {
            return;
        }

        if (resultCode != Result.Ok)
        {
            completion.TrySetResult(null);
            return;
        }

        if (requestCode == CapturePhotoRequest &&
            data?.Extras?.Get("data") is global::Android.Graphics.Bitmap bitmap)
        {
            completion.TrySetResult(AndroidImageInputService.FromAndroidBitmap(this, bitmap));
            return;
        }

        if (requestCode == PickPhotoRequest)
        {
            completion.TrySetResult(AndroidImageInputService.FromContentUri(this, data));
            return;
        }

        completion.TrySetResult(null);
    }
}
