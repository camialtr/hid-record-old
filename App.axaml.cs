using Avalonia;
using Avalonia.Styling;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;
using HidRecorder.Views;
using LibVLCSharp.Shared;

namespace HidRecorder;

/// <summary>
/// Main Avalonia application class.
/// </summary>
public class App : Application
{
    /// <summary>
    /// Initializes the application by loading XAML and configuring the environment.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Initialize LibVLCSharp core for media functionality
        Core.Initialize();

#if DEBUG
        RequestedThemeVariant = ThemeVariant.Dark;
#else
        RequestedThemeVariant = ThemeVariant.Default;
#endif
    }

    /// <summary>
    /// Configures the main window after framework initialization.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new EditorWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}