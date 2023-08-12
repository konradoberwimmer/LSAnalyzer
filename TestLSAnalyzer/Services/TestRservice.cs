using LSAnalyzer.Models;
using LSAnalyzer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestLSAnalyzer.Services
{
    [Collection("Sequential")]
    public class TestRservice
    {
        [Fact]
        public void TestConnect()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
        }

        [Fact]
        public void TestInstallAndCheckNecessaryRPackages()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.InstallNecessaryRPackages());
            Assert.True(rservice.CheckNecessaryRPackages(), "R packages are also necessary for tests");
        }

        [Fact]
        public void TestLoadFileIntoGlobalEnvironment()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.False(rservice.LoadFileIntoGlobalEnvironment(Path.GetTempFileName()));
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_dummy.sav")));
        }

        [Fact]
        public void TestCreateReplicateWeights()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));
            Assert.False(rservice.CreateReplicateWeights(2, "wgt", "jkzoooone", "jkrep", false));
            Assert.True(rservice.CreateReplicateWeights(2, "wgt", "jkzone", "jkrep", false));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));
            Assert.False(rservice.CreateReplicateWeights(2, "wgt", "jkzone", "jkrep", true));
            Assert.True(rservice.CreateReplicateWeights(4, "wgt", "jkzone", "jkrep", true));

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_create_repwgts.sav")));
            Assert.False(rservice.CreateReplicateWeights(2, "wgtstud", "jkzone", "jkrep", false));
        }

        [Fact]
        public void TestCreateBIFIEdataObject()
        {
            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");

            Assert.True(rservice.LoadFileIntoGlobalEnvironment(Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav")));
            Assert.False(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 3, "repwgt", 1));
            Assert.False(rservice.CreateBIFIEdataObject("wgt", 10, null, null, 5, "repwgt", 1));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 1));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 0.5));
        }

        [Fact]
        public void TestGetCurrentDatasetVariables()
        {
            AnalysisConfiguration analysisConfiguration = new()
            {
                FileName = Path.Combine(AssemblyDirectory, "_testData", "test_nmi10_nrep5.sav"),
                DatasetType = new()
                {
                    Weight = "wgt",
                    NMI = 10,
                    MIvar = "mi",
                    Nrep = 5,
                    RepWgts = "repwgt",
                    FayFac = 0.5,
                },
                ModeKeep = true,
            };

            Rservice rservice = new();
            Assert.True(rservice.Connect(), "R must also be available for tests");
            Assert.True(rservice.LoadFileIntoGlobalEnvironment(analysisConfiguration.FileName));
            Assert.True(rservice.CreateBIFIEdataObject("wgt", 10, "mi", null, 5, "repwgt", 0.5));

            var variablesList = rservice.GetCurrentDatasetVariables(analysisConfiguration);
            Assert.NotNull(variablesList);
            Assert.True(variablesList.Count == 10);
            Assert.True(variablesList.Where(var => var.Name == "repwgt1").First().IsSystemVariable);
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
