using Avalonia;
using Avalonia.Styling;
using HidRecorder.Views;
using LibVLCSharp.Shared;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;

namespace HidRecorder;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
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
}