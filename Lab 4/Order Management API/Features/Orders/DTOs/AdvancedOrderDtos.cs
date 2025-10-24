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
    public string Title { get; set; }
    public string Author { get; set; }
    public string ISBN { get; set; }
    public string CategoryDisplayName { get; set; }
    public decimal Price { get; set; }
    public string FormattedPrice { get; set; }
    public DateTime PublishedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; }
    public string PublishedAge { get; set; }
    public string AuthorInitials { get; set; }
    public string AvailabilityStatus { get; set; }
}