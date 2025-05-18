using System;
using Avalonia;
using System.IO;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using HidRecorder.Models;
using HidRecorder.ViewModels;
using Avalonia.Interactivity;

namespace HidRecorder.Views;

public partial class EditorWindow : Window
{
    private bool _isFirstLayout = true;
    private DispatcherTimer? _maximizeTimer;
    
    public EditorWindow()
    {
        InitializeComponent();
        
        LayoutUpdated += EditorWindow_LayoutUpdated;
        PropertyChanged += EditorWindow_PropertyChanged;
        Resized += WindowBase_OnResized;
        PositionChanged += EditorWindow_PositionChanged;

        GotFocus += EditorWindow_GotFocus;
        LostFocus += EditorWindow_LostFocus;

        Activated += EditorWindow_Activated;
        Deactivated += EditorWindow_Deactivated;

        var viewModel = new EditorWindowViewModel();
        viewModel.SetParentWindow(this);
        DataContext = viewModel;
        
        Closed += (_, _) =>
        {
            if (DataContext is EditorWindowViewModel vM)
            {
                vM.CloseVideoWindow();
            }
        };
    }

    public EditorWindow(string projectPath)
    {
        InitializeComponent();

        var viewModel = new EditorWindowViewModel();
        viewModel.SetParentWindow(this);
        DataContext = viewModel;

        if (string.IsNullOrEmpty(projectPath)) return;

        var projectJsonPath = projectPath;

        if (Directory.Exists(projectPath))
        {
            projectJsonPath = Path.Combine(projectPath, "project.json");
        }

        if (File.Exists(projectJsonPath))
        {
            _ = viewModel.OpenProjectFile(projectJsonPath);
        }
    }

    private void SessionsGrid_CellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        if (DataContext is not EditorWindowViewModel viewModel || 
            e.Row.DataContext is not Session session)
            return;
            
        var columnHeader = e.Column.Header.ToString() ?? string.Empty;

        if (columnHeader != "Name")
        {
            if (columnHeader == "Export")
            {
                _ = viewModel.UpdateSessionExport(session);
            }
        }
        else
        {
            _ = viewModel.UpdateSessionName(session);
        }
    }
    private void HidDataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not EditorWindowViewModel viewModel || sender is not DataGrid dataGrid) return;
        viewModel.SelectedHidData.Clear();
        
        foreach (HidData item in dataGrid.SelectedItems)
        {
            viewModel.SelectedHidData.Add(item);
        }
        
        viewModel.OnSelectedHidDataChanged();
    }
    
    private void EditorWindow_Activated(object? sender, EventArgs e)
    {
        if (DataContext is EditorWindowViewModel viewModel)
        {
            viewModel.RestoreVideoWindow();
        }
    }

    private void EditorWindow_Deactivated(object? sender, EventArgs e)
    {
        if (DataContext is EditorWindowViewModel viewModel)
        {
            viewModel.MinimizeVideoWindow();
        }
    }

    private void EditorWindow_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (DataContext is EditorWindowViewModel viewModel)
        {
            viewModel.RestoreVideoWindow();
        }
    }

    private void EditorWindow_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is EditorWindowViewModel viewModel)
        {
            viewModel.MinimizeVideoWindow();
        }
    }

    private void EditorWindow_PositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (IsVideoGridReady())
        {
            UpdateVideoWindowPosition();
        }
    }

    private void EditorWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name != nameof(WindowState)) return;
        
        if ((WindowState)e.NewValue! == WindowState.Maximized)
        {
            _maximizeTimer?.Stop();

            _maximizeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };

            _maximizeTimer.Tick += (_, _) =>
            {
                UpdateVideoWindowPosition();
                _maximizeTimer.Stop();
            };

            _maximizeTimer.Start();
        }
        else if ((WindowState)e.NewValue == WindowState.Normal)
        {
            _maximizeTimer?.Stop();
            _maximizeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };

            _maximizeTimer.Tick += (_, _) =>
            {
                UpdateVideoWindowPosition();
                _maximizeTimer.Stop();
            };

            _maximizeTimer.Start();
        }

        if (DataContext is not EditorWindowViewModel viewModel) return;
        switch ((WindowState)e.NewValue)
        {
            case WindowState.Minimized:
                viewModel.MinimizeVideoWindow();
                break;
            case WindowState.Normal:
            case WindowState.Maximized:
                viewModel.RestoreVideoWindow();
                break;
            case WindowState.FullScreen:
            default:
                break;
        }
    }

    private void EditorWindow_LayoutUpdated(object? sender, EventArgs e)
    {
        if (!_isFirstLayout || VideoGrid is not { Bounds.Width: > 0 } || !IsVideoGridReady()) return;
        _isFirstLayout = false;
        UpdateVideoWindowPosition();
    }

    private void WindowBase_OnResized(object? sender, WindowResizedEventArgs e)
    {
        if (IsVideoGridReady())
        {
            UpdateVideoWindowPosition();
        }
    }

    private bool IsVideoGridReady() => VideoGrid is { IsVisible: true, IsEffectivelyVisible: true } && IsLoaded;

    private void UpdateVideoWindowPosition()
    {
        if (DataContext is not EditorWindowViewModel viewModel || VideoGrid == null || !IsVideoGridReady()) return;
        
        try
        {
            var bounds = VideoGrid.Bounds;

            PixelPoint pixelPos;

            try
            {
                pixelPos = VideoGrid.PointToScreen(new Point(0, 0));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when getting window position: {ex.Message}");
                pixelPos = new PixelPoint(Position.X + 10, Position.Y + 50);
            }

            viewModel.ResizedCommand.Execute(new VideoPositionInfo(
                bounds.Width,
                bounds.Height,
                pixelPos
            ));

            Console.WriteLine($"VideoGrid Size: {bounds.Width}x{bounds.Height}, Position: {pixelPos}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error when updating position: {ex.Message}");
        }
    }
}
            
            