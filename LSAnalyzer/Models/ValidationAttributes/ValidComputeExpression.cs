using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LSAnalyzer.Models.ValidationAttributes;

public class ValidComputeExpression : ValidationAttribute
{
    private readonly string _errorMessage;
    
    public ValidComputeExpression(string errorMessage)
    {
        _errorMessage = errorMessage;
    }

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string || validationContext.ObjectInstance is not VirtualVariableCompute virtualVariableCompute)
        {
            return new ValidationResult(_errorMessage, new List<string> { validationContext.MemberName! });
        }
        
        return 
            virtualVariableCompute.ValidExpression ? 
                ValidationResult.Success! : 
                new ValidationResult(_errorMessage, new List<string> { validationContext.MemberName! });
    }
}