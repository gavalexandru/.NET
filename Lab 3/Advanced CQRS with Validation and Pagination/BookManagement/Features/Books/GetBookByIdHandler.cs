using Microsoft.EntityFrameworkCore;
using BookManagement.Persistence;
using BookManagement.Features.Books;
using Microsoft.AspNetCore.Http;

namespace BookManagement.Features.Books;

public class GetBookByIdHandler(BookManagementContext dbContext)
{
    private readonly BookManagementContext _dbContext = dbContext;

    public async Task<IResult> Handle(GetBookByIdRequest request)
    {
        var book = await _dbContext.Books.FirstOrDefaultAsync(b => b.Id == request.Id);
        if (book == null)
        {
            return Results.NotFound();
        }
        return Results.Ok(book);
    }
}