using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MahjongPoints.Android.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
