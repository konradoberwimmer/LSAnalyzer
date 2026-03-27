using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using LSAnalyzer.ViewModels.VirtualVariableCreation;
using Moq;

namespace TestLSAnalyzer.ViewModels.VirtualVariableCreation;

public class TestDichotomization
{
    [Fact]
    public void TestDichotomizationRejectsContinuousVariable()
    {
        Variable itemA = new(1, "itemA");
        Variable income = new(2, "income");
        
        var rservice = new Mock<IRservice>();
        rservice.Setup(service => service.GetDistinctValues(It.Is<Variable>(v => v.Name == itemA.Name), It.IsAny<List<PlausibleValueVariable>>())).Returns([ 1.0, 2.0, 3.0, 4.0 ]);
        rservice.Setup(service => service.GetDistinctValues(It.Is<Variable>(v => v.Name == income.Name), It.IsAny<List<PlausibleValueVariable>>())).Returns(Enumerable.Range(1, 20).Select(_ => new Random().NextDouble()).ToList());

        VirtualVariables virtualVariables = new VirtualVariables(new Mock<Configuration>().Object, rservice.Object)
        {
            CurrentFileName = "testfile.csv",
            AvailableVariables = [ itemA, income ],
        };
        
        Dichotomization dichotomization = new Dichotomization(virtualVariables);
        Assert.Equal(2, dichotomization.Variables.Count);
        
        dichotomization.SelectedVariable = dichotomization.Variables[0];
        Assert.Equal([ 1.0, 2.0, 3.0, 4.0 ], dichotomization.Categories);
        dichotomization.SelectedCategory = dichotomization.Categories[0];
        
        var messageSent = false;
        WeakReferenceMessenger.Default.Register<Dichotomization.RejectsContinuousVariable>(this, (_, _)  => messageSent = true);
        
        dichotomization.SelectedVariable = dichotomization.Variables[1];
        
        Assert.True(messageSent);
        Assert.Empty(dichotomization.Categories);
        Assert.Equal(0.0, dichotomization.SelectedCategory);
    }
    
    [Fact]
    public void TestCreateDichotomization()
    {
        Variable itemA = new(1, "itemA");
        Variable income = new(2, "income");
        
        var rservice = new Mock<IRservice>();
        rservice.Setup(service => service.GetDistinctValues(It.Is<Variable>(v => v.Name == itemA.Name), It.IsAny<List<PlausibleValueVariable>>())).Returns([ 1.0, 2.0, 3.0, 4.0 ]);
        rservice.Setup(service => service.GetDistinctValues(It.Is<Variable>(v => v.Name == income.Name), It.IsAny<List<PlausibleValueVariable>>())).Returns(Enumerable.Range(1, 20).Select(_ => new Random().NextDouble()).ToList());

        VirtualVariables virtualVariables = new VirtualVariables(new Mock<Configuration>().Object, rservice.Object)
        {
            CurrentFileName = "testfile.csv",
            AvailableVariables = [ itemA, income ],
        };
        
        Dichotomization dichotomization = new Dichotomization(virtualVariables);
        Assert.Equal(2, dichotomization.Variables.Count);
        
        dichotomization.SelectedVariable = dichotomization.Variables[0];
        Assert.Equal([ 1.0, 2.0, 3.0, 4.0 ], dichotomization.Categories);
        Assert.Equal(1.0, dichotomization.SelectedCategory);
        Assert.False(dichotomization.HasErrors);
        
        dichotomization.CreateDichotomizationCommand.Execute(null);
        
        Assert.True(dichotomization.HasErrors);
        Assert.Empty(virtualVariables.CurrentVirtualVariables);
        
        dichotomization.Prefix = "myDichotomization";
        dichotomization.NewLabel = "itemA";
        
        dichotomization.CreateDichotomizationCommand.Execute(null);
        
        Assert.False(dichotomization.HasErrors);
        Assert.Equal(3, virtualVariables.CurrentVirtualVariables.Count);
        Assert.Equal("myDichotomization_c2", virtualVariables.CurrentVirtualVariables[0].Name);
        Assert.Equal("itemA - Category 2", virtualVariables.CurrentVirtualVariables[0].Label);
        Assert.Equal("testfile.csv", virtualVariables.CurrentVirtualVariables[0].ForFileName);
        Assert.False(virtualVariables.CurrentVirtualVariables[0].IsChanged);
        Assert.Equal("recode(itemA, '1=0;2=1;3=0;4=0;else=NA')", virtualVariables.CurrentVirtualVariables[0].Info);
        Assert.Equal("myDichotomization_c3", virtualVariables.CurrentVirtualVariables[1].Name);
        Assert.Equal("recode(itemA, '1=0;2=0;3=1;4=0;else=NA')", virtualVariables.CurrentVirtualVariables[1].Info);
        Assert.Equal("myDichotomization_c4", virtualVariables.CurrentVirtualVariables[2].Name);
        Assert.Equal("recode(itemA, '1=0;2=0;3=0;4=1;else=NA')", virtualVariables.CurrentVirtualVariables[2].Info);
        
        virtualVariables.CurrentVirtualVariables.Clear();
        dichotomization.ReferenceCategory = Dichotomization.ReferenceCategoryType.Select;
        dichotomization.SelectedCategory = 3.0;
        
        dichotomization.CreateDichotomizationCommand.Execute(null);
        
        Assert.True(dichotomization.CategoriesMakesSense);
        Assert.False(dichotomization.HasErrors);
        Assert.Equal(3, virtualVariables.CurrentVirtualVariables.Count);
        Assert.Equal("myDichotomization_c1", virtualVariables.CurrentVirtualVariables[0].Name);
        Assert.Equal("itemA - Category 1", virtualVariables.CurrentVirtualVariables[0].Label);
        Assert.Equal("testfile.csv", virtualVariables.CurrentVirtualVariables[0].ForFileName);
        Assert.False(virtualVariables.CurrentVirtualVariables[0].IsChanged);
        Assert.Equal("recode(itemA, '1=1;2=0;3=0;4=0;else=NA')", virtualVariables.CurrentVirtualVariables[0].Info);
        Assert.Equal("myDichotomization_c2", virtualVariables.CurrentVirtualVariables[1].Name);
        Assert.Equal("recode(itemA, '1=0;2=1;3=0;4=0;else=NA')", virtualVariables.CurrentVirtualVariables[1].Info);
        Assert.Equal("myDichotomization_c4", virtualVariables.CurrentVirtualVariables[2].Name);
        Assert.Equal("recode(itemA, '1=0;2=0;3=0;4=1;else=NA')", virtualVariables.CurrentVirtualVariables[2].Info);
        
        virtualVariables.CurrentVirtualVariables.Clear();
        dichotomization.ReferenceCategory = Dichotomization.ReferenceCategoryType.None;
        
        dichotomization.CreateDichotomizationCommand.Execute(null);
        
        Assert.False(dichotomization.CategoriesMakesSense);
        Assert.False(dichotomization.HasErrors);
        Assert.Equal(4, virtualVariables.CurrentVirtualVariables.Count);
        Assert.Equal("myDichotomization_c1", virtualVariables.CurrentVirtualVariables[0].Name);
        Assert.Equal("itemA - Category 1", virtualVariables.CurrentVirtualVariables[0].Label);
        Assert.Equal("testfile.csv", virtualVariables.CurrentVirtualVariables[0].ForFileName);
        Assert.False(virtualVariables.CurrentVirtualVariables[0].IsChanged);
        Assert.Equal("recode(itemA, '1=1;2=0;3=0;4=0;else=NA')", virtualVariables.CurrentVirtualVariables[0].Info);
        Assert.Equal("myDichotomization_c2", virtualVariables.CurrentVirtualVariables[1].Name);
        Assert.Equal("recode(itemA, '1=0;2=1;3=0;4=0;else=NA')", virtualVariables.CurrentVirtualVariables[1].Info);
        Assert.Equal("myDichotomization_c3", virtualVariables.CurrentVirtualVariables[2].Name);
        Assert.Equal("recode(itemA, '1=0;2=0;3=1;4=0;else=NA')", virtualVariables.CurrentVirtualVariables[2].Info);
        Assert.Equal("myDichotomization_c4", virtualVariables.CurrentVirtualVariables[3].Name);
        Assert.Equal("recode(itemA, '1=0;2=0;3=0;4=1;else=NA')", virtualVariables.CurrentVirtualVariables[3].Info);
    }
}