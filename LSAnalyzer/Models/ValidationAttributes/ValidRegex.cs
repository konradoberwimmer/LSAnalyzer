using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace LSAnalyzer.Models.ValidationAttributes
{
    public sealed class ValidRegex : ValidationAttribute
    {
        private string _errorMessage;

        public ValidRegex(string errorMessage)
        {
            _errorMessage = errorMessage;
        }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || value.ToString()!.Length == 0)
            {
                return ValidationResult.Success!;
            }

            var pattern = value.ToString()!;

            try
            {
                Regex.Match("", pattern);
            }
            catch (ArgumentException)
            {
                return new ValidationResult(_errorMessage, new List<string> { validationContext.MemberName! });
            }

            return ValidationResult.Success!;
        }
    }
}
