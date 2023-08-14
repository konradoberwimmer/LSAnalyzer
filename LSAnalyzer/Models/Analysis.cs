﻿using RDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public abstract class Analysis
    {
        protected readonly AnalysisConfiguration _analysisConfiguration;
        public AnalysisConfiguration AnalysisConfiguration 
        { 
            get => _analysisConfiguration; 
        }
        public List<Variable> Vars { get; set; } = new();
        public List<Variable> GroupBy { get; set; } = new();
        public GenericVector? Result { get; set; }

        public Analysis(AnalysisConfiguration analysisConfiguration) 
        {
            _analysisConfiguration = analysisConfiguration;
        }
    }
}
