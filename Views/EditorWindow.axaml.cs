using System.IO;
using Avalonia.Controls;
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
}