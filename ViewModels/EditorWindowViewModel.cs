using System;
using System.Collections.Generic;
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
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProjectPath))]
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
                CurrentProject = project;
                IsProjectOpen = true;
                
                // Limpar e popular a coleção de sessões com as sessões do projeto
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
        try
        {
            // Caminho para o arquivo de dados HID associado à sessão
            var accdataPath = Path.Combine(ProjectPath, "accdata");
            var hidDataFilePath = Path.Combine(accdataPath, session.Name);
            
            if (!File.Exists(hidDataFilePath))
            {
                Console.WriteLine($"Arquivo HID não encontrado: {hidDataFilePath}");
                return;
            }
            
            // Ler e deserializar os dados HID
            var hidDataJson = File.ReadAllText(hidDataFilePath);
            var hidDataList = JsonConvert.DeserializeObject<List<HidData>>(hidDataJson);
            
            // Atualizar a coleção observável
            HidData.Clear();
            if (hidDataList != null)
            {
                foreach (var data in hidDataList)
                {
                    HidData.Add(data);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar dados HID: {ex.Message}");
        }
    }
}