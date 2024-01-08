using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using System.Reflection;

namespace TestLSAnalyzer
{
    [Collection("Sequential")]
    public class TestApp
    {
        [Fact]
        public void TestWorkflowPirlsAustria()
        {
            var app = Application.Launch(Path.Combine(AssemblyDirectory, "LSAnalyzer.exe"));

            using UIA3Automation automation = new();
            ConditionFactory cf = new(new UIA3PropertyLibrary());

            // start LSAnalyzer
            var mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(5));
            Assert.True(mainWindow != null, "App did not start - bear in mind that R and BIFIEsurvey are necessary for app tests too");

            // open up Select File dialog
            var fileMenu = mainWindow.FindFirstDescendant(cf.ByName("File")).AsMenuItem();
            fileMenu.Click();
            fileMenu.Items.Where(item => item.Name == "Select File ...").First().Click();
            var selectFileDialog = Retry.WhileNull(() => app.GetAllTopLevelWindows(automation).Where(window => window.Title == "Select file for analyses").FirstOrDefault(), TimeSpan.FromSeconds(2)).Result;
            Assert.NotNull(selectFileDialog);
            
            // get PIRLS Austria data file
            var openFileDialogButton = selectFileDialog.FindFirstDescendant(cf.ByName("Select ...")).AsButton();
            openFileDialogButton.Click();
            var openFileDialog = Retry.WhileNull(() => selectFileDialog.ModalWindows.FirstOrDefault(), TimeSpan.FromSeconds(2)).Result;
            Assert.NotNull(openFileDialog);
            var filenameTextField = openFileDialog.FindFirstDescendant(cf.ByControlType(ControlType.ComboBox).And(cf.ByName("Dateiname:"))).AsComboBox();
            Assert.NotNull(filenameTextField);
            filenameTextField.EditableText = Path.Combine(AssemblyDirectory, "_testData", "test_asgautr4.sav");
            var openFileButton = openFileDialog.FindFirstDescendant(cf.ByControlType(ControlType.Button).And(cf.ByClassName("Button")).And(cf.ByName("Öffnen"))).AsButton();
            Assert.NotNull(openFileButton);
            openFileButton.Click();

            // set PIRLS dataset definition
            var selectDatasetTypeList = selectFileDialog.FindFirstChild("comboBoxDatasetType").AsComboBox();
            Assert.NotNull(selectDatasetTypeList);
            selectDatasetTypeList.Select("PIRLS since 2016 - student level");
            Assert.True(selectDatasetTypeList.SelectedItem != null, "Dataset type could not be selected - note that 'PIRLS since 2016 - student level' needs to be configured for your user");
            
            // load PIRLS Austria data
            var useFileForAnalysisButton = selectFileDialog.FindFirstDescendant(cf.ByControlType(ControlType.Button).And(cf.ByName("Go!"))).AsButton();
            Assert.NotNull(useFileForAnalysisButton);
            useFileForAnalysisButton.WaitUntilEnabled();
            useFileForAnalysisButton.Focus();
            useFileForAnalysisButton.Click();
            Retry.WhileNotNull(() => app.GetAllTopLevelWindows(automation).Where(window => window.Title == "Select file for analyses").FirstOrDefault(), TimeSpan.FromSeconds(10));
            
            // close app
            mainWindow.Close();
            app.Dispose();
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
