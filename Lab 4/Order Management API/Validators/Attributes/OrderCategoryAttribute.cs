using System.ComponentModel.DataAnnotations;
using Order_Management_API.Features.Orders;

namespace Order_Management_API.Validators.Attributes;

public class OrderCategoryAttribute : ValidationAttribute
{
    private readonly OrderCategory[] _allowedCategories;

    public OrderCategoryAttribute(params OrderCategory[] allowedCategories)
    {
        _allowedCategories = allowedCategories;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is OrderCategory category)
        {
            if (_allowedCategories.Contains(category))
            {
                return ValidationResult.Success;
            }
            
            return new ValidationResult($"Category '{category}' is not allowed. Allowed values: {string.Join(", ", _allowedCategories)}.");
        }

        return new ValidationResult("Invalid category format.");
    }
}