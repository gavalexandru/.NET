using FluentValidation;
using Microsoft.EntityFrameworkCore;
using BookManagement.Features.Books;
using BookManagement.Persistence;
using BookManagement.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<BookManagementContext>(options =>
    options.UseSqlite("Data Source=bookmanagement.db"));
builder.Services.AddScoped<CreateBookHandler>();
builder.Services.AddScoped<GetAllBooksHandler>();
builder.Services.AddScoped<GetBookByIdHandler>();
builder.Services.AddScoped<UpdateBookHandler>();
builder.Services.AddScoped<DeleteBookHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateBookValidator>();

var app = builder.Build();

// Ensure the database is created at runtime
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BookManagementContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/books", async (CreateBookRequest req, CreateBookHandler handler) =>
    await handler.Handle(req));

app.MapGet("/books", async (int? page, int? pageSize, GetAllBooksHandler handler) =>
    await handler.Handle(new GetAllBooksRequest(page ?? 1, pageSize ?? 10)));

app.MapGet("/books/{id:int}", async (int id, GetBookByIdHandler handler) =>
    await handler.Handle(new GetBookByIdRequest(id)));

app.MapPut("/books/{id:int}", async (int id, UpdateBookPayload payload, UpdateBookHandler handler) =>
{
    var request = new UpdateBookRequest(id, payload.Title, payload.Author, payload.Year);
    return await handler.Handle(request);
});

app.MapDelete("/books/{id:int}", async (int id, DeleteBookHandler handler) =>
{
    return await handler.Handle(new DeleteBookRequest(id));
});

app.Run();