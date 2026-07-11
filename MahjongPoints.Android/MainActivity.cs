using Avalonia.Android;

namespace MahjongPoints.Android;

[global::Android.App.Activity(
    Label = "MahjongPoints",
    MainLauncher = true,
    ConfigurationChanges =
        global::Android.Content.PM.ConfigChanges.Orientation |
        global::Android.Content.PM.ConfigChanges.ScreenSize |
        global::Android.Content.PM.ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity;
