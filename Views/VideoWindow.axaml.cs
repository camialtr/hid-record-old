using System;
using Avalonia;
using System.IO;
using Newtonsoft.Json;
using Avalonia.Controls;
using HidRecorder.Models;
using LibVLCSharp.Shared;
using System.Threading.Tasks;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable SpecifyACultureInStringConversionExplicitly

namespace HidRecorder.Views;

public partial class VideoWindow : Window
{
    private readonly LibVLC? _libVlc;
    public readonly MediaPlayer? MediaPlayer;
    public float VideoStartTime { get; private set; }
    
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
        MediaPlayer = new MediaPlayer(_libVlc);

        MediaPlayer.Hwnd = TryGetPlatformHandle()!.Handle;
    }

    public async Task OpenVideo(string videoPath, string audioPath, string musicTrackPath)
    {
        if (MediaPlayer == null || _libVlc == null || string.IsNullOrEmpty(videoPath))
            return;

        var musicTrack = JsonConvert.DeserializeObject<MusicTrack>(await File.ReadAllTextAsync(musicTrackPath));
        
        VideoStartTime = musicTrack?.COMPONENTS?[0]?.trackData?.structure?.videoStartTime != null
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
            $":audio-desync={(VideoStartTime - (startBeat / 48000f)) * 1000f}"
        ]);

        await media.Parse();

        media.AddSlave(MediaSlaveType.Audio, 0, new Uri(audioPath).AbsoluteUri);

        MediaPlayer.Play(media);

        await Task.Delay(100);

        MediaPlayer.Pause();
        
        Hide();
    }
}

