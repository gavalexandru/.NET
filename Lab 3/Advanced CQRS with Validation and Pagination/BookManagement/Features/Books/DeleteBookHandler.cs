using Microsoft.EntityFrameworkCore;
using BookManagement.Persistence;
using BookManagement.Features.Books;
using Microsoft.AspNetCore.Http;

namespace BookManagement.Features.Books;

public class DeleteBookHandler(BookManagementContext dbContext)
{
    private readonly BookManagementContext _dbContext = dbContext;

    public async Task<IResult> Handle(DeleteBookRequest request)
    {
        var book = await _dbContext.Books.FirstOrDefaultAsync(b => b.Id == request.Id);
        if (book == null)
        {
            return Results.NotFound();
        }

        _dbContext.Books.Remove(book);
        await _dbContext.SaveChangesAsync();
        return Results.NoContent();
    }
}