using BookManagement.Persistence;
using BookManagement.Features.Books;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace BookManagement.Features.Books;

public class CreateBookHandler(BookManagementContext dbContext, IValidator<CreateBookRequest> validator)
{
    private readonly BookManagementContext _dbContext = dbContext;
    private readonly IValidator<CreateBookRequest> _validator = validator;

    public async Task<IResult> Handle(CreateBookRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var book = new Book(0, request.Title, request.Author, request.Year);

        _dbContext.Books.Add(book);
        await _dbContext.SaveChangesAsync();

        return Results.Created($"/books/{book.Id}", book);
    }
}