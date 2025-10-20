using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using LSAnalyzer.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Polly;
using Xunit.Sdk;
using BatchAnalyzeViewModel = LSAnalyzer.ViewModels.BatchAnalyze;

namespace TestLSAnalyzer.ViewModels
{
    [Collection("Sequential")]
    public class TestBatchAnalyze
    {
        [Fact]
        public void TestRunBatchSendsFailureMessageOnInvalidJSON()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            LSAnalyzer.Services.BatchAnalyze batchAnalyzeService = new(rservice);
            LSAnalyzer.ViewModels.BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService);

            bool messageSent = false;
            WeakReferenceMessenger.Default.Register<BatchAnalyzeFailureMessage>(this, (r, m) =>
            {
                messageSent = true; 
            });

            var tmpFile = Path.Combine(Path.GetTempPath(), "stupid.json");
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }

            var fileStream = File.Create(tmpFile);
            var streamWriter = new StreamWriter(fileStream);
            streamWriter.WriteLine("{ WTF! }");
            streamWriter.Flush();
            streamWriter.Close();
            fileStream.Close();

            batchAnalyzeViewModel.FileName = tmpFile;
            batchAnalyzeViewModel.RunBatchCommand.Execute(null);

            Policy.Handle<TrueException>().WaitAndRetry(100, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.True(messageSent));
        }

        [Fact]
        public void TestRunBatchInvokesService()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = true,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration));

            LSAnalyzer.Services.BatchAnalyze batchAnalyzeService = new(rservice);
            LSAnalyzer.ViewModels.BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService);

            batchAnalyzeViewModel.FileName = Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat.json");
            batchAnalyzeViewModel.UseCurrentFile = true;
            batchAnalyzeViewModel.CurrentConfiguration = analysisConfiguration;
            batchAnalyzeViewModel.RunBatchCommand.Execute(null);

            Policy.Handle<NotNullException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.NotNull(batchAnalyzeViewModel.AnalysesTable));
            Assert.Equal(4, batchAnalyzeViewModel.AnalysesTable!.Rows.Count);
            
            Policy.Handle<Exception>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() =>
                {
                    foreach (var row in batchAnalyzeViewModel.AnalysesTable.Rows)
                    {
                        var dataRow = row as DataRow;
                        Assert.True((bool)(dataRow!["Success"]));
                    }
                });
        }

        [Fact]
        public void TransferResultsSendMessages()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = true,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration));

            LSAnalyzer.Services.BatchAnalyze batchAnalyzeService = new(rservice);
            LSAnalyzer.ViewModels.BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService);

            batchAnalyzeViewModel.FileName = Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat.json");
            batchAnalyzeViewModel.UseCurrentFile = true;
            batchAnalyzeViewModel.CurrentConfiguration =analysisConfiguration;
            
            var lastSuccessMessageSent = false;
            WeakReferenceMessenger.Default.Register<BatchAnalyzeMessage>(this, (_, m) =>
            {
                if (m.Id == 4) lastSuccessMessageSent = true;
            });
            
            batchAnalyzeViewModel.RunBatchCommand.Execute(null);
         
            Policy.Handle<TrueException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.True(lastSuccessMessageSent));

            int analysisReadyMessagesSent = 0;
            WeakReferenceMessenger.Default.Register<BatchAnalyzeAnalysisReadyMessage>(this, (r, m) =>
            {
                analysisReadyMessagesSent++;
            });

            batchAnalyzeViewModel.TransferResultsCommand.Execute(null);

            Policy.Handle<EqualException>().WaitAndRetry(200, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.Equal(4, analysisReadyMessagesSent));
        }

        [Fact]
        public void TestClearAnalysisData()
        {
            BatchAnalyzeViewModel batchAnalyzeViewModel = new()
            {
                AnalysesTable = new DataTable { Columns = { "A", "B" } },
                IsBusy = true,
                FinishedAllCalculations = true
            };
            
            batchAnalyzeViewModel.ClearAnalysisData();
            
            Assert.Null(batchAnalyzeViewModel.AnalysesTable);
            Assert.False(batchAnalyzeViewModel.IsBusy);
            Assert.False(batchAnalyzeViewModel.FinishedAllCalculations);
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().Location;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path)!;
            }
        }
    }
}
