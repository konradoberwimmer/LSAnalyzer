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

namespace TestLSAnalyzer.ViewModels
{
    [Collection("Sequential")]
    public class TestBatchAnalyze
    {
        [Fact]
        public async Task TestRunBatchSendsFailureMessageOnInvalidJSON()
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

            await Task.Delay(100);

            Assert.True(messageSent);
        }

        [Fact]
        public async Task TestRunBatchInvokesService()
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
                    Nrep = 1,
                },
                ModeKeep = true,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration));

            LSAnalyzer.Services.BatchAnalyze batchAnalyzeService = new(rservice);
            LSAnalyzer.ViewModels.BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService);

            bool failureMessageSent = false;
            WeakReferenceMessenger.Default.Register<BatchAnalyzeFailureMessage>(this, (r, m) =>
            {
                failureMessageSent = true;
            });

            batchAnalyzeViewModel.FileName = Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat.json");
            batchAnalyzeViewModel.UseCurrentFile = true;
            batchAnalyzeViewModel.CurrentModeKeep = true;
            batchAnalyzeViewModel.RunBatchCommand.Execute(null);

            await Task.Delay(1000);

            Assert.False(failureMessageSent);
            Assert.NotNull(batchAnalyzeViewModel.AnalysesTable);
            Assert.Equal(4, batchAnalyzeViewModel.AnalysesTable.Rows.Count);
            
            foreach (var row in batchAnalyzeViewModel.AnalysesTable.Rows)
            {
                var dataRow = row as DataRow;
                Assert.True((bool)(dataRow!["Success"]));
            }
        }

        [Fact]
        public async Task TransferResultsSendMessages()
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
                    Nrep = 1,
                },
                ModeKeep = true,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfiguration));

            LSAnalyzer.Services.BatchAnalyze batchAnalyzeService = new(rservice);
            LSAnalyzer.ViewModels.BatchAnalyze batchAnalyzeViewModel = new(batchAnalyzeService);

            batchAnalyzeViewModel.FileName = Path.Combine(AssemblyDirectory, "_testData", "analyze_test_nmi10_multicat.json");
            batchAnalyzeViewModel.UseCurrentFile = true;
            batchAnalyzeViewModel.CurrentModeKeep = true;
            batchAnalyzeViewModel.RunBatchCommand.Execute(null);

            await Task.Delay(1000);

            int analysisReadyMessagesSent = 0;
            WeakReferenceMessenger.Default.Register<BatchAnalyzeAnalysisReadyMessage>(this, (r, m) =>
            {
                analysisReadyMessagesSent++;
            });

            batchAnalyzeViewModel.TransferResultsCommand.Execute(null);

            await Task.Delay(200);

            Assert.Equal(4, analysisReadyMessagesSent);
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
