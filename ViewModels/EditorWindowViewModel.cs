using System;
using System.IO;
using Newtonsoft.Json;
using Avalonia.Controls;
using HidRecorder.Views;
using HidRecorder.Models;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HidRecorder.ViewModels;

public partial class EditorWindowViewModel : ViewModelBase
{
    public string ServerContent { get; private set; } = string.Empty;
    
    public ObservableCollection<Session> Sessions { get; } = [];
    
    public ObservableCollection<HidData> HidData { get; } = [];
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartServerCommand))]
    private bool _isProjectOpen;
    
    [ObservableProperty]
    private HidProject? _currentProject;
    
    private Window? _parentWindow;
    
    public void SetParentWindow(Window window)
    {
        _parentWindow = window;
    }

    [RelayCommand]
    private void New()
    {
        var newProjectWindow = new NewProjectWindow();
        newProjectWindow.Show();
        
        _parentWindow?.Close();
    }
    
    [RelayCommand]
    private async Task Open()
    {
        if (_parentWindow == null)
            return;
            
        var storageProvider = _parentWindow.StorageProvider;
        
        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = "Open Project",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("HID Recorder Project")
                {
                    Patterns = ["project.json"],
                    MimeTypes = ["application/json"]
                }
            ]
        };
        
        var result = await storageProvider.OpenFilePickerAsync(filePickerOptions);
        if (result.Count > 0)
        {
            await OpenProjectFile(result[0].Path.LocalPath);
        }
    }
    
    public async Task OpenProjectFile(string projectJsonPath)
    {
        if (string.IsNullOrEmpty(projectJsonPath))
            return;
            
        try
        {
            var projectJson = await File.ReadAllTextAsync(projectJsonPath);
            var project = JsonConvert.DeserializeObject<HidProject>(projectJson);
            
            if (project != null)
            {
                CurrentProject = project;
                IsProjectOpen = true;
                
                if (_parentWindow is { } window)
                {
                    window.Title = $"HID Recorder - {project.Name}";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening project: {ex.Message}");
        }
    }
    
    [RelayCommand(CanExecute = nameof(IsProjectOpen))]
    private void StartRecording()
    {
    }
    
    [RelayCommand(CanExecute = nameof(IsProjectOpen))]
    private void StartServer()
    {
    }
    
    [RelayCommand]
    private static void Exit() => Environment.Exit(0);
}