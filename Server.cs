using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using Newtonsoft.Json;

#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable SYSLIB0006

// ReSharper disable EmptyGeneralCatchClause
// ReSharper disable AsyncVoidMethod

namespace Server;

public static class Middleware
{
    public static DancerServer DancerServer { get; private set; } = new();
}

public class DancerServer
{
    private TcpListener tcpListener;
    private TcpClient tcpClient;
    private Thread listenerThread;
    private string localIP;
    private int localPort;

    public bool Connected = false;
    public bool BreakThread = false;
    public bool ThreadBreaked = false;
    public bool IsQuitting = false;

    public string RawData = "Waiting for raw data...";

    public NetworkData NetworkData = new()
    {
        AccelX = 0,
        AccelY = 0,
        AccelZ = 0,
        GyroX = 0,
        GyroY = 0,
        GyroZ = 0
    };

    public void Connect(string ip, int port)
    {
        localIP = ip;
        localPort = port;
        listenerThread = new Thread(Listen)
        {
            IsBackground = true
        };
        listenerThread.Start();
    }

    public void Disconnect()
    {
        tcpListener.Stop();
        listenerThread.Abort();
        ThreadBreaked = false;
    }

    private async void Listen()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Parse(localIP), localPort);
            tcpListener.Start();
            var bytes = new byte[150];
            while (true)
            {
                using (tcpClient = await tcpListener.AcceptTcpClientAsync())
                {
                    using var stream = tcpClient.GetStream();
                    int length;
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        var incomingData = new byte[length];
                        Array.Copy(bytes, 0, incomingData, 0, length);
                        var clientMessage = Encoding.UTF8.GetString(incomingData);
                        RawData = clientMessage;
                        try
                        {
                            NetworkData = JsonConvert.DeserializeObject<NetworkData>(clientMessage.Replace("*", ""));
                        }
                        catch
                        {
                        }

                        if (!Connected)
                        {
                            SendMessage("0");
                            Connected = true;
                        }

                        if (!BreakThread) continue;
                        SendMessage("1");
                        break;
                    }

                    if (!BreakThread) continue;
                    tcpListener.Stop();
                    tcpClient.Close();
                    Connected = false;
                    BreakThread = false;
                    ThreadBreaked = true;
                    break;
                }
            }
        }
        catch (SocketException socketException)
        {
            //Debug.LogError("SocketException " + socketException.ToString());
        }
        catch (ObjectDisposedException disposedException)
        {
        }
    }

    public void SendMessage(string networkMessage)
    {
        try
        {
            var stream = tcpClient.GetStream();
            if (!stream.CanWrite) return;
            var message = Encoding.UTF8.GetBytes(networkMessage);
            stream.Write(message, 0, message.Length);
        }
        catch (SocketException socketException)
        {
            //Debug.LogError("SocketException " + socketException.ToString());
        }
        catch (ObjectDisposedException disposedException)
        {
            // The famous brazilian "try catch cala a boca"
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