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
        private Application? _testApplication = null;
        public Application? TestApplication { get => _testApplication; }

        [Fact]
        public void TestWorkflowPirlsAustria()
        {
            _testApplication = Application.Launch(Path.Combine(AssemblyDirectory, "LSAnalyzer.exe"));

            using UIA3Automation automation = new();
            ConditionFactory cf = new(new UIA3PropertyLibrary());

            // start LSAnalyzer
            var mainWindow = TestApplication!.GetMainWindow(automation, TimeSpan.FromSeconds(5));
            Assert.True(mainWindow != null, "App did not start - bear in mind that R and BIFIEsurvey are necessary for app tests too");

            // open up Select File dialog
            var selectFileDialog = OpenWindowFromMenuItem(automation, mainWindow, "File", "Select File ...", "Select file for analyses");
            Assert.NotNull(selectFileDialog);
            
            // get PIRLS Austria data file
            var openFileDialogButton = selectFileDialog.FindFirstDescendant(cf.ByName("Select ...")).AsButton();
            openFileDialogButton.Click();
            var openFileDialog = Retry.WhileNull(() => selectFileDialog.ModalWindows.FirstOrDefault(), TimeSpan.FromSeconds(5)).Result;
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
            Retry.WhileNotNull(() => TestApplication.GetAllTopLevelWindows(automation).Where(window => window.Title == "Select file for analyses").FirstOrDefault(), TimeSpan.FromSeconds(10));

            // analzye univariate PVs by sex and overall
            var analyzeUnivariateDialog = OpenWindowFromMenuItem(automation, mainWindow, "Analysis", "Univariate (means and SD) ...", "Univariate statistics");
            SelectVariablesInListBox(analyzeUnivariateDialog, "listBoxVariablesDataset", new string[] { "ASRREA", "ASRLIT", "ASRINF", "ASRIIE", "ASRRSI" }, true);
            MoveVariablesBetweenLists(analyzeUnivariateDialog, "listBoxVariablesDataset", "buttonMoveToAndFromAnalysisVariables", "listBoxVariablesAnalyze");
            SelectVariablesInListBox(analyzeUnivariateDialog, "listBoxVariablesDataset", new string[] { "ITSEX" });
            MoveVariablesBetweenLists(analyzeUnivariateDialog, "listBoxVariablesDataset", "buttonMoveToAndFromGroupByVariables", "listBoxVariablesGroupBy");
            StartAnalysisFromDialog(automation, analyzeUnivariateDialog);
            SaveLastAnalysisAsXlsx(mainWindow, 15, "test_workflow_pirls_austria_univar.xlsx");

            // analyze frequencies of benchmark by sex and overall
            var analyzeFreqDialog = OpenWindowFromMenuItem(automation, mainWindow, "Analysis", "Frequencies ...", "Frequencies");
            SelectVariablesInListBox(analyzeFreqDialog, "listBoxVariablesDataset", new string[] { "ASRIBM" }, true);
            MoveVariablesBetweenLists(analyzeFreqDialog, "listBoxVariablesDataset", "buttonMoveToAndFromAnalysisVariables", "listBoxVariablesAnalyze");
            SelectVariablesInListBox(analyzeFreqDialog, "listBoxVariablesDataset", new string[] { "ITSEX" });
            MoveVariablesBetweenLists(analyzeFreqDialog, "listBoxVariablesDataset", "buttonMoveToAndFromGroupByVariables", "listBoxVariablesGroupBy");
            StartAnalysisFromDialog(automation, analyzeFreqDialog);
            SaveLastAnalysisAsXlsx(mainWindow, 9, "test_workflow_pirls_austria_freq.xlsx");

            // analyze quartiles of main PV by sex and overall like IDBanalyzer
            var analyzePercDialog = OpenWindowFromMenuItem(automation, mainWindow, "Analysis", "Percentiles ...", "Percentiles");
            SelectVariablesInListBox(analyzePercDialog, "listBoxVariablesDataset", new string[] { "ASRREA" }, true);
            MoveVariablesBetweenLists(analyzePercDialog, "listBoxVariablesDataset", "buttonMoveToAndFromAnalysisVariables", "listBoxVariablesAnalyze");
            SelectVariablesInListBox(analyzePercDialog, "listBoxVariablesDataset", new string[] { "ITSEX" });
            MoveVariablesBetweenLists(analyzePercDialog, "listBoxVariablesDataset", "buttonMoveToAndFromGroupByVariables", "listBoxVariablesGroupBy");
            var checkboxUseInterpolation = analyzePercDialog.FindFirstDescendant(cf.ByControlType(ControlType.CheckBox).And(cf.ByName("Use interpolation"))).AsCheckBox();
            checkboxUseInterpolation.Click();
            var checkBoxMimicIDBanalyzer = analyzePercDialog.FindFirstDescendant(cf.ByControlType(ControlType.CheckBox).And(cf.ByName(""))).AsCheckBox();
            checkBoxMimicIDBanalyzer.WaitUntilEnabled();
            checkBoxMimicIDBanalyzer.Click();
            StartAnalysisFromDialog(automation, analyzePercDialog);
            SaveLastAnalysisAsXlsx(mainWindow, 3, "test_workflow_pirls_austria_perc.xlsx");

            // do a forward linear regression for main PV using some predictors
            var analyzeLinregDialog = OpenWindowFromMenuItem(automation, mainWindow, "Analysis", "Linear Regression ...", "Linear Regression");
            SelectVariablesInListBox(analyzeLinregDialog, "listBoxVariablesDataset", new string[] { "ASRREA" }, true);
            MoveVariablesBetweenLists(analyzeLinregDialog, "listBoxVariablesDataset", "buttonMoveToAndFromDependentVariable", "listBoxVariablesDependent");
            SelectVariablesInListBox(analyzeLinregDialog, "listBoxVariablesDataset", new string[] { "ITSEX", "ASBG03", "ASBG04" });
            MoveVariablesBetweenLists(analyzeLinregDialog, "listBoxVariablesDataset", "buttonMoveToAndFromAnalysisVariables", "listBoxVariablesAnalyze");
            var buttonSequenceForward = analyzeLinregDialog.FindFirstDescendant("radioButtonSequenceForward").AsRadioButton();
            buttonSequenceForward.Click();
            StartAnalysisFromDialog(automation, analyzeLinregDialog);
            SaveLastAnalysisAsXlsx(mainWindow, 9, "test_workflow_pirls_austria_linreg.xlsx");

            // close app
            mainWindow.Close();
            TestApplication.Dispose();
        }

        private Window OpenWindowFromMenuItem(UIA3Automation automation, Window mainWindow, string mainMenuItemName, string subMenuItemName, string windowTitle)
        {
            ConditionFactory cf = new(new UIA3PropertyLibrary());
            var mainMenuItem = mainWindow.FindFirstDescendant(cf.ByName(mainMenuItemName)).AsMenuItem();
            mainMenuItem.Click();
            mainMenuItem.Items.Where(item => item.Name == subMenuItemName).First().Click();
            var dialog = Retry.WhileNull(() => TestApplication!.GetAllTopLevelWindows(automation).Where(window => window.Title == windowTitle).FirstOrDefault(), TimeSpan.FromSeconds(5)).Result;

            Assert.NotNull(dialog);

            return dialog;
        }

        private void SelectVariablesInListBox(Window dialog, string listBoxAutomationID, string[] variables, bool clickShowLabels = false)
        {
            var listBox = dialog.FindFirstChild(listBoxAutomationID).AsListBox();
            Assert.NotNull(listBox);

            if (clickShowLabels)
            {
                listBox.RightClick();
                dialog.ContextMenu.Items.Where(item => item.Name == "Show Labels").FirstOrDefault()?.Click();
            }

            while (listBox.Patterns.Scroll.Pattern.VerticalScrollPercent > 0)
            {
                listBox.Patterns.Scroll.Pattern.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeDecrement);
            }

            foreach (var variable in variables)
            {
                while (listBox.Items.Where(item => item.Text == variable).Count() == 0)
                {
                    listBox.Patterns.Scroll.Pattern.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement);
                }

                if (variable == variables[0])
                {
                    listBox.Select(variable);
                } else
                {
                    listBox.AddToSelection(variable);
                }
            }

            Assert.True(listBox.SelectedItems.Length == variables.Length, "variables not found - make sure test data file is valid");
        }

        private void MoveVariablesBetweenLists(Window dialog, string sourceListBoxAutomationID, string buttonAutomationID, string targetListBoxAutomationID)
        {
            var sourceListBox = dialog.FindFirstChild(sourceListBoxAutomationID).AsListBox();
            Assert.NotNull(sourceListBox);
            var countSelected = sourceListBox.SelectedItems.Length;

            var targetListBox = dialog.FindFirstChild(targetListBoxAutomationID).AsListBox();
            Assert.NotNull(targetListBox);
            var oldCount = targetListBox.Items.Length;

            var button = dialog.FindFirstChild(buttonAutomationID).AsButton();
            Assert.NotNull(button);
            button.Click();

            Assert.Equal(oldCount + countSelected, targetListBox.Items.Length);
        }

        private void StartAnalysisFromDialog(UIA3Automation automation, Window dialog)
        {
            ConditionFactory cf = new(new UIA3PropertyLibrary());
            var windowTitle = dialog.Title;

            var button = dialog.FindFirstDescendant(cf.ByControlType(ControlType.Button).And(cf.ByName("Go !"))).AsButton();
            Assert.NotNull(button);
            button.Click();

            var closedDialog = Retry.WhileNotNull(() => TestApplication!.GetAllTopLevelWindows(automation).Where(window => window.Title == windowTitle).FirstOrDefault(), TimeSpan.FromSeconds(10)).Result;
            Assert.True(closedDialog);
        }

        private void SaveLastAnalysisAsXlsx(Window mainWindow, int expectedRowCount, string fileName)
        {
            ConditionFactory cf = new(new UIA3PropertyLibrary());

            var gridView = mainWindow.FindAllDescendants(cf.ByControlType(ControlType.DataGrid)).Last().AsDataGridView();
            Assert.NotNull(gridView);
            var analysisIsFinished = Retry.WhileFalse(() => gridView.Rows.Count() == expectedRowCount, TimeSpan.FromSeconds(60)).Result;
            Assert.True(analysisIsFinished);

            var buttonDownloadXlsx = mainWindow.FindAllDescendants(cf.ByAutomationId("buttonDownloadXlsx")).Last().AsButton();
            Assert.NotNull(buttonDownloadXlsx);
            buttonDownloadXlsx.Click();

            var saveFileDialog = Retry.WhileNull(() => mainWindow.ModalWindows.FirstOrDefault(), TimeSpan.FromSeconds(2)).Result;
            Assert.NotNull(saveFileDialog);

            var xlsxTextField = saveFileDialog.FindFirstDescendant(cf.ByControlType(ControlType.ComboBox).And(cf.ByName("Dateiname:"))).AsComboBox();
            Assert.NotNull(xlsxTextField);

            var xlsxFilename = Path.Combine(Path.GetTempPath(), fileName);
            if (File.Exists(xlsxFilename))
            {
                File.Delete(xlsxFilename);
            }
            xlsxTextField.EditableText = xlsxFilename;

            var saveFileButton = saveFileDialog.FindFirstDescendant(cf.ByControlType(ControlType.Button).And(cf.ByClassName("Button")).And(cf.ByName("Speichern"))).AsButton();
            Assert.NotNull(saveFileButton);

            saveFileButton.Click();
            Assert.True(Retry.WhileFalse(() => File.Exists(xlsxFilename), TimeSpan.FromSeconds(10)).Result);
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
