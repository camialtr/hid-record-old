using System;
using System.IO;
using System.Linq;
using MsBox.Avalonia;
using Newtonsoft.Json;
using Avalonia.Controls;
using HidRecorder.Views;
using Avalonia.Threading;
using HidRecorder.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
// ReSharper disable UnusedParameterInPartialMethod

namespace HidRecorder.ViewModels;

public partial class EditorWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _serverContent = "Start Server";
    [ObservableProperty] private string _serverDataContent = string.Empty;
    [ObservableProperty] private string _recordingButtonContent = "Start Recording";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartServerCommand))]
    private bool _isRecording;

    [ObservableProperty] private bool _gridsEnabled = true;

    [ObservableProperty]
    private string _videoTimeDisplay = string.Empty;

    private bool _isFirstPlay = true;

    public ObservableCollection<Session> Sessions { get; } = [];

    public ObservableCollection<HidData> HidData { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedHidData))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedHidDataCommand))]
    private List<HidData> _selectedHidData = [];

    public bool HasSelectedHidData => SelectedHidData.Count > 0;

    public void OnSelectedHidDataChanged()
    {
        OnPropertyChanged(nameof(SelectedHidData));
        OnPropertyChanged(nameof(HasSelectedHidData));
        DeleteSelectedHidDataCommand.NotifyCanExecuteChanged();
    }

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

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    private bool _isServerReceivingData;

    private Window? _parentWindow;

    private string? _projectFilePath;

    private readonly Dictionary<Session, string> _originalSessionNames = new();

    public string ProjectPath => !string.IsNullOrEmpty(_projectFilePath)
        ? Path.GetDirectoryName(_projectFilePath)!
        : string.Empty;

    private VideoWindow? _videoWindow;

    private DispatcherTimer? _updateTimer;
    private DispatcherTimer? _sampleCountTimer;
    private int _lastSamplesCount;
    private int _fullSamplesCount;
    private int _samplesPerSecond;

    private Server? _server;

    private readonly Stopwatch _recordingStopwatch = new();

    public void SetParentWindow(Window window)
    {
        _parentWindow = window;
    }

    public EditorWindowViewModel()
    {
        SetupSampleCallLoop();
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

                if (!string.IsNullOrEmpty(project.Video))
                {
                    var videoPath = Path.IsPathRooted(project.Video)
                        ? project.Video
                        : Path.Combine(ProjectPath, project.Video);

                    var audioPath = !string.IsNullOrEmpty(project.Audio)
                        ? Path.IsPathRooted(project.Audio)
                            ? project.Audio
                            : Path.Combine(ProjectPath, project.Audio)
                        : null;

                    var musicTrackPath = !string.IsNullOrEmpty(project.MusicTrack)
                        ? Path.IsPathRooted(project.MusicTrack)
                            ? project.MusicTrack
                            : Path.Combine(ProjectPath, project.MusicTrack)
                        : null;

                    if (File.Exists(videoPath) && File.Exists(audioPath) && File.Exists(musicTrackPath))
                    {
                        _videoWindow?.OpenVideo(videoPath, audioPath, musicTrackPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBoxManager.GetMessageBoxStandard("Error", $"Error when opening the project: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Resized(VideoPositionInfo info)
    {
        if (_videoWindow == null)
        {
            _videoWindow = new VideoWindow(info.Width, info.Height, info.Position);

            if (_parentWindow is { } parentWindow)
            {
                _videoWindow.Tag = parentWindow;
            }

            _videoWindow.Show();
        }
        else
        {
            _videoWindow.Width = info.Width;
            _videoWindow.Height = info.Height;
            _videoWindow.Position = info.Position;
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

    private bool CanStartRecording() => IsProjectOpen && SelectedSession != null && _server?.Connected == true &&
                                        IsServerReceivingData;

    [RelayCommand(CanExecute = nameof(CanStartRecording))]
    private async Task StartRecording()
    {
        if (!IsRecording)
        {
            HidData.Clear();

            if (_videoWindow is not null)
            {
                if (!_isFirstPlay)
                {
                    _videoWindow.MediaPlayer.Time = 0;
                    _recordingStopwatch.Reset();
                }

                _isFirstPlay = false;
                _videoWindow.MediaPlayer.Play();
                _recordingStopwatch.Start();
                _videoWindow.Show();
                _videoWindow.Topmost = true;
            }

            IsRecording = true;
            GridsEnabled = false;
            RecordingButtonContent = "Stop Recording";
        }
        else
        {
            if (_videoWindow is not null)
            {
                _videoWindow.MediaPlayer.Stop();
                _videoWindow.MediaPlayer.Time = 0;
                _recordingStopwatch.Reset();
                _videoWindow.Hide();
                _isFirstPlay = true;
            }

            IsRecording = false;
            GridsEnabled = true;
            RecordingButtonContent = "Start Recording";
            VideoTimeDisplay = "Video: 0.0s / 0.0s | Recording: 0.0s";

            if (SelectedSession != null)
            {
                var accdataPath = Path.Combine(ProjectPath, "accdata");
                if (!Directory.Exists(accdataPath))
                {
                    Directory.CreateDirectory(accdataPath);
                }

                var hidDataFilePath = Path.Combine(accdataPath, $"{SelectedSession.Name}.json");
                var hidDataJson = JsonConvert.SerializeObject(HidData, Formatting.Indented);
                await File.WriteAllTextAsync(hidDataFilePath, hidDataJson);
            }
        }
    }

    private bool CanStartServer() => IsProjectOpen && !IsRecording;

    [RelayCommand(CanExecute = nameof(CanStartServer))]
    private void StartServer()
    {
        if (_server is not null && !_server.Connected || _server is not null && _server.ExceptionCalled)
        {
            _server.Dispose();
            _server = null;
            ServerContent = "Start Server";
            ServerDataContent = "Waiting for server...";
            return;
        }

        if (_server is null)
        {
            _server = new Server(App.Settings.CurrentIp, 14444);
            ServerContent = "Stop Server";
            ServerDataContent = $"Listening at {App.Settings.CurrentIp} - Waiting for data...";
            return;
        }

        if (!_server.Connected) return;

        _server.Dispose();
        _server = null;
        ServerContent = "Start Server";
        ServerDataContent = "Waiting for server...";
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
            const string baseName = "New_Session";
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

    [RelayCommand(CanExecute = nameof(HasSelectedHidData))]
    private async Task DeleteSelectedHidData()
    {
        if (CurrentProject == null || string.IsNullOrEmpty(_projectFilePath) ||
            SelectedSession == null || SelectedHidData.Count == 0)
            return;

        try
        {
            var itemCount = SelectedHidData.Count;
            var message = itemCount == 1
                ? "Are you sure you want to delete 1 HID data entry?"
                : $"Are you sure you want to delete {itemCount} HID data entries?";

            var box = MessageBoxManager
                .GetMessageBoxStandard(
                    "Confirm Deletion",
                    message,
                    MsBox.Avalonia.Enums.ButtonEnum.YesNo);

            var result = await box.ShowAsync();

            if (result != MsBox.Avalonia.Enums.ButtonResult.Yes)
                return;

            foreach (var item in SelectedHidData.ToList())
            {
                HidData.Remove(item);
            }

            var accdataPath = Path.Combine(ProjectPath, "accdata");
            if (Directory.Exists(accdataPath))
            {
                var hidDataFilePath = Path.Combine(accdataPath, $"{SelectedSession.Name}.json");
                var hidDataJson = JsonConvert.SerializeObject(HidData, Formatting.Indented);
                await File.WriteAllTextAsync(hidDataFilePath, hidDataJson);

                Console.WriteLine($"Updated HID data file: {hidDataFilePath}");
                Console.WriteLine($"Deleted {itemCount} HID data entries");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting HID data entries: {ex.Message}");
            await ShowErrorMessage($"Error deleting HID data entries: {ex.Message}", "Deletion Error");
        }
    }

    public void MinimizeVideoWindow()
    {
        if (_videoWindow == null) return;
        _videoWindow.Topmost = false;
    }

    public void RestoreVideoWindow()
    {
        if (_videoWindow == null) return;
        _videoWindow.Topmost = true;
    }

    public void CloseVideoWindow()
    {
        if (_videoWindow == null) return;
        _videoWindow.Close();
        _videoWindow = null;
    }

    private void SetupSampleCallLoop()
    {
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 120)
        };
        _updateTimer.Tick += (_, _) => CallSample();
        _updateTimer.Start();

        _sampleCountTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0)
        };
        _sampleCountTimer.Tick += (_, _) => UpdateSamplesPerSecond();
        _sampleCountTimer.Start();
    }

    private float NormalizeValue(float value)
    {
        if (Math.Abs(value) < 0.000001f)
            return 0f;
        
        return (float)Math.Round(value, 6);
    }

    private void CallSample()
    {
        if (_server is not null && _server.ExceptionCalled)
        {
            _server.Dispose();
            _server = null;
            ServerContent = "Start Server";
            ServerDataContent = "Waiting for server...";
            IsServerReceivingData = false;
            return;
        }

        if (_server is null || !_server.Connected)
        {
            IsServerReceivingData = false;
            return;
        }

        if (_videoWindow?.MediaPlayer != null)
        {
            Console.WriteLine(-_videoWindow.VideoStartTime);
            var currentVideoSeconds = (_videoWindow.MediaPlayer.Time / 1000.0) - _videoWindow.VideoStartTime;
            var totalVideoSeconds = (_videoWindow.MediaPlayer.Length / 1000.0) - _videoWindow.VideoStartTime;
            var recordingSeconds = _recordingStopwatch.Elapsed.TotalSeconds - _videoWindow.VideoStartTime;
            
            VideoTimeDisplay =
                $"VT {currentVideoSeconds:F6}s | {recordingSeconds:F6}s RT | Total - {totalVideoSeconds:F6}s";

            if (IsRecording && recordingSeconds >= totalVideoSeconds)
            {
                StartRecordingCommand.Execute(this);
            }
        }

        var lNd = _server.NetworkData;
        IsServerReceivingData = _samplesPerSecond > 0;

        var masterString = string.Empty;
        var accelX = $"{lNd.AccelX:F9}".PadRight(9, '0')[..9];
        var accelY = $"{lNd.AccelY:F9}".PadRight(9, '0')[..9];
        var accelZ = $"{lNd.AccelZ:F9}".PadRight(9, '0')[..9];
        var angleX = $"{lNd.AngleX:F9}".PadRight(9, '0')[..9];
        var angleY = $"{lNd.AngleY:F9}".PadRight(9, '0')[..9];
        var angleZ = $"{lNd.AngleZ:F9}".PadRight(9, '0')[..9];

        masterString +=
            $"Accel - X: {accelX} Y: {accelY} Z: {accelZ} | Angle: {angleX} {angleY} {angleZ} | SPS: {_samplesPerSecond}";

        ServerDataContent = masterString;

        _fullSamplesCount++;

        if (!IsRecording) return;
        
        var adjustedTime = (float)(_recordingStopwatch.Elapsed.TotalSeconds - _videoWindow.VideoStartTime);
        if (adjustedTime < 0) return;
        
        var newSample = new HidData(
            adjustedTime,
            NormalizeValue(lNd.AccelX),
            NormalizeValue(lNd.AccelY),
            NormalizeValue(lNd.AccelZ),
            NormalizeValue(lNd.AngleX),
            NormalizeValue(lNd.AngleY),
            NormalizeValue(lNd.AngleZ)
        );

        HidData.Add(newSample);
    }

    private void UpdateSamplesPerSecond()
    {
        _samplesPerSecond = _fullSamplesCount - _lastSamplesCount;
        _lastSamplesCount = _fullSamplesCount;
    }
}
