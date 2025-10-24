using Microsoft.AspNetCore.Http;

namespace Order_Management_API.Tests;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Order_Management_API.Common.Logging;
using Order_Management_API.Features.Orders;
using Order_Management_API.Features.Orders.DTOs;
using Order_Management_API.Persistence;
using Xunit;
using Microsoft.Extensions.Logging;

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
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<OrderManagementContext>));
                services.AddDbContext<OrderManagementContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });

                var mockLoggerDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ILogger<CreateOrderHandler>));
                if(mockLoggerDescriptor != null)
                {
                    services.Remove(mockLoggerDescriptor);
                }
                var mockLogger = new Mock<ILogger<CreateOrderHandler>>();
                services.AddSingleton(mockLogger.Object);
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<OrderManagementContext>();
        _mockLogger = _scope.ServiceProvider.GetRequiredService<Mock<ILogger<CreateOrderHandler>>>();
    }
    
    public async Task Handle_ValidTechnicalOrderRequest_CreatesOrderWithCorrectMapping()
    {
        // Arrange
        var request = new CreateOrderProfileRequest(
            "Advanced C# Programming", "John Doe", "978-0134491220", OrderCategory.Technical, 
            59.99m, DateTime.UtcNow.AddMonths(-6), null, 10
        );
        
        // Act
        var response = await _client.PostAsJsonAsync("/orders", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var resultDto = await response.Content.ReadFromJsonAsync<OrderProfileDto>();

        Assert.NotNull(resultDto);
        Assert.Equal("Technical & Professional", resultDto.CategoryDisplayName);
        Assert.Equal("JD", resultDto.AuthorInitials); // Two-name author
        Assert.Equal("6 months old", resultDto.PublishedAge); // Check age calculation
        Assert.StartsWith("$", resultDto.FormattedPrice); // Check currency symbol
        Assert.Equal("In Stock", resultDto.AvailabilityStatus); // Based on stock > 5
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                new EventId(LogEvents.OrderCreationStarted, null),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_DuplicateISBN_ThrowsValidationExceptionWithLogging()
    {
        // Arrange: Create existing order in the database
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
            "New Book with Same ISBN", "Some Author", existingIsbn, OrderCategory.NonFiction, 
            29.99m, DateTime.UtcNow.AddMonths(-1), null, 2
        );

        // Act
        var response = await _client.PostAsJsonAsync("/orders", request);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var validationProblem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Contains("already exists", validationProblem.Errors["ISBN"].First());

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                new EventId(LogEvents.OrderValidationFailed, null),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Order validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_ChildrensOrderRequest_AppliesDiscountAndConditionalMapping()
    {
        // Arrange
        var request = new CreateOrderProfileRequest(
            "A Fun Children's Story", "Emily Jones", "978-0399555537", OrderCategory.Children, 
            15.00m, DateTime.UtcNow.AddDays(-10), "http://example.com/image.jpg", 3
        );
        
        // Act
        var response = await _client.PostAsJsonAsync("/orders", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var resultDto = await response.Content.ReadFromJsonAsync<OrderProfileDto>();

        Assert.NotNull(resultDto);
        Assert.Equal("Children's Orders", resultDto.CategoryDisplayName);
        Assert.Equal(13.50m, resultDto.Price); // Check 10% discount (15.00 * 0.9)
        Assert.Null(resultDto.CoverImageUrl); // Check content filtering
    }

    public void Dispose()
    {
        _scope.Dispose();
        _client.Dispose();
    }
}