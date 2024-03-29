﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Services
{
    public class Logging
    {
        private List<LogEntry> _logEntries = new();

        public void AddEntry(LogEntry logEntry)
        {
            _logEntries.Add(logEntry);
        }

        public string Stringify()
        {
            return String.Join("\n", _logEntries.ConvertAll<string>(logEntry => logEntry.Stringify()).ToArray());
        }
    }

    public class LogEntry
    {
        public DateTime When { get; set; }
        public string? AnalysisName { get; set; }
        public string Rcode { get; set; }

        public LogEntry(DateTime when, string rcode, string? analysisName = null)
        {
            When = when;
            Rcode = rcode;
            AnalysisName = analysisName;
        }

        public string Stringify()
        {
            return When.ToString() + " - " + AnalysisName + " - " + Rcode;
        }
    }
}
