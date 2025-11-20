using FluentValidation;
using Microsoft.AspNetCore.Http; 
using Microsoft.AspNetCore.OpenApi; 
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc; 
using Microsoft.AspNetCore.Localization; 
using Microsoft.Extensions.Localization; 
using Order_Management_API.Common.Localization; 
using Order_Management_API.Common.Mapping;
using Order_Management_API.Common.Middleware;
using Order_Management_API.Features.Orders;
using Order_Management_API.Features.Orders.DTOs;
using Order_Management_API.Persistence;
using Order_Management_API.Validators;
using Order_Management_API.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Localization Setup  ---
builder.Services.AddLocalization();
builder.Services.AddSingleton<IStringLocalizer<SharedResource>, MockStringLocalizer>();

builder.Services.AddDbContext<OrderManagementContext>(options =>
    options.UseInMemoryDatabase("OrderManagementDb"));
    
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<OrderMetricsStore>();

// --- 2. Handler Registration  ---
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<GetOrdersByCategoryHandler>();
builder.Services.AddScoped<BatchCreateOrderHandler>(); // Register the new Batch Handler

builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderProfileValidator>();

// --- 3. AutoMapper & Resolver Registration  ---
// Localized resolvers must be registered in DI so AutoMapper can inject IStringLocalizer
builder.Services.AddTransient<LocalizedCategoryDisplayResolver>();
builder.Services.AddTransient<LocalizedAvailabilityStatusResolver>();
builder.Services.AddAutoMapper(typeof(AdvancedOrderMappingProfile));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Order Management API",
        Version = "v1",
        Description = "Advanced .NET API with Batching, Localization, and Metrics."
    });
});

var app = builder.Build();

// --- 4. Localization Middleware  ---
var supportedCultures = new[] { "en-US", "es-ES" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Management API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseMiddleware<CorrelationMiddleware>();
app.UseHttpsRedirection();

app.MapPost("/orders", async (CreateOrderProfileRequest req, CreateOrderHandler handler) => await handler.Handle(req))
    .WithName("CreateOrder")
    .WithOpenApi(operation => new(operation) {
        Summary = "Creates a new order.",
        Description = "Creates a new order with the provided details after extensive validation."
    });

// --- 5. Batch Endpoint  ---
app.MapPost("/orders/batch", async ([FromBody] List<CreateOrderProfileRequest> req, BatchCreateOrderHandler handler) => 
    await handler.Handle(req))
    .WithName("BatchCreateOrders")
    .WithOpenApi(operation => new(operation) {
        Summary = "Batch create orders (Transactional)",
        Description = "Accepts an array of orders. Uses transactions to ensure atomicity for valid items."
    });

app.MapGet("/orders/{id:guid}", async (Guid id, OrderManagementContext context, AutoMapper.IMapper mapper) =>
    await context.Orders.FindAsync(id) is Order order
        ? Results.Ok(mapper.Map<OrderProfileDto>(order))
        : Results.NotFound())
    .WithName("GetOrderById")
    .WithOpenApi(operation => new(operation) {
        Summary = "Gets an order by its unique ID.",
        Description = "Retrieves the details of a specific order."
    });

app.MapGet("/orders", async (OrderManagementContext context, AutoMapper.IMapper mapper) =>
    Results.Ok(mapper.Map<List<OrderProfileDto>>(await context.Orders.ToListAsync())))
    .WithName("GetAllOrders")
    .WithOpenApi(operation => new(operation) {
        Summary = "Gets all orders.",
        Description = "Retrieves a list of all orders in the system."
    });

app.MapGet("/orders/category/{category}", async (OrderCategory category, GetOrdersByCategoryHandler handler) => 
    await handler.Handle(category))
    .WithName("GetOrdersByCategory")
    .WithOpenApi(operation => new(operation) {
        Summary = "Gets orders by category with caching.",
        Description = "Returns orders filtered by category. Results are cached for 5 minutes."
    });

app.MapGet("/admin/metrics", (OrderMetricsStore store) => 
    Results.Ok(store.GetDashboardMetrics()))
    .WithName("GetOrderMetrics")
    .WithOpenApi(operation => new(operation) {
        Summary = "Gets real-time order metrics.",
        Description = "Returns aggregated performance and success metrics for the dashboard."
    });

app.Run();

public partial class Program { }