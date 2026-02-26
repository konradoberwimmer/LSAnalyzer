using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LSAnalyzer.Models
{
    public class Variable : ObservableObject
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

        public Variable Clone()
        {
            return new Variable(Position, Name)
            {
                Label = Label,
                IsSystemVariable = IsSystemVariable,
                FromPlausibleValues = FromPlausibleValues,
                IsVirtual = IsVirtual,
            };
        }

        public string Info => Name + (Label != null ? " (" + Label + ")" : "");
    }
}
