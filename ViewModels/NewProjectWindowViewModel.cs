using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using HidRecorder.Views;
using Avalonia.Controls;
using HidRecorder.Models;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
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
            
            var example01HidData = CreateExampleHidData();
            const string example01HidDataBaseName = "Template_Example_01";
            const string example01HidDataFileName = example01HidDataBaseName + ".json";
            var example01HidDataPath = Path.Combine(accdataPath, example01HidDataFileName);
            var example01DataJson = JsonConvert.SerializeObject(example01HidData, Formatting.Indented);
            File.WriteAllText(example01HidDataPath, example01DataJson);
            
            var example02HidData = CreateExampleHidData();
            const string example02HidDataBaseName = "Template_Example_02";
            const string example02HidDataFileName = example02HidDataBaseName + ".json";
            var example02HidDataPath = Path.Combine(accdataPath, example02HidDataFileName);
            var example02DataJson = JsonConvert.SerializeObject(example02HidData, Formatting.Indented);
            File.WriteAllText(example02HidDataPath, example02DataJson);
            
            var example01Session = new Session(
                platform: "NX", 
                file: example01HidDataBaseName,
                export: true
            );
            
            var example02Session = new Session(
                platform: "NX", 
                file: example02HidDataBaseName,
                export: true
            );
            
            var project = new HidProject
            {
                Name = ProjectName,
                Video = videoFileName,
                Audio = audioFileName,
                MusicTrack = musicTrackFileName,
                Sessions = [example01Session, example02Session]
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
    
    public static List<HidData> CreateExampleHidData()
    {
        var exampleData = new List<HidData>();
        var random = new Random();
        
        for (var i = 0; i < 50; i++)
        {
            var time = i * 0.1f;
            
            var accelY = (float)(random.NextDouble() * 5 - 2);
            var accelX = (float)(random.NextDouble() * 5 - 2);
            var accelZ = (float)(random.NextDouble() * 5 - 2);
            
            var angleX = (float)(random.NextDouble() * 5 - 2);
            var angleY = (float)(random.NextDouble() * 5 - 2);
            var angleZ = (float)(random.NextDouble() * 5 - 2);
            
            exampleData.Add(new HidData(time, accelX, accelY, accelZ, angleX, angleY, angleZ));
        }
        
        return exampleData;
    }
}