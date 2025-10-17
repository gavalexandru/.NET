using FluentValidation;
using BookManagement.Features.Books;

namespace BookManagement.Validators;

public class UpdateBookValidator : AbstractValidator<UpdateBookRequest>
{
    public UpdateBookValidator()
    {
        RuleFor(x => x.Title).NotNull().NotEmpty().WithMessage("Title is required.");
        RuleFor(x => x.Author).NotNull().NotEmpty().WithMessage("Author is required.");
        RuleFor(x => x.Year).InclusiveBetween(1000, DateTime.UtcNow.Year).WithMessage("Please provide a valid year.");
    }
}