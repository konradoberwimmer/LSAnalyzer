﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class AnalysisCorr : Analysis
    {
        public bool CalculateOverall { get; set; } = true;

        public AnalysisCorr(AnalysisConfiguration analysisConfiguration) : base(analysisConfiguration)
        {

        }

        public override string AnalysisName => "Correlations";
    }
}