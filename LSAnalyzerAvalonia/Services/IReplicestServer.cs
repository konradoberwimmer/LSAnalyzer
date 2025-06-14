using System;
using System.Threading.Tasks;

namespace LSAnalyzerAvalonia.Services;

public interface IReplicestServer
{
    public Task<(bool success, Exception? exception)> StartServer();
    
    public (bool success, Exception? exception) InitializeConnection();
    
    public bool IsServerResponding();
    
    public (bool success, Exception? exception) ShutdownServer();
}