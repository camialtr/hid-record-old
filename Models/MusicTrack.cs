using System.Collections.Generic;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming

namespace HidRecorder.Models;

public class MusicTrack
{
    public string __class { get; set; } = string.Empty;
    public int WIP { get; set; }
    public int LOWUPDATE { get; set; }
    public int UPDATE_LAYER { get; set; }
    public int PROCEDURAL { get; set; }
    public int STARTPAUSED { get; set; }
    public int FORCEISENVIRONMENT { get; set; }
    public List<COMPONENT> COMPONENTS { get; set; } = [];
    
    public class COMPONENT
    {
        public string __class { get; set; } = string.Empty;
        public TrackData trackData { get; set; } = new();
    }

    public class Section
    {
        public string __class { get; set; } = string.Empty;
        public int marker { get; set; }
        public int sectionType { get; set; }
        public string comment { get; set; } = string.Empty;
    }

    public class Signature
    {
        public string __class { get; set; } = string.Empty;
        public int marker { get; set; }
        public int beats { get; set; }
    }

    public class Structure
    {
        public string __class { get; set; } = string.Empty;
        public List<int> markers { get; set; } = [];
        public List<Signature> signatures { get; set; } = [];
        public List<Section> sections { get; set; } = [];
        public int startBeat { get; set; }
        public int endBeat { get; set; }
        public int fadeStartBeat { get; set; }
        public bool useFadeStartBeat { get; set; }
        public int fadeEndBeat { get; set; }
        public bool useFadeEndBeat { get; set; }
        public double videoStartTime { get; set; }
        public int previewEntry { get; set; }
        public int previewLoopStart { get; set; }
        public int previewLoopEnd { get; set; }
        public int volume { get; set; }
        public int fadeInDuration { get; set; }
        public int fadeInType { get; set; }
        public int fadeOutDuration { get; set; }
        public int fadeOutType { get; set; }
    }

    public class TrackData
    {
        public string __class { get; set; } = string.Empty;
        public Structure structure { get; set; } = new();
        public string path { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
    }
}