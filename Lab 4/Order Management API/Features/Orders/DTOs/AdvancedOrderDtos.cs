namespace Order_Management_API.Features.Orders.DTOs;
using Order_Management_API.Features.Orders;

public record CreateOrderProfileRequest(
    string Title,
    string Author,
    string ISBN,
    OrderCategory Category,
    decimal Price,
    DateTime PublishedDate,
    string? CoverImageUrl,
    int StockQuantity = 1
);

public record OrderProfileDto
{
    public Guid Id { get; set; }
  
    public required string Title { get; set; }
    public required string Author { get; set; }
    public required string ISBN { get; set; }
    public required string CategoryDisplayName { get; set; }
    public decimal Price { get; set; }
    public required string FormattedPrice { get; set; }
    public DateTime PublishedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; }
    public required string PublishedAge { get; set; }
    public required string AuthorInitials { get; set; }
    public required string AvailabilityStatus { get; set; }
}