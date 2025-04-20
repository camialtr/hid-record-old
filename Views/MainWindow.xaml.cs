using System.IO;
using System.Windows;
using Microsoft.Win32;
using rec_tool.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using LibVLCSharp.Shared;
using System.Windows.Threading;

namespace rec_tool;

public partial class MainWindow : Window
{
    private DispatcherTimer? _updateTimer;
    private DispatcherTimer? _sampleCountTimer;
    private readonly Stopwatch _stopwatch = new();
    private int _lastSamplesCount;
    private int _fullSamplesCount;
    private int _samplesPerSecond;

    private Server? _server;

    private readonly LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;

    private MusicTrack? _musicTrack;

    private float _minMapTime;
    private float _maxMapTime;
    private float _videoStartTime;

    private string? _projectFolderName;

    public MainWindow()
    {
        InitializeComponent();
        SetupSampleCallLoop();

        Core.Initialize();

        _libVLC = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVLC);
        VideoView.MediaPlayer = _mediaPlayer;
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

    private void CallSample()
    {
        if (_server is not null && _server.ExceptionCalled)
        {
            _server.Dispose();
            _server = null;
            StartServer.Content = "Start Server";
            ServerDataInfo.Content = "Waiting for server...";
            return;
        }

        if (_server is null || !_server.Connected) return;

        var masterString = string.Empty;

        var lNd = _server.NetworkData;

        var accelX = $"{lNd.AccelX:F9}".PadRight(9, '0')[..9];
        var accelY = $"{lNd.AccelY:F9}".PadRight(9, '0')[..9];
        var accelZ = $"{lNd.AccelZ:F9}".PadRight(9, '0')[..9];
        var angleX = $"{lNd.GyroX:F9}".PadRight(9, '0')[..9];
        var angleY = $"{lNd.GyroY:F9}".PadRight(9, '0')[..9];
        var angleZ = $"{lNd.GyroZ:F9}".PadRight(9, '0')[..9];

        masterString +=
            $"Accel - X: {accelX} Y: {accelY} Z: {accelZ} | Angle: {angleX} {angleY} {angleZ} | SPS: {_samplesPerSecond}";

        ServerDataInfo.Content = masterString;

        _fullSamplesCount++;
    }

    private void UpdateSamplesPerSecond()
    {
        _samplesPerSecond = _fullSamplesCount - _lastSamplesCount;
        _lastSamplesCount = _fullSamplesCount;
    }

    private void StartServer_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_server is not null && !_server.Connected || _server is not null && _server.ExceptionCalled)
        {
            _server.Dispose();
            _server = null;
            StartServer.Content = "Start Server";
            ServerDataInfo.Content = "Waiting for server...";
            return;
        }

        if (_server is null)
        {
            _server = new Server(App.Settings!.CurrentIp, 14444);
            StartServer.Content = "Stop Server";
            ServerDataInfo.Content = $"Listening at {App.Settings.CurrentIp} - Waiting for data...";
            return;
        }

        if (!_server.Connected) return;

        _server.Dispose();
        _server = null;
        StartServer.Content = "Start Server";
        ServerDataInfo.Content = "Waiting for server...";
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void StartRecording_Click(object sender, RoutedEventArgs e)
    {        
        _mediaPlayer.Play();
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select a project folder",
        };

        if (dialog.ShowDialog() != true) return;

        var folder = dialog.FolderName;
        _projectFolderName = folder;

        if (!File.Exists(@$"{folder}\musictrack.json") || !File.Exists(@$"{folder}\video") || !File.Exists(@$"{folder}\audio"))
        {
            MessageBox.Show("Cannot find all needed files inside this folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _musicTrack = JsonConvert.DeserializeObject<MusicTrack>(File.ReadAllText(@$"{folder}\musictrack.json"));
        }
        catch (Exception)
        {
            MessageBox.Show("Failed to load musictrack.json.", "Deserialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _videoStartTime = _musicTrack?.COMPONENTS?[0]?.trackData?.structure?.videoStartTime != null
            ? float.Parse(_musicTrack.COMPONENTS[0].trackData.structure.videoStartTime.ToString().Replace("-", ""))
            : 0f;

        var startBeatIdx = _musicTrack?.COMPONENTS?[0]?.trackData?.structure?.startBeat != null
            ? float.Parse(_musicTrack.COMPONENTS[0].trackData.structure.startBeat.ToString().Replace("-", ""))
            : 0f;

        var startBeat = _musicTrack?.COMPONENTS?[0]?.trackData?.structure ?.markers?[(int)startBeatIdx] != null
            ? _musicTrack.COMPONENTS[0].trackData.structure.markers[(int)startBeatIdx]
            : 0f;

        var videoPath = Path.Combine(_projectFolderName!, "video");
        var audioPath = Path.Combine(_projectFolderName!, "audio");

        var media = new Media(_libVLC, videoPath, FromType.FromPath,
        [
            $":audio-desync={(_videoStartTime - (startBeat / 48000f)) * 1000f}"
        ]);

        await media.Parse(MediaParseOptions.ParseLocal);

        media.AddSlave(MediaSlaveType.Audio, 0, new Uri(audioPath).AbsoluteUri);

        _mediaPlayer.Play(media);

        await Task.Delay(100);

        _mediaPlayer.Pause();

        _minMapTime = 0 - _videoStartTime;
        _maxMapTime = (_mediaPlayer.Length / 1000f) - _videoStartTime;

        TimeLabel.Content = $"Map Time: {(_stopwatch.ElapsedMilliseconds / 1000f) - _videoStartTime:F2}ms / {_maxMapTime:F2}ms ";

        VideoView.Visibility = Visibility.Visible;
        StartRecording.IsEnabled = true;
        StartServer.IsEnabled = true;
    }
}