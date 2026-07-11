using Avalonia.Controls;
using MahjongPoints.Android.Services;

namespace MahjongPoints.Preview;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        ImageInput.Current = new PreviewImageInputService(this);
        InitializeComponent();
    }
}
