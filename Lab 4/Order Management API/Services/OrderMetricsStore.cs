using System.Collections.Concurrent;
using Order_Management_API.Common.Logging;

namespace Order_Management_API.Services;

public class OrderMetricsStore
{
    private readonly ConcurrentBag<OrderCreationMetrics> _metrics = new();

    public void AddMetric(OrderCreationMetrics metric)
    {
        _metrics.Add(metric);
    }

    public OrderMetricsDashboardDto GetDashboardMetrics()
    {
        var allMetrics = _metrics.ToList();
        var totalCount = allMetrics.Count;
        
        if (totalCount == 0) return new OrderMetricsDashboardDto();

        return new OrderMetricsDashboardDto
        {
            TotalOrdersProcessed = totalCount,
            SuccessRate = (double)allMetrics.Count(m => m.Success) / totalCount * 100,
            AverageTotalDurationMs = allMetrics.Average(m => m.TotalDuration.TotalMilliseconds),
            AverageValidationDurationMs = allMetrics.Average(m => m.ValidationDuration.TotalMilliseconds),
            AverageDatabaseDurationMs = allMetrics.Average(m => m.DatabaseSaveDuration.TotalMilliseconds),
            LastErrors = allMetrics
                .Where(m => !m.Success)
                .OrderByDescending(m => m.OperationId)
                .Take(5)
                .Select(m => $"{m.OrderTitle}: {m.ErrorReason}")
                .ToList()
        };
    }
}

public class OrderMetricsDashboardDto
{
    public int TotalOrdersProcessed { get; set; }
    public double SuccessRate { get; set; }
    public double AverageTotalDurationMs { get; set; }
    public double AverageValidationDurationMs { get; set; }
    public double AverageDatabaseDurationMs { get; set; }
    public List<string> LastErrors { get; set; } = new();
}