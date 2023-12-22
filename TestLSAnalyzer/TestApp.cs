using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System;
using System.Reflection;

namespace TestLSAnalyzer
{
    [Collection("Sequential")]
    public sealed class TestApp : IDisposable
    {
        private readonly Application _app = Application.Launch(@"LSAnalyzer.exe");

        [Fact]
        public void TestWorkflowPirlsAustria()
        {
            using var automation = new UIA3Automation();
            Window window = _app.GetMainWindow(automation, TimeSpan.FromSeconds(3));

            Assert.True(window != null, "App did not start - bear in mind that R and BIFIEsurvey are necessary for app tests too");
        }

        public void Dispose()
        {
            _app.Close();
            _app.Dispose();
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
