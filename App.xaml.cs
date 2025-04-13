using System.Windows;
using System.Text.Json;
using System.IO;
using rec_tool.Models;

namespace rec_tool;

public partial class App : Application
{
    private const string SettingsPath = "settings.json";
    public static Settings? Settings { get; private set; }

    public App()
    {
        InitializeComponent();
        LoadSettings();
    }

    private static void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                Settings = new Settings { CurrentIp = "0.0.0.0" };
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            else
            {
                string jsonContent = File.ReadAllText(SettingsPath);
                Settings = JsonSerializer.Deserialize<Settings>(jsonContent) ?? new Settings { CurrentIp = "0.0.0.0" };
            }
        }
        catch (Exception)
        {
            Settings = new Settings { CurrentIp = "0.0.0.0" };
        }
    }
}

