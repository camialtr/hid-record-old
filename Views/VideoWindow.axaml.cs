using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using HidRecorder.Models;
using LibVLCSharp.Shared;
using Newtonsoft.Json;

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

    public async Task OpenVideo(string videoPath, string audioPath, string musicTrackPath)
    {
        if (_mediaPlayer == null || _libVlc == null || string.IsNullOrEmpty(videoPath))
            return;

        var musicTrack = JsonConvert.DeserializeObject<MusicTrack>(File.ReadAllText(musicTrackPath));
        
        var videoStartTime = musicTrack?.COMPONENTS?[0]?.trackData?.structure?.videoStartTime != null
            ? float.Parse(musicTrack.COMPONENTS[0].trackData.structure.videoStartTime.ToString().Replace("-", ""))
            : 0f;

        var startBeatIdx = musicTrack?.COMPONENTS?[0]?.trackData?.structure?.startBeat != null
            ? float.Parse(musicTrack.COMPONENTS[0].trackData.structure.startBeat.ToString().Replace("-", ""))
            : 0f;

        var startBeat = musicTrack?.COMPONENTS?[0]?.trackData?.structure ?.markers?[(int)startBeatIdx] != null
            ? musicTrack.COMPONENTS[0].trackData.structure.markers[(int)startBeatIdx]
            : 0f;
        
        var media = new Media(_libVlc, videoPath, FromType.FromPath,
        [
            $":audio-desync={(videoStartTime - (startBeat / 48000f)) * 1000f}"
        ]);

        await media.Parse(MediaParseOptions.ParseLocal);

        media.AddSlave(MediaSlaveType.Audio, 0, new Uri(audioPath).AbsoluteUri);

        _mediaPlayer.Play(media);

        await Task.Delay(100);

        _mediaPlayer.Pause();
    }
}

