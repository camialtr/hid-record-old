using System.Collections.Generic;

namespace HidRecorder.Models;

public class HidProject
{
    public string Name { get; init; } = string.Empty;
    
    public string Video { get; init; } = string.Empty;
    
    public string Audio { get; init; } = string.Empty;
    
    public string MusicTrack { get; init; } = string.Empty;
    
    public List<Session> Sessions { get; init; } = [];
}