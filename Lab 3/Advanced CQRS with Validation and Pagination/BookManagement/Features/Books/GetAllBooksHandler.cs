using Microsoft.EntityFrameworkCore;
using BookManagement.Persistence;
using BookManagement.Features.Books;
using Microsoft.AspNetCore.Http;

namespace BookManagement.Features.Books;

public class GetAllBooksHandler(BookManagementContext context)
{
    private readonly BookManagementContext _context = context;

    public async Task<IResult> Handle(GetAllBooksRequest request)
    {
        var books = await _context.Books
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();
        return Results.Ok(books);
    }
}