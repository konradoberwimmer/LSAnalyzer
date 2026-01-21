using System;

namespace LSAnalyzer.Models;

public class LogEntry
{
    public DateTime When { get; set; }
    public string? AnalysisName { get; set; }
    public string Rcode { get; set; }
    public bool OneLiner { get; set; }

    public string RcodeForTableCell => (OneLiner && Rcode.Contains('\n')) ? Rcode.Substring(0, Rcode.IndexOf('\n')) + "\n..." : Rcode;

    public LogEntry(DateTime when, string rcode, string? analysisName = null, bool oneLiner = false)
    {
        When = when;
        Rcode = rcode;
        AnalysisName = analysisName;
        OneLiner = oneLiner;
    }

    public string ToFullText()
    {
        return When.ToString() + " - " + AnalysisName + " - " + Rcode;
    }
}