using Order_Management_API.Common.Logging;
using Microsoft.Extensions.Logging;
namespace Order_Management_API.Common.Logging;


public static class LoggingExtensions
{
   
    public static void LogOrderCreationMetrics(this ILogger logger, OrderCreationMetrics metrics)
    {
        var logLevel = metrics.Success ? LogLevel.Information : LogLevel.Warning;
        var eventId = metrics.Success ? LogEvents.OrderCreationCompleted : LogEvents.OrderValidationFailed;

        logger.Log(logLevel, eventId,
            "Order creation metrics for OperationId {OperationId}: " +
            "Title='{OrderTitle}', ISBN='{ISBN}', Category='{Category}', " +
            "ValidationMs={ValidationMs}, DatabaseMs={DatabaseMs}, TotalMs={TotalMs}, " +
            "Success={Success}, ErrorReason='{ErrorReason}'",
            metrics.OperationId,
            metrics.OrderTitle,
            metrics.ISBN,
            metrics.Category,
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.Success,
            metrics.ErrorReason ?? "N/A"
        );
    }
}