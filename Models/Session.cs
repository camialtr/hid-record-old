namespace HidRecorder.Models;

public class Session(string platform, string file, bool export = true)
{
    public string Platform { get; set; } = platform;
    
    public string Name { get; set; } = System.IO.Path.GetFileNameWithoutExtension(file);
    
    public bool Export { get; set; } = export;
}