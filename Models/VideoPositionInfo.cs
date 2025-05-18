using Avalonia;

namespace HidRecorder.Models;

public class VideoPositionInfo(double width, double height, PixelPoint position)
{
    public double Width { get; } = width;
    public double Height { get; } = height;
    public PixelPoint Position { get; } = position;
}