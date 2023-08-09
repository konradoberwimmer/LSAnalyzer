using LSAnalyzer.Models;
using LSAnalyzer.ViewModels;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;

namespace TestLSAnalyzer.ViewModels
{
    public class TestSelectAnalysisFile
    {
        [Fact]
        public void TestUseFileForAnalysisSendsMessageOnSuccess()
        {
            Configuration datasetTypesConfiguration = new(Path.GetTempFileName());
            foreach (var datasetType in DatasetType.CreateDefaultDatasetTypes())
            {
                datasetTypesConfiguration.StoreDatasetType(datasetType);
            }

            SelectAnalysisFile selectAnalysisFileViewModel = new(datasetTypesConfiguration);
            selectAnalysisFileViewModel.FileName = "C:\\dummy.sav";
            selectAnalysisFileViewModel.SelectedDatasetType = selectAnalysisFileViewModel.DatasetTypes.First();

            bool messageSent = false;
            WeakReferenceMessenger.Default.Register<SetAnalysisConfigurationMessage>(this, (r, m) =>
            {
                messageSent = true;
            });

            selectAnalysisFileViewModel.UseFileForAnalysisCommand.Execute(null);

            Assert.True(messageSent);
        }
    }
}
