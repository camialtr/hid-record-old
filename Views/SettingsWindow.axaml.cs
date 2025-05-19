using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HidRecorder.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new ViewModels.SettingsWindowViewModel();
    }
}
