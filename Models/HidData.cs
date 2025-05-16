namespace HidRecorder.Models;

public class HidData(float time, float accelX, float accelY, float accelZ, float angleX, float angleY, float angleZ)
{
    public float Time { get; set; } = time;
    
    public float AccelX { get; set; } = accelX;
    
    public float AccelY { get; set; } = accelY;
    
    public float AccelZ { get; set; } = accelZ;
    
    public float AngleX { get; set; } = angleX;
    
    public float AngleY{ get; set; } = angleY;
    
    public float AngleZ{ get; set; } = angleZ;
}