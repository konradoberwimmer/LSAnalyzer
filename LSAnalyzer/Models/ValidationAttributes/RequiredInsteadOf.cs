using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Models.ValidationAttributes
{
    public sealed class RequiredInsteadOf : ValidationAttribute
    {
        private string _errorMessage;

        public RequiredInsteadOf(string propertyName, string errorMessage)
        {
            PropertyName = propertyName;
            _errorMessage = errorMessage;
        }

        public string PropertyName { get; }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            if (value != null && value.ToString()!.Length > 0)
            {
                return ValidationResult.Success!;
            }

            var instance = validationContext.ObjectInstance;
            var otherValue = instance.GetType().GetProperty(PropertyName)!.GetValue(instance);

            if (otherValue == null || otherValue.ToString()!.Length == 0)
            {
                return new ValidationResult(_errorMessage, new List<string> { validationContext.MemberName! });
            }

            return ValidationResult.Success!;
        }
    }
}
