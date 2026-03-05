using LSAnalyzer.Models;

namespace TestLSAnalyzer.Models;

public class TestVirtualVariableCombine
{
    [Fact]
    public void TestChangingVariablesChangesFromPlausibelValues()
    {
        VirtualVariableCombine virtualVariable = new();
        
        Assert.False(virtualVariable.FromPlausibleValues);

        virtualVariable.Variables = [];
        
        Assert.False(virtualVariable.FromPlausibleValues);
        
        virtualVariable.Variables.Add(new Variable(1, "x"));
        
        Assert.False(virtualVariable.FromPlausibleValues);
        
        virtualVariable.Variables.Add(new Variable(2, "y") { FromPlausibleValues = true });
        
        Assert.True(virtualVariable.FromPlausibleValues);

        virtualVariable.Variables.RemoveAt(0);
        
        Assert.True(virtualVariable.FromPlausibleValues);

        virtualVariable.Variables = [];
        
        Assert.False(virtualVariable.FromPlausibleValues);
    }
}