using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;
using TestLSAnalyzer.Services;

namespace TestLSAnalyzer.ViewModels;

public class TestRequestAnalysis
{
    [Fact]
    public void TestMoveVariablesBackAndForth()
    {
        RequestAnalysis requestAnalysisViewModel = new()
        {
            AnalysisConfiguration = new()
            {
                DatasetType = new(),
                FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                ModeKeep = true,
            },
            AvailableVariables = new(GetCurrentVariables())
        };

        Assert.Equal(11, requestAnalysisViewModel.AvailableVariables.Count());

        requestAnalysisViewModel.MoveToAndFromAnalysisVariablesCommand.Execute(new MoveToAndFromVariablesCommandParameters()
        {
            SelectedFrom = requestAnalysisViewModel.AvailableVariables.Where(var => (var.Name == "x" || var.Name == "y")).ToList(),
            SelectedTo = new(),
        });

        Assert.Equal(9, requestAnalysisViewModel.AvailableVariables.Count());
        Assert.Equal(2, requestAnalysisViewModel.AnalysisVariables.Count());

        requestAnalysisViewModel.MoveToAndFromGroupByVariablesCommand.Execute(new MoveToAndFromVariablesCommandParameters()
        {
            SelectedFrom = requestAnalysisViewModel.AvailableVariables.Where(var => var.Name == "cat").ToList(),
            SelectedTo = new(),
        });

        Assert.Equal(8, requestAnalysisViewModel.AvailableVariables.Count());
        Assert.Single(requestAnalysisViewModel.GroupByVariables);

        requestAnalysisViewModel.MoveToAndFromAnalysisVariablesCommand.Execute(new MoveToAndFromVariablesCommandParameters()
        {
            SelectedFrom = new(),
            SelectedTo = requestAnalysisViewModel.AnalysisVariables.Where(var => var.Name == "x").ToList(),
        });

        Assert.Equal(9, requestAnalysisViewModel.AvailableVariables.Count());
        Assert.Single(requestAnalysisViewModel.AnalysisVariables);

        requestAnalysisViewModel.MoveToAndFromGroupByVariablesCommand.Execute(new MoveToAndFromVariablesCommandParameters()
        {
            SelectedFrom = requestAnalysisViewModel.AvailableVariables.Where(var => var.Name == "x").ToList(),
            SelectedTo = requestAnalysisViewModel.GroupByVariables.Where(var => var.Name == "cat").ToList(),
        });

        Assert.Equal(9, requestAnalysisViewModel.AvailableVariables.Count());
        Assert.Single(requestAnalysisViewModel.GroupByVariables);
        Assert.Empty(requestAnalysisViewModel.GroupByVariables.Where(var => var.Name == "cat"));
        Assert.Single(requestAnalysisViewModel.GroupByVariables.Where(var => var.Name == "x"));

        requestAnalysisViewModel.MoveToAndFromDependentVariablesCommand.Execute(new MoveToAndFromVariablesCommandParameters()
        {
            SelectedFrom = requestAnalysisViewModel.AvailableVariables.Where(var => var.Name == "cat").ToList(),
            SelectedTo = new(),
        });

        Assert.Equal(8, requestAnalysisViewModel.AvailableVariables.Count());
        Assert.Single(requestAnalysisViewModel.DependentVariables);

        requestAnalysisViewModel.MoveToAndFromDependentVariablesCommand.Execute(new MoveToAndFromVariablesCommandParameters()
        {
            SelectedFrom = new(),
            SelectedTo = requestAnalysisViewModel.DependentVariables.Where(var => var.Name == "cat").ToList(),
        });

        Assert.Equal(9, requestAnalysisViewModel.AvailableVariables.Count());
        Assert.Empty(requestAnalysisViewModel.DependentVariables);
    }

    [Fact]
    public void TestAddPercentile()
    {
        RequestAnalysis requestAnalysisViewModel = new()
        {
            AnalysisConfiguration = new()
            {
                DatasetType = new(),
                FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                ModeKeep = true,
            },
            AvailableVariables = new(GetCurrentVariables())
        };
        
        Assert.Equal(3, requestAnalysisViewModel.Percentiles.Count);
        Assert.False(requestAnalysisViewModel.HasErrors);
        
        requestAnalysisViewModel.NewPercentile = "abc";
        requestAnalysisViewModel.AddPercentileCommand.Execute(null);

        Assert.True(requestAnalysisViewModel.HasErrors);
        Assert.Contains("NewPercentile", requestAnalysisViewModel.Errors.Keys);
        
        requestAnalysisViewModel.NewPercentile = "0.500";
        requestAnalysisViewModel.AddPercentileCommand.Execute(null);
        
        Assert.False(requestAnalysisViewModel.HasErrors);
        Assert.Equal(3, requestAnalysisViewModel.Percentiles.Count);
        Assert.True(string.IsNullOrEmpty(requestAnalysisViewModel.NewPercentile));
        
        requestAnalysisViewModel.NewPercentile = "0.95";
        requestAnalysisViewModel.AddPercentileCommand.Execute(null);
        requestAnalysisViewModel.NewPercentile = "0.05";
        requestAnalysisViewModel.AddPercentileCommand.Execute(null);
        
        Assert.Equal(5, requestAnalysisViewModel.Percentiles.Count);
        Assert.True(requestAnalysisViewModel.Percentiles.Select(perc => perc.Value).SequenceEqual([0.05, 0.25, 0.50, 0.75, 0.95]));
    }
    
    [Fact]
    public void TestRemovePercentile()
    {
        RequestAnalysis requestAnalysisViewModel = new()
        {
            AnalysisConfiguration = new()
            {
                DatasetType = new(),
                FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                ModeKeep = true,
            },
            AvailableVariables = new(GetCurrentVariables())
        };
        
        // expect not to raise an exception
        requestAnalysisViewModel.RemovePercentileCommand.Execute(new PercentileWrapper { Value = -0.33 });
        
        Assert.Equal(3, requestAnalysisViewModel.Percentiles.Count);
        requestAnalysisViewModel.RemovePercentileCommand.Execute(requestAnalysisViewModel.Percentiles.First());
        Assert.Equal(2, requestAnalysisViewModel.Percentiles.Count);
    }

    [Fact]
    public void TestInitializeWithAnalysis()
    {
        RequestAnalysis requestAnalysisViewModel = new()
        {
            AnalysisConfiguration = new()
            {
                DatasetType = new(),
                FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                ModeKeep = true,
            },
            AvailableVariables = new(GetCurrentVariables())
        };

        AnalysisUnivar analysisUnivar = new(requestAnalysisViewModel.AnalysisConfiguration)
        {
            Vars = new() { new(1, "x", false), new(2, "y", false) },
            GroupBy = new() { new(3, "cat", false) },
            CalculateOverall = false,
        };

        requestAnalysisViewModel.InitializeWithAnalysis(analysisUnivar);
        Assert.Equal(8, requestAnalysisViewModel.AvailableVariables.Count);
        Assert.Equal(2, requestAnalysisViewModel.AnalysisVariables.Count);
        Assert.Equal("cat", requestAnalysisViewModel.GroupByVariables[0].Name);
        Assert.False(requestAnalysisViewModel.CalculateOverall);

        AnalysisMeanDiff analysisMeanDiff = new(requestAnalysisViewModel.AnalysisConfiguration)
        {
            Vars = new() { new(1, "x", false) },
            GroupBy = new() { new(2, "mi", true) },
            CalculateSeparately = true,
        };

        requestAnalysisViewModel.InitializeWithAnalysis(analysisMeanDiff);
        Assert.Equal(9, requestAnalysisViewModel.AvailableVariables.Count);
        Assert.Single(requestAnalysisViewModel.AnalysisVariables);
        Assert.Equal("mi", requestAnalysisViewModel.GroupByVariables[0].Name);
        Assert.True(requestAnalysisViewModel.CalculateSeparately);

        AnalysisFreq analysisFreq = new(requestAnalysisViewModel.AnalysisConfiguration)
        {
            Vars = new() { new(1, "cat", false) },
            GroupBy = new() { },
            CalculateOverall = true,
            CalculateCrosswise = false
        };

        requestAnalysisViewModel.InitializeWithAnalysis(analysisFreq);
        Assert.Equal(10, requestAnalysisViewModel.AvailableVariables.Count);
        Assert.Single(requestAnalysisViewModel.AnalysisVariables);
        Assert.Empty(requestAnalysisViewModel.GroupByVariables);
        Assert.True(requestAnalysisViewModel.CalculateOverall);
        Assert.False(requestAnalysisViewModel.CalculateCrosswise);

        AnalysisPercentiles analysisPercentiles = new(requestAnalysisViewModel.AnalysisConfiguration)
        {
            Percentiles = new() { 0.20, 0.40, 0.60, 0.80 },
            Vars = new() { new(1, "x", false), new(2, "y", false) },
            GroupBy = new() { new(3, "cat", false) },
            CalculateOverall = false,
            UseInterpolation = false,
        };

        requestAnalysisViewModel.InitializeWithAnalysis(analysisPercentiles);
        Assert.Equal(4, requestAnalysisViewModel.Percentiles.Count);
        Assert.Equal(8, requestAnalysisViewModel.AvailableVariables.Count);
        Assert.Equal(2, requestAnalysisViewModel.AnalysisVariables.Count);
        Assert.Equal("cat", requestAnalysisViewModel.GroupByVariables[0].Name);
        Assert.False(requestAnalysisViewModel.CalculateOverall);
        Assert.False(requestAnalysisViewModel.UseInterpolation);

        AnalysisCorr analysisCorr = new(requestAnalysisViewModel.AnalysisConfiguration)
        {
            Vars = new() { new(1, "x", false), new(2, "y", false) },
            GroupBy = new() { new(3, "cat", false) },
            CalculateOverall = false,
        };

        requestAnalysisViewModel.InitializeWithAnalysis(analysisCorr);
        Assert.Equal(8, requestAnalysisViewModel.AvailableVariables.Count);
        Assert.Equal(2, requestAnalysisViewModel.AnalysisVariables.Count);
        Assert.Equal("cat", requestAnalysisViewModel.GroupByVariables[0].Name);
        Assert.False(requestAnalysisViewModel.CalculateOverall);

        AnalysisLinreg analysisLinreg = new(requestAnalysisViewModel.AnalysisConfiguration)
        {
            Dependent = new(2, "y", false),
            Vars = new() { new(1, "x", false) },
            GroupBy = new() { new(3, "cat", false) },
            CalculateOverall = false,
            CalculateCrosswise = false,
            Sequence = AnalysisRegression.RegressionSequence.Forward,
        };

        requestAnalysisViewModel.InitializeWithAnalysis(analysisLinreg);
        Assert.Equal(8, requestAnalysisViewModel.AvailableVariables.Count);
        Assert.Single(requestAnalysisViewModel.DependentVariables);
        Assert.Single(requestAnalysisViewModel.AnalysisVariables);
        Assert.Equal("cat", requestAnalysisViewModel.GroupByVariables[0].Name);
        Assert.False(requestAnalysisViewModel.CalculateOverall);
        Assert.False(requestAnalysisViewModel.CalculateCrosswise);
        Assert.Equal(AnalysisRegression.RegressionSequence.Forward, requestAnalysisViewModel.RegressionSequence);

        AnalysisLinreg analysisLogistReg = new(requestAnalysisViewModel.AnalysisConfiguration)
        {
            Dependent = new(1, "cat", false),
            Vars = new() { new(2, "x", false), new(3, "y", false) },
            CalculateOverall = false,
            Sequence = AnalysisRegression.RegressionSequence.Backward,
        };

        requestAnalysisViewModel.InitializeWithAnalysis(analysisLogistReg);
        Assert.Equal(8, requestAnalysisViewModel.AvailableVariables.Count);
        Assert.Single(requestAnalysisViewModel.DependentVariables);
        Assert.Equal(2, requestAnalysisViewModel.AnalysisVariables.Count);
        Assert.Empty(requestAnalysisViewModel.GroupByVariables);
        Assert.False(requestAnalysisViewModel.CalculateOverall);
        Assert.True(requestAnalysisViewModel.CalculateCrosswise);
        Assert.Equal(AnalysisRegression.RegressionSequence.Backward, requestAnalysisViewModel.RegressionSequence);
    }

    [Fact]
    public void TestResetAnalysis()
    {
        RequestAnalysis requestAnalysisViewModel = new()
        {
            AnalysisConfiguration = new()
            {
                DatasetType = new(),
                FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                ModeKeep = true,
            },
            AvailableVariables = new(GetCurrentVariables())
        };

        AnalysisPercentiles analysisPercentiles = new(requestAnalysisViewModel.AnalysisConfiguration)
        {
            Percentiles = new() { 0.20, 0.40, 0.60, 0.80 },
            Vars = new() { new(1, "x", false), new(2, "y", false) },
            GroupBy = new() { new(3, "cat", false) },
            CalculateOverall = false,
            CalculateCrosswise = false,
            UseInterpolation = false,
        };

        requestAnalysisViewModel.InitializeWithAnalysis(analysisPercentiles);
        Assert.Equal(4, requestAnalysisViewModel.Percentiles.Count);
        Assert.Equal(8, requestAnalysisViewModel.AvailableVariables.Count);
        Assert.Equal(2, requestAnalysisViewModel.AnalysisVariables.Count);
        Assert.Equal("cat", requestAnalysisViewModel.GroupByVariables[0].Name);
        Assert.False(requestAnalysisViewModel.CalculateOverall);
        Assert.False(requestAnalysisViewModel.CalculateCrosswise);
        Assert.False(requestAnalysisViewModel.UseInterpolation);

        requestAnalysisViewModel.ResetAnalysisRequestCommand.Execute(null);
        Assert.Equal(3, requestAnalysisViewModel.Percentiles.Count);
        Assert.Equal(11, requestAnalysisViewModel.AvailableVariables.Count);
        Assert.Empty(requestAnalysisViewModel.AnalysisVariables);
        Assert.Empty(requestAnalysisViewModel.GroupByVariables);
        Assert.True(requestAnalysisViewModel.CalculateOverall);
        Assert.True(requestAnalysisViewModel.CalculateCrosswise);
        Assert.True(requestAnalysisViewModel.UseInterpolation);

        requestAnalysisViewModel.RegressionSequence = AnalysisRegression.RegressionSequence.Forward;
        requestAnalysisViewModel.ResetAnalysisRequestCommand.Execute(null);
        Assert.Equal(AnalysisRegression.RegressionSequence.AllIn, requestAnalysisViewModel.RegressionSequence);
    }

    [Fact]
    public void TestRequestAnalysisSendsMessage()
    {
        DatasetType datasetType = new()
        {
            Weight = "wgt"
        };
        var fileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav");

        RequestAnalysis requestAnalysisViewModel = new()
        {
            AnalysisConfiguration = new()
            {
                DatasetType = datasetType,
                FileName = fileName,
                ModeKeep = true,
            },
            AvailableVariables = new(GetCurrentVariables())
        };

        requestAnalysisViewModel.MoveToAndFromAnalysisVariablesCommand.Execute(new MoveToAndFromVariablesCommandParameters()
        {
            SelectedFrom = requestAnalysisViewModel.AvailableVariables.Where(var => (var.Name == "x" || var.Name == "y")).ToList(),
            SelectedTo = new(),
        });

        bool messageSent = false;
        Analysis? requestedAnalysis = null;
        WeakReferenceMessenger.Default.Register<RequestAnalysisMessage>(this, (r, m) =>
        {
            messageSent = true;
            requestedAnalysis = m.Value as Analysis;
        });

        requestAnalysisViewModel.SendAnalysisRequestCommand.Execute(new MockRequestingAnalysisUnivar());

        Assert.True(messageSent);
        Assert.NotNull(requestedAnalysis);
        Assert.Equal(2, requestedAnalysis.Vars.Count);
        Assert.Empty(requestedAnalysis.GroupBy);
        Assert.Equal("wgt", requestedAnalysis.AnalysisConfiguration.DatasetType?.Weight);
        Assert.Equal(fileName, requestedAnalysis.AnalysisConfiguration.FileName);

        messageSent = false;
        requestAnalysisViewModel.SendAnalysisRequestCommand.Execute(new MockRequestingAnalysisMeanDiff());
        Assert.True(messageSent);

        messageSent = false;
        requestAnalysisViewModel.SendAnalysisRequestCommand.Execute(new MockRequestingAnalysisFreq());
        Assert.True(messageSent);

        messageSent = false;
        requestAnalysisViewModel.SendAnalysisRequestCommand.Execute(new MockRequestingAnalysisPercentiles());
        Assert.True(messageSent);

        messageSent = false;
        requestAnalysisViewModel.SendAnalysisRequestCommand.Execute(new MockRequestingAnalysisCorr());
        Assert.True(messageSent);

        messageSent = false;
        requestAnalysisViewModel.SendAnalysisRequestCommand.Execute(new MockRequestingAnalysisLinreg());
        Assert.False(messageSent);

        requestAnalysisViewModel.MoveToAndFromDependentVariablesCommand.Execute(new MoveToAndFromVariablesCommandParameters()
        {
            SelectedFrom = requestAnalysisViewModel.AvailableVariables.Where(var => var.Name == "cat").ToList(),
            SelectedTo = new(),
        });
        requestAnalysisViewModel.SendAnalysisRequestCommand.Execute(new MockRequestingAnalysisLinreg());
        Assert.True(messageSent);

        messageSent = false;
        requestAnalysisViewModel.SendAnalysisRequestCommand.Execute(new MockRequestingAnalysisLogistReg());
        Assert.True(messageSent);
    }

    private List<Variable> GetCurrentVariables()
    {
        return
        [
            new Variable(1, "mi", true),
            new Variable(2, "x", false),
            new Variable(3, "y", false),
            new Variable(4, "cat", false),
            new Variable(5, "wgt", true),
            new Variable(6, "repwgt1", true),
            new Variable(7, "repwgt2", true),
            new Variable(8, "repwgt3", true),
            new Variable(9, "repwgt4", true),
            new Variable(10, "repwgt5", true),
            new Variable(11, "one", false)
        ];
    }
}

internal class MockRequestingAnalysisUnivar : IRequestingAnalysis
{
    public void Close()
    {
        
    }

    public Type GetAnalysisType()
    {
        return Type.GetType("LSAnalyzer.Models.AnalysisUnivar,LSAnalyzer")!;
    }
}

internal class MockRequestingAnalysisMeanDiff : IRequestingAnalysis
{
    public void Close()
    {

    }

    public Type GetAnalysisType()
    {
        return Type.GetType("LSAnalyzer.Models.AnalysisMeanDiff,LSAnalyzer")!;
    }
}

internal class MockRequestingAnalysisFreq : IRequestingAnalysis
{
    public void Close()
    {

    }

    public Type GetAnalysisType()
    {
        return Type.GetType("LSAnalyzer.Models.AnalysisFreq,LSAnalyzer")!;
    }
}

internal class MockRequestingAnalysisPercentiles : IRequestingAnalysis
{
    public void Close()
    {

    }

    public Type GetAnalysisType()
    {
        return Type.GetType("LSAnalyzer.Models.AnalysisPercentiles,LSAnalyzer")!;
    }
}

internal class MockRequestingAnalysisCorr : IRequestingAnalysis
{
    public void Close()
    {

    }

    public Type GetAnalysisType()
    {
        return Type.GetType("LSAnalyzer.Models.AnalysisCorr,LSAnalyzer")!;
    }
}

internal class MockRequestingAnalysisLinreg : IRequestingAnalysis
{
    public void Close()
    {

    }

    public Type GetAnalysisType()
    {
        return Type.GetType("LSAnalyzer.Models.AnalysisLinreg,LSAnalyzer")!;
    }
}

internal class MockRequestingAnalysisLogistReg : IRequestingAnalysis
{
    public void Close()
    {

    }

    public Type GetAnalysisType()
    {
        return Type.GetType("LSAnalyzer.Models.AnalysisLogistReg,LSAnalyzer")!;
    }
}
