using System.Windows;
using rec_tool.Models;
using System.Diagnostics;
using System.Windows.Threading;
using LibVLCSharp.Shared;

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
        VideoView.Visibility = Visibility.Visible;
        using var media = new Media(_libVLC, @"C:\Users\Administrator\Documents\Repositories\mad.webm");
        _mediaPlayer.Play(media);
    }

    private void MediaPlayer_TimeChanged()
    {
        try
        {
            TimeLabel.Dispatcher.Invoke(() =>
            {
                TimeLabel.Content = $"Map Time: {_mediaPlayer.Time / 1000:F2}ms / {_mediaPlayer.Length / 1000:F2}ms";
            });
        }
        catch (Exception) { }
    }
}