using Avalonia;
using System.IO;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HidRecorder.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _ipAddress = App.Settings?.CurrentIp ?? "0.0.0.0";

    [RelayCommand]
    private void Save()
    {
        if (App.Settings != null)
        {
            App.Settings.CurrentIp = IpAddress;
            File.WriteAllText("settings.json", JsonConvert.SerializeObject(App.Settings));
        }

        if (Application.Current?.ApplicationLifetime is not
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) return;
        if (desktop.Windows.Count > 0 && desktop.Windows[^1] is Views.SettingsWindow window)
        {
            window.Close();
        }
    }
}
