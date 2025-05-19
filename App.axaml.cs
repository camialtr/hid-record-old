using System;
using Avalonia;
using System.IO;
using Newtonsoft.Json;
using Avalonia.Styling;
using HidRecorder.Views;
using LibVLCSharp.Shared;
using HidRecorder.Models;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;

namespace HidRecorder;

public class App : Application
{
    private const string SettingsPath = "settings.json";
    public static Settings? Settings { get; private set; }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        LoadSettings();
        
        // Initialize LibVLCSharp core for media functionality
        Core.Initialize();

#if DEBUG
        RequestedThemeVariant = ThemeVariant.Dark;
#else
        RequestedThemeVariant = ThemeVariant.Default;
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new EditorWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private static void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                Settings = new Settings { CurrentIp = "0.0.0.0" };
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(Settings, Formatting.Indented));
            }
            else
            {
                var jsonContent = File.ReadAllText(SettingsPath);
                Settings = JsonConvert.DeserializeObject<Settings>(jsonContent) ?? new Settings { CurrentIp = "0.0.0.0" };
            }
        }
        catch (Exception)
        {
            Settings = new Settings { CurrentIp = "0.0.0.0" };
        }
    }
}

