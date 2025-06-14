using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LSAnalyzerAvalonia.Services;

public class ReplicestServer(string serverAddress, string dataStreamAddress) : IReplicestServer
{
    private Socket? _serverSocket;

    public async Task<(bool success, Exception? exception)> StartServer()
    {
        try
        {
            // note that this assumes that the streaming socket is created after the server (datagram) socket
            var fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(dataStreamAddress)!)
            {
                Filter = Path.GetFileName(dataStreamAddress),
                EnableRaisingEvents = true,
            };
            SemaphoreSlim signal = new(0, 1);
            fileSystemWatcher.Created += (sender, args) => signal.Release();
            
            Process.Start(Path.Combine(AppContext.BaseDirectory, "replicest_server"), $"-s {serverAddress} -d {dataStreamAddress}");

            var filesCreated = await signal.WaitAsync(TimeSpan.FromSeconds(3));
            
            return (filesCreated, null);
        }
        catch (Exception exception)
        {
            return (false, exception);
        }
    }

    public (bool success, Exception? exception) InitializeConnection()
    {
        try
        {
            var unixSocket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified);
            unixSocket.Bind(new UnixDomainSocketEndPoint(Path.GetTempPath() + Guid.NewGuid()));
            unixSocket.Connect(new UnixDomainSocketEndPoint(serverAddress));
            _serverSocket = unixSocket;
        }
        catch (Exception exception)
        {
            return (false, exception);
        }

        return (true, null);
    }
    
    public bool IsServerResponding()
    {
        if (_serverSocket == null) return false;
        
        _serverSocket.Send("dummy command"u8);
        
        var buffer = new byte[1024];
        _serverSocket.Receive(buffer);
        
        return Encoding.ASCII.GetString(buffer).StartsWith("unknown");
    }

    public (bool success, Exception? exception) ShutdownServer()
    {
        try
        {
            _serverSocket?.Send("shutdown"u8);
            _serverSocket?.Close();
        }
        catch (Exception exception)
        {
            return (false, exception);
        }
        
        return (true, null);
    }
}