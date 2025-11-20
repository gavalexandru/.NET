using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Order_Management_API.Validators.Attributes;

public class ValidISBNAttribute : ValidationAttribute, IClientModelValidator
{
    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes.Add("data-val", "true");
        context.Attributes.Add("data-val-isbn", ErrorMessage ?? "Invalid ISBN format.");
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string isbn)
        {
            return ValidationResult.Success; // Allow null/empty here, use [Required] for that
        }

        var sanitized = isbn.Replace("-", "").Replace(" ", "");
        
        if ((sanitized.Length == 10 || sanitized.Length == 13) && sanitized.All(char.IsDigit))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? "ISBN must be 10 or 13 digits.");
    }
}