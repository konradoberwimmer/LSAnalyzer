using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LSAnalyzerAvalonia.Helper;

namespace LSAnalyzerAvalonia.Models.ValidationAttributes;

public sealed class ValidItems : ValidationAttribute
{
    private string _errorMessage;

    public ValidItems(string errorMessage)
    {
        _errorMessage = errorMessage;
    }

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not ICollection collection)
        {
            return ValidationResult.Success!;
        }

        foreach (var item in collection)
        {
            if (item is ObservableValidatorExtended validator) 
            {
                validator.Validate();
                if (validator.Errors.Any())
                {
                    return new ValidationResult(_errorMessage, new List<string> { validationContext.MemberName! });
                }
            }
        }

        return ValidationResult.Success!;
    }
}
