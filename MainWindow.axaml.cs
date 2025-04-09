using System;
using System.Diagnostics;
using Server;
using System.Net;
using System.Timers;
using Avalonia.Controls;
using System.Net.Sockets;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace rec_tool;

public partial class MainWindow : Window
{
    private DispatcherTimer _updateTimer;
    private Stopwatch _stopwatch = new();
    private int _frameCount = 0;

    public MainWindow()
    {
        InitializeComponent();
        SetupSampleCallLoop();
    }

    private void StartServer_OnClick(object? sender, RoutedEventArgs e)
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
            Middleware.DancerServer.Connect(ip.ToString(), 14444);
            StartServer.IsEnabled = false;
            IpInfo.Content = $"Current Ip: {ip}";
            break;
        }
    }

    private void SetupSampleCallLoop()
    {
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 120)
        };
        _updateTimer.Tick += (sender, args) => CallSample();
        _updateTimer.Start();
        _stopwatch.Start();
    }

    private void CallSample()
    {
        //if (!_dancerServer.Connected) return;

        //if (NetworkDataIsNull())
        //{
        //    HidContent.Content = "Waiting for data...";
        //    return;
        //}

        var uiTextString = "";

        uiTextString += $"\nRaw Data: {Middleware.DancerServer.RawData}";
        uiTextString +=
            $"\nAcceleration: {Middleware.DancerServer.NetworkData.AccelX}, {Middleware.DancerServer.NetworkData.AccelY}, {Middleware.DancerServer.NetworkData.AccelZ}";
        uiTextString +=
            $"\nAngle: {Middleware.DancerServer.NetworkData.GyroX}, {Middleware.DancerServer.NetworkData.GyroY}, {Middleware.DancerServer.NetworkData.GyroZ}";

        HidContent.Content = uiTextString;

        _frameCount++;
        HidContent.Content += $"\nSamples Call Count: {_frameCount}";
    }

    private bool NetworkDataIsNull()
    {
        if (Middleware.DancerServer.NetworkData.AccelX != 0) return false;
        if (Middleware.DancerServer.NetworkData.AccelY != 0) return false;
        if (Middleware.DancerServer.NetworkData.AccelZ != 0) return false;
        if (Middleware.DancerServer.NetworkData.GyroX != 0) return false;
        if (Middleware.DancerServer.NetworkData.GyroY != 0) return false;
        if (Middleware.DancerServer.NetworkData.GyroZ != 0) return false;
        return true;
    }
}