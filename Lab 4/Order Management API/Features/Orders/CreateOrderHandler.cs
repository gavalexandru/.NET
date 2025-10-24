using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Order_Management_API.Features.Orders;

using System.Diagnostics;
using AutoMapper;
using FluentValidation;
using Order_Management_API.Common.Logging;
using Order_Management_API.Features.Orders.DTOs;
using Order_Management_API.Persistence;

public class CreateOrderHandler
{
    private readonly OrderManagementContext _context;
    private readonly ILogger<CreateOrderHandler> _logger;
    private readonly IValidator<CreateOrderProfileRequest> _validator;
    private readonly IMapper _mapper;

    public CreateOrderHandler(OrderManagementContext context, ILogger<CreateOrderHandler> logger, IValidator<CreateOrderProfileRequest> validator, IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _validator = validator;
        _mapper = mapper;
    }

    public async Task<IResult> Handle(CreateOrderProfileRequest request)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var totalStopwatch = Stopwatch.StartNew();
        var validationStopwatch = new Stopwatch();
        var dbStopwatch = new Stopwatch();

        using var scope = _logger.BeginScope("OperationId: {OperationId}, Title: {Title}, Author: {Author}, ISBN: {ISBN}", 
            operationId, request.Title, request.Author, request.ISBN);
        
        _logger.LogInformation(LogEvents.OrderCreationStarted, "Starting order creation process.");
        
        OrderCreationMetrics metrics;

        try
        {
            validationStopwatch.Start();
            var validationResult = await _validator.ValidateAsync(request);
            validationStopwatch.Stop();

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning(LogEvents.OrderValidationFailed, "Order validation failed: {Errors}", string.Join(", ", errors));
                
                metrics = new OrderCreationMetrics { 
                    OperationId = operationId, OrderTitle = request.Title, ISBN = request.ISBN, Category = request.Category,
                    ValidationDuration = validationStopwatch.Elapsed, TotalDuration = totalStopwatch.Elapsed, Success = false, ErrorReason = "Validation failed" 
                };
                _logger.LogOrderCreationMetrics(metrics);

                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var order = _mapper.Map<Order>(request);

            dbStopwatch.Start();
            _logger.LogInformation(LogEvents.DatabaseOperationStarted, "Starting database operation to save new order.");
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            dbStopwatch.Stop();
            _logger.LogInformation(LogEvents.DatabaseOperationCompleted, "Database operation completed for OrderId {OrderId}.", order.Id);

            _logger.LogInformation(LogEvents.CacheOperationPerformed, "Cache invalidation signal sent for key 'all_orders'.");

            totalStopwatch.Stop();
            
            metrics = new OrderCreationMetrics { 
                OperationId = operationId, OrderTitle = request.Title, ISBN = request.ISBN, Category = request.Category,
                ValidationDuration = validationStopwatch.Elapsed, DatabaseSaveDuration = dbStopwatch.Elapsed, TotalDuration = totalStopwatch.Elapsed, Success = true 
            };
            _logger.LogOrderCreationMetrics(metrics);

            var resultDto = _mapper.Map<OrderProfileDto>(order);
            return Results.Created($"/orders/{order.Id}", resultDto);
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            _logger.LogError(ex, "An unhandled exception occurred during order creation.");

            metrics = new OrderCreationMetrics { 
                OperationId = operationId, OrderTitle = request.Title, ISBN = request.ISBN, Category = request.Category,
                ValidationDuration = validationStopwatch.Elapsed, DatabaseSaveDuration = dbStopwatch.Elapsed, TotalDuration = totalStopwatch.Elapsed, 
                Success = false, ErrorReason = ex.Message 
            };
             _logger.LogOrderCreationMetrics(metrics);
            
            // Re-throw for global exception handler
            throw;
        }
    }
}