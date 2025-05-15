using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using HidRecorder.Views;
using Avalonia.Controls;
using HidRecorder.Models;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HidRecorder.ViewModels;

public partial class NewProjectWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProject))]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string _projectName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProject))]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string _video = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProject))]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string _audio = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateProject))]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string _musicTrack = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool CanCreateProject => 
        !string.IsNullOrWhiteSpace(ProjectName) && 
        !string.IsNullOrWhiteSpace(Video) && File.Exists(Video) &&
        !string.IsNullOrWhiteSpace(Audio) && File.Exists(Audio) &&
        !string.IsNullOrWhiteSpace(MusicTrack) && File.Exists(MusicTrack);

    private Window? _parent;

    public void SetParent(Window parent)
    {
        _parent = parent;
    }

    [RelayCommand]
    private async Task BrowseVideo()
    {
        var file = await OpenFileDialog("Select a Video", "WEBM Video", ["webm"]);
        if (!string.IsNullOrEmpty(file))
            Video = file;
    }

    [RelayCommand]
    private async Task BrowseAudio()
    {
        var file = await OpenFileDialog("Select an Audio","Audio Files", ["wav", "ogg"]);
        if (!string.IsNullOrEmpty(file))
            Audio = file;
    }

    [RelayCommand]
    private async Task BrowseMusicTrack()
    {
        var file = await OpenFileDialog("Select a MusicTrack","Ubisoft MusicTrack", ["tpl.ckd"]);
        if (!string.IsNullOrEmpty(file))
            MusicTrack = file;
    }

    [RelayCommand(CanExecute = nameof(CanCreateProject))]
    private void CreateProject()
    {
        string? projectPath = null;
        
        try
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var hidRProjectsPath = Path.Combine(documentsPath, "Hid Recorder");
            
            if (!Directory.Exists(hidRProjectsPath))
                Directory.CreateDirectory(hidRProjectsPath);
            
            var projectDirectoryName = SanitizeFileName(ProjectName);
            projectPath = Path.Combine(hidRProjectsPath, projectDirectoryName);
            
            if (Directory.Exists(projectPath))
            {
                ErrorMessage = $"Project '{ProjectName}' already exists. Please choose a different name.";
                return;
            }
            
            Directory.CreateDirectory(projectPath);
            var accdataPath = Path.Combine(projectPath, "accdata");
            Directory.CreateDirectory(accdataPath);
            
            var videoFileName = Path.GetFileName(Video);
            var audioFileName = Path.GetFileName(Audio);
            var musicTrackFileName = Path.GetFileName(MusicTrack);
            
            var videoDestPath = Path.Combine(projectPath, videoFileName);
            var audioDestPath = Path.Combine(projectPath, audioFileName);
            var musicTrackDestPath = Path.Combine(projectPath, musicTrackFileName);
            
            File.Copy(Video, videoDestPath);
            File.Copy(Audio, audioDestPath);
            
            var musicTrackJson = File.ReadAllText(MusicTrack);
            var musicTrack = JsonConvert.DeserializeObject<MusicTrack>(musicTrackJson);
            var indentedMusicTrackJson = JsonConvert.SerializeObject(musicTrack, Formatting.Indented);
            File.WriteAllText(musicTrackDestPath.Replace(".tpl.ckd", ".json"), indentedMusicTrackJson);
            
            // Criar arquivo de dados HID de exemplo
            var exampleHidData = CreateExampleHidData();
            var exampleHidDataFileName = "example_data.json";
            var exampleHidDataPath = Path.Combine(accdataPath, exampleHidDataFileName);
            
            // Salvar os dados HID como JSON no arquivo exemplo
            var hidDataJson = JsonConvert.SerializeObject(exampleHidData, Formatting.Indented);
            File.WriteAllText(exampleHidDataPath, hidDataJson);
            
            // Criar uma sessão de exemplo e adicionar ao projeto
            var exampleSession = new Session(
                platform: "Example", 
                file: exampleHidDataFileName, 
                linkId: 1, 
                export: true
            );
            
            var project = new HidProject
            {
                Name = ProjectName,
                Video = videoFileName,
                Audio = audioFileName,
                MusicTrack = musicTrackFileName,
                Sessions = [exampleSession]
            };
            
            var projectJsonPath = Path.Combine(projectPath, "project.json");
            
            var json = JsonConvert.SerializeObject(project, Formatting.Indented);
            File.WriteAllText(projectJsonPath, json);
            
            var editorWindow = new EditorWindow(projectJsonPath);
            editorWindow.Show();
            
            _parent?.Close(project);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating project: {ex.Message}";
            
            if (projectPath != null && Directory.Exists(projectPath))
            {
                try
                {
                    Directory.Delete(projectPath, true);
                }
                catch (Exception deleteEx)
                {
                    ErrorMessage += $" (Failed to clean up temporary files: {deleteEx.Message})";
                }
            }
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        var editorWindow = new EditorWindow();
        editorWindow.Show();

        _parent?.Close();
    }

    private async Task<string> OpenFileDialog(string title, string type, string[] extensions)
    {
        if (_parent == null)
            return string.Empty;

        var storageProvider = _parent.StorageProvider;

        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(type)
                {
                    Patterns = extensions.Select(ext => $"*.{ext}").ToArray(),
                    MimeTypes = extensions.Select(ext => $"application/{ext}").ToArray()
                }
            ]
        };

        var result = await storageProvider.OpenFilePickerAsync(filePickerOptions);
        return result.Count > 0 ? result[0].Path.LocalPath : string.Empty;
    }
    
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray()).Trim();
    }
    
    private static List<HidData> CreateExampleHidData()
    {
        // Criar um conjunto de dados HID de exemplo com movimentos simulados
        var exampleData = new List<HidData>();
        
        // Gerar 50 registros de exemplo
        for (int i = 0; i < 50; i++)
        {
            // Tempo aumenta 100ms a cada amostra
            float time = i * 0.1f;
            
            // Simular um movimento de rotação e aceleração simples
            float accelX = (float)Math.Sin(time) * 0.5f;
            float accelY = (float)Math.Cos(time) * 0.3f;
            float accelZ = 9.8f + (float)Math.Sin(time * 0.5f) * 0.2f; // Gravidade + pequena variação
            
            float gyroX = (float)Math.Sin(time * 0.7f) * 10f;
            float gyroY = (float)Math.Cos(time * 0.7f) * 8f;
            float gyroZ = (float)Math.Sin(time * 0.3f) * 5f;
            
            exampleData.Add(new HidData(time, accelX, accelY, accelZ, gyroX, gyroY, gyroZ));
        }
        
        return exampleData;
    }
}