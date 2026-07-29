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

    [Fact]
    public void TestInputVariableNamesExistIn()
    {
        VirtualVariableCombine virtualVariable = new()
        {
            Variables = [ new Variable(1, "x1"), new Variable(2, "X2") ]
        };
        
        Assert.True(virtualVariable.InputVariableNamesExistIn(new List<Variable> { new Variable(14, "X1"), new Variable(15, "X2") }));
        Assert.False(virtualVariable.InputVariableNamesExistIn(new List<Variable> { new Variable(2, "X2") }));
    }
}