using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LSAnalyzer.Models.ValidationAttributes
{
    public sealed class MutuallyExclusive : ValidationAttribute
    {
        private string _errorMessage;

        public MutuallyExclusive(string propertyName, string errorMessage)
        {
            PropertyName = propertyName;
            _errorMessage = errorMessage;
        }

        public string PropertyName { get; }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || value.ToString()!.Length == 0)
            {
                return ValidationResult.Success!;
            }

            var instance = validationContext.ObjectInstance;
            var otherValue = instance.GetType().GetProperty(PropertyName)!.GetValue(instance);

            if (otherValue == null || otherValue.ToString()!.Length == 0)
            {
                return ValidationResult.Success!;
            }

            return new ValidationResult(_errorMessage, new List<string> { validationContext.MemberName! });
        }
    }
}
