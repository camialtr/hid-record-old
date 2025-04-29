using Avalonia;
using Avalonia.Controls;
using LibVLCSharp.Shared;

namespace HidRecorder.Views;

public partial class VideoWindow : Window
{
    private readonly LibVLC? _libVlc;
    private readonly MediaPlayer? _mediaPlayer;
    
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
}