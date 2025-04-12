using System.Windows;
using rec_tool.Models;
using System.Diagnostics;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using System.Text.Json;
using System.IO;
using System.Collections.ObjectModel;
using Microsoft.Win32;

#pragma warning disable CS8601

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

    private LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;

    public ObservableCollection<LittleTest> LittleTestList;

    private MusicTrack _musicTrack;

    private float _videoStartTime;

    public MainWindow()
    {
        InitializeComponent();
        SetupSampleCallLoop();

        Core.Initialize();

        _libVLC = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVLC);
        _mediaPlayer.TimeChanged += (_, _) => MediaPlayer_TimeChanged();
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

        _stopwatch.Start();
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
            _server = new Server(App.Settings.CurrentIp, 14444);
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

    private void PlayVideo_Click(object sender, RoutedEventArgs e)
    {
        if (!_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Play();
            PlayButton.Content = "Pause Video";
            TimeSlider.IsEnabled = false;
            TimeSlider.Opacity = 0.25;
        }
        else
        {
            _mediaPlayer.Pause();
            PlayButton.Content = "Play Video";
            TimeSlider.IsEnabled = true;
            TimeSlider.Opacity = 1.0;
        }
    }

    private void MediaPlayer_TimeChanged()
    {
        if (!_mediaPlayer.IsPlaying) return;

        try
        {
            TimeLabel.Dispatcher.Invoke(() =>
            {
                TimeLabel.Content = $"Map Time: {(_mediaPlayer.Time / 1000)}ms / {(_mediaPlayer.Length / 1000) - _videoStartTime}ms";
                TimeSlider.Value = _mediaPlayer.Time + _videoStartTime;
            });
        }
        catch (Exception) { }
    }

    private void StartRecording_Click(object sender, RoutedEventArgs e)
    {
        MusicTrack mt = JsonSerializer.Deserialize<MusicTrack>(File.ReadAllText(@"C:\Users\Administrator\Documents\Testing Projects\Rec Tool\Mad\musictrack.json"));

        LittleTestList = new();

        foreach (int marker in mt.COMPONENTS[0].trackData.structure.markers)
        {
            LittleTestList.Add(new() { TestInt = marker, TestString = "AAAAAAAAAAAAAAAAA" });
        }

        RecordedDataContext.ItemsSource = LittleTestList;
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select a project folder",
        };

        if (dialog.ShowDialog() != true) return;

        var folder = dialog.FolderName;

        if (!File.Exists(@$"{folder}\musictrack.json") || !File.Exists(@$"{folder}\video.webm") || !File.Exists(@$"{folder}\audio.ogg"))
        {
            MessageBox.Show("Cannot find all needed files inside this folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _musicTrack = JsonSerializer.Deserialize<MusicTrack>(File.ReadAllText(@$"{folder}\musictrack.json"));
        }
        catch (Exception)
        {
            MessageBox.Show("Failed to load musictrack.json.", "Deserialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _videoStartTime = float.Parse(_musicTrack.COMPONENTS[0].trackData.structure.videoStartTime.ToString().Replace("-", ""));

        using var media = new Media(_libVLC, @"C:\Users\Administrator\Documents\Testing Projects\Rec Tool\Mad\video.webm");
        _mediaPlayer.Play(media);
        VideoView.Visibility = Visibility.Visible;
        await Task.Delay(100);
        _mediaPlayer.Pause();

        TimeSlider.Maximum = _mediaPlayer.Length - _videoStartTime;
        TimeSlider.Minimum = 0;

        TimeLabel.Dispatcher.Invoke(() =>
        {
            _mediaPlayer.Time = (long)Convert.ToDouble(_videoStartTime);
            TimeLabel.Content = $"Map Time: {(_mediaPlayer.Time / 1000)}ms / {(_mediaPlayer.Length / 1000) - _videoStartTime}ms";
        });

        _mediaPlayer.NextFrame();

        TimeSlider.Opacity = 1.0;
        TimeSlider.Visibility = Visibility.Visible;
        TimeSlider.IsEnabled = true;
        StartRecording.IsEnabled = true;
        PlayButton.IsEnabled = true;
        StartServer.IsEnabled = true;
    }

    private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_mediaPlayer.IsPlaying) return;

        TimeLabel.Dispatcher.Invoke(() =>
        {
            _mediaPlayer.Time = (long)Convert.ToDouble(TimeSlider.Value + _videoStartTime);
            TimeLabel.Content = $"Map Time: {(_mediaPlayer.Time / 1000)}ms / {(_mediaPlayer.Length / 1000) - _videoStartTime}ms";
        });

        _mediaPlayer.NextFrame();
    }
}

public class LittleTest
{
    public int TestInt { get; set; }
    public string TestString { get; set; }
}