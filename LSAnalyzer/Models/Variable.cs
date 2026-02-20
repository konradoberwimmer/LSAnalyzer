using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models
{
    public class Variable
    {
        public int Position { get; set; }
        public string Name { get; set; }
        public string? Label { get; set; } = null;
        public bool IsSystemVariable { get; set; } = false;
        
        public bool FromPlausibleValues { get; set; } = false;
        
        public bool IsVirtual { get; set; } = false;

        public Variable(int position, string name)
        {
            Position = position;
            Name = name;
        }

        public string Info => Name + (Label != null ? " (" + Label + ")" : "");
    }
}
