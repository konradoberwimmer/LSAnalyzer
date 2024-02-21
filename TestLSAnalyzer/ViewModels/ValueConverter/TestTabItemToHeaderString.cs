using LSAnalyzer.ViewModels.ValueConverter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TestLSAnalyzer.ViewModels.ValueConverter
{
    public class TestTabItemToHeaderString
    {
        [Fact]
        public async void TestConvertBack()
        {
            await StartSTATask(() =>
            {
                var testCases = new List<object?[]>() {
                    new object?[] { null, string.Empty },
                    new object?[] { 1, string.Empty },
                    new object?[] { new TabItem(), string.Empty },
                    new object?[] { new TabItem() { Header = "above" }, "above" },
                };

                foreach (var testCase in testCases)
                {
                    TabItemToHeaderString converter = new();
                    Assert.Equal(testCase[1], converter.ConvertBack(testCase[0], typeof(string), string.Empty, CultureInfo.InvariantCulture));
                }
            });
        }

        private static Task StartSTATask(Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(new object());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }
    }
}
