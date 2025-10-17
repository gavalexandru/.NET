namespace BookManagement.Features.Books;

public record GetAllBooksRequest(int Page = 1, int PageSize = 10);