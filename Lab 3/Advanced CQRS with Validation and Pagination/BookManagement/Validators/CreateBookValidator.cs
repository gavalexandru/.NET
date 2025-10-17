using FluentValidation;
using BookManagement.Features.Books;

namespace BookManagement.Validators;

public class CreateBookValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookValidator()
    {
        RuleFor(x => x.Title).NotNull().NotEmpty().WithMessage("Title is required.");
        RuleFor(x => x.Author).NotNull().NotEmpty().WithMessage("Author is required.");
        RuleFor(x => x.Year).InclusiveBetween(1000, DateTime.UtcNow.Year).WithMessage("Please provide a valid year.");
    }
}