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
        public bool IsSystemVariable { get; set; }

        public Variable(int position, string name, bool isSystemVariable)
        {
            Position = position;
            Name = name;
            IsSystemVariable = isSystemVariable;
        }
    }
}
