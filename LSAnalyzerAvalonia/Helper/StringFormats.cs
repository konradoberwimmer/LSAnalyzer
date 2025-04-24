using System;

namespace LSAnalyzerAvalonia.Helper
{
    public class StringFormats
    {
        public static int GetMaxRelevantDigits(double[] values, int maxDigits = 3)
        {
            var digits = 0;
            while (digits < maxDigits)
            {
                var allSatisfied = true;

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

        /**
         * Wraps ^ and $ around regex if encapsulate = true.
         */
        public static string? EncapsulateRegex(string? regex, bool encapsulate = true)
        {
            if (regex == null || !encapsulate)
            {
                return regex;
            }

            var encapsulatedRegex = regex;

            if (!regex.StartsWith("^"))
            {
                encapsulatedRegex = "^" + encapsulatedRegex;
            }

            if (!regex.EndsWith("$"))
            {
                encapsulatedRegex += "$";
            }

            return encapsulatedRegex;
        }
    }
}
