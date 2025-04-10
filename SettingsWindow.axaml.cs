using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace rec_tool;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        IpTextBox.Text = Settings.CurrentIp;
    }

    private void IpButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Settings.CurrentIp = IpTextBox.Text ?? string.Empty;
    }
}