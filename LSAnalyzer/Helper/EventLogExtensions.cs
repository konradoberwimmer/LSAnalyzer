using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Helper
{
    public static partial class EventLogExtensions
    {
        [LoggerMessage(EventId = 101, Level = LogLevel.Warning, Message = "Unexpected parameters in function call '{functionName}'")]
        public static partial void LogUnexpectedParametersInFunctionCall(this ILogger logger, string functionName);
    }
}
