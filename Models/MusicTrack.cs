namespace rec_tool.Models;

#pragma warning disable CS8618

public class MusicTrack
{
    public string __class { get; set; }
    public int WIP { get; set; }
    public int LOWUPDATE { get; set; }
    public int UPDATE_LAYER { get; set; }
    public int PROCEDURAL { get; set; }
    public int STARTPAUSED { get; set; }
    public int FORCEISENVIRONMENT { get; set; }
    public List<COMPONENT> COMPONENTS { get; set; }

    public class COMPONENT
    {
        public string __class { get; set; }
        public TrackData trackData { get; set; }
    }

    public class TrackData
    {
        public string __class { get; set; }
        public Structure structure { get; set; }
        public string path { get; set; }
        public string url { get; set; }
    }

    public class Structure
    {
        public string __class { get; set; }
        public List<int> markers { get; set; }
        public List<Signature> signatures { get; set; }
        public List<Section> sections { get; set; }
        public int startBeat { get; set; }
        public int endBeat { get; set; }
        public double videoStartTime { get; set; }
        public int previewEntry { get; set; }
        public int previewLoopStart { get; set; }
        public int previewLoopEnd { get; set; }
        public int volume { get; set; }
    }
    public class Signature
    {
        public string __class { get; set; }
        public int marker { get; set; }
        public int beats { get; set; }
    }

    public class Section
    {
        public string __class { get; set; }
        public int marker { get; set; }
        public int sectionType { get; set; }
        public string comment { get; set; }
    }
}
