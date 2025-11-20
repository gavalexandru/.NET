using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics; 
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Order_Management_API.Common.Logging;
using Order_Management_API.Features.Orders;
using Order_Management_API.Features.Orders.DTOs;
using Order_Management_API.Persistence;
using Order_Management_API.Services; 
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Order_Management_API.Tests;

public class CreateOrderHandlerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly OrderManagementContext _context;
    private readonly HttpClient _client;
    private readonly Mock<ILogger<CreateOrderHandler>> _mockLogger;

    public CreateOrderHandlerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = Guid.NewGuid().ToString();
        _factory = factory.WithWebHostBuilder(builder =>
        {
			builder.UseSetting("https_port", "7100");
			
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<OrderManagementContext>));
                services.AddDbContext<OrderManagementContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName)
                           // Allows BatchHandler to call BeginTransactionAsync without crashing
                           .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });

                // Remove existing logger and replace with Mock
                services.RemoveAll(typeof(ILogger<CreateOrderHandler>));
                var mockLogger = new Mock<ILogger<CreateOrderHandler>>();
                services.AddSingleton(mockLogger);
                services.AddSingleton(mockLogger.Object);
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<OrderManagementContext>();
        _mockLogger = _scope.ServiceProvider.GetRequiredService<Mock<ILogger<CreateOrderHandler>>>();
    }

    // --- MANDATORY TESTS  ---

    [Fact] 
    public async Task Handle_ValidTechnicalOrderRequest_CreatesOrderWithCorrectMapping()
    {
        var request = new CreateOrderProfileRequest(
            "Advanced C# Programming", "John Doe", "978-0134491220", OrderCategory.Technical, 
            59.99m, DateTime.UtcNow.AddMonths(-6), null, 10
        );
        
        var response = await _client.PostAsJsonAsync("/orders", request);

        response.EnsureSuccessStatusCode();
        var resultDto = await response.Content.ReadFromJsonAsync<OrderProfileDto>();

        Assert.NotNull(resultDto);
        Assert.Equal("Technical & Professional", resultDto.CategoryDisplayName);
        Assert.Equal("JD", resultDto.AuthorInitials); 
        Assert.StartsWith("$", resultDto.FormattedPrice); 
        
        _mockLogger.Verify(x => x.Log(
                LogLevel.Information,
                new EventId(LogEvents.OrderCreationStarted, null),
                It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
    
    [Fact]
    public async Task Handle_DuplicateISBN_ThrowsValidationExceptionWithLogging()
    {
        var existingIsbn = "978-1491904244";
        var existingOrder = new Order
        {
            Id = Guid.NewGuid(), Title = "Existing Book", Author = "Jane Smith", ISBN = existingIsbn, 
            Category = OrderCategory.Fiction, Price = 19.99m, StockQuantity = 5, IsAvailable = true,
            PublishedDate = DateTime.UtcNow.AddYears(-2), CreatedAt = DateTime.UtcNow
        };
        await _context.Orders.AddAsync(existingOrder);
        await _context.SaveChangesAsync();

        var request = new CreateOrderProfileRequest(
            "New Book", "Author", existingIsbn, OrderCategory.NonFiction, 
            29.99m, DateTime.UtcNow.AddMonths(-1), null, 2
        );

        var response = await _client.PostAsJsonAsync("/orders", request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var validationProblem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        
        Assert.NotNull(validationProblem);
        if (validationProblem!.Errors.TryGetValue("ISBN", out var errors))
        {
            Assert.Contains("already exists", errors.First());
        }
    }
    
    [Fact]
    public async Task Handle_ChildrensOrderRequest_AppliesDiscountAndConditionalMapping()
    {
        var request = new CreateOrderProfileRequest(
            "Kids Story", "Emily", "978-0399555537", OrderCategory.Children, 
            15.00m, DateTime.UtcNow.AddDays(-10), "http://img.com/a.jpg", 3
        );
        
        var response = await _client.PostAsJsonAsync("/orders", request);

        response.EnsureSuccessStatusCode();
        var resultDto = await response.Content.ReadFromJsonAsync<OrderProfileDto>();

        Assert.NotNull(resultDto);
        Assert.Equal("Children's Orders", resultDto.CategoryDisplayName);
        Assert.Equal(13.50m, resultDto.Price); // 10% discount
        Assert.Null(resultDto.CoverImageUrl); 
    }

    // --- BONUS CHALLENGE TESTS ---

    [Fact]
    public async Task Bonus_Handle_BatchOrders_ProcessValidAndInvalidItems()
    {
        // Arrange: One valid, one invalid (price too high)
        var requests = new List<CreateOrderProfileRequest>
        {
            new("Batch Book 1", "Author A", "978-1111111111", OrderCategory.Fiction, 20m, DateTime.UtcNow.AddYears(-1), null, 5),
            new("Batch Book 2", "Author B", "978-2222222222", OrderCategory.Children, 200m, DateTime.UtcNow.AddYears(-1), null, 5) // Invalid Price > 50
        };

        // Act
        var response = await _client.PostAsJsonAsync("/orders/batch", requests);

        // Assert
        response.EnsureSuccessStatusCode();
        var batchResult = await response.Content.ReadFromJsonAsync<BatchOrderResponse>();
        
        Assert.NotNull(batchResult);
        Assert.Equal(2, batchResult.TotalRequested);
        Assert.Equal(1, batchResult.SuccessCount); // Only one should succeed
        Assert.Equal(1, batchResult.FailedCount);
    }

    [Fact]
    public async Task Bonus_Metrics_Endpoint_ReturnsData()
    {
        // Arrange: Create an order first to populate metrics
        var request = new CreateOrderProfileRequest("Metrics Book", "Auth", "978-3333333333", OrderCategory.Technical, 30m, DateTime.UtcNow.AddYears(-1), null, 5);
        await _client.PostAsJsonAsync("/orders", request);

        // Act
        var response = await _client.GetAsync("/admin/metrics");

        // Assert
        response.EnsureSuccessStatusCode();
        var metrics = await response.Content.ReadFromJsonAsync<OrderMetricsDashboardDto>();
        Assert.NotNull(metrics);
        Assert.True(metrics.TotalOrdersProcessed > 0);
    }

    [Fact]
    public async Task Bonus_Localization_ReturnsSpanish_WhenHeaderSet()
    {
        // Arrange
        var request = new CreateOrderProfileRequest("Spanish Book", "Auth", "978-4444444444", OrderCategory.Fiction, 30m, DateTime.UtcNow.AddYears(-1), null, 5);
        var createResponse = await _client.PostAsJsonAsync("/orders", request);
        var order = await createResponse.Content.ReadFromJsonAsync<OrderProfileDto>();

        // Act: Request the created order with Spanish header
        _client.DefaultRequestHeaders.Add("Accept-Language", "es-ES");
        var response = await _client.GetAsync($"/orders/{order!.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var resultDto = await response.Content.ReadFromJsonAsync<OrderProfileDto>();
        
        // Check for Spanish translation defined in MockStringLocalizer
        Assert.Equal("Ficción y Literatura", resultDto!.CategoryDisplayName); 
        
        // Reset header for other tests
        _client.DefaultRequestHeaders.Remove("Accept-Language");
    }

    [Fact]
    public async Task Bonus_Caching_ReturnsResults()
    {
        // Arrange
        var request = new CreateOrderProfileRequest("Cached Book", "Auth", "978-5555555555", OrderCategory.NonFiction, 30m, DateTime.UtcNow.AddYears(-1), null, 5);
        await _client.PostAsJsonAsync("/orders", request);

        // Act
        var response = await _client.GetAsync("/orders/category/NonFiction");

        // Assert
        response.EnsureSuccessStatusCode();
        var orders = await response.Content.ReadFromJsonAsync<List<OrderProfileDto>>();
        Assert.NotNull(orders);
        Assert.Contains(orders, o => o.Title == "Cached Book");
    }

    public void Dispose()
    {
        _scope.Dispose();
        _client.Dispose();
    }
}