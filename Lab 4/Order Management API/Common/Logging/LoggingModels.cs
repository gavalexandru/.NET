namespace Order_Management_API.Common.Logging;
using Order_Management_API.Features.Orders;

public static class LogEvents
{
    public const int OrderCreationStarted = 2001;
    public const int OrderValidationFailed = 2002;
    public const int OrderCreationCompleted = 2003;
    public const int DatabaseOperationStarted = 2004;
    public const int DatabaseOperationCompleted = 2005;
    public const int CacheOperationPerformed = 2006;
    public const int ISBNValidationPerformed = 2007;
    public const int StockValidationPerformed = 2008;
}

public record OrderCreationMetrics
{
    
    public required string OperationId { get; init; }
    public required string OrderTitle { get; init; }
    public required string ISBN { get; init; }
    public OrderCategory Category { get; init; }
    public TimeSpan ValidationDuration { get; init; }
    public TimeSpan DatabaseSaveDuration { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public bool Success { get; init; }
    public string? ErrorReason { get; init; }
}