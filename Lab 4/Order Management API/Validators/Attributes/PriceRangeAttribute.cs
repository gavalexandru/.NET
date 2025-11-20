using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Order_Management_API.Validators.Attributes;

public class PriceRangeAttribute : ValidationAttribute
{
    private readonly decimal _min;
    private readonly decimal _max;

    public PriceRangeAttribute(double min, double max)
    {
        _min = (decimal)min;
        _max = (decimal)max;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is decimal price)
        {
            if (price >= _min && price <= _max)
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage ?? $"Price must be between {_min.ToString("C", CultureInfo.CreateSpecificCulture("en-US"))} and {_max.ToString("C", CultureInfo.CreateSpecificCulture("en-US"))}.");
        }

        return new ValidationResult("Invalid price format.");
    }
}