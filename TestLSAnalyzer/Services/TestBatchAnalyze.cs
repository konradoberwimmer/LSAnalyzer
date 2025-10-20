using CommunityToolkit.Mvvm.Messaging;
using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Polly;
using Xunit.Sdk;

namespace TestLSAnalyzer.Services
{
    [Collection("Sequential")]
    public class TestBatchAnalyze
    {
        [Fact]
        public void TestRunBatchSendsMessage()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            AnalysisConfiguration analysisConfiguration = new()
            {
                DatasetType = new() { },
                FileName = "dummy.sav",
                ModeKeep = true,
            };

            BatchAnalyze batchAnalyze = new(rservice);

            int messageCounter = 0;
            BatchAnalyzeMessage? lastMessage = null;
            WeakReferenceMessenger.Default.Register<BatchAnalyzeMessage>(this, (r, m) =>
            {
                messageCounter++;
                lastMessage = m;
            });

            batchAnalyze.RunBatch(new()
            {
                { 1, new AnalysisUnivar(analysisConfiguration) }, 
                { 2, new AnalysisFreq(analysisConfiguration) }, 
            }, true, analysisConfiguration);

            Policy.Handle<EqualException>().WaitAndRetry(500, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.Equal(4, messageCounter));
            
            Assert.NotNull(lastMessage);
            Assert.False(lastMessage.Success);
        }

        [Fact]
        public void TestRunBatchWithReloadingFiles()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            AnalysisConfiguration analysisConfigurationInvalid = new()
            {
                DatasetType = new() { },
                FileName = "dummy.sav",
            };
            AnalysisConfiguration analysisConfigurationNmi10Rep5 = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10, MIvar = "mi",
                    RepWgts = "repwgt", FayFac = 0.5,
                },
                ModeKeep = true,
            };
            AnalysisConfiguration analysisConfigurationNmi10Multicat = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10, MIvar = "mi",
                },
                ModeKeep = false,
            };

            BatchAnalyze batchAnalyze = new(rservice);

            List<BatchAnalyzeMessage> messages = new();
            WeakReferenceMessenger.Default.Register<BatchAnalyzeMessage>(this, (r, m) =>
            {
                messages.Add(m);
            });

            Dictionary<int, Analysis> analyses = new()
            {
                { 1, new AnalysisUnivar(analysisConfigurationNmi10Rep5) {
                        Vars = new() { new(1, "doesntExist", false) },
                    } },
                { 2, new AnalysisUnivar(analysisConfigurationNmi10Rep5) {
                        Vars = new() { new(1, "x", false) },
                        GroupBy = new() { new(2, "cat", false) },
                    }},
                { 3, new AnalysisFreq(analysisConfigurationNmi10Multicat) {
                        Vars = new() { new(1, "doesntExist", false) },
                    } },
                { 4, new AnalysisFreq(analysisConfigurationNmi10Multicat) {
                        Vars = new() { new(1, "item1", false), new(2, "item2", false) },
                        GroupBy = new() { new(2, "cat", false) },
                        CalculateBivariate = true,
                    } },
                { 5, new AnalysisCorr(analysisConfigurationInvalid) {
                        Vars = new() { new(1, "dummy", false) },
                    } },
            };

            batchAnalyze.RunBatch(analyses, false, null);

            Policy.Handle<EqualException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.Equal(10, messages.Count));
            
            Assert.False(messages[1].Success);
            Assert.True(messages[3].Success);
            Assert.False(messages[5].Success);
            Assert.True(messages[7].Success);
            Assert.False(messages[9].Success);
        }

        [Fact]
        public void TestRunBatchOnCurrentFileModeKeep()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            AnalysisConfiguration analysisConfigurationNmi10Rep5 = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                    RepWgts = "repwgt",
                    FayFac = 0.5,
                },
                ModeKeep = true,
            };
            AnalysisConfiguration analysisConfigurationNmi10Multicat = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = false,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationNmi10Rep5.FileName));
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfigurationNmi10Rep5));

            BatchAnalyze batchAnalyze = new(rservice);

            List<BatchAnalyzeMessage> messages = new();
            WeakReferenceMessenger.Default.Register<BatchAnalyzeMessage>(this, (r, m) =>
            {
                messages.Add(m);
            });

            Dictionary<int, Analysis> analyses = new()
            {
                { 1, new AnalysisUnivar(analysisConfigurationNmi10Rep5) {
                        Vars = new() { new(1, "doesntExist", false) },
                    } },
                { 2, new AnalysisUnivar(analysisConfigurationNmi10Rep5) {
                        Vars = new() { new(1, "x", false) },
                        GroupBy = new() { new(2, "cat", false) },
                    }},
                { 3, new AnalysisFreq(analysisConfigurationNmi10Multicat) {
                        Vars = new() { new(1, "cat", false) },
                    } },
                { 4, new AnalysisFreq(analysisConfigurationNmi10Multicat) {
                        Vars = new() { new(1, "item1", false), new(2, "item2", false) },
                        GroupBy = new() { new(2, "cat", false) },
                        CalculateBivariate = true,
                    } },
            };

            batchAnalyze.RunBatch(analyses, true, analysisConfigurationNmi10Rep5);

            Policy.Handle<EqualException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.Equal(8, messages.Count));

            Assert.False(messages[1].Success);
            Assert.True(messages[3].Success);
            Assert.True(messages[5].Success);
            Assert.False(messages[7].Success);
        }

        [Fact]
        public void TestRunBatchOnCurrentFileModeBuild()
        {
            Rservice rservice = new(new());
            Assert.True(rservice.Connect(), "R must also be available for tests");

            AnalysisConfiguration analysisConfigurationNmi10Rep5 = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                    RepWgts = "repwgt",
                    FayFac = 0.5,
                },
                ModeKeep = true,
            };
            AnalysisConfiguration analysisConfigurationNmi10Multicat = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_multicat.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                },
                ModeKeep = false,
            };

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfigurationNmi10Multicat.FileName));
            Assert.True(rservice.TestAnalysisConfiguration(analysisConfigurationNmi10Multicat));

            BatchAnalyze batchAnalyze = new(rservice);

            List<BatchAnalyzeMessage> messages = new();
            WeakReferenceMessenger.Default.Register<BatchAnalyzeMessage>(this, (r, m) =>
            {
                messages.Add(m);
            });

            Dictionary<int, Analysis> analyses = new()
            {
                { 1, new AnalysisUnivar(analysisConfigurationNmi10Rep5) {
                        Vars = new() { new(1, "item1", false) },
                    } },
                { 2, new AnalysisUnivar(analysisConfigurationNmi10Rep5) {
                        Vars = new() { new(1, "x", false) },
                        GroupBy = new() { new(2, "cat", false) },
                    }},
                { 3, new AnalysisFreq(analysisConfigurationNmi10Multicat) {
                        Vars = new() { new(1, "cat", false) },
                    } },
                { 4, new AnalysisFreq(analysisConfigurationNmi10Multicat) {
                        Vars = new() { new(1, "item1", false), new(2, "item2", false) },
                        GroupBy = new() { new(2, "cat", false) },
                        CalculateBivariate = true,
                    } },
            };

            batchAnalyze.RunBatch(analyses, true, analysisConfigurationNmi10Multicat);

            Policy.Handle<EqualException>().WaitAndRetry(1000, _ => TimeSpan.FromMilliseconds(1))
                .Execute(() => Assert.Equal(8, messages.Count));

            Assert.False(messages[1].Success);
            Assert.False(messages[3].Success);
            Assert.True(messages[5].Success);
            Assert.True(messages[7].Success);
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
