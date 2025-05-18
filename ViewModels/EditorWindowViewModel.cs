using System;
using System.IO;
using System.Linq;
using MsBox.Avalonia;
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
    [ObservableProperty] private string _serverContent = string.Empty;

    public ObservableCollection<Session> Sessions { get; } = [];

    public ObservableCollection<HidData> HidData { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartServerCommand))]
    private bool _isProjectOpen;

    [ObservableProperty] private HidProject? _currentProject;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProjectPath))]
    [NotifyPropertyChangedFor(nameof(HasSelectedSession))]
    [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSessionCommand))]
    private Session? _selectedSession;

    public bool HasSelectedSession => SelectedSession != null;

    private Window? _parentWindow;

    private string? _projectFilePath;

    private readonly Dictionary<Session, string> _originalSessionNames = new();

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
                SelectedSession = null;

                HidData.Clear();

                CurrentProject = project;
                IsProjectOpen = true;

                Sessions.Clear();

                _originalSessionNames.Clear();
                foreach (var session in project.Sessions)
                {
                    _originalSessionNames[session] = session.Name;
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

    public async Task UpdateSessionName(Session session)
    {
        if (CurrentProject == null || string.IsNullOrEmpty(_projectFilePath))
            return;

        try
        {
            if (!_originalSessionNames.TryGetValue(session, out var oldName))
            {
                return;
            }

            var newName = session.Name;

            if (oldName == newName)
                return;

            if (IsSessionNameDuplicate(session, newName))
            {
                Console.WriteLine($"ERROR: Session with name '{newName}' already exists. Reverting to '{oldName}'");

                session.Name = oldName;

                RefreshSessionsCollection();

                await ShowErrorMessage(
                    $"A session with the name '{newName}' already exists.\nThe session name was not changed.",
                    "Duplicate Name");

                return;
            }

            Console.WriteLine($"Updating session name from '{oldName}' to '{newName}'");

            var accdataPath = Path.Combine(ProjectPath, "accdata");

            if (Directory.Exists(accdataPath))
            {
                var oldFilePath = Path.Combine(accdataPath, $"{oldName}.json");
                var newFilePath = Path.Combine(accdataPath, $"{newName}.json");

                if (File.Exists(newFilePath))
                {
                    Console.WriteLine(
                        $"ERROR: File with name '{newName}.json' already exists. Reverting to '{oldName}'");

                    session.Name = oldName;

                    RefreshSessionsCollection();

                    await ShowErrorMessage(
                        $"A file with the name '{newName}.json' already exists.\nThe session name was not changed.",
                        "Duplicate File");

                    return;
                }

                if (File.Exists(oldFilePath))
                {
                    Console.WriteLine($"Renaming file from '{oldFilePath}' to '{newFilePath}'");
                    File.Move(oldFilePath, newFilePath);
                }
            }

            Console.WriteLine("Saving changes to project.json");
            var projectJson = JsonConvert.SerializeObject(CurrentProject, Formatting.Indented);
            await File.WriteAllTextAsync(_projectFilePath, projectJson);

            if (SelectedSession == session)
            {
                try
                {
                    LoadHidDataForSession(session);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reloading HID data: {ex.Message}");
                }
            }

            Console.WriteLine("Session name update completed successfully");

            _originalSessionNames[session] = newName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating session name: {ex.Message}");

            if (_originalSessionNames.TryGetValue(session, out var originalName))
            {
                session.Name = originalName;

                RefreshSessionsCollection();

                await ShowErrorMessage($"Error updating session name: {ex.Message}", "Update Error");
            }
        }
    }

    private async Task SaveProjectChanges()
    {
        if (CurrentProject == null || string.IsNullOrEmpty(_projectFilePath))
            return;

        try
        {
            Console.WriteLine($"Saving project to {_projectFilePath}");

            var projectJson = JsonConvert.SerializeObject(CurrentProject, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            await File.WriteAllTextAsync(_projectFilePath, projectJson, System.Text.Encoding.UTF8);

            Console.WriteLine("Project saved successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving project: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateSessionExport(Session session)
    {
        if (CurrentProject == null || string.IsNullOrEmpty(_projectFilePath))
            return;

        try
        {
            Console.WriteLine($"Updating export value for session '{session.Name}' to {session.Export}");

            OnPropertyChanged(nameof(CurrentProject));

            await SaveProjectChanges();

            Console.WriteLine("Export setting saved successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating session export setting: {ex.Message}");
            await ShowErrorMessage($"Error saving export setting: {ex.Message}", "Save Error");
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

    private bool IsSessionNameDuplicate(Session session, string newName)
    {
        return CurrentProject != null && CurrentProject.Sessions
            .Where(existingSession => !ReferenceEquals(existingSession, session)).Any(existingSession =>
                string.Equals(existingSession.Name, newName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ShowErrorMessage(string message, string title = "Error")
    {
        Console.WriteLine($"ERROR: {message}");

        if (_parentWindow != null)
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard(title, message);

            await box.ShowAsync();
        }
    }

    private void RefreshSessionsCollection()
    {
        if (CurrentProject == null)
            return;

        var tempSessions = new List<Session>(Sessions);

        Sessions.Clear();

        foreach (var session in tempSessions)
        {
            Sessions.Add(session);
        }
    }

    [RelayCommand]
    private async Task NewSession()
    {
        if (CurrentProject == null || string.IsNullOrEmpty(_projectFilePath))
            return;

        try
        {
            var baseName = "New_Session";
            var sessionName = baseName;
            var counter = 1;

            while (CurrentProject.Sessions.Any(s =>
                       string.Equals(s.Name, sessionName, StringComparison.OrdinalIgnoreCase)))
            {
                sessionName = $"{baseName}_{counter}";
                counter++;
            }

            var newSession = new Session(
                platform: "NX",
                file: sessionName,
                export: true
            );

            CurrentProject.Sessions.Add(newSession);
            Sessions.Add(newSession);
            _originalSessionNames[newSession] = sessionName;

            await SaveProjectChanges();

            var accdataPath = Path.Combine(ProjectPath, "accdata");
            if (!Directory.Exists(accdataPath))
            {
                Directory.CreateDirectory(accdataPath);
            }

            var newHidData = NewProjectWindowViewModel.CreateExampleHidData();
            var newHidDataFileName = $"{sessionName}.json";
            var newHidDataPath = Path.Combine(accdataPath, newHidDataFileName);
            var newHidDataJson = JsonConvert.SerializeObject(newHidData, Formatting.Indented);

            await File.WriteAllTextAsync(newHidDataPath, newHidDataJson);

            SelectedSession = newSession;

            Console.WriteLine($"New session '{sessionName}' created successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating new session: {ex.Message}");
            await ShowErrorMessage($"Error creating new session: {ex.Message}", "Creation Error");
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSession))]
    private async Task DeleteSession()
    {
        if (CurrentProject == null || string.IsNullOrEmpty(_projectFilePath) || SelectedSession == null)
            return;

        try
        {
            var sessionToDelete = SelectedSession;
            var sessionName = sessionToDelete.Name;

            var box = MessageBoxManager
                .GetMessageBoxStandard(
                    "Confirm Deletion",
                    $"Are you sure you want to delete the session '{sessionName}'?",
                    MsBox.Avalonia.Enums.ButtonEnum.YesNo);

            var result = await box.ShowAsync();

            if (result != MsBox.Avalonia.Enums.ButtonResult.Yes)
            {
                return;
            }

            Sessions.Remove(sessionToDelete);

            CurrentProject.Sessions.Remove(sessionToDelete);

            _originalSessionNames.Remove(sessionToDelete);

            await SaveProjectChanges();

            var accdataPath = Path.Combine(ProjectPath, "accdata");
            if (Directory.Exists(accdataPath))
            {
                var hidFilePath = Path.Combine(accdataPath, $"{sessionName}.json");
                if (File.Exists(hidFilePath))
                {
                    File.Delete(hidFilePath);
                    Console.WriteLine($"Deleted file: {hidFilePath}");
                }
            }

            HidData.Clear();

            SelectedSession = null;

            Console.WriteLine($"Session '{sessionName}' deleted successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting session: {ex.Message}");
            await ShowErrorMessage($"Error deleting session: {ex.Message}", "Deletion Error");
        }
    }
}