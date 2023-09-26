using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Helper;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestLSAnalyzer.Services;

namespace TestLSAnalyzer.ViewModels
{
    public class TestRequestAnalysis
    {
        [Fact]
        public void TestConstructorAndMoveVariablesBackAndForth()
        {
            MockRserviceForTestRequestAnalysis rservice = new();
            RequestAnalysis requestAnalysisViewModel = new(rservice);

            requestAnalysisViewModel.AnalysisConfiguration = new()
            {
                DatasetType = new(),
                FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                ModeKeep = true,
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
        public void TestInitializeWithAnalysis()
        {
            MockRserviceForTestRequestAnalysis rservice = new();
            RequestAnalysis requestAnalysisViewModel = new(rservice);

            requestAnalysisViewModel.AnalysisConfiguration = new()
            {
                DatasetType = new(),
                FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                ModeKeep = true,
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
            };

            requestAnalysisViewModel.InitializeWithAnalysis(analysisFreq);
            Assert.Equal(10, requestAnalysisViewModel.AvailableVariables.Count);
            Assert.Single(requestAnalysisViewModel.AnalysisVariables);
            Assert.Empty(requestAnalysisViewModel.GroupByVariables);
            Assert.True(requestAnalysisViewModel.CalculateOverall);

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
                Sequence = AnalysisRegression.RegressionSequence.Forward,
            };

            requestAnalysisViewModel.InitializeWithAnalysis(analysisLinreg);
            Assert.Equal(8, requestAnalysisViewModel.AvailableVariables.Count);
            Assert.Single(requestAnalysisViewModel.DependentVariables);
            Assert.Single(requestAnalysisViewModel.AnalysisVariables);
            Assert.Equal("cat", requestAnalysisViewModel.GroupByVariables[0].Name);
            Assert.False(requestAnalysisViewModel.CalculateOverall);
            Assert.Equal(AnalysisRegression.RegressionSequence.Forward, requestAnalysisViewModel.RegressionSequence);
        }

        [Fact]
        public void TestResetAnalysis()
        {
            MockRserviceForTestRequestAnalysis rservice = new();
            RequestAnalysis requestAnalysisViewModel = new(rservice);

            requestAnalysisViewModel.AnalysisConfiguration = new()
            {
                DatasetType = new(),
                FileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                ModeKeep = true,
            };

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

            requestAnalysisViewModel.ResetAnalysisRequestCommand.Execute(null);
            Assert.Equal(3, requestAnalysisViewModel.Percentiles.Count);
            Assert.Equal(11, requestAnalysisViewModel.AvailableVariables.Count);
            Assert.Empty(requestAnalysisViewModel.AnalysisVariables);
            Assert.Empty(requestAnalysisViewModel.GroupByVariables);
            Assert.True(requestAnalysisViewModel.CalculateOverall);
            Assert.True(requestAnalysisViewModel.UseInterpolation);

            requestAnalysisViewModel.RegressionSequence = AnalysisRegression.RegressionSequence.Forward;
            requestAnalysisViewModel.ResetAnalysisRequestCommand.Execute(null);
            Assert.Equal(AnalysisRegression.RegressionSequence.AllIn, requestAnalysisViewModel.RegressionSequence);
        }

        [Fact]
        public void TestRequestAnalysisSendsMessage()
        {
            MockRserviceForTestRequestAnalysis rservice = new();
            RequestAnalysis requestAnalysisViewModel = new(rservice);

            DatasetType datasetType = new()
            {
                Weight = "wgt"
            };
            var fileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav");
            
            requestAnalysisViewModel.AnalysisConfiguration = new()
            {
                DatasetType = datasetType,
                FileName = fileName,
                ModeKeep = true,
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
        }
    }

    internal class MockRserviceForTestRequestAnalysis : Rservice
    {
        public override List<Variable>? GetCurrentDatasetVariables(AnalysisConfiguration analysisConfiguration)
        {
            return new()
            {
                new(1, "mi", true),
                new(2, "x", false),
                new(3, "y", false),
                new(4, "cat", false),
                new(5, "wgt", true),
                new(6, "repwgt1", true),
                new(7, "repwgt2", true),
                new(8, "repwgt3", true),
                new(9, "repwgt4", true),
                new(10, "repwgt5", true),
                new(11, "one", false),
            };
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
}
