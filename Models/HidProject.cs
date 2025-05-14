using System.Collections.Generic;

namespace HidRecorder.Models;

public class HidProject
{
    public string Name { get; set; } = string.Empty;
    
    public string Video { get; set; } = string.Empty;
    
    public string Audio { get; set; } = string.Empty;
    
    public string MusicTrack { get; set; } = string.Empty;
    
    public List<Session> Sessions { get; set; } = [];
}