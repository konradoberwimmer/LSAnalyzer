using System.Runtime.InteropServices;
using System.Security.AccessControl;
using LSAnalyzerAvalonia.Services;

namespace TestLSAnalyzerAvalonia.Services;

public class TestReplicestServer
{
    [Fact]
    public async Task TestServerCommunicationWorkflow()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;
        
        var serverAddress = Path.GetTempFileName();
        var dataStreamAddress = Path.GetTempFileName();
        
        ReplicestServer replicestServerService = new(serverAddress, dataStreamAddress);
        
        var (resultStart, exceptionStart) = await replicestServerService.StartServer();
        
        Assert.True(resultStart);
        Assert.Null(exceptionStart);

        var (resultConnection, exceptionConnection) = replicestServerService.InitializeConnection();
        
        Assert.True(resultConnection);
        Assert.Null(exceptionConnection);
        
        Assert.True(replicestServerService.IsServerResponding());
        
        var (resultShutdown, exceptionShutDown) = replicestServerService.ShutdownServer();
        
        Assert.True(resultShutdown);
        Assert.Null(exceptionShutDown);
    }
    
    [Fact]
    public async Task TestServerConfigLeadsToError()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;
        
        ReplicestServer replicestServerService = new("/", "/");
        
        var (resultStart, exceptionStart) = await replicestServerService.StartServer();
        
        Assert.False(resultStart);
        Assert.NotNull(exceptionStart);
    }
    
    [Fact]
    public async Task TestServerNotCreatableAtAddress()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;
        
        ReplicestServer replicestServerService = new("/here", "/there");
        
        var (resultStart, exceptionStart) = await replicestServerService.StartServer();
        
        Assert.False(resultStart);
        Assert.Null(exceptionStart);
    }

    [Fact]
    public void TestInitializeConnectionError()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;
        
        ReplicestServer replicestServerService = new("/somewhere", "/somewhere");
        
        var (resultConnection, exceptionConnection) = replicestServerService.InitializeConnection();
        
        Assert.False(resultConnection);
        Assert.NotNull(exceptionConnection);
    }
    
    [Fact]
    public void TestIsServerRespondingWhenNotConnected()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;
        
        ReplicestServer replicestServerService = new("/somewhere", "/somewhere");
        
        Assert.False(replicestServerService.IsServerResponding());
    }
    
    [Fact]
    public async Task TestShutDownServerFailsWhenConnectionAlreadyClosed()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;
        
        var serverAddress = Path.GetTempFileName();
        var dataStreamAddress = Path.GetTempFileName();
        
        ReplicestServer replicestServerService = new(serverAddress, dataStreamAddress);
        
        await replicestServerService.StartServer();
        replicestServerService.InitializeConnection();
        replicestServerService.ShutdownServer();
        
        Thread.Sleep(1000);
        
        var (resultShutdown, exceptionShutDown) = replicestServerService.ShutdownServer();
        
        Assert.False(resultShutdown);
        Assert.NotNull(exceptionShutDown);
    }

}