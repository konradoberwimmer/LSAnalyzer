using System.Collections.Generic;
using LSAnalyzer.Models;

namespace LSAnalyzer.Services.Stubs;

public class LoggingStub : ILogging
{
    public List<LogEntry> LogEntries { get; } = [];
    
    public void AddEntry(LogEntry logEntry)
    {
        
    }

    public string GetRcode()
    {
        throw new System.NotImplementedException();
    }

    public string GetFullText()
    {
        throw new System.NotImplementedException();
    }
}