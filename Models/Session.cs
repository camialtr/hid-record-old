namespace HidRecorder.Models;

public class Session(string platform, string file, int linkId = 0, bool export = true)
{
    public string Platform { get; set; } = platform;
    public string Name { get; set; } = file;
    public int LinkId { get; set; } = linkId;
    public bool Export { get; set; } = export;
}