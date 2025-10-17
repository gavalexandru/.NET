using Microsoft.EntityFrameworkCore;
using BookManagement.Persistence;
using BookManagement.Features.Books;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace BookManagement.Features.Books;

public class UpdateBookHandler(BookManagementContext dbContext, IValidator<UpdateBookRequest> validator)
{
    private readonly BookManagementContext _dbContext = dbContext;
    private readonly IValidator<UpdateBookRequest> _validator = validator;

    public async Task<IResult> Handle(UpdateBookRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var book = await _dbContext.Books.FirstOrDefaultAsync(b => b.Id == request.Id);
        if (book == null)
        {
            return Results.NotFound();
        }

        var updatedBook = book with { Title = request.Title, Author = request.Author, Year = request.Year };

        _dbContext.Entry(book).CurrentValues.SetValues(updatedBook);
        await _dbContext.SaveChangesAsync();

        return Results.Ok(updatedBook);
    }
}