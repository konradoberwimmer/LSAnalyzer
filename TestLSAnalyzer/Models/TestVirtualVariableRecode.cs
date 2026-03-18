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
        
        Assert.Equal(VirtualVariableRecode.ElseAction.SetNa, virtualVariableRecode.Else);
        Assert.False(virtualVariableRecode.ElseCopyMakesSense);
        
        virtualVariableRecode.AddRule();
        
        Assert.Equal(2, virtualVariableRecode.Rules.First().Criteria.Count);
        Assert.Equal([0, 1], virtualVariableRecode.Rules.First().Criteria.Select(crit => crit.VariableIndex));
        
        virtualVariableRecode.RemoveLastVariable();
        
        Assert.Single(virtualVariableRecode.Rules.First().Criteria);
        Assert.Equal([0], virtualVariableRecode.Rules.First().Criteria.Select(crit => crit.VariableIndex));
        
        virtualVariableRecode.AddVariable(new Variable(3, "item3"));
        
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
        
        Assert.Equal(VirtualVariableRecode.ElseAction.SetNa, virtualVariableRecodeDeserialized.Else);
        Assert.False(virtualVariableRecodeDeserialized.ElseCopyMakesSense);
        
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

        virtualVariableRecode.Rules.First().Criteria.First().Type = VirtualVariableRecode.Term.TermType.IsNa;
        virtualVariableRecode.Rules.First().Criteria.Last().Type = VirtualVariableRecode.Term.TermType.IsBetween;
        virtualVariableRecode.Rules.First().Criteria.Last().Value = 1.0;
        virtualVariableRecode.Rules.First().Criteria.Last().MaxValue = 2.0;
        virtualVariableRecode.Rules.First().ResultNa = true;
        
        Assert.Equal("recode([item1,item2], '[NA,1-2]=NA;else=NA')", virtualVariableRecode.Info);
        
        virtualVariableRecode.AddRule();
        
        Assert.Equal("recode([item1,item2], '[NA,1-2]=NA;[0,0]=0;else=NA')", virtualVariableRecode.Info);
        
        virtualVariableRecode.RemoveLastVariable();
        
        Assert.Equal("recode(item1, 'NA=NA;0=0;else=NA')", virtualVariableRecode.Info);
    }

    [Fact]
    public void TestIsChanged()
    {
        VirtualVariableRecode virtualVariableRecode = new();
        
        Assert.True(virtualVariableRecode.IsChanged);
        virtualVariableRecode.AcceptChanges();
        Assert.False(virtualVariableRecode.IsChanged);
        
        virtualVariableRecode.AddVariable(new Variable(1, "item1"));
        
        Assert.True(virtualVariableRecode.IsChanged);
        virtualVariableRecode.AcceptChanges();
        Assert.False(virtualVariableRecode.IsChanged);
        
        virtualVariableRecode.AddRule();
        
        Assert.True(virtualVariableRecode.IsChanged);
        virtualVariableRecode.AcceptChanges();
        Assert.False(virtualVariableRecode.IsChanged);
        
        virtualVariableRecode.AddVariable(new Variable(2, "item2"));
        
        Assert.True(virtualVariableRecode.IsChanged);
        virtualVariableRecode.AcceptChanges();
        Assert.False(virtualVariableRecode.IsChanged);
        
        virtualVariableRecode.Rules.First().Criteria.First().Type = VirtualVariableRecode.Term.TermType.IsNa;
        
        Assert.True(virtualVariableRecode.IsChanged);
        virtualVariableRecode.AcceptChanges();
        Assert.False(virtualVariableRecode.IsChanged);
        
        virtualVariableRecode.Rules.First().Criteria.Last().Type = VirtualVariableRecode.Term.TermType.IsBetween;
        
        Assert.True(virtualVariableRecode.IsChanged);
        virtualVariableRecode.AcceptChanges();
        Assert.False(virtualVariableRecode.IsChanged);
        
        virtualVariableRecode.Rules.First().Criteria.Last().Value = 1.0;
        
        Assert.True(virtualVariableRecode.IsChanged);
        virtualVariableRecode.AcceptChanges();
        Assert.False(virtualVariableRecode.IsChanged);
        
        virtualVariableRecode.Rules.First().Criteria.Last().MaxValue = 2.0;
        
        Assert.True(virtualVariableRecode.IsChanged);
        virtualVariableRecode.AcceptChanges();
        Assert.False(virtualVariableRecode.IsChanged);
        
        virtualVariableRecode.Rules.First().ResultNa = true;
        
        Assert.True(virtualVariableRecode.IsChanged);
        virtualVariableRecode.AcceptChanges();
        Assert.False(virtualVariableRecode.IsChanged);
        
        virtualVariableRecode.AddRule();
        
        Assert.True(virtualVariableRecode.IsChanged);
        virtualVariableRecode.AcceptChanges();
        Assert.False(virtualVariableRecode.IsChanged);
        
        virtualVariableRecode.RemoveLastVariable();
        
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
        
        virtualVariableRecode.Rules.First().Criteria.First().Type = VirtualVariableRecode.Term.TermType.IsNa;
        virtualVariableRecode.Rules.First().Criteria.Last().Type = VirtualVariableRecode.Term.TermType.IsBetween;
        
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
}