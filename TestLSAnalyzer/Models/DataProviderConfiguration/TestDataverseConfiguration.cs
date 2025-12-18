using LSAnalyzer.Helper;
using LSAnalyzer.Models.DataProviderConfiguration;

namespace TestLSAnalyzer.Models.DataProviderConfiguration;

public class TestDataverseConfiguration
{
    [Fact]
    public void TestSecureClone()
    {
        DataverseConfiguration config = new()
        {
            Id = 17,
            Url = "https://test.me",
            ApiToken = "myToken123",
            Name = "Test-DV"
        };

        var secureClone = (config.SecureClone() as DataverseConfiguration)!;
        
        Assert.True(ObjectTools.PublicInstancePropertiesEqual(secureClone, config, [ "Errors", "IsChanged", "ApiToken" ]));
        
        Assert.True(string.IsNullOrWhiteSpace(secureClone.ApiToken));
    }
}