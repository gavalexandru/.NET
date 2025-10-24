using FluentValidation;
using Microsoft.AspNetCore.Http; 
using Microsoft.AspNetCore.OpenApi; 
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Order_Management_API.Common.Mapping;
using Order_Management_API.Common.Middleware;
using Order_Management_API.Features.Orders;
using Order_Management_API.Features.Orders.DTOs;
using Order_Management_API.Persistence;
using Order_Management_API.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<OrderManagementContext>(options =>
    options.UseInMemoryDatabase("OrderManagementDb"));
    
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderProfileValidator>();
builder.Services.AddAutoMapper(typeof(AdvancedOrderMappingProfile));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Order Management API",
        Version = "v1",
        Description = "An advanced .NET exercise API for managing orders."
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Management API V1");
        c.RoutePrefix = string.Empty;
    });
}

// Use correlation ID middleware
app.UseMiddleware<CorrelationMiddleware>();

app.UseHttpsRedirection();

// Map Order Endpoints
app.MapPost("/orders", async (CreateOrderProfileRequest req, CreateOrderHandler handler) => await handler.Handle(req))
    .WithName("CreateOrder")
    .WithOpenApi(operation => new(operation) {
        Summary = "Creates a new order.",
        Description = "Creates a new order with the provided details after extensive validation."
    });

app.MapGet("/orders/{id:guid}", async (Guid id, OrderManagementContext context) =>
    await context.Orders.FindAsync(id) is Order order
        ? Results.Ok(order)
        : Results.NotFound())
    .WithName("GetOrderById")
    .WithOpenApi(operation => new(operation) {
        Summary = "Gets an order by its unique ID.",
        Description = "Retrieves the details of a specific order."
    });

app.MapGet("/orders", async (OrderManagementContext context) =>
    await context.Orders.ToListAsync())
    .WithName("GetAllOrders")
    .WithOpenApi(operation => new(operation) {
        Summary = "Gets all orders.",
        Description = "Retrieves a list of all orders in the system."
    });
    

app.Run();


public partial class Program { }