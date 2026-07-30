using LSAnalyzer.Models;

namespace TestLSAnalyzer.Models;

public class TestVirtualVariableScale
{
    [Fact]
    public void TestInputVariableNamesExistIn()
    {
        VirtualVariableScale virtualVariable = new()
        {
            InputVariable = new Variable(1, "X1"),
            WeightVariable = new Variable(2, "wgt")
        };
        
        Assert.True(virtualVariable.InputVariableNamesExistIn(new List<Variable> { new Variable(14, "x1"), new Variable(15, "X2"), new Variable(16, "wgt") }));
        Assert.False(virtualVariable.InputVariableNamesExistIn(new List<Variable> { new Variable(2, "X2") }));

        virtualVariable.WeightVariable = null;
        
        Assert.True(virtualVariable.InputVariableNamesExistIn(new List<Variable> { new Variable(14, "x1"), new Variable(15, "X2"), new Variable(16, "wgt") }));

        virtualVariable.MiVariable = new Variable(3, "mi");
        
        Assert.False(virtualVariable.InputVariableNamesExistIn(new List<Variable> { new Variable(14, "x1"), new Variable(15, "X2"), new Variable(16, "wgt") }));

        virtualVariable.MiVariable = null;
        virtualVariable.InputVariable = null;
        
        Assert.False(virtualVariable.InputVariableNamesExistIn(new List<Variable> { new Variable(2, "X2") }));
    }
}