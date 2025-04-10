using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace rec_tool;

public class Server : IDisposable
{
    private TcpListener? _tcpListener;
    private TcpClient? _tcpClient;
    private readonly Thread _listenerThread;
    private readonly string _localIp;
    private readonly int _localPort;
    private bool _breakThread = false;

    public bool Connected = false;
    public bool ExceptionCalled = false;
    public bool ThreadBroken = false;

    public string RawData = "Waiting for raw data...";

    public NetworkData NetworkData = new();

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
        ThreadBroken = false;
        _tcpListener?.Stop();
        _tcpClient?.Close();
        _listenerThread?.Join();
        GC.SuppressFinalize(this);
    }

    private void Listen()
    {
        try
        {
            _tcpListener = new TcpListener(IPAddress.Parse(_localIp), _localPort);
            _tcpListener.Start();
            var bytes = new byte[150];
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
                        
                        // Treat received data here...

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
                    ThreadBroken = true;
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
    public float GyroX { get; set; }
    public float GyroY { get; set; }
    public float GyroZ { get; set; }
}