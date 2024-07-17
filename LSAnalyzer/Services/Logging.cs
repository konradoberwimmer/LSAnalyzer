using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Services
{
    public class Logging
    {
        private List<LogEntry> _logEntries = new();
        public List<LogEntry> LogEntries { get => _logEntries; }

        public void AddEntry(LogEntry logEntry)
        {
            _logEntries.Add(logEntry);
        }

        public string GetRcode()
        {
            return String.Join("\n", _logEntries.ConvertAll<string>(logEntry => logEntry.Rcode).ToArray());
        }

        public string GetFullText()
        {
            return String.Join("\n", _logEntries.ConvertAll<string>(logEntry => logEntry.ToFullText()).ToArray());
        }
    }

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
}
