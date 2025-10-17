namespace BookManagement.Features.Books;

public record UpdateBookPayload(string Title, string Author, int Year);