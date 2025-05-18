using Avalonia;
using Avalonia.Controls;
using LibVLCSharp.Shared;

namespace HidRecorder.Views;

public partial class VideoWindow : Window
{
    private readonly LibVLC? _libVlc;
    private readonly MediaPlayer? _mediaPlayer;
    
    public VideoWindow()
    {
        InitializeComponent();
    }
    
    public VideoWindow(double width, double height, PixelPoint position)
    {
        InitializeComponent();
        
        Width = width;
        Height = height;
        Position = position;
            
        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);

        _mediaPlayer.Hwnd = TryGetPlatformHandle()!.Handle;
    }

    public void OpenVideo(string videoPath)
    {
        if (_mediaPlayer == null || _libVlc == null || string.IsNullOrEmpty(videoPath))
            return;

        var media = new Media(_libVlc, videoPath);
        _mediaPlayer.Play(media);
        media.Dispose();
    }
}

