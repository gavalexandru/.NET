using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order_Management_API.Common.Logging;
using Order_Management_API.Features.Orders.DTOs;
using Order_Management_API.Persistence;
using Order_Management_API.Services;

namespace Order_Management_API.Features.Orders;

public class BatchCreateOrderHandler
{
    private readonly OrderManagementContext _context;
    private readonly IValidator<CreateOrderProfileRequest> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<BatchCreateOrderHandler> _logger;
    private readonly OrderMetricsStore _metricsStore;

    public BatchCreateOrderHandler(
        OrderManagementContext context,
        IValidator<CreateOrderProfileRequest> validator,
        IMapper mapper,
        ILogger<BatchCreateOrderHandler> logger,
        OrderMetricsStore metricsStore)
    {
        _context = context;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
        _metricsStore = metricsStore;
    }

    public async Task<IResult> Handle(List<CreateOrderProfileRequest> requests)
    {
        var batchId = Guid.NewGuid();
        var response = new BatchOrderResponse 
        { 
            BatchId = batchId, 
            TotalRequested = requests.Count 
        };

        using var scope = _logger.BeginScope("BatchId: {BatchId}", batchId);
        _logger.LogInformation("Starting batch processing for {Count} orders", requests.Count);

        // 1. Use Transaction for Atomicity
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var ordersToAdd = new List<Order>();

            foreach (var request in requests)
            {
                // 2. Validate Item
                var validationResult = await _validator.ValidateAsync(request);
                
                if (!validationResult.IsValid)
                {
                    response.FailedCount++;
                    response.Results.Add(new BatchItemResult 
                    { 
                        Title = request.Title, 
                        Success = false, 
                        Message = "Validation Failed: " + string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)) 
                    });
                    continue; 
                }

                // 3. Check Batch Uniqueness 
                if (ordersToAdd.Any(o => o.ISBN == request.ISBN))
                {
                    response.FailedCount++;
                    response.Results.Add(new BatchItemResult { Title = request.Title, Success = false, Message = "Duplicate ISBN within batch" });
                    continue;
                }

                var order = _mapper.Map<Order>(request);
                ordersToAdd.Add(order);
                
                response.Results.Add(new BatchItemResult 
                { 
                    Title = request.Title, 
                    Success = true, 
                    OrderId = order.Id 
                });
            }

            // 4. Bulk Insert
            if (ordersToAdd.Any())
            {
                await _context.Orders.AddRangeAsync(ordersToAdd);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                response.SuccessCount = ordersToAdd.Count;
                _logger.LogInformation("Batch committed. Success: {Success}, Failed: {Failed}", response.SuccessCount, response.FailedCount);

                // Add Metrics 
                foreach(var order in ordersToAdd)
                {
                    _metricsStore.AddMetric(new OrderCreationMetrics { 
                        OperationId = $"BATCH-{batchId}", OrderTitle = order.Title, ISBN = order.ISBN, Category = order.Category, Success = true, TotalDuration = TimeSpan.Zero 
                    });
                }
            }
            else
            {
                response.Message = "No valid orders to process.";
            }

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Batch transaction failed.");
            return Results.Problem("Batch processing failed due to an internal error.");
        }
    }
}



public class BatchOrderResponse
{
    public Guid BatchId { get; set; }
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public string? Message { get; set; }
    public List<BatchItemResult> Results { get; set; } = new();
}

    
public class BatchItemResult
{
    public required string Title { get; set; } 
    public bool Success { get; set; }
    public Guid? OrderId { get; set; }
    public string? Message { get; set; }
}

