using System;
using Avalonia;
using Avalonia.Android;

namespace MahjongPoints.Android;

[global::Android.App.Application]
public class MainApplication(
    IntPtr handle,
    global::Android.Runtime.JniHandleOwnership ownership)
    : AvaloniaAndroidApplication<App>(handle, ownership)
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .LogToTrace();
}
