using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Helper
{
    public class StringFormats
    {
        public static int getMaxRelevantDigits(double[] values, int maxDigits = 3)
        {
            int digits = 0;
            while (digits < maxDigits)
            {
                bool allSatisfied = true;

                foreach (var value in values)
                {
                    if (Math.Round(value, digits) != value)
                    {
                        allSatisfied = false;
                        digits++;
                        break;
                    }
                }

                if (allSatisfied)
                {
                    return digits;
                }
            }
            return digits;
        }
    }
}
