using System.Collections.Generic;
using LSAnalyzer.Models;

namespace LSAnalyzer.Services;

public class Logging : ILogging
{
    public List<LogEntry> LogEntries { get; } = [];

    public void AddEntry(LogEntry logEntry)
    {
        LogEntries.Add(logEntry);
    }

    public string GetRcode()
    {
        return string.Join("\n", LogEntries.ConvertAll<string>(logEntry => logEntry.Rcode).ToArray());
    }

    public string GetFullText()
    {
        return string.Join("\n", LogEntries.ConvertAll<string>(logEntry => logEntry.ToFullText()).ToArray());
    }
}
