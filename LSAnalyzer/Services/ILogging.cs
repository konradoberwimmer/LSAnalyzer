using System.Collections.Generic;
using LSAnalyzer.Models;

namespace LSAnalyzer.Services;

public interface ILogging
{
    public List<LogEntry> LogEntries { get; }

    public void AddEntry(LogEntry logEntry);

    public string GetRcode();

    public string GetFullText();
}