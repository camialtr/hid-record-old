using System.IO;
using Avalonia.Controls;
using HidRecorder.Models;
using HidRecorder.ViewModels;

namespace HidRecorder.Views;

public partial class EditorWindow : Window
{
    public EditorWindow()
    {
        InitializeComponent();

        var viewModel = new EditorWindowViewModel();
        viewModel.SetParentWindow(this);
        DataContext = viewModel;
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
}
            
            