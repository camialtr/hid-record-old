using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace HidRecorder.Models;

public class Server : IDisposable
{
    private TcpListener? _tcpListener;
    private TcpClient? _tcpClient;
    private readonly Thread _listenerThread;
    private readonly string _localIp;
    private readonly int _localPort;
    private bool _breakThread;

    public bool Connected;
    public bool ExceptionCalled;

    public readonly NetworkData NetworkData = new();

    public Server(string ip, int port)
    {
        _localIp = ip;
        _localPort = port;
        _listenerThread = new Thread(Listen)
        {
            IsBackground = true
        };
        _listenerThread.Start();
    }

    public void Dispose()
    {
        _breakThread = true;
        _tcpListener?.Stop();
        _tcpClient?.Close();
        _listenerThread.Join();
        GC.SuppressFinalize(this);
    }

    private void Listen()
    {
        try
        {
            _tcpListener = new TcpListener(IPAddress.Parse(_localIp), _localPort);
            _tcpListener.Start();
            var bytes = new byte[59];
            while (true)
            {
                using (_tcpClient = _tcpListener.AcceptTcpClient())
                {
                    using var stream = _tcpClient.GetStream();
                    int length;
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        var incomingData = new byte[length];
                        Array.Copy(bytes, 0, incomingData, 0, length);
                        var clientMessage = Encoding.UTF8.GetString(incomingData);

                        var dataArray = clientMessage.Split('|', StringSplitOptions.RemoveEmptyEntries);

                        if (dataArray.Length == 6)
                        {
                            NetworkData.AccelX = float.Parse(dataArray[0], System.Globalization.CultureInfo.InvariantCulture);
                            NetworkData.AccelY = float.Parse(dataArray[1], System.Globalization.CultureInfo.InvariantCulture);
                            NetworkData.AccelZ = float.Parse(dataArray[2], System.Globalization.CultureInfo.InvariantCulture);
                            NetworkData.AngleX = float.Parse(dataArray[3], System.Globalization.CultureInfo.InvariantCulture);
                            NetworkData.AngleY = float.Parse(dataArray[4], System.Globalization.CultureInfo.InvariantCulture);
                            NetworkData.AngleZ = float.Parse(dataArray[5], System.Globalization.CultureInfo.InvariantCulture);
                        }

                        if (!Connected)
                        {
                            SendMessage("0");
                            Connected = true;
                        }

                        if (!_breakThread) continue;
                        SendMessage("1");
                        break;
                    }

                    if (!_breakThread) continue;
                    _tcpListener.Stop();
                    _tcpClient.Close();
                    Connected = false;
                    _breakThread = false;
                    break;
                }
            }
        }
        catch (Exception)
        {
            ExceptionCalled = true;
        }
    }

    private void SendMessage(string networkMessage)
    {
        try
        {
            if (_tcpClient is null) return;
            var stream = _tcpClient.GetStream();
            if (!stream.CanWrite) return;
            var message = Encoding.UTF8.GetBytes(networkMessage);
            stream.Write(message, 0, message.Length);
        }
        catch (Exception)
        {
            ExceptionCalled = true;
        }
    }
}

public class NetworkData
{
    public float AccelX { get; set; }
    public float AccelY { get; set; }
    public float AccelZ { get; set; }
    public float AngleX { get; set; }
    public float AngleY { get; set; }
    public float AngleZ { get; set; }
}