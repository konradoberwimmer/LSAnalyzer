using RDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public abstract class Analysis
    {
        public int Id { get; set; }
        public List<string> Vars { get; set; } = new List<string>();
        public List<string> GroupBy { get; set; } = new List<string>();
        public GenericVector? Result { get; set; }
    }
}
