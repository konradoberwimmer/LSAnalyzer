using System.Text.Json;
using LSAnalyzer.Models;

namespace TestLSAnalyzer.Models;

public class TestVirtualVariableRecode
{
    [Fact]
    public void TestVariablesRulesConsistency()
    {
        VirtualVariableRecode virtualVariableRecode = new();
        
        virtualVariableRecode.AddVariable(new Variable(1, "item1"));
        virtualVariableRecode.Else = VirtualVariableRecode.ElseAction.Copy;

        virtualVariableRecode.AddVariable(new Variable(2, "item2"));
        
        Assert.Equal(VirtualVariableRecode.ElseAction.Missing, virtualVariableRecode.Else);
        Assert.False(virtualVariableRecode.ElseValueMakesSense);
        
        virtualVariableRecode.AddRule();
        
        Assert.Equal(2, virtualVariableRecode.Rules.First().Criteria.Count);
        Assert.Equal([0, 1], virtualVariableRecode.Rules.First().Criteria.Select(crit => crit.VariableIndex));
        
        virtualVariableRecode.RemoveLastVariable();
        
        Assert.Single(virtualVariableRecode.Rules.First().Criteria);
        Assert.Equal([0], virtualVariableRecode.Rules.First().Criteria.Select(crit => crit.VariableIndex));
        
        virtualVariableRecode.AddVariable(new Variable(3, "item3"));
        virtualVariableRecode.Else = VirtualVariableRecode.ElseAction.Set;
        
        Assert.True(virtualVariableRecode.ElseValueMakesSense);
        Assert.Equal(2, virtualVariableRecode.Rules.First().Criteria.Count);
        Assert.Equal([0, 1], virtualVariableRecode.Rules.First().Criteria.Select(crit => crit.VariableIndex));
        
        virtualVariableRecode.RemoveLastVariable();
        virtualVariableRecode.RemoveLastVariable();

        Assert.Empty(virtualVariableRecode.Rules);
    }
    
    [Fact]
    public void TestVariablesRulesConsistencyAfterDeserialization()
    {
        VirtualVariableRecode virtualVariableRecode = new();

        virtualVariableRecode.Else = VirtualVariableRecode.ElseAction.Copy;
        
        virtualVariableRecode.AddVariable(new Variable(1, "item1"));
        
        var virtualVariableRecodeAsJson = JsonSerializer.Serialize(virtualVariableRecode);
        var virtualVariableRecodeDeserialized = JsonSerializer.Deserialize<VirtualVariableRecode>(virtualVariableRecodeAsJson)!;
        
        virtualVariableRecodeDeserialized.AddVariable(new Variable(2, "item2"));
        
        Assert.Equal(VirtualVariableRecode.ElseAction.Missing, virtualVariableRecodeDeserialized.Else);
        Assert.False(virtualVariableRecodeDeserialized.ElseValueMakesSense);
        
        virtualVariableRecodeDeserialized.AddRule();
        
        virtualVariableRecodeAsJson = JsonSerializer.Serialize(virtualVariableRecodeDeserialized);
        virtualVariableRecodeDeserialized = JsonSerializer.Deserialize<VirtualVariableRecode>(virtualVariableRecodeAsJson)!;
        
        Assert.Equal(2, virtualVariableRecodeDeserialized.Rules.First().Criteria.Count);
        Assert.Equal([0, 1], virtualVariableRecodeDeserialized.Rules.First().Criteria.Select(crit => crit.VariableIndex));
    }

    [Fact]
    public void TestInfo()
    {
        VirtualVariableRecode virtualVariableRecode = new();

        virtualVariableRecode.AddVariable(new Variable(1, "item1"));
        virtualVariableRecode.Else = VirtualVariableRecode.ElseAction.Copy;
        virtualVariableRecode.AddRule();
        
        Assert.Equal("recode(item1, '0=0;else=copy')", virtualVariableRecode.Info);
        
        virtualVariableRecode.AddVariable(new Variable(2, "item2"));
        
        Assert.Equal("recode([item1,item2], '[0,0]=0;else=NA')", virtualVariableRecode.Info);

        virtualVariableRecode.Rules.First().Criteria.First().Type = VirtualVariableRecode.Term.TermType.Missing;
        virtualVariableRecode.Rules.First().Criteria.Last().Type = VirtualVariableRecode.Term.TermType.Between;
        virtualVariableRecode.Rules.First().Criteria.Last().Value = 1.0;
        virtualVariableRecode.Rules.First().Criteria.Last().MaxValue = 2.0;
        virtualVariableRecode.Rules.First().ResultNa = true;
        
        Assert.Equal("recode([item1,item2], '[NA,1-2]=NA;else=NA')", virtualVariableRecode.Info);
        
        virtualVariableRecode.AddRule();
        
        Assert.Equal("recode([item1,item2], '[NA,1-2]=NA;[0,0]=0;else=NA')", virtualVariableRecode.Info);

        virtualVariableRecode.Else = VirtualVariableRecode.ElseAction.Set;
        virtualVariableRecode.ElseValue = -1;
        
        Assert.Equal("recode([item1,item2], '[NA,1-2]=NA;[0,0]=0;else=-1')", virtualVariableRecode.Info);
        
        virtualVariableRecode.RemoveLastVariable();
        
        Assert.Equal("recode(item1, 'NA=NA;0=0;else=-1')", virtualVariableRecode.Info);
    }
    
    [Fact]
    public void TestRecodeInfo()
    {
        VirtualVariableRecode virtualVariableRecode = new();

        virtualVariableRecode.AddVariable(new Variable(1, "item1"));
        virtualVariableRecode.Else = VirtualVariableRecode.ElseAction.Copy;
        virtualVariableRecode.AddRule();
        
        Assert.Equal("'0=0;else=copy'", virtualVariableRecode.RecodeInfo);
        
        virtualVariableRecode.AddVariable(new Variable(2, "item2"));
        
        Assert.Equal("'[0,0]=0;else=NA'", virtualVariableRecode.RecodeInfo);

        virtualVariableRecode.Rules.First().Criteria.First().Type = VirtualVariableRecode.Term.TermType.Missing;
        virtualVariableRecode.Rules.First().Criteria.Last().Type = VirtualVariableRecode.Term.TermType.Between;
        virtualVariableRecode.Rules.First().Criteria.Last().Value = 1.0;
        virtualVariableRecode.Rules.First().Criteria.Last().MaxValue = 2.0;
        virtualVariableRecode.Rules.First().ResultNa = true;
        
        Assert.Equal("'[NA,1-2]=NA;else=NA'", virtualVariableRecode.RecodeInfo);
        
        virtualVariableRecode.AddRule();
        
        Assert.Equal("'[NA,1-2]=NA;[0,0]=0;else=NA'", virtualVariableRecode.RecodeInfo);

        virtualVariableRecode.Else = VirtualVariableRecode.ElseAction.Set;
        virtualVariableRecode.ElseValue = -1;
        
        Assert.Equal("'[NA,1-2]=NA;[0,0]=0;else=-1'", virtualVariableRecode.RecodeInfo);
        
        virtualVariableRecode.RemoveLastVariable();
        
        Assert.Equal("'NA=NA;0=0;else=-1'", virtualVariableRecode.RecodeInfo);
    }

    [Fact]
    public void TestIsChanged()
    {
        VirtualVariableRecode virtualVariableRecode = new();
        AssertChange(virtualVariableRecode);
        
        virtualVariableRecode.AddVariable(new Variable(1, "item1"));
        AssertChange(virtualVariableRecode);
        
        virtualVariableRecode.AddRule();
        AssertChange(virtualVariableRecode);
        
        virtualVariableRecode.AddVariable(new Variable(2, "item2"));
        AssertChange(virtualVariableRecode);
        
        virtualVariableRecode.Rules.First().Criteria.First().Type = VirtualVariableRecode.Term.TermType.Missing;
        AssertChange(virtualVariableRecode);
        
        virtualVariableRecode.Rules.First().Criteria.Last().Type = VirtualVariableRecode.Term.TermType.Between;
        AssertChange(virtualVariableRecode);
        
        virtualVariableRecode.Rules.First().Criteria.Last().Value = 1.0;
        AssertChange(virtualVariableRecode);
        
        virtualVariableRecode.Rules.First().Criteria.Last().MaxValue = 2.0;
        AssertChange(virtualVariableRecode);

        virtualVariableRecode.Rules.First().ResultValue = 10.0;
        AssertChange(virtualVariableRecode);

        virtualVariableRecode.Rules.First().Label = "high";
        AssertChange(virtualVariableRecode);
        
        virtualVariableRecode.Rules.First().ResultNa = true;
        Assert.Empty(virtualVariableRecode.Rules.First().Label);
        AssertChange(virtualVariableRecode);
        
        virtualVariableRecode.AddRule();
        AssertChange(virtualVariableRecode);
        
        virtualVariableRecode.Else = VirtualVariableRecode.ElseAction.Set;
        AssertChange(virtualVariableRecode);

        virtualVariableRecode.ElseValue = -1.0;
        AssertChange(virtualVariableRecode);

        virtualVariableRecode.ElseLabel = "N/A";
        AssertChange(virtualVariableRecode);
        
        virtualVariableRecode.RemoveLastVariable();
        AssertChange(virtualVariableRecode);
    }

    private static void AssertChange(VirtualVariableRecode virtualVariableRecode)
    {
        Assert.True(virtualVariableRecode.IsChanged);
        virtualVariableRecode.AcceptChanges();
        Assert.False(virtualVariableRecode.IsChanged);
    }
    
    [Fact]
    public void TestValidity()
    {
        VirtualVariableRecode virtualVariableRecode = new();
        
        Assert.False(virtualVariableRecode.ValidateDeep());

        virtualVariableRecode.Name = "newRecode";
        
        Assert.True(virtualVariableRecode.ValidateDeep());
        
        virtualVariableRecode.AddVariable(new Variable(1, "item1"));
        
        Assert.True(virtualVariableRecode.ValidateDeep());
        
        virtualVariableRecode.AddRule();
        
        Assert.True(virtualVariableRecode.ValidateDeep());
        
        virtualVariableRecode.AddVariable(new Variable(2, "item2"));
        
        Assert.True(virtualVariableRecode.ValidateDeep());
        
        virtualVariableRecode.Rules.First().Criteria.First().Type = VirtualVariableRecode.Term.TermType.Missing;
        virtualVariableRecode.Rules.First().Criteria.Last().Type = VirtualVariableRecode.Term.TermType.Between;
        
        Assert.True(virtualVariableRecode.ValidateDeep());
        
        virtualVariableRecode.Rules.First().Criteria.Last().Value = 1.0;
        virtualVariableRecode.Rules.First().Criteria.Last().MaxValue = 2.0;
        virtualVariableRecode.Rules.First().ResultNa = true;
        
        Assert.True(virtualVariableRecode.ValidateDeep());
        
        virtualVariableRecode.AddRule();
        
        Assert.True(virtualVariableRecode.ValidateDeep());
        
        virtualVariableRecode.RemoveLastVariable();
        
        Assert.True(virtualVariableRecode.ValidateDeep());

        var virtualVariableRecodeClone = (virtualVariableRecode.Clone() as VirtualVariableRecode)!;
        
        Assert.True(virtualVariableRecodeClone.ValidateDeep());
        
        virtualVariableRecodeClone.Rules.First().Criteria = [];
        
        Assert.False(virtualVariableRecodeClone.ValidateDeep());
        
        virtualVariableRecodeClone = (virtualVariableRecode.Clone() as VirtualVariableRecode)!;
        
        Assert.True(virtualVariableRecodeClone.ValidateDeep());
    }

    [Fact]
    public void TestInputVariableNamesExistIn()
    {
        VirtualVariableRecode virtualVariable = new()
        {
            Variables = [ new Variable(1, "x1"), new Variable(2, "X2") ]
        };
        
        Assert.True(virtualVariable.InputVariableNamesExistIn(new List<Variable> { new Variable(14, "X1"), new Variable(15, "X2") }));
        Assert.False(virtualVariable.InputVariableNamesExistIn(new List<Variable> { new Variable(2, "X2") }));
    }
}