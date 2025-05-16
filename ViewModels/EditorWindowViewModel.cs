using System;
using System.IO;
using Newtonsoft.Json;
using Avalonia.Controls;
using HidRecorder.Views;
using HidRecorder.Models;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

// ReSharper disable UnusedParameterInPartialMethod

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
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProjectPath))]
    [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    private Session? _selectedSession;
    
    private Window? _parentWindow;
    
    private string? _projectFilePath;
    
    public string ProjectPath => !string.IsNullOrEmpty(_projectFilePath) 
        ? Path.GetDirectoryName(_projectFilePath)!
        : string.Empty;
    
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
            
        _projectFilePath = projectJsonPath;
            
        try
        {
            var projectJson = await File.ReadAllTextAsync(projectJsonPath);
            var project = JsonConvert.DeserializeObject<HidProject>(projectJson);
            
            if (project != null)
            {
                // Limpar a seleção atual e dados HID antes de carregar o novo projeto
                SelectedSession = null;
                HidData.Clear();
                
                CurrentProject = project;
                IsProjectOpen = true;
                
                Sessions.Clear();
                
                foreach (var session in project.Sessions)
                {
                    Sessions.Add(session);
                }
                
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
    
    private bool CanStartRecording() => IsProjectOpen && SelectedSession != null;
    
    [RelayCommand(CanExecute = nameof(CanStartRecording))]
    private void StartRecording()
    {
    }
    
    [RelayCommand(CanExecute = nameof(IsProjectOpen))]
    private void StartServer()
    {
    }
    
    [RelayCommand]
    private static void Exit() => Environment.Exit(0);
    
    partial void OnSelectedSessionChanged(Session? oldValue, Session? newValue)
    {
        if (newValue == null || string.IsNullOrEmpty(ProjectPath))
        {
            return;
        }
        
        LoadHidDataForSession(newValue);
    }
    
    private void LoadHidDataForSession(Session session)
    {
        var accdataPath = Path.Combine(ProjectPath, "accdata");
        var hidDataFilePath = Path.Combine(accdataPath, $"{session.Name}.json");
            
        if (!File.Exists(hidDataFilePath))
        {
            throw new FileNotFoundException($"HID file not found: {hidDataFilePath}");
        }
            
        var hidDataJson = File.ReadAllText(hidDataFilePath);
        var hidDataList = JsonConvert.DeserializeObject<List<HidData>>(hidDataJson);
            
        HidData.Clear();
        if (hidDataList == null) return;
        foreach (var data in hidDataList)
        {
            HidData.Add(data);
        }
    }
}