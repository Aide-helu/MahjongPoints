using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MahjongPoints.Android.Services;

namespace MahjongPoints.Preview;

public sealed class PreviewImageInputService(Window owner) : IImageInputService
{
    private static readonly FilePickerFileType Images = new("图片")
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"],
        MimeTypes = ["image/png", "image/jpeg", "image/bmp", "image/webp"]
    };

    public Task<ImageInputResult?> CapturePhotoAsync() => PickPhotoAsync();

    public async Task<ImageInputResult?> PickPhotoAsync()
    {
        var storage = TopLevel.GetTopLevel(owner)?.StorageProvider;
        if (storage is null)
        {
            return null;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择图片",
            AllowMultiple = false,
            FileTypeFilter = [Images]
        });

        var file = files.FirstOrDefault();
        if (file is null)
        {
            return null;
        }

        if (file.Path.IsFile)
        {
            return new ImageInputResult(file.Path.LocalPath, file.Name);
        }

        await using var source = await file.OpenReadAsync();
        var path = Path.Combine(Path.GetTempPath(), $"mahjongpoints-preview-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png");
        await using var target = File.Create(path);
        await source.CopyToAsync(target);
        return new ImageInputResult(path, file.Name);
    }
}
