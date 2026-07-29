using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;
using LSAnalyzer.ViewModels.VirtualVariableCreation;

namespace TestLSAnalyzer.ViewModels.VirtualVariableCreation;

public class TestMassRecoding
{
    [Fact]
    public void TestCanCreate()
    {
        MassRecoding massRecoding = new();
        massRecoding.Recodings.Clear();
        Assert.False(massRecoding.CanCreate);
        
        massRecoding.Recodings.Add(new MassRecoding.Recoding
        {
            Parent = massRecoding,
            InputVariable = new Variable(1, "X1"),
        });
        Assert.False(massRecoding.CanCreate);

        massRecoding.Recodings.Last().OutputVariableLabel = "Label Y1";
        Assert.False(massRecoding.CanCreate);
        
        massRecoding.Recodings.Last().OutputVariableName = "Y2";
        Assert.True(massRecoding.CanCreate);
        
        massRecoding.Recodings.Add(new MassRecoding.Recoding
        {
            Parent = massRecoding,
            InputVariable = new Variable(2, "X2"),
        });
        Assert.False(massRecoding.CanCreate);
        
        massRecoding.Recodings.Remove(massRecoding.Recodings.Last());
        Assert.True(massRecoding.CanCreate);
    }
    
    [Fact]
    public void TestHandleAvailableVariables()
    {
        VirtualVariables virtualVariables = new()
        {
            AvailableVariables = [ new Variable(1, "X1"), new Variable(2, "X2"), new Variable(3, "X3"), new Variable(4, "X4") ],
            SelectedVirtualVariable = new VirtualVariableRecode
            {
                Variables = [ new Variable(4, "X4") ],
                Rules = [
                    new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.AtMost, MaxValue = 500, } ], ResultValue = 0 },
                    new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.AtLeast, Value = 500, } ], ResultValue = 1 },
                ],
                Else = VirtualVariableRecode.ElseAction.Missing
            }
        };

        MassRecoding massRecoding = new(virtualVariables);
        Assert.Empty(massRecoding.Recodings);
        Assert.Equal(3, massRecoding.AvailableVariables.Count);
        
        massRecoding.HandleAvailableVariablesCommand.Execute([
            new Variable(2, "X2"),
            new Variable(3, "X3"),
        ]);
        Assert.Equal(2, massRecoding.Recodings.Count);
        Assert.Single(massRecoding.AvailableVariables);
    }

    [Fact]
    public void TestRemoveRecoding()
    {
        VirtualVariables virtualVariables = new()
        {
            AvailableVariables = [ new Variable(1, "X1"), new Variable(2, "X2"), new Variable(3, "X3"), new Variable(4, "X4") ],
            SelectedVirtualVariable = new VirtualVariableRecode
            {
                Variables = [ new Variable(4, "X4") ],
                Rules = [
                    new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.AtMost, MaxValue = 500, } ], ResultValue = 0 },
                    new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.AtLeast, Value = 500, } ], ResultValue = 1 },
                ],
                Else = VirtualVariableRecode.ElseAction.Missing
            }
        };
        
        MassRecoding massRecoding = new(virtualVariables);
        massRecoding.HandleAvailableVariablesCommand.Execute([
            new Variable(2, "X1"),
            new Variable(3, "X3"),
        ]);
        Assert.Equal(2, massRecoding.Recodings.Count);
        Assert.Single(massRecoding.AvailableVariables);
        
        massRecoding.RemoveRecodingCommand.Execute(massRecoding.Recodings.First());
        Assert.Single(massRecoding.Recodings);
        Assert.Equal(2, massRecoding.AvailableVariables.Count);
        Assert.Equal(["X1", "X2"], massRecoding.AvailableVariables.Select(v => v.Name).ToList());
    }

    [Fact]
    public void TestCreateRecodings()
    {
        VirtualVariables virtualVariables = new()
        {
            AvailableVariables = [ new Variable(1, "X1"), new Variable(2, "X2"), new Variable(3, "X3"), new Variable(4, "X4"), new Variable(5, "existing") ],
            SelectedVirtualVariable = new VirtualVariableRecode
            {
                Variables = [ new Variable(4, "X4") ],
                Rules = [
                    new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.AtMost, MaxValue = 500, } ], ResultValue = 0 },
                    new VirtualVariableRecode.Rule { Criteria = [ new VirtualVariableRecode.Term { VariableIndex = 0, Type = VirtualVariableRecode.Term.TermType.AtLeast, Value = 500, } ], ResultValue = 1 },
                ],
                Else = VirtualVariableRecode.ElseAction.Missing
            }
        };
        virtualVariables.CurrentVirtualVariables = [virtualVariables.SelectedVirtualVariable];
        
        MassRecoding massRecoding = new(virtualVariables);
        massRecoding.HandleAvailableVariablesCommand.Execute([
            new Variable(2, "X1"),
            new Variable(3, "X3"),
        ]);
        Assert.Equal(2, massRecoding.Recodings.Count);
        Assert.False(massRecoding.CanCreate);
        Assert.DoesNotContain(massRecoding.Recodings, r => r.HasErrors);
        
        massRecoding.Recodings[0].OutputVariableName = "1";
        massRecoding.Recodings[1].OutputVariableName = "2";
        Assert.True(massRecoding.CanCreate);

        massRecoding.CreateRecodingsCommand.Execute(null);
        Assert.Contains(massRecoding.Recodings, r => r.HasErrors);

        var variableNameExistsMessageSent = false;
        WeakReferenceMessenger.Default.Register<MassRecoding.VariableNameExistsMessage>(this, (_, _) =>
        {
            variableNameExistsMessageSent = true;
        });
        
        massRecoding.Recodings[0].OutputVariableName = "recoded1";
        massRecoding.Recodings[1].OutputVariableName = "existing";
        
        massRecoding.CreateRecodingsCommand.Execute(null);
        Assert.DoesNotContain(massRecoding.Recodings, r => r.HasErrors);
        Assert.True(variableNameExistsMessageSent);
        
        var variableNameDuplicatedMessageSent = false;
        WeakReferenceMessenger.Default.Register<MassRecoding.VariableNameDuplicatedMessage>(this, (_, _) =>
        {
            variableNameDuplicatedMessageSent = true;
        });
        
        massRecoding.Recodings[1].OutputVariableName = "recoded1";
        
        massRecoding.CreateRecodingsCommand.Execute(null);
        Assert.True(variableNameDuplicatedMessageSent);
        
        massRecoding.Recodings[1].OutputVariableName = "recoded2";
        
        massRecoding.CreateRecodingsCommand.Execute(null);
        
        Assert.Equal(3, virtualVariables.CurrentVirtualVariables.Count);
        Assert.Equal("recode(X1, '≤500=0;≥500=1;else=NA')", virtualVariables.CurrentVirtualVariables[1].Info);
    }
}