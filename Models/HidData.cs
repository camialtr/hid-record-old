namespace HidRecorder.Models;

public class HidData(float time, float accelX, float accelY, float accelZ, float gyroX, float gyroY, float gyroZ)
{
    public float Time { get; set; } = time;
    
    public float AccelX { get; set; } = accelX;
    
    public float AccelY { get; set; } = accelY;
    
    public float AccelZ { get; set; } = accelZ;
    
    public float GyroX { get; set; } = gyroX;
    
    public float GyroY{ get; set; } = gyroY;
    
    public float GyroZ{ get; set; } = gyroZ;
}