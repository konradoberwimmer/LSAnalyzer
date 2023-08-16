using CommunityToolkit.Mvvm.Messaging;
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
    [Collection("Sequential")]
    public class TestRequestAnalysis
    {
        [Fact]
        public void TestConstructorAndMoveVariablesBackAndForth()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.CheckNecessaryRPackages(), "R packages are necessary for this test");

            DatasetType datasetType = new()
            {
                Id = 999991,
                Name = "Test with NMI 10 and NREP 5",
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
                Nrep = 5,
                RepWgts = "repwgt",
                FayFac = 1,
            };
            var fileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(fileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 1));

            RequestAnalysis requestAnalysisViewModel = new(rservice);
            requestAnalysisViewModel.AnalysisConfiguration = new()
            {
                DatasetType = datasetType,
                FileName = fileName,
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
            Assert.Single(requestAnalysisViewModel.GrouyByVariables);

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
                SelectedTo = requestAnalysisViewModel.GrouyByVariables.Where(var => var.Name == "cat").ToList(),
            });

            Assert.Equal(9, requestAnalysisViewModel.AvailableVariables.Count());
            Assert.Single(requestAnalysisViewModel.GrouyByVariables);
            Assert.Empty(requestAnalysisViewModel.GrouyByVariables.Where(var => var.Name == "cat"));
            Assert.Single(requestAnalysisViewModel.GrouyByVariables.Where(var => var.Name == "x"));
        }

        [Fact]
        public void TestRequestAnalysisSendsMessage()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.CheckNecessaryRPackages(), "R packages are necessary for this test");

            DatasetType datasetType = new()
            {
                Id = 999991,
                Name = "Test with NMI 10 and NREP 5",
                Weight = "wgt",
                NMI = 10,
                MIvar = "mi",
                Nrep = 5,
                RepWgts = "repwgt",
                FayFac = 1,
            };
            var fileName = Path.Combine(TestRservice.AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(fileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 1));

            RequestAnalysis requestAnalysisViewModel = new(rservice);
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

            requestAnalysisViewModel.SendAnalysisRequestCommand.Execute(null);

            Assert.True(messageSent);
            Assert.NotNull(requestedAnalysis);
            Assert.Equal(2, requestedAnalysis.Vars.Count);
            Assert.Empty(requestedAnalysis.GroupBy);
            Assert.Equal("wgt", requestedAnalysis.AnalysisConfiguration.DatasetType?.Weight);
            Assert.Equal(fileName, requestedAnalysis.AnalysisConfiguration.FileName);
        }
    }


}
