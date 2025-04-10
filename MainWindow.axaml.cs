using System;
using Avalonia.Controls;
using System.Diagnostics;
using Avalonia.Threading;
using Avalonia.Interactivity;

namespace rec_tool;

public partial class MainWindow : Window
{
    private DispatcherTimer? _updateTimer;
    private DispatcherTimer? _sampleCountTimer;
    private readonly Stopwatch _stopwatch = new();
    private int _lastSamplesCount = 0;
    private int _fullSamplesCount = 0;
    private int _samplesPerSecond;
    private Server? _server;

    public MainWindow()
    {
        InitializeComponent();
        SetupSampleCallLoop();
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

        masterString += $"RawData: {_server.RawData} | Cut Data: ";
        
        ServerDataInfo.Content = masterString;
    }

    private void UpdateSamplesPerSecond()
    {
        _samplesPerSecond = _fullSamplesCount - _lastSamplesCount;
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
            _server = new Server("192.168.1.3", 14444);
            StartServer.Content = "Stop Server";
            ServerDataInfo.Content = "Listening at 192.168.1.3 - Waiting for data...";
            return;
        }

        if (!_server.Connected) return;
        
        _server.Dispose();
        _server = null;
        StartServer.Content = "Start Server";
        ServerDataInfo.Content = "Waiting for server...";
    }
}