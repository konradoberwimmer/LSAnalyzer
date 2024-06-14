using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLSAnalyzerUI;

[Collection("Sequential")]
public class TestAppAussda : SystemTestsBase
{
    internal static string GetTestApiToken()
    {
        ConfigurationBuilder builder = new();
        builder.AddUserSecrets<TestAppAussda>();

        var configuration = builder.Build();
        return (string)configuration["testDataverseKey"]!;
    }

    [Fact]
    public void TestAussdaWorkflow()
    {
        _testApplication = Application.Launch(Path.Combine(AssemblyDirectory, "LSAnalyzer.exe"));

        using UIA3Automation automation = new();
        ConditionFactory cf = new(new UIA3PropertyLibrary());

        // start LSAnalyzer
        var mainWindow = TestApplication!.GetMainWindow(automation, TimeSpan.FromSeconds(5));
        Assert.True(mainWindow != null, "App did not start - bear in mind that R and BIFIEsurvey are necessary for app tests too");

        // configure AUSSDA data provider - thx to AUSSDA team
        var dataProviderDialog = OpenWindowFromMenuItem(automation, mainWindow, "Config", "Data providers ...", "Data providers");
        var newProviderSelect = dataProviderDialog.FindAllDescendants(cf.ByAutomationId("comboBoxSelectedType")).First().AsComboBox();
        newProviderSelect.Select("Dataverse");
        newProviderSelect.Collapse();
        var newProviderName = Retry.WhileNull(() => dataProviderDialog.FindAllDescendants(cf.ByControlType(ControlType.Edit)).FirstOrDefault().AsTextBox(), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(newProviderName);
        newProviderName.Text = "Test AUSSDA";
        var newProviderUrl = dataProviderDialog.FindAllDescendants(cf.ByControlType(ControlType.Edit))[1].AsTextBox();
        newProviderUrl.Text = "https://data.aussda.at/";
        var newProviderApiToken = dataProviderDialog.FindAllDescendants(cf.ByControlType(ControlType.Edit)).LastOrDefault().AsTextBox();
        Assert.NotNull(newProviderApiToken);
        var apiToken = GetTestApiToken();
        Assert.True(!string.IsNullOrWhiteSpace(apiToken), "dataverse access (https://data.aussda.at/) not working - be sure to set up an API key in your user secrets");
        newProviderApiToken.Text = apiToken;
        var buttonSaveDataProvider = dataProviderDialog.FindAllDescendants(cf.ByName("Save")).First().AsButton();
        buttonSaveDataProvider.Click();
        var buttonTestDataProvider = dataProviderDialog.FindAllDescendants(cf.ByName("Test")).First().AsButton();
        var buttonTestDataProviderEnabled = Retry.WhileFalse(() => buttonTestDataProvider.IsEnabled, TimeSpan.FromSeconds(5)).Result;
        Assert.True(buttonTestDataProviderEnabled);
        buttonTestDataProvider.Click();
        var testResultText = Retry.WhileNull(() => dataProviderDialog.FindAllDescendants(cf.ByName("Data provider works")).FirstOrDefault(), TimeSpan.FromSeconds(5)).Result;
        Assert.True(testResultText != null, "dataverse access (https://data.aussda.at/) not working - be sure to set up an API key in your user secrets");
        dataProviderDialog.Close();
        Retry.WhileNotNull(() => TestApplication!.GetAllTopLevelWindows(automation).Where(window => window.Title == "Data providers").FirstOrDefault(), TimeSpan.FromSeconds(10));

        // test data access from AUSSDA
        var selectFileDialog = OpenWindowFromMenuItem(automation, mainWindow, "File", "Select File ...", "Select file for analyses");
        Assert.NotNull(selectFileDialog);
        var tabDataProviders = selectFileDialog.FindAllDescendants(cf.ByControlType(ControlType.TabItem)).Last().AsTabItem();
        tabDataProviders.Click();
        var dataProviderSelect = Retry.WhileNull(() => tabDataProviders.FindAllDescendants(cf.ByControlType(ControlType.ComboBox)).First().AsComboBox(), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(dataProviderSelect);
        dataProviderSelect.Select("Test AUSSDA");
        dataProviderSelect.Collapse();
        var dataProviderFileName = Retry.WhileNull(() => tabDataProviders.FindAllDescendants(cf.ByControlType(ControlType.Edit)).FirstOrDefault().AsTextBox(), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(dataProviderFileName);
        dataProviderFileName.Text = "10715_vi_de_v1_0.tab";
        var dataProviderDataset = tabDataProviders.FindAllDescendants(cf.ByControlType(ControlType.Edit)).Last().AsTextBox();
        dataProviderDataset.Text = "doi:10.11587/5ZCVJY";
        var dataProviderTestButton = Retry.WhileNull(() => tabDataProviders.FindAllDescendants(cf.ByName("Test")).First().AsButton(), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(dataProviderTestButton);
        dataProviderTestButton.Click();
        testResultText = Retry.WhileNull(() => tabDataProviders.FindAllDescendants(cf.ByName("File access works")).FirstOrDefault(), TimeSpan.FromSeconds(5)).Result;
        Assert.NotNull(testResultText);
        selectFileDialog.Close();

        // remove AUSSDA data provider afterwards
        dataProviderDialog = OpenWindowFromMenuItem(automation, mainWindow, "Config", "Data providers ...", "Data providers");
        var selectProvider = dataProviderDialog.FindAllDescendants(cf.ByAutomationId("comboBoxSelectedConfiguration")).First().AsComboBox();
        selectProvider.Select("Test AUSSDA");
        selectProvider.Collapse();
        var buttonRemoveProvider = dataProviderDialog.FindAllDescendants(cf.ByName("Remove")).First().AsButton();
        var buttonRemoveEnabled = Retry.WhileFalse(() => buttonRemoveProvider.IsEnabled, TimeSpan.FromSeconds(5)).Result;
        Assert.True(buttonRemoveEnabled);
        buttonRemoveProvider.Click();
        var selectProviderEmpty = Retry.WhileFalse(() => selectProvider.SelectedItem == null, TimeSpan.FromSeconds(5)).Result;
        Assert.True(selectProviderEmpty);
        dataProviderDialog.Close();
        Retry.WhileNotNull(() => TestApplication!.GetAllTopLevelWindows(automation).Where(window => window.Title == "Data providers").FirstOrDefault(), TimeSpan.FromSeconds(10));

        // close app
        mainWindow.Close();
        TestApplication.Dispose();
    }
}
