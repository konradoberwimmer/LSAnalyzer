using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using System.Reflection;

namespace TestLSAnalyzerUI;

[Collection("Sequential")]
public class TestAppPirls : SystemTestsBase
{
    [Fact]
    public void TestWorkflowPirlsAustria()
    {
        Environment.SetEnvironmentVariable("SHOW_DATASET_TYPES_GROUPED", "0");
        _testApplication = Application.Launch(Path.Combine(AssemblyDirectory, "LSAnalyzer.exe"));

        using UIA3Automation automation = new();
        ConditionFactory cf = new(new UIA3PropertyLibrary());

        // start LSAnalyzer
        var mainWindow = TestApplication!.GetMainWindow(automation, TimeSpan.FromSeconds(5));
        Assert.True(mainWindow != null, "App did not start - bear in mind that R and BIFIEsurvey are necessary for app tests too");

        // load PIRLS 2016 Austria data
        LoadFileFromFileSystem(automation, mainWindow, "test_asgautr4.sav", "PIRLS since 2016 - student level");

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

        // calculate correlation betweeen PVs, but only for girls
        var subsettingDialog = OpenWindowFromMenuItem(automation, mainWindow, "File", "Subsetting ...", "Subsetting");
        var expressionTextBox = subsettingDialog.FindAllDescendants(cf.ByControlType(ControlType.Edit).And(cf.ByAutomationId("textBoxSubsettingExpression"))).First().AsTextBox();
        expressionTextBox.Text = "ITSEX == 1";
        var useSubsettingButton = subsettingDialog.FindAllDescendants(cf.ByControlType(ControlType.Button).And(cf.ByName("OK"))).First().AsButton();
        useSubsettingButton.Click();
        var closedDialog = Retry.WhileNotNull(() => TestApplication!.GetAllTopLevelWindows(automation).Where(window => window.Title == "Subsetting").FirstOrDefault(), TimeSpan.FromSeconds(10)).Result;
        Assert.True(closedDialog);

        var analyzeCorrelDialog = OpenWindowFromMenuItem(automation, mainWindow, "Analysis", "Correlations ...", "Correlations");
        SelectVariablesInListBox(analyzeCorrelDialog, "listBoxVariablesDataset", new string[] { "ASRREA", "ASRLIT", "ASRINF", "ASRIIE", "ASRRSI" }, true);
        MoveVariablesBetweenLists(analyzeCorrelDialog, "listBoxVariablesDataset", "buttonMoveToAndFromAnalysisVariables", "listBoxVariablesAnalyze");
        StartAnalysisFromDialog(automation, analyzeCorrelDialog);
        SaveLastAnalysisAsXlsx(mainWindow, 15, "test_workflow_pirls_austria_correl.xlsx");

        // save those analyses
        mainWindow.FindFirstDescendant("buttonDownloadAnalysesDefinitions").Click();
        var saveAnalysesDialog = Retry.WhileNull(() => mainWindow.ModalWindows.FirstOrDefault(), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(saveAnalysesDialog);
        var jsonTextField = saveAnalysesDialog.FindFirstDescendant(cf.ByControlType(ControlType.ComboBox).And(cf.ByName("Dateiname:"))).AsComboBox();
        Assert.NotNull(jsonTextField);
        var jsonFilename = Path.Combine(Path.GetTempPath(), "test_workflow_pirls_austria_linreg.json");
        if (File.Exists(jsonFilename))
        {
            File.Delete(jsonFilename);
        }
        jsonTextField.EditableText = jsonFilename;
        var saveFileButton = saveAnalysesDialog.FindFirstDescendant(cf.ByControlType(ControlType.Button).And(cf.ByClassName("Button")).And(cf.ByName("Speichern"))).AsButton();
        Assert.NotNull(saveFileButton);
        saveFileButton.Click();
        Assert.True(Retry.WhileFalse(() => File.Exists(jsonFilename), TimeSpan.FromSeconds(10)).Result);

        // load PIRLS 2011 Austria data
        LoadFileFromFileSystem(automation, mainWindow, "test_asgautr3.sav", "PIRLS until 2011 - student level");

        // batch analyze
        var batchAnalyzeDialog = OpenWindowFromMenuItem(automation, mainWindow, "Analysis", "Batch analyze ...", "Batch analyze");
        var openFileDialogButton = batchAnalyzeDialog.FindFirstDescendant(cf.ByName("Select ...")).AsButton();
        openFileDialogButton.Click();
        var openFileDialog = Retry.WhileNull(() => batchAnalyzeDialog.ModalWindows.FirstOrDefault(), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(openFileDialog);
        var filenameTextField = openFileDialog.FindFirstDescendant(cf.ByControlType(ControlType.ComboBox).And(cf.ByName("Dateiname:"))).AsComboBox();
        Assert.NotNull(filenameTextField);
        filenameTextField.EditableText = jsonFilename;
        var openFileButton = openFileDialog.FindFirstDescendant(cf.ByControlType(ControlType.Button).And(cf.ByClassName("Button")).And(cf.ByName("Öffnen"))).AsButton();
        Assert.NotNull(openFileButton);
        openFileButton.Click();
        var analyzeButton = batchAnalyzeDialog.FindFirstDescendant(cf.ByName("Analyze")).AsButton();
        analyzeButton.Click();
        var gridView = batchAnalyzeDialog.FindAllDescendants(cf.ByControlType(ControlType.DataGrid)).First().AsDataGridView();
        Assert.NotNull(gridView);
        var analysisIsFinished = Retry.WhileFalse(() => gridView.Rows.All(row => row.Cells[3].Value == "Success!"), TimeSpan.FromSeconds(10)).Result;
        Assert.True(analysisIsFinished);
        var transferButton = batchAnalyzeDialog.FindFirstDescendant(cf.ByName("OK")).AsButton();
        Retry.WhileFalse(() => transferButton.IsEnabled, TimeSpan.FromSeconds(10));
        transferButton.Click();
        closedDialog = Retry.WhileNotNull(() => TestApplication!.GetAllTopLevelWindows(automation).Where(window => window.Title == "Batch analyze").FirstOrDefault(), TimeSpan.FromSeconds(10)).Result;
        Assert.True(closedDialog);

        // close app
        mainWindow.Close();
        TestApplication.Dispose();
    }
}
