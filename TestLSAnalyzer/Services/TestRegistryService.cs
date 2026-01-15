using LSAnalyzer.Services;

namespace TestLSAnalyzer.Services;

public class TestRegistryService
{
    [Fact]
    public void TestGetDefaultRLocation()
    {
        RegistryService registryService = new();
        
        var defaultLocation = registryService.GetDefaultRLocation();
        
        Assert.True(defaultLocation is not null, "A default R installation is necessary for this test to succeed");
        Assert.True(Directory.Exists(defaultLocation));
        Assert.True(Directory.Exists(Path.Combine(defaultLocation, "bin", "x64")));
    }
}